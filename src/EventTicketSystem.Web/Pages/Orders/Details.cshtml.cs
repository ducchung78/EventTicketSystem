using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Orders;

public class OrderDetailsModel(AppDbContext db) : PageModel
{
    public Order? Order { get; set; }

    public async Task OnGetAsync(int id)
    {
        Order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.Status == OrderStatus.Confirmed)
        {
            order.Status = OrderStatus.Cancelled;
            foreach (var item in order.OrderItems)
                item.TicketType.SoldQuantity -= item.Quantity;

            await db.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }
}
