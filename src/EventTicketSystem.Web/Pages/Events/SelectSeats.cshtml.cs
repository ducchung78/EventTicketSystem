using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Events;

[Authorize]
public class SelectSeatsModel(AppDbContext db, CartService cartService) : PageModel
{
    private static readonly TimeSpan ReservationWindow = TimeSpan.FromMinutes(10);

    public Event?  Event    { get; set; }
    public int     GridRows { get; private set; }
    public int     GridCols { get; private set; }
    public Dictionary<(int row, int col), Seat> SeatGrid { get; private set; } = [];
    public List<Seat> Seats { get; private set; } = [];

    // ── GET ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Event = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive && e.HasSeatMap);

        if (Event == null)
            return RedirectToPage("/Events/Details", new { id });

        // Release expired reservations so grid shows correct availability
        await ReleaseExpiredAsync(id);
        await LoadGridAsync(id);
        return Page();
    }

    // ── Add to cart + reserve seats ──────────────────────────────────────────
    public async Task<IActionResult> OnPostAddToCartAsync(int eventId, string selectedSeatIds)
    {
        if (string.IsNullOrWhiteSpace(selectedSeatIds))
        {
            TempData["SeatError"] = "Vui lòng chọn ít nhất 1 ghế.";
            return RedirectToPage(new { id = eventId });
        }

        var ids = selectedSeatIds.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var x) ? x : 0)
            .Where(x => x > 0).Distinct().ToList();

        if (ids.Count == 0)
        {
            TempData["SeatError"] = "Danh sách ghế không hợp lệ.";
            return RedirectToPage(new { id = eventId });
        }

        // Release any expired first
        await ReleaseExpiredAsync(eventId);

        var seats = await db.Seats
            .Include(s => s.TicketType)
            .Where(s => ids.Contains(s.Id) && s.EventId == eventId)
            .ToListAsync();

        if (seats.Count != ids.Count || seats.Any(s => s.Status != SeatStatus.Available))
        {
            TempData["SeatError"] = "Một hoặc nhiều ghế vừa được đặt. Vui lòng chọn lại.";
            return RedirectToPage(new { id = eventId });
        }

        // Count physical seats (couple = 2)
        int physicalCount = seats.Sum(s => s.SeatType == SeatType.Couple ? 2 : 1);
        if (physicalCount > 8)
        {
            TempData["SeatError"] = "Tối đa 8 chỗ ngồi mỗi lần đặt.";
            return RedirectToPage(new { id = eventId });
        }

        var evt = await db.Events.FindAsync(eventId);
        if (evt == null) return NotFound();

        // Reserve seats
        var reserveUntil = DateTime.UtcNow.Add(ReservationWindow);
        foreach (var s in seats)
        {
            s.Status       = SeatStatus.Reserved;
            s.ReservedUntil = reserveUntil;
        }
        await db.SaveChangesAsync();

        // Group by TicketType → CartItem per group
        foreach (var grp in seats.GroupBy(s => s.TicketTypeId))
        {
            var tt = grp.First().TicketType;
            if (tt == null) continue;

            var groupSeats  = grp.ToList();
            var qty         = groupSeats.Sum(s => s.SeatType == SeatType.Couple ? 2 : 1);
            var seatIds     = groupSeats.Select(s => s.Id).ToList();
            var seatLabels  = groupSeats.Select(s => s.Label).ToList();

            cartService.AddSeatedItemToCart(HttpContext.Session, new CartItem
            {
                EventId        = eventId,
                TicketTypeId   = tt.Id,
                Quantity       = qty,
                Price          = tt.Price,
                EventName      = evt.Title,
                TicketTypeName = tt.Name,
                EventImageUrl  = evt.ImageUrl,
                SeatIds        = seatIds,
                SeatLabels     = seatLabels
            });
        }

        TempData["CartSuccess"] = $"Đã thêm {physicalCount} chỗ ngồi vào giỏ! Ghế được giữ trong 10 phút.";
        return RedirectToPage("/Cart/Index");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private async Task LoadGridAsync(int eventId)
    {
        Seats = await db.Seats
            .Include(s => s.TicketType)
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.GridRow).ThenBy(s => s.GridCol)
            .ToListAsync();

        if (Seats.Count > 0)
        {
            GridRows = Seats.Max(s => s.GridRow) + 1;
            GridCols = Seats.Max(s => s.GridCol + (s.SeatType == SeatType.Couple ? 2 : 1));
            SeatGrid = Seats.ToDictionary(s => (s.GridRow, s.GridCol));
        }
    }

    private async Task ReleaseExpiredAsync(int eventId)
    {
        var expired = await db.Seats
            .Where(s => s.EventId == eventId
                     && s.Status == SeatStatus.Reserved
                     && s.ReservedUntil < DateTime.UtcNow)
            .ToListAsync();

        if (expired.Count > 0)
        {
            foreach (var s in expired)
            {
                s.Status        = SeatStatus.Available;
                s.ReservedUntil = null;
            }
            await db.SaveChangesAsync();
        }
    }
}
