using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public class ChatMessage
{
    public int    Id        { get; set; }

    [MaxLength(450)]
    public string UserId    { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Message   { get; set; } = string.Empty;

    [MaxLength(8000)]
    public string Response  { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
