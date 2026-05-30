using System.Text.Json;
using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Reports;

[Authorize(Roles = "Admin,SuperAdmin")]
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

    public DateTime? FromDate  { get; set; }
    public DateTime? ToDate    { get; set; }
    public string    PeriodLabel { get; set; } = "";

    public async Task OnGetAsync(DateTime? fromDate, DateTime? toDate)
    {
        FromDate = fromDate;
        ToDate   = toDate;

        // Effective range (UTC dates)
        var effFrom = (fromDate ?? DateTime.UtcNow.AddDays(-29)).Date;
        var effTo   = (toDate   ?? DateTime.UtcNow).Date.AddDays(1); // exclusive upper bound

        PeriodLabel = $"{effFrom:dd/MM/yyyy} – {effTo.AddDays(-1):dd/MM/yyyy}";

        var confirmedOrders = db.Orders
            .Where(o => o.Status == OrderStatus.Confirmed
                     && o.OrderDate >= effFrom
                     && o.OrderDate <  effTo);

        TongDoanhThu = await confirmedOrders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        TongDonHang  = await confirmedOrders.CountAsync();
        TongVeBan    = await db.OrderItems
            .Where(oi => oi.Order.Status == OrderStatus.Confirmed
                      && oi.Order.OrderDate >= effFrom
                      && oi.Order.OrderDate <  effTo)
            .SumAsync(oi => (int?)oi.Quantity) ?? 0;

        // By day — every date in range
        var byDay = await confirmedOrders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Date)
            .ToListAsync();

        DayLabels = JsonSerializer.Serialize(byDay.Select(x => x.Date.ToString("dd/MM")).ToList());
        DayData   = JsonSerializer.Serialize(byDay.Select(x => (double)x.Total).ToList());

        // By month — group across years if range spans multiple years
        var byMonth = await confirmedOrders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        string[] vietMonths = ["T1","T2","T3","T4","T5","T6","T7","T8","T9","T10","T11","T12"];
        // Show "T5/2026" only when range spans more than one year
        bool multiYear = byMonth.Select(x => x.Year).Distinct().Count() > 1;
        MonthLabels = JsonSerializer.Serialize(
            byMonth.Select(x => multiYear
                ? $"{vietMonths[x.Month - 1]}/{x.Year}"
                : vietMonths[x.Month - 1]).ToList());
        MonthData = JsonSerializer.Serialize(byMonth.Select(x => (double)x.Total).ToList());

        // By event – top 8
        var byEvent = await db.OrderItems
            .Where(oi => oi.Order.Status == OrderStatus.Confirmed
                      && oi.Order.OrderDate >= effFrom
                      && oi.Order.OrderDate <  effTo)
            .GroupBy(oi => oi.TicketType.Event.Title)
            .Select(g => new { EventTitle = g.Key, Total = g.Sum(oi => oi.UnitPrice * oi.Quantity) })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToListAsync();

        EventLabels = JsonSerializer.Serialize(byEvent.Select(x => x.EventTitle).ToList());
        EventData   = JsonSerializer.Serialize(byEvent.Select(x => (double)x.Total).ToList());
    }
}
