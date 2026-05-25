using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public class ContactMessage
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string HoTen { get; set; } = "";

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MaxLength(200)]
    public string Subject { get; set; } = "";

    [Required, MaxLength(2000)]
    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
