namespace EventTicketSystem.Web.Models;

public class FooterSettings
{
    public int Id { get; set; }
    public string Hotline { get; set; } = "1900 6208";
    public string SupportHours { get; set; } = "8:00 – 22:00 (Thứ 2 – CN)";
    public string Email { get; set; } = "support@tickethub.vn";
    public string Address { get; set; } = "Hà Nội & TP. Hồ Chí Minh, Việt Nam";
    public string BusinessLicense { get; set; } = "0123456789";
    public string LicenseDate { get; set; } = "01/01/2024";
    public string LicensePlace { get; set; } = "Hà Nội";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
