using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ReportsApiController(AppDbContext db, PredictionLogService logService) : ControllerBase
{
    /// <summary>GET /api/reports/ai-comparison?from=...&amp;to=...</summary>
    [HttpGet("ai-comparison")]
    public async Task<IActionResult> GetAIComparison(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        // Refresh logs (no-op if done within last 24 h)
        await logService.EnsureLogsUpToDateAsync();

        var effFrom = (from ?? DateTime.UtcNow.AddMonths(-6)).ToUniversalTime();
        var effTo   = (to   ?? DateTime.UtcNow).ToUniversalTime().AddDays(1);

        // Latest prediction log per event within the period
        var logs = await db.PredictionLogs
            .Include(p => p.Event)
            .Where(p => p.PredictedAt >= effFrom && p.PredictedAt < effTo)
            .ToListAsync();

        var latest = logs
            .GroupBy(p => p.EventId)
            .Select(g => g.OrderByDescending(p => p.PredictedAt).First())
            .OrderBy(p => p.Event.StartDate)
            .ToList();

        var items = latest.Select(p =>
        {
            double? errorPct = null;
            if (p.ActualTicketsSold.HasValue && p.PredictedTicketsSold > 0)
                errorPct = (p.ActualTicketsSold.Value - (double)p.PredictedTicketsSold)
                           / (double)p.PredictedTicketsSold * 100.0;

            return new
            {
                p.EventId,
                eventTitle       = p.Event.Title,
                eventDate        = p.Event.StartDate.ToLocalTime().ToString("dd/MM/yyyy"),
                hasEnded         = p.Event.EndDate.HasValue && p.Event.EndDate < DateTime.UtcNow,
                predictedTickets = (int)MathF.Round(p.PredictedTicketsSold),
                actualTickets    = p.ActualTicketsSold,
                errorPct         = errorPct.HasValue ? Math.Round(errorPct.Value, 1) : (double?)null,
                predictedRevenue = (long)p.PredictedRevenue,
                actualRevenue    = p.ActualRevenue.HasValue ? (long?)p.ActualRevenue.Value : null,
                predictedAt      = p.PredictedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            };
        }).ToList();

        return Ok(new { items, total = items.Count });
    }
}
