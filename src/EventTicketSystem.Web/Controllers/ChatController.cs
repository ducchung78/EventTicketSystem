using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EventTicketSystem.Web.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController(
    AppDbContext db,
    GroqService groq,
    IMemoryCache cache,
    ILogger<ChatController> logger) : ControllerBase
{
    private const int RateLimitPerMinute = 20;

    // ── POST /api/chat/send ────────────────────────────────────────────────────
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Tin nhắn không được để trống." });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.Identity?.Name
                  ?? "unknown";

        // ── Rate limit: 20 msg/phút mỗi user ──────────────────────────────────
        var rateCacheKey = $"chat_rate_{userId}_{DateTime.UtcNow:yyyyMMddHHmm}";
        var msgCount = cache.GetOrCreate(rateCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });

        if (msgCount >= RateLimitPerMinute)
            return StatusCode(429, new { error = "Bạn đã gửi quá nhiều tin nhắn. Vui lòng chờ 1 phút." });

        cache.Set(rateCacheKey, msgCount + 1, TimeSpan.FromMinutes(1));

        // ── Lấy sự kiện đang mở bán để đưa vào context ────────────────────────
        var events = await db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive && e.StartDate >= DateTime.UtcNow.AddDays(-1))
            .OrderBy(e => e.StartDate)
            .Take(12)
            .ToListAsync(ct);

        var eventsContext = BuildEventsContext(events);

        // ── System prompt ──────────────────────────────────────────────────────
        var systemPrompt = $"""
            Bạn là trợ lý AI của TicketHub - nền tảng đặt vé sự kiện hàng đầu Việt Nam.
            Hãy trả lời bằng tiếng Việt, thân thiện, ngắn gọn và chính xác.

            Bạn hỗ trợ khách hàng về:
            - Thông tin sự kiện đang diễn ra và sắp diễn ra
            - Cách đặt vé: vào trang sự kiện → chọn loại vé → thêm vào giỏ → thanh toán
            - Thanh toán: hỗ trợ đặt vé trực tuyến, xác nhận qua email
            - Hoàn vé: liên hệ support@tickethub.vn trong vòng 24h trước sự kiện
            - Mã giảm giá: nhập tại bước thanh toán trong giỏ hàng
            - Tài khoản: đăng nhập để xem lịch sử vé tại mục "Vé của tôi"

            Danh sách sự kiện hiện đang mở bán trên hệ thống:
            {eventsContext}

            Nếu khách hỏi về sự kiện không có trong danh sách, hãy nói không có thông tin
            và gợi ý họ xem trang sự kiện tại /Events.
            Không bịa đặt thông tin. Không cung cấp thông tin ngoài phạm vi TicketHub.
            """;

        // ── Gọi Groq ───────────────────────────────────────────────────────────
        logger.LogInformation("Sending chat from user {UserId}: {Msg}",
            userId, request.Message.Trim()[..Math.Min(request.Message.Length, 80)]);

        var aiResponse = await groq.ChatAsync(systemPrompt, request.Message.Trim(), ct);

        if (aiResponse == null)
        {
            if (!groq.IsConfigured)
            {
                logger.LogWarning("AI call failed: API key not configured.");
                return StatusCode(503, new { error = "Tính năng AI chưa được cấu hình. Vui lòng liên hệ quản trị viên." });
            }

            logger.LogError("Groq call returned null — check GroqService logs above for details.");
            return StatusCode(503, new { error = "AI tạm thời không khả dụng. Vui lòng thử lại sau." });
        }

        // ── Lưu lịch sử ───────────────────────────────────────────────────────
        try
        {
            db.ChatMessages.Add(new ChatMessage
            {
                UserId    = userId,
                Message   = request.Message.Trim()[..Math.Min(request.Message.Length, 2000)],
                Response  = aiResponse[..Math.Min(aiResponse.Length, 8000)],
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi lưu lịch sử chat.");
        }

        return Ok(new { response = aiResponse });
    }

    private static string BuildEventsContext(IList<Event> events)
    {
        if (!events.Any()) return "Hiện tại không có sự kiện nào đang mở bán.";

        var sb = new System.Text.StringBuilder();
        foreach (var evt in events)
        {
            sb.AppendLine($"• {evt.Title}");
            sb.AppendLine($"  Thời gian: {evt.StartDate.ToLocalTime():dd/MM/yyyy HH:mm}");
            sb.AppendLine($"  Địa điểm : {evt.Venue}");
            sb.AppendLine($"  Danh mục : {evt.Category}");

            foreach (var tt in evt.TicketTypes)
            {
                var available = tt.AvailableQuantity > 0 ? $"còn {tt.AvailableQuantity} vé" : "HẾT VÉ";
                sb.AppendLine($"  Loại vé  : {tt.Name} – {tt.Price:N0}đ ({available})");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // ── GET /api/chat/test — debug endpoint (Admin only) ──────────────────────
    [HttpGet("test")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        var keyConfigured = groq.IsConfigured;
        var (statusCode, body) = await groq.TestRawAsync(ct);

        logger.LogInformation("Groq test: HTTP {Status}, snippet: {Snippet}",
            statusCode, body.Length > 300 ? body[..300] + "…" : body);

        return Ok(new
        {
            keyConfigured,
            httpStatusCode = statusCode,
            groqResponseBody = body
        });
    }

    public record ChatRequest(string Message);
}
