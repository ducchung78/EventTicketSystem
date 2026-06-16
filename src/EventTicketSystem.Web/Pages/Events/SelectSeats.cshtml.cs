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
    private static readonly TimeSpan ReservationWindow = TimeSpan.FromMinutes(15);

    public Event?    Event                { get; set; }
    public int       GridRows             { get; private set; }
    public int       GridCols             { get; private set; }
    public Dictionary<(int row, int col), Seat> SeatGrid { get; private set; } = [];
    public List<Seat> Seats              { get; private set; } = [];

    // Session tracking cho UI
    public string    CurrentSessionId        { get; private set; } = string.Empty;
    public DateTime? ReservationExpiresAt    { get; private set; }
    public string    PreReservedSeatsJson    { get; private set; } = "[]";
    public int?      PreSelectedTicketTypeId { get; private set; }

    // ── GET ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int id, int? ticketTypeId)
    {
        PreSelectedTicketTypeId = ticketTypeId;
        Event = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive && e.HasSeatMap);

        if (Event == null)
            return RedirectToPage("/Events/Details", new { id });

        // ASP.NET Core session cookie độc lập với Identity auth cookie →
        // Session ID giữ nguyên trước và sau khi đăng nhập (không bị reset)
        await HttpContext.Session.CommitAsync();
        CurrentSessionId = HttpContext.Session.Id;

        await ReleaseExpiredAsync(id);
        await LoadGridAsync(id);

        // Ghế user đang giữ (pre-selected khi vào lại trang)
        var myReserved = Seats.Where(s =>
            s.Status == SeatStatus.Reserved &&
            s.ReservedBySessionId == CurrentSessionId &&
            s.ReservedUntil.HasValue &&
            s.ReservedUntil.Value > DateTime.UtcNow
        ).ToList();

        ReservationExpiresAt = myReserved.Any()
            ? myReserved.Max(s => s.ReservedUntil!.Value)
            : null;

        PreReservedSeatsJson = System.Text.Json.JsonSerializer.Serialize(
            myReserved.Select(s => new {
                id       = s.Id,
                label    = s.Label,
                price    = (double)(s.TicketType?.Price ?? 0),
                isCouple = s.SeatType == SeatType.Couple
            })
        );

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

        int physicalCount = seats.Sum(s => s.SeatType == SeatType.Couple ? 2 : 1);
        if (physicalCount > 8)
        {
            TempData["SeatError"] = "Tối đa 8 chỗ ngồi mỗi lần đặt.";
            return RedirectToPage(new { id = eventId });
        }

        var evt = await db.Events.FindAsync(eventId);
        if (evt == null) return NotFound();

        var sessionId    = HttpContext.Session.Id;
        var reserveUntil = DateTime.UtcNow.Add(ReservationWindow);
        foreach (var s in seats)
        {
            s.Status              = SeatStatus.Reserved;
            s.ReservedUntil       = reserveUntil;
            s.ReservedBySessionId = sessionId;
        }
        await db.SaveChangesAsync();

        foreach (var grp in seats.GroupBy(s => s.TicketTypeId))
        {
            var tt = grp.First().TicketType;
            if (tt == null) continue;

            var groupSeats = grp.ToList();
            var qty        = groupSeats.Sum(s => s.SeatType == SeatType.Couple ? 2 : 1);

            cartService.AddSeatedItemToCart(HttpContext.Session, new CartItem
            {
                EventId        = eventId,
                TicketTypeId   = tt.Id,
                Quantity       = qty,
                Price          = tt.Price,
                EventName      = evt.Title,
                TicketTypeName = tt.Name,
                EventImageUrl  = evt.ImageUrl,
                SeatIds        = groupSeats.Select(s => s.Id).ToList(),
                SeatLabels     = groupSeats.Select(s => s.Label).ToList()
            });
        }

        TempData["CartSuccess"] = $"Đã thêm {physicalCount} chỗ ngồi vào giỏ! Ghế được giữ trong {(int)ReservationWindow.TotalMinutes} phút.";
        return RedirectToPage("/Cart/Index");
    }

    // ── Release: user chủ động huỷ ghế đang giữ ─────────────────────────────
    public async Task<IActionResult> OnPostReleaseAsync(int eventId)
    {
        var sessionId = HttpContext.Session.Id;
        var seats = await db.Seats
            .Where(s => s.EventId == eventId &&
                        s.Status  == SeatStatus.Reserved &&
                        s.ReservedBySessionId == sessionId)
            .ToListAsync();

        foreach (var seat in seats)
        {
            seat.Status              = SeatStatus.Available;
            seat.ReservedUntil       = null;
            seat.ReservedBySessionId = null;
        }

        if (seats.Count > 0)
        {
            await db.SaveChangesAsync();
            // Xóa cart items liên quan để cart không lưu ghế đã hủy
            var cart = cartService.GetCart(HttpContext.Session);
            foreach (var item in cart.Where(c => c.EventId == eventId && c.HasSeats).ToList())
                cartService.RemoveFromCart(HttpContext.Session, item.TicketTypeId);
        }

        return RedirectToPage("/Events/Details", new { id = eventId });
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
                s.Status              = SeatStatus.Available;
                s.ReservedUntil       = null;
                s.ReservedBySessionId = null;
            }
            await db.SaveChangesAsync();
        }
    }
}
