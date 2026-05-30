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
    public Event?                           Event       { get; set; }
    public Dictionary<string, List<Seat>>   SeatsByZone { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Event = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive && e.HasSeatMap);

        if (Event == null)
            return RedirectToPage("/Events/Details", new { id });

        await LoadSeatsAsync(id);
        return Page();
    }

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

        if (ids.Count == 0 || ids.Count > 10)
        {
            TempData["SeatError"] = "Số lượng ghế không hợp lệ (tối đa 10 ghế).";
            return RedirectToPage(new { id = eventId });
        }

        var seats = await db.Seats
            .Include(s => s.TicketType)
            .Where(s => ids.Contains(s.Id) && s.EventId == eventId)
            .ToListAsync();

        if (seats.Count != ids.Count || seats.Any(s => s.Status != SeatStatus.Available))
        {
            TempData["SeatError"] = "Một hoặc nhiều ghế đã được đặt. Vui lòng chọn lại.";
            return RedirectToPage(new { id = eventId });
        }

        var evt = await db.Events.FindAsync(eventId);
        if (evt == null) return NotFound();

        // Group by TicketType → separate CartItem per zone/type
        foreach (var grp in seats.GroupBy(s => s.TicketTypeId))
        {
            var tt = grp.First().TicketType;
            if (tt == null) continue;

            var groupSeatIds    = grp.Select(s => s.Id).ToList();
            var groupSeatLabels = grp.Select(s => s.Label).ToList();

            cartService.AddSeatedItemToCart(HttpContext.Session, new CartItem
            {
                EventId        = eventId,
                TicketTypeId   = tt.Id,
                Quantity       = groupSeatIds.Count,
                Price          = tt.Price,
                EventName      = evt.Title,
                TicketTypeName = tt.Name,
                EventImageUrl  = evt.ImageUrl,
                SeatIds        = groupSeatIds,
                SeatLabels     = groupSeatLabels
            });
        }

        TempData["CartSuccess"] = $"Đã thêm {ids.Count} ghế vào giỏ hàng!";
        return RedirectToPage("/Cart/Index");
    }

    private async Task LoadSeatsAsync(int eventId)
    {
        var seats = await db.Seats
            .Include(s => s.TicketType)
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Zone).ThenBy(s => s.RowLabel).ThenBy(s => s.SeatNumber)
            .ToListAsync();

        SeatsByZone = seats
            .GroupBy(s => s.Zone)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
