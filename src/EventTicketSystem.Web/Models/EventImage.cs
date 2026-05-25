using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public class EventImage
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    [Required, MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Caption { get; set; }

    public int SortOrder { get; set; }
}
