using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    // Mutual exclusive: percent OR fixed amount
    public decimal DiscountPercent { get; set; }   // 0–100, 0 = not used
    public decimal DiscountAmount { get; set; }    // 0 = not used

    public decimal MinOrderValue { get; set; }

    public int MaxUses { get; set; }
    public int UsedCount { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public bool IsValid(decimal orderTotal, out string error)
    {
        if (!IsActive) { error = "Mã giảm giá không còn hiệu lực."; return false; }
        if (ExpiryDate.HasValue && DateTime.UtcNow > ExpiryDate.Value) { error = "Mã giảm giá đã hết hạn."; return false; }
        if (MaxUses > 0 && UsedCount >= MaxUses) { error = "Mã giảm giá đã được sử dụng hết."; return false; }
        if (orderTotal < MinOrderValue) { error = $"Đơn hàng tối thiểu {MinOrderValue:N0}đ để áp dụng mã này."; return false; }
        error = string.Empty;
        return true;
    }

    public decimal CalculateDiscount(decimal orderTotal)
    {
        if (DiscountPercent > 0)
            return Math.Round(orderTotal * DiscountPercent / 100, 0);
        return Math.Min(DiscountAmount, orderTotal);
    }
}
