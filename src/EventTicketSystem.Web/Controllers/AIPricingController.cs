using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EventTicketSystem.Web.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AIPricingController(
    AppDbContext db,
    TicketPredictionService predictionService,
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory) : ControllerBase
{
    // ── Pricing suggestions ───────────────────────────────────────────────────

    [HttpGet("pricing-suggestion/{eventId:int}")]
    public async Task<IActionResult> GetPricingSuggestion(int eventId)
    {
        var suggestions = await predictionService.SuggestPricingAsync(eventId);
        if (suggestions.Count == 0)
            return NotFound(new { error = "Sự kiện không tồn tại hoặc không có loại vé." });
        return Ok(suggestions);
    }

    [HttpPost("apply-pricing")]
    public async Task<IActionResult> ApplyPricing([FromBody] ApplyPricingDto dto)
    {
        if (dto.NewPrice < 0) return BadRequest(new { error = "Giá không hợp lệ." });
        var tt = await db.TicketTypes.FindAsync(dto.TicketTypeId);
        if (tt == null) return NotFound(new { error = "Không tìm thấy loại vé." });
        var old = tt.Price;
        tt.Price = dto.NewPrice;
        await db.SaveChangesAsync();
        return Ok(new { success = true, ticketTypeId = dto.TicketTypeId, oldPrice = old, newPrice = tt.Price });
    }

    [HttpPost("apply-pricing/batch")]
    public async Task<IActionResult> ApplyPricingBatch([FromBody] List<ApplyPricingDto> items)
    {
        if (items == null || items.Count == 0) return BadRequest(new { error = "Danh sách trống." });
        var ids = items.Select(i => i.TicketTypeId).ToList();
        var tts = await db.TicketTypes.Where(t => ids.Contains(t.Id)).ToListAsync();
        var results = new List<object>();
        foreach (var dto in items)
        {
            var tt = tts.FirstOrDefault(t => t.Id == dto.TicketTypeId);
            if (tt == null) continue;
            var old = tt.Price; tt.Price = dto.NewPrice;
            results.Add(new { ticketTypeId = dto.TicketTypeId, oldPrice = old, newPrice = dto.NewPrice });
        }
        await db.SaveChangesAsync();
        return Ok(new { success = true, applied = results.Count, results });
    }

    // ── AI Config ─────────────────────────────────────────────────────────────

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var cfg = await db.AIConfigs.FindAsync(1) ?? new AIConfig { Id = 1 };
        return Ok(cfg);
    }

    [HttpPut("config")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateConfig([FromBody] AIConfigDto dto)
    {
        var cfg = await db.AIConfigs.FindAsync(1);
        if (cfg == null) { cfg = new AIConfig { Id = 1 }; db.AIConfigs.Add(cfg); }

        cfg.AutoRefundThresholdMinutes = Math.Clamp(dto.AutoRefundThresholdMinutes, 5, 1440);
        cfg.MinTrainingSamples         = Math.Clamp(dto.MinTrainingSamples, 10, 2000);
        cfg.Iterations                 = Math.Clamp(dto.Iterations, 10, 1000);
        cfg.L1Regularization           = Math.Clamp(dto.L1Regularization, 0f, 1f);
        cfg.L2Regularization           = Math.Clamp(dto.L2Regularization, 0f, 1f);

        await db.SaveChangesAsync();
        return Ok(new { success = true, config = cfg });
    }

    // ── Retrain ───────────────────────────────────────────────────────────────

    [HttpPost("retrain")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult StartRetrain()
    {
        var jobId = Guid.NewGuid().ToString("N")[..12];
        var cacheKey = $"retrain_{jobId}";

        cache.Set(cacheKey, new TrainJobStatus("running", null, null, null, null, null),
            TimeSpan.FromHours(1));

        // Fire-and-forget background training
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var bgDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cfg    = await bgDb.AIConfigs.FindAsync(1) ?? new AIConfig();
                var prevMae = cfg.LastModelMAE;

                var (mae, rmse, r2) = await predictionService.ForceRetrainAsync(cfg);

                cfg.LastTrainedAt = DateTime.UtcNow;
                cfg.LastModelMAE  = mae;
                cfg.LastModelRMSE = rmse;
                cfg.LastModelR2   = r2;
                await bgDb.SaveChangesAsync();

                cache.Set(cacheKey,
                    new TrainJobStatus("completed", mae, rmse, r2, prevMae, null),
                    TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                cache.Set(cacheKey,
                    new TrainJobStatus("failed", null, null, null, null, ex.Message),
                    TimeSpan.FromHours(1));
            }
        });

        return Ok(new { jobId });
    }

    [HttpGet("retrain-status/{jobId}")]
    public IActionResult RetrainStatus(string jobId)
    {
        var cacheKey = $"retrain_{jobId}";
        if (!cache.TryGetValue<TrainJobStatus>(cacheKey, out var status))
            return NotFound(new { error = "Job không tìm thấy hoặc đã hết hạn." });

        bool? improved = null;
        if (status!.Status == "completed" && status.PreviousMae.HasValue && status.Mae.HasValue)
            improved = status.Mae < status.PreviousMae;

        return Ok(new
        {
            jobId,
            status!.Status,
            status.Mae,
            status.Rmse,
            status.R2,
            status.PreviousMae,
            improved,
            status.Error
        });
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public record ApplyPricingDto(int TicketTypeId, decimal NewPrice);
    public record AIConfigDto(
        int   AutoRefundThresholdMinutes,
        int   MinTrainingSamples,
        int   Iterations,
        float L1Regularization,
        float L2Regularization);
    private record TrainJobStatus(
        string  Status,
        double? Mae,
        double? Rmse,
        double? R2,
        double? PreviousMae,
        string? Error);
}
