using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Pages.Contact;

[Authorize]
public class ContactIndexModel(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    RefundService refundService) : PageModel
{
    public bool IsAdmin      { get; set; }
    public bool Sent         { get; set; }
    public bool RefundSent   { get; set; }
    public bool RefundAutoApproved { get; set; }
    public string PrefillName  { get; set; } = "";
    public string PrefillEmail { get; set; } = "";

    [BindProperty]
    public ContactInputModel Input { get; set; } = new();

    [BindProperty]
    public RefundInputModel RefundInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        IsAdmin = User.IsInRole("Admin");
        Sent    = TempData["ContactSent"]  as string == "true";
        RefundSent          = TempData["RefundSent"]          as string == "true";
        RefundAutoApproved  = TempData["RefundAutoApproved"]  as string == "true";

        var user = await userManager.GetUserAsync(User);
        PrefillName  = user?.HoTen ?? user?.UserName ?? "";
        PrefillEmail = user?.Email ?? "";
    }

    // ── Contact form ─────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        if (User.IsInRole("Admin"))
            return RedirectToPage();

        var user = await userManager.GetUserAsync(User);
        var hoTen = user?.HoTen ?? user?.UserName ?? "";
        var email = user?.Email ?? "";

        if (!ModelState.IsValid)
        {
            PrefillName  = hoTen;
            PrefillEmail = email;
            return Page();
        }

        db.ContactMessages.Add(new ContactMessage
        {
            HoTen     = hoTen,
            Email     = email,
            Phone     = string.IsNullOrWhiteSpace(Input.Phone) ? null : Input.Phone.Trim(),
            Subject   = Input.Subject.Trim(),
            Message   = Input.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        TempData["ContactSent"] = "true";
        return RedirectToPage();
    }

    // ── Refund form ───────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostRefundAsync()
    {
        // Only validate RefundInput fields
        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(RefundInput.ConfirmationCode))
            validationErrors.Add("Vui lòng nhập mã đơn hàng.");
        if (string.IsNullOrWhiteSpace(RefundInput.Email))
            validationErrors.Add("Vui lòng nhập email.");
        if (RefundInput.Reason == null)
            validationErrors.Add("Vui lòng chọn lý do hoàn vé.");
        if (RefundInput.Reason == RefundReason.Other && string.IsNullOrWhiteSpace(RefundInput.Description))
            validationErrors.Add("Vui lòng mô tả chi tiết lý do.");

        var user = await userManager.GetUserAsync(User);
        PrefillName  = user?.HoTen ?? user?.UserName ?? "";
        PrefillEmail = user?.Email ?? "";

        if (validationErrors.Count > 0)
        {
            foreach (var err in validationErrors)
                ModelState.AddModelError("refund", err);
            return Page();
        }

        var (request, error) = await refundService.SubmitAsync(
            RefundInput.ConfirmationCode!,
            RefundInput.Email!,
            RefundInput.Reason!.Value,
            RefundInput.Description,
            user?.Id);

        if (error != null)
        {
            ModelState.AddModelError("refund", error);
            return Page();
        }

        TempData["RefundSent"]         = "true";
        TempData["RefundAutoApproved"] = (request!.Status == RefundStatus.AutoApproved).ToString().ToLower();
        return RedirectToPage();
    }

    // ── Input models ──────────────────────────────────────────────────────────
    public class ContactInputModel
    {
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chủ đề.")]
        [MaxLength(200)]
        public string Subject { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập lời nhắn.")]
        [MaxLength(2000)]
        public string Message { get; set; } = "";
    }

    public class RefundInputModel
    {
        public string? ConfirmationCode { get; set; }
        public string? Email            { get; set; }
        public RefundReason? Reason     { get; set; }
        public string? Description      { get; set; }
    }
}
