using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Cart;

[Authorize]
public class CartConfirmationModel(AppDbContext db, EmailService emailService) : PageModel
{
    public List<Order> Orders { get; set; } = [];
    public decimal TotalPaid { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var raw = TempData.Peek("CartOrderIds") as string;
        if (string.IsNullOrEmpty(raw)) return RedirectToPage("/Index");

        var ids = raw.Split(',').Select(int.Parse).ToList();

        Orders = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
            .Include(o => o.Coupon)
            .Where(o => ids.Contains(o.Id))
            .OrderBy(o => o.Id)
            .ToListAsync();

        if (!Orders.Any()) return RedirectToPage("/Index");

        // Confirm all pending orders and send email for each newly confirmed order
        var toEmail = new List<Order>();
        foreach (var o in Orders.Where(o => o.Status == OrderStatus.Pending))
        {
            o.Status = OrderStatus.Confirmed;
            toEmail.Add(o);
        }
        if (toEmail.Count > 0) await db.SaveChangesAsync();

        foreach (var o in toEmail)
            await emailService.SendOrderConfirmationAsync(o);

        TotalPaid = Orders.Sum(o => o.TotalAmount);
        return Page();
    }
}
