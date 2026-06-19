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
public class ProfileModel(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IWebHostEnvironment env) : PageModel
{
    public ApplicationUser? CurrentUser { get; set; }
    public int OrderCount { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    public bool SavedOk { get; set; }

    private static readonly HashSet<string> _allowedExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB

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

        // Validate avatar file if provided
        if (AvatarFile != null)
        {
            var ext = Path.GetExtension(AvatarFile.FileName);
            if (!_allowedExt.Contains(ext))
                ModelState.AddModelError(nameof(AvatarFile), "Chỉ chấp nhận file ảnh: JPG, PNG, WEBP.");
            if (AvatarFile.Length > MaxAvatarBytes)
                ModelState.AddModelError(nameof(AvatarFile), "Ảnh tối đa 5MB.");
        }

        if (!ModelState.IsValid) return Page();

        // Save avatar
        if (AvatarFile != null && AvatarFile.Length > 0)
        {
            var avatarsDir = Path.Combine(env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(avatarsDir);

            // Delete old avatar file
            if (!string.IsNullOrEmpty(CurrentUser.AvatarUrl))
            {
                var oldPath = Path.Combine(env.WebRootPath, CurrentUser.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var ext = Path.GetExtension(AvatarFile.FileName);
            var fileName = $"{CurrentUser.Id}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
            var filePath = Path.Combine(avatarsDir, fileName);

            using (var fs = System.IO.File.Create(filePath))
                await AvatarFile.CopyToAsync(fs);

            CurrentUser.AvatarUrl = $"/uploads/avatars/{fileName}";
        }

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
