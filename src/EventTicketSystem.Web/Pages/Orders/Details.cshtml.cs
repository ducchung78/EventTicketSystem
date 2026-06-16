using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Orders;

[Authorize]
public class OrderDetailsModel(AppDbContext db) : PageModel
{
    public Order? Order { get; set; }

    public async Task OnGetAsync(int id)
    {
        Order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seat)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.TicketType)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Seat)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.Status == OrderStatus.Confirmed)
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            order.Status = OrderStatus.Cancelled;
            foreach (var item in order.OrderItems)
            {
                item.TicketType.SoldQuantity -= item.Quantity;
                if (item.Seat != null)
                {
                    item.Seat.Status = SeatStatus.Available;
                    item.Seat.ReservedBySessionId = null;
                    item.Seat.ReservedUntil = null;
                }
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        return RedirectToPage(new { id });
    }
}
