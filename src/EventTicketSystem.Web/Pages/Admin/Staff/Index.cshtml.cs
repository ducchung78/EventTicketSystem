using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace EventTicketSystem.Web.Pages.Admin.Staff;

[Authorize(Roles = "SuperAdmin")]
public class StaffIndexModel(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    EmailService emailService) : PageModel
{
    public List<StaffViewModel> Staff { get; set; } = [];
    public string? Search { get; set; }
    public string? CreatedPassword { get; set; }

    public async Task OnGetAsync(string? search)
    {
        Search = search;
        await LoadStaffAsync(search);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        [Required] string ho,
        [Required] string ten,
        [Required][EmailAddress] string email,
        [Required] string role)
    {
        if (!ModelState.IsValid)
        {
            await LoadStaffAsync(null);
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return Page();
        }

        if (!new[] { "Admin", "SuperAdmin" }.Contains(role))
        {
            TempData["Error"] = "Vai trò không hợp lệ.";
            return RedirectToPage();
        }

        if (await userManager.FindByEmailAsync(email) != null)
        {
            TempData["Error"] = $"Email {email} đã tồn tại trong hệ thống.";
            return RedirectToPage();
        }

        var tempPwd = GenerateRandomPassword();
        var user = new ApplicationUser
        {
            UserName       = email,
            Email          = email,
            Ho             = ho.Trim(),
            Ten            = ten.Trim(),
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var result = await userManager.CreateAsync(user, tempPwd);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Tạo tài khoản thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        await userManager.AddToRoleAsync(user, role);
        if (role == "SuperAdmin")
            await userManager.AddToRoleAsync(user, "Admin");

        await emailService.SendTempPasswordAsync(email, $"{ho} {ten}".Trim(), tempPwd);

        TempData["Success"] = $"Đã tạo tài khoản {role} cho {email}. Mật khẩu tạm đã gửi email.";
        TempData["CreatedPwd"] = tempPwd;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string id, string action)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Không thể tự khóa tài khoản mình.";
            return RedirectToPage();
        }

        if (action == "lock")
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        else
            await userManager.SetLockoutEndDateAsync(user, null);

        TempData["Success"] = action == "lock"
            ? $"Đã khóa tài khoản {user.Email}."
            : $"Đã mở khóa tài khoản {user.Email}.";

        return RedirectToPage();
    }

    private async Task LoadStaffAsync(string? search)
    {
        var adminRole      = await roleManager.FindByNameAsync("Admin");
        var superAdminRole = await roleManager.FindByNameAsync("SuperAdmin");

        if (adminRole == null) return;

        var adminIds      = userManager.GetUsersInRoleAsync("Admin").Result.Select(u => u.Id).ToHashSet();
        var superAdminIds = superAdminRole != null
            ? userManager.GetUsersInRoleAsync("SuperAdmin").Result.Select(u => u.Id).ToHashSet()
            : new HashSet<string>();

        var allStaff = userManager.Users
            .Where(u => adminIds.Contains(u.Id))
            .OrderBy(u => u.Ho).ThenBy(u => u.Ten)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
            allStaff = allStaff.Where(u =>
                (u.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.HoTen.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        var currentId = userManager.GetUserId(User);

        Staff = allStaff.Select(u => new StaffViewModel
        {
            Id          = u.Id,
            HoTen       = u.HoTen,
            Email       = u.Email ?? "",
            IsSuperAdmin = superAdminIds.Contains(u.Id),
            IsLocked    = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
            NgayTao     = u.NgayTao,
            IsSelf      = u.Id == currentId
        }).ToList();
    }

    private static string GenerateRandomPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$";
        var all = upper + lower + digits + special;
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        var chars = new List<char> { upper[bytes[0] % upper.Length], digits[bytes[1] % digits.Length], special[bytes[2] % special.Length] };
        for (int i = 3; i < 12; i++) chars.Add(all[bytes[i] % all.Length]);
        RandomNumberGenerator.Fill(bytes);
        for (int i = chars.Count - 1; i > 0; i--) { int j = bytes[i % bytes.Length] % (i + 1); (chars[i], chars[j]) = (chars[j], chars[i]); }
        return new string([.. chars]);
    }

    public class StaffViewModel
    {
        public string   Id           { get; set; } = "";
        public string   HoTen        { get; set; } = "";
        public string   Email        { get; set; } = "";
        public bool     IsSuperAdmin { get; set; }
        public bool     IsLocked     { get; set; }
        public DateTime NgayTao      { get; set; }
        public bool     IsSelf       { get; set; }
    }
}
