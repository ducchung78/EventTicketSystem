using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Pages.Account;

[Authorize]
public class ChangePasswordModel(UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool Success { get; set; }

    public void OnGet()
    {
        Success = TempData["ChangePwdOk"] as string == "true";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage("/Account/Login");

        var result = await userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, Localize(err.Code));
            return Page();
        }

        TempData["ChangePwdOk"] = "true";
        return RedirectToPage();
    }

    private static string Localize(string code) => code switch
    {
        "PasswordMismatch"              => "Mật khẩu hiện tại không đúng.",
        "PasswordTooShort"              => "Mật khẩu mới phải có ít nhất 6 ký tự.",
        "PasswordRequiresDigit"         => "Mật khẩu phải chứa ít nhất một chữ số (0–9).",
        "PasswordRequiresUpper"         => "Mật khẩu phải chứa ít nhất một chữ hoa (A–Z).",
        "PasswordRequiresLower"         => "Mật khẩu phải chứa ít nhất một chữ thường (a–z).",
        "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải chứa ít nhất một ký tự đặc biệt (!, @, #…).",
        _                               => "Đổi mật khẩu thất bại. Vui lòng thử lại."
    };

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }
}
