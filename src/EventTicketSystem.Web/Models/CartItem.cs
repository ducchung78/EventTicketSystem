namespace EventTicketSystem.Web.Models;

public class CartItem
{
    public int EventId { get; set; }
    public int TicketTypeId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string TicketTypeName { get; set; } = string.Empty;
    public string? EventImageUrl { get; set; }

    public decimal Subtotal => Price * Quantity;
}
