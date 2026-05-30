using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Admin.Messages;

[Authorize(Roles = "Admin,SuperAdmin")]
public class MessagesIndexModel(AppDbContext db) : PageModel
{
    public List<ContactMessage> Messages { get; set; } = [];
    public int UnreadCount { get; set; }

    public async Task OnGetAsync()
    {
        Messages = await db.ContactMessages
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        UnreadCount = Messages.Count(m => !m.IsRead);
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int id)
    {
        var msg = await db.ContactMessages.FindAsync(id);
        if (msg != null && !msg.IsRead)
        {
            msg.IsRead = true;
            await db.SaveChangesAsync();
        }
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var msg = await db.ContactMessages.FindAsync(id);
        if (msg != null)
        {
            db.ContactMessages.Remove(msg);
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
