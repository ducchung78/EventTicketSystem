using System.ComponentModel.DataAnnotations;
using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventTicketSystem.Web.Pages.Admin.PaymentMethods;

[Authorize(Roles = "Admin,SuperAdmin")]
public class EditPaymentMethodModel(AppDbContext db, IWebHostEnvironment env) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public PaymentMethod? Method { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Method = await db.PaymentMethods.FindAsync(id);
        if (Method == null) return NotFound();

        Input = new InputModel
        {
            Name            = Method.Name,
            BankAccountInfo = Method.BankAccountInfo,
            IsActive        = Method.IsActive,
            SortOrder       = Method.SortOrder,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Method = await db.PaymentMethods.FindAsync(id);
        if (Method == null) return NotFound();

        if (!ModelState.IsValid) return Page();

        Method.Name            = Input.Name.Trim();
        Method.BankAccountInfo = Input.BankAccountInfo?.Trim();
        Method.IsActive        = Input.IsActive;
        Method.SortOrder       = Input.SortOrder;

        if (Input.RemoveLogo)
        {
            DeletePhysical(Method.LogoUrl);
            Method.LogoUrl = null;
        }
        else if (Input.LogoFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.LogoFile.FileName).ToLowerInvariant();
            if (!AllowedExts.Contains(ext))
            {
                ModelState.AddModelError("Input.LogoFile", "Chỉ chấp nhận .jpg .png .gif .webp");
                return Page();
            }
            DeletePhysical(Method.LogoUrl);
            Method.LogoUrl = await SaveAsync(Input.LogoFile);
        }

        if (Input.RemoveQr)
        {
            DeletePhysical(Method.QrCodeUrl);
            Method.QrCodeUrl = null;
        }
        else if (Input.QrFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.QrFile.FileName).ToLowerInvariant();
            if (!AllowedExts.Contains(ext))
            {
                ModelState.AddModelError("Input.QrFile", "Chỉ chấp nhận .jpg .png .gif .webp");
                return Page();
            }
            DeletePhysical(Method.QrCodeUrl);
            Method.QrCodeUrl = await SaveAsync(Input.QrFile);
        }

        await db.SaveChangesAsync();
        TempData["AdminMsg"] = $"Đã cập nhật phương thức '{Method.Name}'.";
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

    private void DeletePhysical(string? urlPath)
    {
        if (string.IsNullOrEmpty(urlPath)) return;
        var full = Path.Combine(env.WebRootPath, urlPath.TrimStart('/'));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên phương thức"), MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? BankAccountInfo { get; set; }

        public bool IsActive  { get; set; } = true;
        public int  SortOrder { get; set; } = 0;

        public IFormFile? LogoFile { get; set; }
        public bool RemoveLogo { get; set; }

        public IFormFile? QrFile { get; set; }
        public bool RemoveQr { get; set; }
    }
}
