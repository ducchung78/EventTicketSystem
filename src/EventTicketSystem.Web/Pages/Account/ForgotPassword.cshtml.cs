using System.ComponentModel.DataAnnotations;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventTicketSystem.Web.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel(
    UserManager<ApplicationUser> userManager,
    EmailService emailService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool EmailSent { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Always show success — never reveal whether email exists
        EmailSent = true;

        var user = await userManager.FindByEmailAsync(Input.Email.Trim());
        if (user != null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { token, email = user.Email },
                protocol: Request.Scheme)!;

            var displayName = user.HoTen.Length > 0 ? user.HoTen : (user.UserName ?? user.Email!);
            await emailService.SendPasswordResetAsync(user.Email!, callbackUrl, displayName);
        }

        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;
    }
}
