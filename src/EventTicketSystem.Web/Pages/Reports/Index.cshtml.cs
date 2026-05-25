using System.Text.Json;
using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Reports;

[Authorize(Roles = "Admin")]
public class ReportsIndexModel(AppDbContext db) : PageModel
{
    public decimal TongDoanhThu   { get; set; }
    public int     TongDonHang    { get; set; }
    public int     TongVeBan      { get; set; }

    public string DayLabels   { get; set; } = "[]";
    public string DayData     { get; set; } = "[]";
    public string MonthLabels { get; set; } = "[]";
    public string MonthData   { get; set; } = "[]";
    public string EventLabels { get; set; } = "[]";
    public string EventData   { get; set; } = "[]";

    public async Task OnGetAsync()
    {
        var confirmedOrders = db.Orders.Where(o => o.Status == OrderStatus.Confirmed);

        TongDoanhThu = await confirmedOrders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        TongDonHang  = await confirmedOrders.CountAsync();
        TongVeBan    = await db.OrderItems
            .Where(oi => oi.Order.Status == OrderStatus.Confirmed)
            .SumAsync(oi => (int?)oi.Quantity) ?? 0;

        // By day – last 30 days
        var from30 = DateTime.UtcNow.AddDays(-29).Date;
        var byDay = await confirmedOrders
            .Where(o => o.OrderDate >= from30)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Date)
            .ToListAsync();

        DayLabels = JsonSerializer.Serialize(byDay.Select(x => x.Date.ToString("dd/MM")).ToList());
        DayData   = JsonSerializer.Serialize(byDay.Select(x => (double)x.Total).ToList());

        // By month – current year
        int year = DateTime.UtcNow.Year;
        var byMonth = await confirmedOrders
            .Where(o => o.OrderDate.Year == year)
            .GroupBy(o => o.OrderDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Month)
            .ToListAsync();

        string[] vietMonths = ["T1","T2","T3","T4","T5","T6","T7","T8","T9","T10","T11","T12"];
        MonthLabels = JsonSerializer.Serialize(byMonth.Select(x => vietMonths[x.Month - 1]).ToList());
        MonthData   = JsonSerializer.Serialize(byMonth.Select(x => (double)x.Total).ToList());

        // By event – top 8
        var byEvent = await db.OrderItems
            .Where(oi => oi.Order.Status == OrderStatus.Confirmed)
            .GroupBy(oi => oi.TicketType.Event.Title)
            .Select(g => new { EventTitle = g.Key, Total = g.Sum(oi => oi.UnitPrice * oi.Quantity) })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToListAsync();

        EventLabels = JsonSerializer.Serialize(byEvent.Select(x => x.EventTitle).ToList());
        EventData   = JsonSerializer.Serialize(byEvent.Select(x => (double)x.Total).ToList());
    }
}
