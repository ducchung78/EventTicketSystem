namespace EventTicketSystem.Web.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int TicketTypeId { get; set; }
    public TicketType TicketType { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
}
