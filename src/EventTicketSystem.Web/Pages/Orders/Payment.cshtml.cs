using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Orders;

[Authorize]
public class PaymentModel(
    AppDbContext db,
    CartService cartService,
    UserManager<ApplicationUser> userManager,
    EmailService emailService) : PageModel
{
    public List<CartItem> CartItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public ApplicationUser? CurrentUser { get; set; }
    public List<PaymentMethod> PaymentMethods { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        CartItems = cartService.GetCart(HttpContext.Session);
        if (!CartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToPage("/Cart/Index");
        }

        Subtotal = CartItems.Sum(c => c.Subtotal);
        Total = Subtotal;
        CurrentUser = await userManager.GetUserAsync(User);
        PaymentMethods = await db.PaymentMethods
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string? couponCode,
        int? paymentMethodId)
    {
        var cart = cartService.GetCart(HttpContext.Session);
        if (!cart.Any())
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToPage("/Cart/Index");
        }

        var appUser = await userManager.GetUserAsync(User);
        var customerName  = appUser?.HoTen is { Length: > 0 } n ? n : (appUser?.UserName ?? "");
        var customerEmail = appUser?.Email ?? "";
        var customerPhone = appUser?.PhoneNumber;

        Coupon? coupon = null;
        decimal subtotal = cart.Sum(c => c.Subtotal);
        decimal discountAmount = 0;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode.ToUpper().Trim());
            if (coupon != null && coupon.IsValid(subtotal, out _))
            {
                discountAmount = coupon.CalculateDiscount(subtotal);
                coupon.UsedCount++;
            }
        }

        var currentSession = HttpContext.Session.Id;
        decimal discountRatio = subtotal > 0 ? discountAmount / subtotal : 0;
        var orderIds = new List<int>();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in cart)
            {
                var ticketType = await db.TicketTypes
                    .Include(t => t.Event)
                    .FirstOrDefaultAsync(t => t.Id == item.TicketTypeId);

                if (ticketType == null)
                {
                    TempData["Error"] = $"Vé '{item.TicketTypeName}' không hợp lệ.";
                    await transaction.RollbackAsync();
                    return RedirectToPage();
                }

                var itemTotal    = item.Price * item.Quantity;
                var itemDiscount = Math.Round(itemTotal * discountRatio, 0);

                var order = new Order
                {
                    CustomerName      = customerName,
                    CustomerEmail     = customerEmail,
                    CustomerPhone     = customerPhone,
                    Status            = OrderStatus.Confirmed,
                    ApplicationUserId = appUser?.Id,
                    OriginalAmount    = itemTotal,
                    DiscountAmount    = itemDiscount,
                    TotalAmount       = itemTotal - itemDiscount,
                    CouponId          = coupon?.Id
                };

                if (item.HasSeats)
                {
                    var seats = await db.Seats
                        .Where(s => item.SeatIds.Contains(s.Id) && s.EventId == item.EventId)
                        .ToListAsync();

                    var now = DateTime.UtcNow;
                    var takenSeats = seats.Where(s =>
                        s.Status == SeatStatus.Sold ||
                        s.Status == SeatStatus.Disabled ||
                        (s.Status == SeatStatus.Reserved && (
                            (s.ReservedUntil.HasValue && s.ReservedUntil.Value <= now) ||
                            s.ReservedBySessionId != currentSession
                        ))
                    ).ToList();

                    if (seats.Count != item.SeatIds.Count || takenSeats.Any())
                    {
                        var msg = takenSeats.Any()
                            ? $"Ghế {string.Join(", ", takenSeats.Select(s => s.Label))} đã được đặt bởi người khác. Vui lòng chọn lại."
                            : "Một hoặc nhiều ghế không còn khả dụng. Vui lòng chọn lại.";
                        TempData["Error"] = msg;
                        await transaction.RollbackAsync();
                        return RedirectToPage();
                    }

                    foreach (var seat in seats)
                    {
                        order.OrderItems.Add(new OrderItem
                        {
                            TicketTypeId = ticketType.Id,
                            Quantity     = 1,
                            UnitPrice    = item.Price,
                            SeatId       = seat.Id
                        });
                        seat.Status              = SeatStatus.Sold;
                        seat.ReservedUntil       = null;
                        seat.ReservedBySessionId = null;
                    }
                    ticketType.SoldQuantity += seats.Count;
                }
                else
                {
                    if (ticketType.AvailableQuantity < item.Quantity)
                    {
                        TempData["Error"] = $"Vé '{item.TicketTypeName}' không còn đủ số lượng.";
                        await transaction.RollbackAsync();
                        return RedirectToPage();
                    }

                    order.OrderItems.Add(new OrderItem
                    {
                        TicketTypeId = ticketType.Id,
                        Quantity     = item.Quantity,
                        UnitPrice    = item.Price
                    });
                    ticketType.SoldQuantity += item.Quantity;
                }

                db.Orders.Add(order);
                await db.SaveChangesAsync();
                orderIds.Add(order.Id);
            }

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán. Vui lòng thử lại.";
            return RedirectToPage();
        }

        cartService.ClearCart(HttpContext.Session);

        // Send confirmation email (non-fatal)
        var firstOrder = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
            .FirstOrDefaultAsync(o => o.Id == orderIds[0]);
        if (firstOrder != null)
            await emailService.SendOrderConfirmationAsync(firstOrder);

        return RedirectToPage("/Orders/Confirmation", new { id = orderIds[0] });
    }
}
