using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EventTicketSystem.Web.Pages.Customers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class CustomerDetailsModel(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    EmailService emailService) : PageModel
{
    public ApplicationUser?  Customer   { get; set; }
    public List<string>      Roles      { get; set; } = [];
    public List<Order>       Orders     { get; set; } = [];
    public bool              IsLocked   { get; set; }
    public DateTimeOffset?   LockoutEnd { get; set; }
    public bool              CanDelete  { get; set; }
    public bool              IsSelf     { get; set; }
    public bool              IsSuperAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        await LoadAsync(id);
        return Customer == null ? NotFound() : Page();
    }

    // ── Lock ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostLockAsync(string id, string duration)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Không thể khóa chính tài khoản của bạn.";
            return RedirectToPage(new { id });
        }

        await userManager.SetLockoutEnabledAsync(user, true);

        var end = duration switch
        {
            "1"  => DateTimeOffset.UtcNow.AddDays(1),
            "7"  => DateTimeOffset.UtcNow.AddDays(7),
            "30" => DateTimeOffset.UtcNow.AddDays(30),
            _    => DateTimeOffset.MaxValue   // vĩnh viễn
        };

        await userManager.SetLockoutEndDateAsync(user, end);
        TempData["Success"] = end == DateTimeOffset.MaxValue
            ? $"Đã khóa vĩnh viễn tài khoản {user.Email}."
            : $"Đã khóa tài khoản {user.Email} đến {end.ToLocalTime():HH:mm dd/MM/yyyy}.";

        return RedirectToPage(new { id });
    }

    // ── Unlock ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUnlockAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        TempData["Success"] = $"Đã mở khóa tài khoản {user.Email}.";
        return RedirectToPage(new { id });
    }

    // ── Reset password ────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostResetPasswordAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var tempPwd  = GenerateRandomPassword();
        var token    = await userManager.GeneratePasswordResetTokenAsync(user);
        var result   = await userManager.ResetPasswordAsync(user, token, tempPwd);

        if (result.Succeeded)
        {
            await emailService.SendTempPasswordAsync(user.Email!, user.HoTen, tempPwd);
            TempData["Success"] = $"Đã đặt lại mật khẩu cho {user.Email} và gửi email thông báo.";
        }
        else
        {
            TempData["Error"] = "Đặt lại mật khẩu thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToPage(new { id });
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Không thể xóa chính tài khoản của bạn.";
            return RedirectToPage(new { id });
        }

        var hasOrders = await db.Orders
            .AnyAsync(o => o.ApplicationUserId == id || o.CustomerEmail == user.Email);
        if (hasOrders)
        {
            TempData["Error"] = "Không thể xóa tài khoản này vì đã có đơn hàng liên quan.";
            return RedirectToPage(new { id });
        }

        await userManager.DeleteAsync(user);
        TempData["Success"] = $"Đã xóa tài khoản {user.Email}.";
        return RedirectToPage("/Customers/Index");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task LoadAsync(string id)
    {
        Customer = await userManager.FindByIdAsync(id);
        if (Customer == null) return;

        Roles      = [.. await userManager.GetRolesAsync(Customer)];
        IsLocked   = await userManager.IsLockedOutAsync(Customer);
        LockoutEnd = await userManager.GetLockoutEndDateAsync(Customer);
        IsSelf     = Customer.Id == userManager.GetUserId(User);
        IsSuperAdmin = User.IsInRole("SuperAdmin");

        Orders = await db.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.ApplicationUserId == id || o.CustomerEmail == Customer.Email)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        CanDelete = !Orders.Any() && !IsSelf;
    }

    private static string GenerateRandomPassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghijkmnopqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "!@#$";
        var all = upper + lower + digits + special;

        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);

        var chars = new List<char>
        {
            upper  [bytes[0]  % upper.Length],
            digits [bytes[1]  % digits.Length],
            special[bytes[2]  % special.Length]
        };
        for (int i = 3; i < 12; i++)
            chars.Add(all[bytes[i] % all.Length]);

        RandomNumberGenerator.Fill(bytes);
        for (int i = chars.Count - 1; i > 0; i--)
        {
            int j = bytes[i % bytes.Length] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string([.. chars]);
    }
}
