using System.ComponentModel.DataAnnotations;
using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventTicketSystem.Web.Pages.Events;

[Authorize(Roles = "Admin")]
public class CreateEventModel(AppDbContext db, IWebHostEnvironment env) : PageModel
{
    [BindProperty]
    public EventInputModel Input { get; set; } = new();

    [BindProperty]
    public List<TicketTypeInputModel> TicketTypes { get; set; } = [];

    [BindProperty]
    public List<IFormFile> GalleryImages { get; set; } = [];

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        // Require at least one ticket type with a non-empty name
        if (!TicketTypes.Any(t => !string.IsNullOrWhiteSpace(t.Name)))
            ModelState.AddModelError("", "Vui lòng thêm ít nhất một loại vé.");

        if (!ModelState.IsValid)
            return Page();

        string? imageUrl = null;
        if (Input.BannerImage != null && Input.BannerImage.Length > 0)
        {
            var ext = Path.GetExtension(Input.BannerImage.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("Input.BannerImage", "Chỉ chấp nhận file ảnh (.jpg, .png, .gif, .webp).");
                return Page();
            }

            var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "events");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = System.IO.File.Create(filePath);
            await Input.BannerImage.CopyToAsync(stream);
            imageUrl = $"/uploads/events/{fileName}";
        }

        var evt = new Event
        {
            Title = Input.Title,
            Description = Input.Description ?? "",
            StartDate = Input.StartDate.ToUniversalTime(),
            EndDate = Input.EndDate?.ToUniversalTime(),
            Venue = Input.Venue,
            Category = Input.Category ?? "",
            ImageUrl = imageUrl,
            IsActive = true
        };

        foreach (var tt in TicketTypes.Where(t => !string.IsNullOrWhiteSpace(t.Name)))
        {
            evt.TicketTypes.Add(new TicketType
            {
                Name = tt.Name,
                Description = tt.Description ?? "",
                Price = tt.Price,
                TotalQuantity = tt.TotalQuantity
            });
        }

        db.Events.Add(evt);
        await db.SaveChangesAsync();

        // Gallery images
        var allowed2 = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        int sort = 0;
        var galleryDir = Path.Combine(env.WebRootPath, "uploads", "gallery");
        Directory.CreateDirectory(galleryDir);

        foreach (var file in GalleryImages.Where(f => f.Length > 0))
        {
            var ext2 = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed2.Contains(ext2)) continue;

            var fileName2 = $"{Guid.NewGuid()}{ext2}";
            await using var s = System.IO.File.Create(Path.Combine(galleryDir, fileName2));
            await file.CopyToAsync(s);
            evt.Images.Add(new EventImage
            {
                ImagePath = $"/uploads/gallery/{fileName2}",
                SortOrder = sort++
            });
        }

        if (sort > 0) await db.SaveChangesAsync();

        return RedirectToPage("Details", new { id = evt.Id });
    }

    public class EventInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sự kiện")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa điểm")]
        [MaxLength(300)]
        public string Venue { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        public IFormFile? BannerImage { get; set; }
    }

    public class TicketTypeInputModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Range(0, 1_000_000_000)]
        public decimal Price { get; set; }
        [Range(1, 1_000_000)]
        public int TotalQuantity { get; set; } = 1;
    }
}
