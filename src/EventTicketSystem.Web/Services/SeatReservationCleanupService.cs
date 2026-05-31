using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Services;

public class SeatReservationCleanupService(IServiceScopeFactory scopeFactory, ILogger<SeatReservationCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Seat reservation cleanup encountered an error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    private async Task ReleaseExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expired = await db.Seats
            .Where(s => s.Status == SeatStatus.Reserved && s.ReservedUntil < DateTime.UtcNow)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var seat in expired)
        {
            seat.Status        = SeatStatus.Available;
            seat.ReservedUntil = null;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Released {Count} expired seat reservations.", expired.Count);
    }
}
