using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Coupons;

[Authorize(Roles = "Admin")]
public class EditCouponModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Coupon Coupon { get; set; } = null!;

    [BindProperty]
    public string DiscountType { get; set; } = "percent";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Coupon = await db.Coupons.FindAsync(id) ?? null!;
        if (Coupon == null) return NotFound();

        DiscountType = Coupon.DiscountAmount > 0 ? "amount" : "percent";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await db.Coupons.FindAsync(Coupon.Id);
        if (existing == null) return NotFound();

        Coupon.Code = Coupon.Code.ToUpper().Trim();

        if (DiscountType == "amount")
            Coupon.DiscountPercent = 0;
        else
            Coupon.DiscountAmount = 0;

        if (Coupon.DiscountPercent <= 0 && Coupon.DiscountAmount <= 0)
            ModelState.AddModelError("", "Vui lòng nhập giá trị giảm (% hoặc số tiền).");

        if (await db.Coupons.AnyAsync(c => c.Code == Coupon.Code && c.Id != Coupon.Id))
            ModelState.AddModelError("Coupon.Code", "Mã giảm giá này đã tồn tại.");

        if (!ModelState.IsValid) return Page();

        existing.Code            = Coupon.Code;
        existing.Description     = Coupon.Description;
        existing.DiscountPercent = Coupon.DiscountPercent;
        existing.DiscountAmount  = Coupon.DiscountAmount;
        existing.MinOrderValue   = Coupon.MinOrderValue;
        existing.MaxUses         = Coupon.MaxUses;
        existing.ExpiryDate      = Coupon.ExpiryDate;
        existing.IsActive        = Coupon.IsActive;

        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật mã {existing.Code}.";
        return RedirectToPage("Index");
    }
}
