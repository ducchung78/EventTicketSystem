using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Events;

public class EventsIndexModel(AppDbContext db, IWebHostEnvironment env) : PageModel
{
    public List<Event> Events { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public string? Search { get; set; }
    public string? Category { get; set; }

    public async Task OnGetAsync(string? search, string? category)
    {
        Search = search;
        Category = category;

        Categories = await db.Events
            .Where(e => e.Category != null && e.Category != "")
            .Select(e => e.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var query = db.Events.Include(e => e.TicketTypes).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Title.Contains(search) || e.Venue.Contains(search) || e.Description.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        Events = await query
            .OrderByDescending(e => e.IsHot)
            .ThenBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleHotAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var evt = await db.Events.FindAsync(id);
        if (evt == null) return NotFound();

        evt.IsHot = !evt.IsHot;
        await db.SaveChangesAsync();

        return RedirectToPage(new { search = Request.Form["search"].ToString(), category = Request.Form["category"].ToString() });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var evt = await db.Events
            .Include(e => e.TicketTypes)
            .Include(e => e.Images)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null) return RedirectToPage();

        var ttIds = evt.TicketTypes.Select(t => t.Id).ToList();
        if (ttIds.Any())
        {
            var items = await db.OrderItems.Where(oi => ttIds.Contains(oi.TicketTypeId)).ToListAsync();
            db.OrderItems.RemoveRange(items);
        }

        foreach (var img in evt.Images)
            DeletePhysical(img.ImagePath);

        DeletePhysical(evt.ImageUrl);

        db.Events.Remove(evt);
        await db.SaveChangesAsync();

        TempData["Success"] = "Da xoa su kien.";
        return RedirectToPage();
    }

    private void DeletePhysical(string? path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("/uploads/")) return;
        var full = Path.Combine(env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }
}
