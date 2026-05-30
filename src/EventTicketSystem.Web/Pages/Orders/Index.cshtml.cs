using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Orders;

[Authorize(Roles = "Admin,SuperAdmin")]
public class OrdersIndexModel(AppDbContext db) : PageModel
{
    public List<Order> Orders { get; set; } = [];
    public string? Email { get; set; }

    public async Task OnGetAsync(string? email)
    {
        Email = email;
        var query = db.Orders.Include(o => o.OrderItems).AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(o => o.CustomerEmail.Contains(email));

        Orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }
}
