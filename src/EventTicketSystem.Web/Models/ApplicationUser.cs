using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventTicketSystem.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string Ho { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public string? AvatarUrl { get; set; }

    // Backward-compatible computed property — not stored in DB
    [NotMapped]
    public string HoTen
    {
        get => $"{Ho} {Ten}".Trim();
        set
        {
            var trimmed = (value ?? "").Trim();
            var idx = trimmed.LastIndexOf(' ');
            if (idx < 0)
            {
                Ho = string.Empty;
                Ten = trimmed;
            }
            else
            {
                Ho = trimmed[..idx];
                Ten = trimmed[(idx + 1)..];
            }
        }
    }
}
