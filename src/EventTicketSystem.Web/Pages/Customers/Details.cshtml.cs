using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Customers;

[Authorize(Roles = "Admin")]
public class CustomerDetailsModel(AppDbContext db, UserManager<ApplicationUser> userManager) : PageModel
{
    public ApplicationUser?  Customer { get; set; }
    public List<string>      Roles    { get; set; } = [];
    public List<Order>       Orders   { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Customer = await userManager.FindByIdAsync(id);
        if (Customer == null) return NotFound();

        Roles = [.. await userManager.GetRolesAsync(Customer)];

        Orders = await db.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.ApplicationUserId == id || o.CustomerEmail == Customer.Email)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Page();
    }
}
