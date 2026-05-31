using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Orders;

[AllowAnonymous]
public class LookupModel(AppDbContext db, UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public string? Email { get; set; }

    public Order?  Order                  { get; set; }
    public bool    Searched               { get; set; }
    public new bool NotFound              { get; set; }
    public bool    NeedsEmailVerification { get; set; }
    public string? EmailError             { get; set; }

    public async Task<IActionResult> OnGetAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Page();

        Code     = code.Trim().ToUpper();
        Searched = true;

        var order = await FindOrderAsync(Code);
        if (order == null) { NotFound = true; return Page(); }

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = userManager.GetUserId(User);
            if (order.ApplicationUserId == userId)
                return RedirectToPage("/Orders/Details", new { id = order.Id });
        }

        NeedsEmailVerification = true;
        return Page();
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

        Code = key;
        var order = await FindOrderAsync(key);

        if (order == null) { NotFound = true; return Page(); }

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = userManager.GetUserId(User);
            if (order.ApplicationUserId == userId)
                return RedirectToPage("/Orders/Details", new { id = order.Id });
        }

        NeedsEmailVerification = true;
        var email = (Email ?? "").Trim();

        if (string.IsNullOrEmpty(email))
            return Page();

        if (!order.CustomerEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            EmailError = "Email không khớp với đơn hàng này. Vui lòng kiểm tra lại.";
            return Page();
        }

        Order = order;
        return Page();
    }

    private Task<Order?> FindOrderAsync(string key) =>
        db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.TicketType)
                    .ThenInclude(t => t!.Event)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Seat)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.ConfirmationCode == key);
}
