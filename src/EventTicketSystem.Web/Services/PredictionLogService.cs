using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Services;

public class PredictionLogService(
    AppDbContext db,
    TicketPredictionService predictionService,
    ILogger<PredictionLogService> logger)
{
    // Log predictions for all active events; skip those logged in last 24 h.
    // Then back-fill actuals for events that have already ended.
    public async Task EnsureLogsUpToDateAsync()
    {
        try
        {
            await predictionService.EnsureTrainedAsync();
            await LogPredictionsAsync();
            await UpdateActualsAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PredictionLogService failed.");
        }
    }

    private async Task LogPredictionsAsync()
    {
        var events = await db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive && e.TicketTypes.Any(tt => tt.TotalQuantity > 0))
            .ToListAsync();

        var cutoff = DateTime.UtcNow.AddHours(-24);

        // Load all recent logs in one query
        var recentEventIds = await db.PredictionLogs
            .Where(p => p.PredictedAt >= cutoff)
            .Select(p => p.EventId)
            .Distinct()
            .ToListAsync();

        var recentSet = recentEventIds.ToHashSet();

        foreach (var evt in events)
        {
            if (recentSet.Contains(evt.Id)) continue;

            float  predictedTickets = 0f;
            decimal predictedRevenue = 0m;

            foreach (var tt in evt.TicketTypes.Where(t => t.TotalQuantity > 0))
            {
                var pred = predictionService.PredictForEvent(evt, tt);
                predictedTickets += pred;
                predictedRevenue += (decimal)pred * tt.Price;
            }

            db.PredictionLogs.Add(new PredictionLog
            {
                EventId              = evt.Id,
                PredictedAt          = DateTime.UtcNow,
                PredictedTicketsSold = predictedTickets,
                PredictedRevenue     = predictedRevenue
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task UpdateActualsAsync()
    {
        var now = DateTime.UtcNow;

        // Find latest log per event for events that have ended but have no actuals yet
        var outdatedLogs = await db.PredictionLogs
            .Include(p => p.Event).ThenInclude(e => e.TicketTypes)
            .Where(p => p.ActualTicketsSold == null
                     && p.Event.EndDate.HasValue
                     && p.Event.EndDate < now)
            .ToListAsync();

        if (outdatedLogs.Count == 0) return;

        var eventIds = outdatedLogs.Select(p => p.EventId).Distinct().ToList();

        var actuals = await db.OrderItems
            .Where(oi => eventIds.Contains(oi.TicketType.EventId)
                      && oi.Order.Status == OrderStatus.Confirmed)
            .GroupBy(oi => oi.TicketType.EventId)
            .Select(g => new
            {
                EventId = g.Key,
                Tickets = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
            })
            .ToListAsync();

        var actualDict = actuals.ToDictionary(a => a.EventId);

        foreach (var log in outdatedLogs)
        {
            if (!actualDict.TryGetValue(log.EventId, out var a)) continue;
            log.ActualTicketsSold = a.Tickets;
            log.ActualRevenue     = a.Revenue;
            log.UpdatedAt         = now;
        }

        await db.SaveChangesAsync();
    }
}
