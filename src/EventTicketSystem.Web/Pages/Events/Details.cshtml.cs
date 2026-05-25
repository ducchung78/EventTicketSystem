using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Events;

public class EventDetailsModel(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    CartService cartService) : PageModel
{
    public Event?            Event         { get; set; }
    public ApplicationUser?  CurrentUser   { get; set; }
    public List<EventImage>  GalleryImages { get; set; } = [];

    public async Task OnGetAsync(int id)
    {
        Event = await db.Events
            .Include(e => e.TicketTypes)
            .Include(e => e.Images)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (Event != null)
            GalleryImages = [.. Event.Images.OrderBy(i => i.SortOrder)];

        if (signInManager.IsSignedIn(User))
            CurrentUser = await userManager.GetUserAsync(User);
    }

    public async Task<IActionResult> OnPostBuyAsync(
        int eventId,
        int ticketTypeId,
        int qty,
        string customerName,
        string customerEmail,
        string? customerPhone,
        string? couponCode)
    {
        var ticketType = await db.TicketTypes
            .FirstOrDefaultAsync(t => t.Id == ticketTypeId && t.EventId == eventId);

        if (ticketType == null)
        {
            TempData["Error"] = "Loại vé không hợp lệ.";
            return RedirectToPage(new { id = eventId });
        }

        qty = Math.Clamp(qty, 1, 10);

        if (ticketType.AvailableQuantity < qty)
        {
            TempData["Error"] = $"Chỉ còn {ticketType.AvailableQuantity} vé loại '{ticketType.Name}'.";
            return RedirectToPage(new { id = eventId });
        }

        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerEmail))
        {
            TempData["Error"] = "Vui lòng điền đầy đủ họ tên và email.";
            return RedirectToPage(new { id = eventId });
        }

        var appUser = signInManager.IsSignedIn(User) ? await userManager.GetUserAsync(User) : null;

        // Handle coupon
        Coupon? coupon = null;
        decimal originalAmount = ticketType.Price * qty;
        decimal discountAmount = 0;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode.ToUpper().Trim());
            if (coupon != null && coupon.IsValid(originalAmount, out _))
            {
                discountAmount = coupon.CalculateDiscount(originalAmount);
                coupon.UsedCount++;
            }
        }

        var order = new Order
        {
            CustomerName      = customerName,
            CustomerEmail     = customerEmail,
            CustomerPhone     = customerPhone,
            Status            = OrderStatus.Pending,
            ApplicationUserId = appUser?.Id,
            OriginalAmount    = originalAmount,
            DiscountAmount    = discountAmount,
            TotalAmount       = originalAmount - discountAmount,
            CouponId          = coupon?.Id
        };

        order.OrderItems.Add(new OrderItem
        {
            TicketTypeId = ticketType.Id,
            Quantity     = qty,
            UnitPrice    = ticketType.Price
        });

        ticketType.SoldQuantity += qty;

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return RedirectToPage("/Orders/Payment", new { id = order.Id });
    }

    public async Task<IActionResult> OnPostAddToCartAsync(
        int eventId,
        int ticketTypeId,
        int qty)
    {
        var ticketType = await db.TicketTypes
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.Id == ticketTypeId && t.EventId == eventId);

        if (ticketType == null)
        {
            TempData["Error"] = "Loại vé không hợp lệ.";
            return RedirectToPage(new { id = eventId });
        }

        qty = Math.Clamp(qty, 1, 10);

        if (ticketType.AvailableQuantity < qty)
        {
            TempData["Error"] = $"Chỉ còn {ticketType.AvailableQuantity} vé.";
            return RedirectToPage(new { id = eventId });
        }

        cartService.AddToCart(HttpContext.Session, new CartItem
        {
            EventId       = eventId,
            TicketTypeId  = ticketTypeId,
            Quantity      = qty,
            Price         = ticketType.Price,
            EventName     = ticketType.Event.Title,
            TicketTypeName = ticketType.Name,
            EventImageUrl = ticketType.Event.ImageUrl
        });

        TempData["CartSuccess"] = $"Đã thêm {qty} vé '{ticketType.Name}' vào giỏ hàng!";
        return RedirectToPage(new { id = eventId });
    }
}
