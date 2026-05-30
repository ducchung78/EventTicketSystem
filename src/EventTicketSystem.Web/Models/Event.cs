using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required, MaxLength(300)]
    public string Venue { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsHot { get; set; } = false;

    public bool IsSpecial { get; set; } = false;

    public bool HasSeatMap { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    public ICollection<EventImage> Images { get; set; } = new List<EventImage>();
}
