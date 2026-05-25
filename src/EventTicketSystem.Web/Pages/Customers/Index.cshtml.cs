using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Customers;

[Authorize(Roles = "Admin")]
public class CustomersIndexModel(AppDbContext db, UserManager<ApplicationUser> userManager) : PageModel
{
    public List<CustomerViewModel> Customers { get; set; } = [];
    public string? Search { get; set; }

    public async Task OnGetAsync(string? search)
    {
        Search = search;

        var usersQuery = userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            usersQuery = usersQuery.Where(u =>
                u.HoTen.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));

        var users = await usersQuery.OrderByDescending(u => u.NgayTao).ToListAsync();

        var orderStats = await db.Orders
            .Where(o => o.Status == OrderStatus.Confirmed)
            .GroupBy(o => o.CustomerEmail)
            .Select(g => new { Email = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        var statsDict = orderStats.ToDictionary(x => x.Email);

        Customers = users.Select(u =>
        {
            statsDict.TryGetValue(u.Email ?? "", out var stats);
            return new CustomerViewModel
            {
                Id       = u.Id,
                HoTen    = u.HoTen,
                Email    = u.Email ?? "",
                Phone    = u.PhoneNumber,
                NgayTao  = u.NgayTao,
                SoDon    = stats?.Count ?? 0,
                TongTien = stats?.Total ?? 0m
            };
        }).ToList();
    }

    public class CustomerViewModel
    {
        public string  Id       { get; set; } = string.Empty;
        public string  HoTen    { get; set; } = string.Empty;
        public string  Email    { get; set; } = string.Empty;
        public string? Phone    { get; set; }
        public DateTime NgayTao { get; set; }
        public int     SoDon    { get; set; }
        public decimal TongTien { get; set; }
    }
}
