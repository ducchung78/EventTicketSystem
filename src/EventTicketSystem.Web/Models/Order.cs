using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Cancelled
}

public class Order
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }

    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    public string ConfirmationCode { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();

    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
