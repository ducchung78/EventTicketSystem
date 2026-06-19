using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class FooterSettingsModel : PageModel
{
    private readonly AppDbContext _db;

    public FooterSettingsModel(AppDbContext db) => _db = db;

    [BindProperty]
    public FooterSettings Settings { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Settings = await _db.FooterSettings.FirstOrDefaultAsync()
            ?? new FooterSettings();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var existing = await _db.FooterSettings.FirstOrDefaultAsync();
        if (existing == null)
        {
            Settings.Id = 0;
            Settings.UpdatedAt = DateTime.UtcNow;
            _db.FooterSettings.Add(Settings);
        }
        else
        {
            existing.Hotline = Settings.Hotline;
            existing.SupportHours = Settings.SupportHours;
            existing.Email = Settings.Email;
            existing.Address = Settings.Address;
            existing.BusinessLicense = Settings.BusinessLicense;
            existing.LicenseDate = Settings.LicenseDate;
            existing.LicensePlace = Settings.LicensePlace;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu cài đặt footer.";
        return RedirectToPage();
    }
}
