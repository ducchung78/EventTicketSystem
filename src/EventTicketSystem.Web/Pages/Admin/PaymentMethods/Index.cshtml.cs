using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Admin.PaymentMethods;

[Authorize(Roles = "Admin,SuperAdmin")]
public class PaymentMethodsIndexModel(AppDbContext db) : PageModel
{
    public List<PaymentMethod> Methods { get; set; } = [];

    public async Task OnGetAsync()
    {
        Methods = await db.PaymentMethods.OrderBy(m => m.SortOrder).ThenBy(m => m.Id).ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var method = await db.PaymentMethods.FindAsync(id);
        if (method == null) return NotFound();

        method.IsActive = !method.IsActive;
        await db.SaveChangesAsync();
        TempData["AdminMsg"] = method.IsActive
            ? $"Đã bật phương thức '{method.Name}'."
            : $"Đã tắt phương thức '{method.Name}'.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var method = await db.PaymentMethods.FindAsync(id);
        if (method == null) return NotFound();

        db.PaymentMethods.Remove(method);
        await db.SaveChangesAsync();
        TempData["AdminMsg"] = $"Đã xóa phương thức '{method.Name}'.";
        return RedirectToPage();
    }
}
