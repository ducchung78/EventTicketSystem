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
    public List<Event> TrendingEvents { get; set; } = [];
    public record CityCard(string Name, string SearchTerm, string ImageUrl);

    public static readonly IReadOnlyList<CityCard> MainCities = new CityCard[]
    {
        new("Tp. Hồ Chí Minh", "Hồ Chí Minh",
            "https://images.unsplash.com/photo-1593449227036-9de17c6316e2?w=800&q=80&fit=crop&auto=format"),
        new("Hà Nội", "Hà Nội",
            "https://images.unsplash.com/photo-1723665479556-147440fd9527?w=800&q=80&fit=crop&auto=format"),
        new("Đà Lạt", "Đà Lạt",
            "https://images.unsplash.com/photo-1552310065-aad9ebece999?w=800&q=80&fit=crop&auto=format"),
    };

    public static readonly IReadOnlyList<CityCard> MiniCities = new CityCard[]
    {
        new("Vịnh Hạ Long", "Hạ Long",
            "https://images.unsplash.com/photo-1561461221-959c3f16234b?w=400&q=80&fit=crop&auto=format"),
        new("Cầu Vàng Đà Nẵng", "Đà Nẵng",
            "https://images.unsplash.com/photo-1741138327956-dfa75763b50d?w=400&q=80&fit=crop&auto=format"),
        new("Hội An", "Hội An",
            "https://images.unsplash.com/photo-1639458110591-17c4cede0c4b?w=400&q=80&fit=crop&auto=format"),
        new("Ninh Bình", "Ninh Bình",
            "https://images.unsplash.com/photo-1560079561-3086e6dbde25?w=400&q=80&fit=crop&auto=format"),
    };

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

        // Trending: RankScore = TicketsSold + PriorityBoost * factor
        // 1 điểm PriorityBoost ~ 75 vé bán — đủ để admin đẩy nhẹ, không ghim tuyệt đối
        const int PriorityBoostFactor = 75;

        var trendingRanked = await db.Events
            .Where(e => e.IsActive)
            .Select(e => new {
                EventId = e.Id,
                RankScore = e.TicketTypes.Sum(t => t.SoldQuantity) + e.PriorityBoost * PriorityBoostFactor
            })
            .OrderByDescending(x => x.RankScore)
            .Take(6)
            .ToListAsync();

        if (trendingRanked.Count > 0)
        {
            var trendingIds = trendingRanked.Select(x => x.EventId).ToList();
            var eventsById = await db.Events
                .Include(e => e.TicketTypes)
                .Where(e => trendingIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id);

            TrendingEvents = trendingIds
                .Where(id => eventsById.ContainsKey(id))
                .Select(id => eventsById[id])
                .ToList();
        }

    }

    private Task<List<Event>> ByCategoryAsync(string category, DateTime from) =>
        db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive && e.Category == category && e.StartDate >= from)
            .OrderBy(e => e.StartDate)
            .Take(10)
            .ToListAsync();
}
