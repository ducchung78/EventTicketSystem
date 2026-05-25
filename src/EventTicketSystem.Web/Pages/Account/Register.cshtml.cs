using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventTicketSystem.Web.Pages.Account;

public class RegisterModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (!Regex.IsMatch(Input.Password, @"[A-Z]"))
        {
            ModelState.AddModelError("Input.Password", "Mật khẩu phải có ít nhất 1 chữ hoa (A-Z).");
            return Page();
        }
        if (!Regex.IsMatch(Input.Password, @"[!@#$%^&*()\-_=+\[\]{};:'"",.<>?/\\|`~]"))
        {
            ModelState.AddModelError("Input.Password", "Mật khẩu phải có ít nhất 1 ký tự đặc biệt (!@#$%^&*...).");
            return Page();
        }

        var existingByName = await userManager.FindByNameAsync(Input.UserName);
        if (existingByName != null)
        {
            ModelState.AddModelError("Input.UserName", "Tên người dùng này đã được sử dụng.");
            return Page();
        }
        var existingByEmail = await userManager.FindByEmailAsync(Input.Email);
        if (existingByEmail != null)
        {
            ModelState.AddModelError("Input.Email", "Email này đã được đăng ký.");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.UserName,
            Email = Input.Email,
            Ho = Input.Ho.Trim(),
            Ten = Input.Ten.Trim(),
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "User");
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("/Index");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);
        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [MaxLength(50, ErrorMessage = "Họ tối đa 50 ký tự.")]
        public string Ho { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [MaxLength(50, ErrorMessage = "Tên tối đa 50 ký tự.")]
        public string Ten { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên người dùng.")]
        [MaxLength(50, ErrorMessage = "Tên người dùng tối đa 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9._@+\-]+$",
            ErrorMessage = "Tên người dùng chỉ được chứa chữ cái, số và ký tự . _ @ + -")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
