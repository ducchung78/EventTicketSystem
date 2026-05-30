using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Admin.RefundRequests;

[Authorize(Roles = "Admin,SuperAdmin")]
public class RefundRequestsIndexModel(
    AppDbContext db,
    RefundService refundService,
    UserManager<ApplicationUser> userManager) : PageModel
{
    public List<RefundRequest> Requests { get; set; } = [];
    public string Filter { get; set; } = "All";

    public int PendingCount      { get; set; }
    public int AutoApprovedCount { get; set; }
    public int ApprovedCount     { get; set; }
    public int RejectedCount     { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        Filter = filter ?? "All";

        var query = db.RefundRequests
            .Include(r => r.Order)
            .AsQueryable();

        if (Enum.TryParse<RefundStatus>(Filter, out var statusFilter))
            query = query.Where(r => r.Status == statusFilter);

        Requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        PendingCount      = await db.RefundRequests.CountAsync(r => r.Status == RefundStatus.Pending);
        AutoApprovedCount = await db.RefundRequests.CountAsync(r => r.Status == RefundStatus.AutoApproved);
        ApprovedCount     = await db.RefundRequests.CountAsync(r => r.Status == RefundStatus.Approved);
        RejectedCount     = await db.RefundRequests.CountAsync(r => r.Status == RefundStatus.Rejected);
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, string? note)
    {
        var adminId = userManager.GetUserId(User)!;
        await refundService.ApproveAsync(id, adminId, note);
        TempData["AdminMsg"] = "Đã duyệt yêu cầu hoàn vé.";
        return RedirectToPage(new { filter = "Pending" });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string? note)
    {
        var adminId = userManager.GetUserId(User)!;
        await refundService.RejectAsync(id, adminId, note);
        TempData["AdminMsg"] = "Đã từ chối yêu cầu hoàn vé.";
        return RedirectToPage(new { filter = "Pending" });
    }
}
