using EventTicketSystem.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Controllers;

[ApiController]
[Route("api/coupons")]
public class CouponsApiController(AppDbContext db) : ControllerBase
{
    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] string code, [FromQuery] decimal total)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { valid = false, error = "Vui lòng nhập mã giảm giá." });

        var coupon = await db.Coupons
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper().Trim());

        if (coupon == null)
            return Ok(new { valid = false, error = "Mã giảm giá không tồn tại." });

        if (!coupon.IsValid(total, out var error))
            return Ok(new { valid = false, error });

        var discount = coupon.CalculateDiscount(total);
        return Ok(new
        {
            valid = true,
            couponId = coupon.Id,
            discount,
            discountText = discount.ToString("N0") + "đ",
            finalTotal = (total - discount).ToString("N0") + "đ",
            message = coupon.Description ?? $"Áp dụng mã {coupon.Code} thành công!"
        });
    }
}
