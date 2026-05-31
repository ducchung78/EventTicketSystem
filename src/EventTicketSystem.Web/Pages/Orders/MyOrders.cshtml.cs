using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Orders;

[Authorize]
public class MyOrdersModel(AppDbContext db, UserManager<ApplicationUser> userManager) : PageModel
{
    public List<Order> Orders { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = userManager.GetUserId(User);
        var email  = User.Identity?.Name ?? "";

        Orders = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.TicketType)
                    .ThenInclude(t => t!.Event)
            .Where(o => o.ApplicationUserId == userId || o.CustomerEmail == email)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}
