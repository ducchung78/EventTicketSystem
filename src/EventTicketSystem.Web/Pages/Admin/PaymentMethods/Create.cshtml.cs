using System.ComponentModel.DataAnnotations;
using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventTicketSystem.Web.Pages.Admin.PaymentMethods;

[Authorize(Roles = "Admin,SuperAdmin")]
public class CreatePaymentMethodModel(AppDbContext db, IWebHostEnvironment env) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var method = new PaymentMethod
        {
            Name            = Input.Name.Trim(),
            BankAccountInfo = Input.BankAccountInfo?.Trim(),
            IsActive        = Input.IsActive,
            SortOrder       = Input.SortOrder,
            CreatedAt       = DateTime.UtcNow,
        };

        if (Input.LogoFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.LogoFile.FileName).ToLowerInvariant();
            if (!AllowedExts.Contains(ext))
            {
                ModelState.AddModelError("Input.LogoFile", "Chỉ chấp nhận .jpg .png .gif .webp");
                return Page();
            }
            method.LogoUrl = await SaveAsync(Input.LogoFile);
        }

        if (Input.QrFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.QrFile.FileName).ToLowerInvariant();
            if (!AllowedExts.Contains(ext))
            {
                ModelState.AddModelError("Input.QrFile", "Chỉ chấp nhận .jpg .png .gif .webp");
                return Page();
            }
            method.QrCodeUrl = await SaveAsync(Input.QrFile);
        }

        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync();
        TempData["AdminMsg"] = $"Đã thêm phương thức '{method.Name}'.";
        return RedirectToPage("Index");
    }

    private static readonly string[] AllowedExts = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    private async Task<string> SaveAsync(IFormFile file)
    {
        var dir = Path.Combine(env.WebRootPath, "uploads", "payments");
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using var stream = System.IO.File.Create(Path.Combine(dir, fileName));
        await file.CopyToAsync(stream);
        return $"/uploads/payments/{fileName}";
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên phương thức"), MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? BankAccountInfo { get; set; }

        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public IFormFile? LogoFile { get; set; }
        public IFormFile? QrFile { get; set; }
    }
}
