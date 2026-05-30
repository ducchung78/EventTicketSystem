using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Orders;

[AllowAnonymous]
public class LookupModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public string Code { get; set; } = string.Empty;

    public Order?  Order     { get; set; }
    public bool    Searched  { get; set; }
    public bool    NotFound  { get; set; }

    public void OnGet(string? code)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            Code     = code.Trim().ToUpper();
            Searched = true;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Searched = true;
        var key = (Code ?? "").Trim().ToUpper();

        if (string.IsNullOrEmpty(key))
        {
            ModelState.AddModelError(nameof(Code), "Vui lòng nhập mã xác nhận.");
            return Page();
        }

        Order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.TicketType)
                    .ThenInclude(t => t!.Event)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Seat)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.ConfirmationCode == key);

        NotFound = Order == null;

        // Keep the typed code visible
        Code = key;
        return Page();
    }
}
