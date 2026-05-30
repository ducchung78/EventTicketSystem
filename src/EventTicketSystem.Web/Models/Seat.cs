using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public enum SeatStatus { Available, Sold, Disabled, Reserved }

public class Seat
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int? TicketTypeId { get; set; }
    public TicketType? TicketType { get; set; }

    [Required, MaxLength(50)]
    public string Zone { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string RowLabel { get; set; } = string.Empty;

    public int SeatNumber { get; set; }

    public SeatStatus Status { get; set; } = SeatStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Label => $"{Zone}-{RowLabel}{SeatNumber}";
}
