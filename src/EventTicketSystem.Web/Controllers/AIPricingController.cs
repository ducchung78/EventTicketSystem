using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AIPricingController(AppDbContext db, TicketPredictionService predictionService) : ControllerBase
{
    /// <summary>GET /api/ai/pricing-suggestion/{eventId}</summary>
    [HttpGet("pricing-suggestion/{eventId:int}")]
    public async Task<IActionResult> GetPricingSuggestion(int eventId)
    {
        var suggestions = await predictionService.SuggestPricingAsync(eventId);
        if (suggestions.Count == 0)
            return NotFound(new { error = "Sự kiện không tồn tại hoặc không có loại vé." });

        return Ok(suggestions);
    }

    /// <summary>POST /api/ai/apply-pricing  body: { ticketTypeId, newPrice }</summary>
    [HttpPost("apply-pricing")]
    public async Task<IActionResult> ApplyPricing([FromBody] ApplyPricingDto dto)
    {
        if (dto.NewPrice < 0)
            return BadRequest(new { error = "Giá không hợp lệ." });

        var ticketType = await db.TicketTypes.FindAsync(dto.TicketTypeId);
        if (ticketType == null)
            return NotFound(new { error = "Không tìm thấy loại vé." });

        var oldPrice        = ticketType.Price;
        ticketType.Price    = dto.NewPrice;
        await db.SaveChangesAsync();

        return Ok(new { success = true, ticketTypeId = dto.TicketTypeId, oldPrice, newPrice = ticketType.Price });
    }

    /// <summary>POST /api/ai/apply-pricing/batch  body: [{ticketTypeId, newPrice}, ...]</summary>
    [HttpPost("apply-pricing/batch")]
    public async Task<IActionResult> ApplyPricingBatch([FromBody] List<ApplyPricingDto> items)
    {
        if (items == null || items.Count == 0)
            return BadRequest(new { error = "Danh sách trống." });

        var ids = items.Select(i => i.TicketTypeId).ToList();
        var ticketTypes = await db.TicketTypes
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();

        var results = new List<object>();
        foreach (var dto in items)
        {
            var tt = ticketTypes.FirstOrDefault(t => t.Id == dto.TicketTypeId);
            if (tt == null) continue;
            var old = tt.Price;
            tt.Price = dto.NewPrice;
            results.Add(new { ticketTypeId = dto.TicketTypeId, oldPrice = old, newPrice = dto.NewPrice });
        }

        await db.SaveChangesAsync();
        return Ok(new { success = true, applied = results.Count, results });
    }

    public record ApplyPricingDto(int TicketTypeId, decimal NewPrice);
}
