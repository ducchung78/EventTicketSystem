using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Pages.Account;

[Authorize]
public class ProfileModel(UserManager<ApplicationUser> userManager, AppDbContext db) : PageModel
{
    public ApplicationUser? CurrentUser { get; set; }
    public int OrderCount { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool SavedOk { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentUser = await userManager.GetUserAsync(User);
        if (CurrentUser == null) return RedirectToPage("/Account/Login");

        SavedOk = TempData["ProfileSaved"] as string == "true";

        Input.HoTen = CurrentUser.HoTen;
        Input.PhoneNumber = CurrentUser.PhoneNumber ?? "";

        OrderCount = await db.Orders.CountAsync(o => o.ApplicationUserId == CurrentUser.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CurrentUser = await userManager.GetUserAsync(User);
        if (CurrentUser == null) return RedirectToPage("/Account/Login");

        OrderCount = await db.Orders.CountAsync(o => o.ApplicationUserId == CurrentUser.Id);

        if (!ModelState.IsValid) return Page();

        CurrentUser.HoTen = Input.HoTen.Trim();
        CurrentUser.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();

        var result = await userManager.UpdateAsync(CurrentUser);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return Page();
        }

        TempData["ProfileSaved"] = "true";
        return RedirectToPage();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string HoTen { get; set; } = "";

        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string PhoneNumber { get; set; } = "";
    }
}
