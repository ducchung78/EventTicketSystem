using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventTicketSystem.Web.Pages.Events.SeatMap;

[Authorize(Roles = "Admin")]
public class SeatMapIndexModel(AppDbContext db) : PageModel
{
    public Event?                           Event        { get; set; }
    public List<Seat>                       Seats        { get; set; } = [];
    public bool                             HasSoldSeats { get; set; }
    public Dictionary<string, List<Seat>>   SeatsByZone  { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        Event = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (Event == null) return NotFound();

        await LoadSeatsAsync(eventId);
        return Page();
    }

    // ── Generate seats from zone config ──────────────────────────────────────
    public async Task<IActionResult> OnPostConfigureAsync(int eventId, string zonesJson)
    {
        var evt = await db.Events.Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (evt == null) return NotFound();

        var hasSold = await db.Seats.AnyAsync(s => s.EventId == eventId && s.Status == SeatStatus.Sold);
        if (hasSold)
        {
            TempData["Error"] = "Không thể cấu hình lại khi đã có ghế đã bán.";
            return RedirectToPage(new { eventId });
        }

        List<ZoneConfigDto>? zones = null;
        try { zones = JsonSerializer.Deserialize<List<ZoneConfigDto>>(zonesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { /* fall through */ }

        if (zones == null || zones.Count == 0)
        {
            TempData["Error"] = "Cấu hình khu vực không hợp lệ.";
            return RedirectToPage(new { eventId });
        }

        // Validate each zone
        foreach (var z in zones)
        {
            if (string.IsNullOrWhiteSpace(z.Zone) || z.Rows < 1 || z.SeatsPerRow < 1)
            {
                TempData["Error"] = $"Khu vực '{z.Zone}' có cấu hình không hợp lệ.";
                return RedirectToPage(new { eventId });
            }
        }

        // Delete existing Available/Disabled seats
        var old = await db.Seats
            .Where(s => s.EventId == eventId && s.Status != SeatStatus.Sold)
            .ToListAsync();
        db.Seats.RemoveRange(old);

        const string RowAlpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var newSeats = new List<Seat>();
        var now = DateTime.UtcNow;

        foreach (var zone in zones)
        {
            int maxRows = Math.Min(zone.Rows, 26);
            int maxCols = Math.Min(zone.SeatsPerRow, 99);
            int? ttId = zone.TicketTypeId > 0 ? zone.TicketTypeId : null;

            for (int r = 0; r < maxRows; r++)
            {
                for (int s = 1; s <= maxCols; s++)
                {
                    newSeats.Add(new Seat
                    {
                        EventId      = eventId,
                        TicketTypeId = ttId,
                        Zone         = zone.Zone.Trim().ToUpper(),
                        RowLabel     = RowAlpha[r].ToString(),
                        SeatNumber   = s,
                        Status       = SeatStatus.Available,
                        CreatedAt    = now
                    });
                }
            }
        }

        db.Seats.AddRange(newSeats);
        evt.HasSeatMap = true;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo {newSeats.Count} ghế cho {zones.Count} khu vực.";
        return RedirectToPage(new { eventId });
    }

    // ── Toggle single seat Available ↔ Disabled (AJAX) ───────────────────────
    public async Task<IActionResult> OnPostToggleSeatAsync(int eventId, int seatId)
    {
        var seat = await db.Seats.FirstOrDefaultAsync(s => s.Id == seatId && s.EventId == eventId);
        if (seat == null) return NotFound();
        if (seat.Status == SeatStatus.Sold || seat.Status == SeatStatus.Reserved)
            return new JsonResult(new { error = "Không thể thay đổi ghế này." }) { StatusCode = 400 };

        seat.Status = seat.Status == SeatStatus.Disabled ? SeatStatus.Available : SeatStatus.Disabled;
        await db.SaveChangesAsync();

        return new JsonResult(new { status = seat.Status.ToString() });
    }

    // ── Reset all (only if no Sold) ───────────────────────────────────────────
    public async Task<IActionResult> OnPostResetAsync(int eventId)
    {
        var hasSold = await db.Seats.AnyAsync(s => s.EventId == eventId && s.Status == SeatStatus.Sold);
        if (hasSold)
        {
            TempData["Error"] = "Không thể xóa sơ đồ khi đã có ghế đã bán.";
            return RedirectToPage(new { eventId });
        }

        var all = await db.Seats.Where(s => s.EventId == eventId).ToListAsync();
        db.Seats.RemoveRange(all);

        var evt = await db.Events.FindAsync(eventId);
        if (evt != null) evt.HasSeatMap = false;

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa toàn bộ sơ đồ ghế.";
        return RedirectToPage(new { eventId });
    }

    private async Task LoadSeatsAsync(int eventId)
    {
        Seats = await db.Seats
            .Include(s => s.TicketType)
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Zone).ThenBy(s => s.RowLabel).ThenBy(s => s.SeatNumber)
            .ToListAsync();

        HasSoldSeats = Seats.Any(s => s.Status == SeatStatus.Sold);
        SeatsByZone  = Seats
            .GroupBy(s => s.Zone)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public record ZoneConfigDto(string Zone, int TicketTypeId, int Rows, int SeatsPerRow);
}
