using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Coupons;

[Authorize(Roles = "Admin,SuperAdmin")]
public class CreateCouponModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Coupon Coupon { get; set; } = new() { MaxUses = 100, IsActive = true };

    [BindProperty]
    public string DiscountType { get; set; } = "percent";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        Coupon.Code = Coupon.Code.ToUpper().Trim();

        if (DiscountType == "amount")
            Coupon.DiscountPercent = 0;
        else
            Coupon.DiscountAmount = 0;

        if (Coupon.DiscountPercent <= 0 && Coupon.DiscountAmount <= 0)
            ModelState.AddModelError("", "Vui lòng nhập giá trị giảm (% hoặc số tiền).");

        if (await db.Coupons.AnyAsync(c => c.Code == Coupon.Code))
            ModelState.AddModelError("Coupon.Code", "Mã giảm giá này đã tồn tại.");

        if (!ModelState.IsValid) return Page();

        db.Coupons.Add(Coupon);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo mã giảm giá {Coupon.Code}.";
        return RedirectToPage("Index");
    }
}
