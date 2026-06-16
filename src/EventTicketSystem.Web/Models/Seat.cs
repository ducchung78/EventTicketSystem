using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public enum SeatStatus { Available, Sold, Disabled, Reserved }
public enum SeatType   { Normal, VIP, Couple, Empty }

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

    public SeatStatus Status             { get; set; } = SeatStatus.Available;
    public SeatType   SeatType           { get; set; } = SeatType.Normal;
    public int        GridRow            { get; set; }
    public int        GridCol            { get; set; }
    public DateTime?  ReservedUntil      { get; set; }
    public string?    ReservedBySessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Label => SeatType == SeatType.Couple
        ? $"{RowLabel}{SeatNumber}-{RowLabel}{SeatNumber + 1}"
        : $"{RowLabel}{SeatNumber}";
}
