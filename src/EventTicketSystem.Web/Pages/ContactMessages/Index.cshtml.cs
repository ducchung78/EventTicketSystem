using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.ContactMessages;

[Authorize(Roles = "Admin")]
public class ContactMessagesIndexModel(AppDbContext db) : PageModel
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
        if (msg != null)
        {
            msg.IsRead = true;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
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

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        await db.ContactMessages
            .Where(m => !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
        return RedirectToPage();
    }
}
