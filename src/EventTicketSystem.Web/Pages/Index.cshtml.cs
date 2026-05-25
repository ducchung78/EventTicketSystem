using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Event> HotEvents { get; set; } = [];
    public List<Event> SpecialEvents { get; set; } = [];
    public List<Event> WeekendEvents { get; set; } = [];
    public List<Event> ThisMonthEvents { get; set; } = [];
    public List<Event> MusicEvents { get; set; } = [];
    public List<Event> ArtEvents { get; set; } = [];
    public List<Event> WorkshopEvents { get; set; } = [];
    public List<Event> TourEvents { get; set; } = [];
    public List<Event> SportsEvents { get; set; } = [];
    public List<VenueInfo> FeaturedVenues { get; set; } = [];

    public record VenueInfo(string Name, int EventCount, string? ImageUrl);

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;

        HotEvents = await db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive && e.IsHot)
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        SpecialEvents = await db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive && e.IsSpecial && e.StartDate >= now)
            .OrderBy(e => e.StartDate)
            .Take(10)
            .ToListAsync();

        // Cuối tuần: thứ 7 hoặc CN trong 14 ngày tới
        var upcoming14 = await db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive && e.StartDate >= now && e.StartDate <= now.AddDays(14))
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        WeekendEvents = upcoming14
            .Where(e => e.StartDate.ToLocalTime().DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            .ToList();

        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

        ThisMonthEvents = await db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive && e.StartDate >= now && e.StartDate <= endOfMonth)
            .OrderBy(e => e.StartDate)
            .Take(10)
            .ToListAsync();

        MusicEvents    = await ByCategoryAsync("Âm nhạc", now);
        ArtEvents      = await ByCategoryAsync("Nghệ thuật", now);
        WorkshopEvents = await ByCategoryAsync("Hội thảo", now);
        TourEvents     = await ByCategoryAsync("Tham quan", now);
        SportsEvents   = await ByCategoryAsync("Thể thao", now);

        var rawVenues = await db.Events
            .Where(e => e.IsActive && e.StartDate >= now)
            .Select(e => new { e.Venue, e.ImageUrl })
            .ToListAsync();

        FeaturedVenues = rawVenues
            .GroupBy(e => e.Venue)
            .Select(g => new VenueInfo(g.Key, g.Count(), g.FirstOrDefault(x => x.ImageUrl != null)?.ImageUrl))
            .OrderByDescending(v => v.EventCount)
            .Take(8)
            .ToList();
    }

    private Task<List<Event>> ByCategoryAsync(string category, DateTime from) =>
        db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive && e.Category == category && e.StartDate >= from)
            .OrderBy(e => e.StartDate)
            .Take(10)
            .ToListAsync();
}
