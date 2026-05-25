using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public class TicketType
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int TotalQuantity { get; set; }

    public int SoldQuantity { get; set; } = 0;

    public int AvailableQuantity => TotalQuantity - SoldQuantity;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
