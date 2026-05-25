using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Orders;

public class PaymentModel(AppDbContext db) : PageModel
{
    public Order? Order { get; set; }
    public DateTime ExpiresAt { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (Order == null) return NotFound();

        ExpiresAt = Order.OrderDate.AddMinutes(5);

        if (Order.Status == OrderStatus.Pending && DateTime.UtcNow > ExpiresAt)
        {
            await CancelOrderAsync(Order);
            TempData["Error"] = "Đơn hàng đã hết thời gian thanh toán và bị tự động hủy.";
            var eventId = Order.OrderItems.FirstOrDefault()?.TicketType?.EventId;
            if (eventId.HasValue)
                return RedirectToPage("/Events/Details", new { id = eventId.Value });
            return RedirectToPage("/Events/Index");
        }

        if (Order.Status == OrderStatus.Confirmed)
            return RedirectToPage("Confirmation", new { id });

        if (Order.Status == OrderStatus.Cancelled)
        {
            TempData["Error"] = "Đơn hàng này đã bị hủy.";
            var eventId = Order.OrderItems.FirstOrDefault()?.TicketType?.EventId;
            if (eventId.HasValue)
                return RedirectToPage("/Events/Details", new { id = eventId.Value });
            return RedirectToPage("/Events/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.Status == OrderStatus.Pending)
        {
            var expiresAt = order.OrderDate.AddMinutes(5);
            if (DateTime.UtcNow > expiresAt)
            {
                await CancelOrderAsync(order);
                TempData["Error"] = "Đơn hàng đã hết thời gian thanh toán và bị tự động hủy.";
                var expiredEventId = order.OrderItems.FirstOrDefault()?.TicketType?.EventId;
                if (expiredEventId.HasValue)
                    return RedirectToPage("/Events/Details", new { id = expiredEventId.Value });
                return RedirectToPage("/Events/Index");
            }

            order.Status = OrderStatus.Confirmed;
            await db.SaveChangesAsync();
        }

        return RedirectToPage("Confirmation", new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.Status == OrderStatus.Pending)
        {
            await CancelOrderAsync(order);
        }

        var eventId = order.OrderItems.FirstOrDefault()?.TicketType?.EventId;
        if (eventId.HasValue)
            return RedirectToPage("/Events/Details", new { id = eventId.Value });
        return RedirectToPage("/Events/Index");
    }

    private async Task CancelOrderAsync(Order order)
    {
        order.Status = OrderStatus.Cancelled;
        foreach (var item in order.OrderItems)
        {
            if (item.TicketType != null)
                item.TicketType.SoldQuantity = Math.Max(0, item.TicketType.SoldQuantity - item.Quantity);
        }
        await db.SaveChangesAsync();
    }
}
