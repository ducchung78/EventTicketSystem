using System.ComponentModel.DataAnnotations;
using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Events;

[Authorize(Roles = "Admin,SuperAdmin")]
public class EditEventModel(AppDbContext db, IWebHostEnvironment env) : PageModel
{
    public Event? Event { get; set; }
    public List<EventImage> ExistingGalleryImages { get; set; } = [];

    [BindProperty] public EventEditInput Input { get; set; } = new();
    [BindProperty] public List<TicketTypeEditInput> TicketTypes { get; set; } = [];
    [BindProperty] public List<IFormFile> GalleryImages { get; set; } = [];
    [BindProperty] public List<int> DeletedGalleryImageIds { get; set; } = [];

    // ── GET ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadEventAsync(id);
        if (Event == null) return Page();

        Input = new EventEditInput
        {
            Title       = Event.Title,
            Description = Event.Description,
            StartDate   = Event.StartDate.ToLocalTime(),
            EndDate     = Event.EndDate?.ToLocalTime(),
            Venue       = Event.Venue,
            Category    = Event.Category,
            IsActive    = Event.IsActive,
            IsSpecial   = Event.IsSpecial
        };

        TicketTypes = Event.TicketTypes.Select(tt => new TicketTypeEditInput
        {
            Id            = tt.Id,
            Name          = tt.Name,
            Description   = tt.Description,
            Price         = tt.Price,
            TotalQuantity = tt.TotalQuantity,
            SoldQuantity  = tt.SoldQuantity
        }).ToList();

        return Page();
    }

    // ── POST ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync(int id)
    {
        await LoadEventAsync(id);
        if (Event == null) return NotFound();

        if (!ModelState.IsValid) return Page();

        // Banner image
        if (Input.BannerImage is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.BannerImage.FileName).ToLowerInvariant();
            if (!AllowedImageExts.Contains(ext))
            {
                ModelState.AddModelError("Input.BannerImage", "Chỉ chấp nhận .jpg .png .gif .webp");
                return Page();
            }
            DeletePhysical(Event.ImageUrl);
            Event.ImageUrl = await SaveFileAsync(Input.BannerImage, "events");
        }
        else if (Input.RemoveImage)
        {
            DeletePhysical(Event.ImageUrl);
            Event.ImageUrl = null;
        }

        // Event fields
        Event.Title       = Input.Title;
        Event.Description = Input.Description ?? "";
        Event.StartDate   = Input.StartDate.ToUniversalTime();
        Event.EndDate     = Input.EndDate?.ToUniversalTime();
        Event.Venue       = Input.Venue;
        Event.Category    = Input.Category ?? "";
        Event.IsActive    = Input.IsActive;
        Event.IsSpecial   = Input.IsSpecial;

        // Ticket types ─────────────────────────────────────────────────────
        var deletedSet = TicketTypes.Where(t => t.IsDeleted && t.Id > 0).Select(t => t.Id).ToHashSet();

        foreach (var ttId in deletedSet)
        {
            var dbTt = Event.TicketTypes.FirstOrDefault(t => t.Id == ttId);
            if (dbTt != null && dbTt.SoldQuantity == 0)
                db.TicketTypes.Remove(dbTt);
        }

        foreach (var tt in TicketTypes.Where(t => !t.IsDeleted))
        {
            if (tt.Id > 0)
            {
                var dbTt = Event.TicketTypes.FirstOrDefault(t => t.Id == tt.Id);
                if (dbTt != null)
                {
                    dbTt.Name          = tt.Name.Trim();
                    dbTt.Description   = tt.Description ?? "";
                    dbTt.Price         = tt.Price;
                    dbTt.TotalQuantity = Math.Max(tt.TotalQuantity, dbTt.SoldQuantity);
                }
            }
            else if (!string.IsNullOrWhiteSpace(tt.Name))
            {
                Event.TicketTypes.Add(new TicketType
                {
                    Name          = tt.Name.Trim(),
                    Description   = tt.Description ?? "",
                    Price         = tt.Price,
                    TotalQuantity = Math.Max(tt.TotalQuantity, 1)
                });
            }
        }

        // Gallery: delete ─────────────────────────────────────────────────
        foreach (var imgId in DeletedGalleryImageIds)
        {
            var img = Event.Images.FirstOrDefault(i => i.Id == imgId);
            if (img == null) continue;
            DeletePhysical(img.ImagePath);
            db.EventImages.Remove(img);
        }

        // Gallery: add new ────────────────────────────────────────────────
        int nextSort = Event.Images.Any() ? Event.Images.Max(i => i.SortOrder) + 1 : 0;
        foreach (var file in GalleryImages.Where(f => f.Length > 0))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExts.Contains(ext)) continue;
            var path = await SaveFileAsync(file, "gallery");
            Event.Images.Add(new EventImage { ImagePath = path, SortOrder = nextSort++ });
        }

        await db.SaveChangesAsync();
        return RedirectToPage("Details", new { id });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static readonly string[] AllowedImageExts = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    private async Task LoadEventAsync(int id)
    {
        Event = await db.Events
            .Include(e => e.TicketTypes)
            .Include(e => e.Images)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (Event != null)
            ExistingGalleryImages = [.. Event.Images.OrderBy(i => i.SortOrder)];
    }

    private async Task<string> SaveFileAsync(IFormFile file, string subFolder)
    {
        var dir = Path.Combine(env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using var stream = System.IO.File.Create(Path.Combine(dir, fileName));
        await file.CopyToAsync(stream);
        return $"/uploads/{subFolder}/{fileName}";
    }

    private void DeletePhysical(string? urlPath)
    {
        if (string.IsNullOrEmpty(urlPath)) return;
        var full = Path.Combine(env.WebRootPath, urlPath.TrimStart('/'));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }

    // ── Input models ─────────────────────────────────────────────────────────
    public class EventEditInput
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sự kiện"), MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa điểm"), MaxLength(300)]
        public string Venue { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        public bool IsActive  { get; set; }
        public bool IsSpecial { get; set; }

        public IFormFile? BannerImage { get; set; }
        public bool RemoveImage { get; set; }
    }

    public class TicketTypeEditInput
    {
        public int     Id            { get; set; }
        public bool    IsDeleted     { get; set; }
        public string  Name          { get; set; } = string.Empty;
        public string? Description   { get; set; }
        public decimal Price         { get; set; }
        public int     TotalQuantity { get; set; } = 1;
        public int     SoldQuantity  { get; set; }  // view-only, from DB
    }
}
