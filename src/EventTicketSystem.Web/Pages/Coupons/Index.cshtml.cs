using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Coupons;

[Authorize(Roles = "Admin,SuperAdmin")]
public class CouponsIndexModel(AppDbContext db) : PageModel
{
    public List<Coupon> Coupons { get; set; } = [];

    public async Task OnGetAsync() =>
        Coupons = await db.Coupons.OrderByDescending(c => c.Id).ToListAsync();

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon != null)
        {
            db.Coupons.Remove(coupon);
            await db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa mã giảm giá.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon != null)
        {
            coupon.IsActive = !coupon.IsActive;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
