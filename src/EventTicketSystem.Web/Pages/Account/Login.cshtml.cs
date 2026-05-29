using System.ComponentModel.DataAnnotations;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventTicketSystem.Web.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool ShowCheckoutMessage { get; set; }

    private static readonly string[] CheckoutPaths =
        ["/Cart", "/Orders/Payment", "/Cart/Confirmation", "/Orders/Details"];

    public void OnGet(string? returnUrl, string? reason)
    {
        ReturnUrl = returnUrl;
        ShowCheckoutMessage = reason == "checkout"
            || (!string.IsNullOrEmpty(returnUrl)
                && CheckoutPaths.Any(p =>
                    returnUrl.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Accept both username and email
        var user = await userManager.FindByNameAsync(Input.UsernameOrEmail)
                ?? await userManager.FindByEmailAsync(Input.UsernameOrEmail);

        if (user != null)
        {
            var result = await signInManager.PasswordSignInAsync(
                user.UserName!, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                    return Redirect(ReturnUrl);
                return RedirectToPage("/Index");
            }
        }

        ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email.")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
