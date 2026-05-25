using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Pages.Contact;

[Authorize]
public class ContactIndexModel(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : PageModel
{
    public bool IsAdmin   { get; set; }
    public bool Sent      { get; set; }
    public string PrefillName  { get; set; } = "";
    public string PrefillEmail { get; set; } = "";

    [BindProperty]
    public ContactInputModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        IsAdmin = User.IsInRole("Admin");
        Sent    = TempData["ContactSent"] as string == "true";

        var user = await userManager.GetUserAsync(User);
        PrefillName  = user?.HoTen ?? user?.UserName ?? "";
        PrefillEmail = user?.Email ?? "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.IsInRole("Admin"))
            return RedirectToPage();

        var user = await userManager.GetUserAsync(User);
        var hoTen = user?.HoTen ?? user?.UserName ?? "";
        var email = user?.Email ?? "";

        if (!ModelState.IsValid)
        {
            PrefillName  = hoTen;
            PrefillEmail = email;
            return Page();
        }

        db.ContactMessages.Add(new ContactMessage
        {
            HoTen     = hoTen,
            Email     = email,
            Phone     = string.IsNullOrWhiteSpace(Input.Phone) ? null : Input.Phone.Trim(),
            Subject   = Input.Subject.Trim(),
            Message   = Input.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        TempData["ContactSent"] = "true";
        return RedirectToPage();
    }

    public class ContactInputModel
    {
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chủ đề.")]
        [MaxLength(200)]
        public string Subject { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập lời nhắn.")]
        [MaxLength(2000)]
        public string Message { get; set; } = "";
    }
}
