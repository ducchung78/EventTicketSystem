using System.ComponentModel.DataAnnotations;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventTicketSystem.Web.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel(UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool InvalidLink { get; set; }

    public IActionResult OnGet(string? token, string? email)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
        {
            InvalidLink = true;
            return Page();
        }
        Input.Token = token;
        Input.Email = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // Don't reveal that the user doesn't exist
            TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới.";
            return RedirectToPage("/Account/Login");
        }

        var result = await userManager.ResetPasswordAsync(user, Input.Token, Input.Password);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới.";
            return RedirectToPage("/Account/Login");
        }

        foreach (var error in result.Errors)
        {
            var msg = error.Code switch
            {
                "InvalidToken" => "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn (1 giờ). Vui lòng yêu cầu link mới.",
                "PasswordTooShort" => $"Mật khẩu phải có ít nhất {userManager.Options.Password.RequiredLength} ký tự.",
                _ => error.Description
            };
            ModelState.AddModelError(string.Empty, msg);
        }

        return Page();
    }

    public class InputModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
