using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Cart;

public class CartIndexModel(
    CartService cartService,
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    public List<CartItem> CartItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public ApplicationUser? CurrentUser { get; set; }

    public async Task OnGetAsync()
    {
        CartItems = cartService.GetCart(HttpContext.Session);
        Subtotal  = CartItems.Sum(c => c.Subtotal);
        Total     = Subtotal;

        if (signInManager.IsSignedIn(User))
            CurrentUser = await userManager.GetUserAsync(User);
    }

    public async Task<IActionResult> OnPostUpdateQtyAsync(int ticketTypeId, int qty)
    {
        if (qty < 1)
        {
            await ReleaseSeatsForItemAsync(ticketTypeId);
            cartService.RemoveFromCart(HttpContext.Session, ticketTypeId);
        }
        else
            cartService.UpdateQuantity(HttpContext.Session, ticketTypeId, qty);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int ticketTypeId)
    {
        await ReleaseSeatsForItemAsync(ticketTypeId);
        cartService.RemoveFromCart(HttpContext.Session, ticketTypeId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearAsync()
    {
        await ReleaseAllSeatsForSessionAsync();
        cartService.ClearCart(HttpContext.Session);
        return RedirectToPage();
    }

    // Giải phóng ghế Reserved của 1 item khi user xóa khỏi giỏ
    private async Task ReleaseSeatsForItemAsync(int ticketTypeId)
    {
        var sessionId = HttpContext.Session.Id;
        var cart = cartService.GetCart(HttpContext.Session);
        var item = cart.FirstOrDefault(c => c.TicketTypeId == ticketTypeId);
        if (item == null || !item.HasSeats) return;

        var seats = await db.Seats
            .Where(s => item.SeatIds.Contains(s.Id) &&
                        s.Status == SeatStatus.Reserved &&
                        s.ReservedBySessionId == sessionId)
            .ToListAsync();

        foreach (var seat in seats)
        {
            seat.Status              = SeatStatus.Available;
            seat.ReservedUntil       = null;
            seat.ReservedBySessionId = null;
        }
        if (seats.Count > 0) await db.SaveChangesAsync();
    }

    // Giải phóng tất cả ghế Reserved của session khi xóa toàn bộ giỏ
    private async Task ReleaseAllSeatsForSessionAsync()
    {
        var sessionId = HttpContext.Session.Id;
        var cart = cartService.GetCart(HttpContext.Session);
        var seatIds = cart.Where(c => c.HasSeats).SelectMany(c => c.SeatIds).Distinct().ToList();
        if (seatIds.Count == 0) return;

        var seats = await db.Seats
            .Where(s => seatIds.Contains(s.Id) &&
                        s.Status == SeatStatus.Reserved &&
                        s.ReservedBySessionId == sessionId)
            .ToListAsync();

        foreach (var seat in seats)
        {
            seat.Status              = SeatStatus.Available;
            seat.ReservedUntil       = null;
            seat.ReservedBySessionId = null;
        }
        if (seats.Count > 0) await db.SaveChangesAsync();
    }

    public async Task<IActionResult> OnPostCheckoutAsync(
        string customerName,
        string customerEmail,
        string? customerPhone,
        string? couponCode)
    {
        if (!signInManager.IsSignedIn(User))
            return Redirect("/Account/Login?ReturnUrl=/Cart&reason=checkout");

        var cart = cartService.GetCart(HttpContext.Session);
        if (cart.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerEmail))
        {
            TempData["Error"] = "Vui lòng điền họ tên và email.";
            return RedirectToPage();
        }

        // Validate coupon (optional)
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

        var appUser       = await userManager.GetUserAsync(User);
        var currentSession = HttpContext.Session.Id;
        decimal discountRatio = subtotal > 0 ? discountAmount / subtotal : 0;
        var orderIds = new List<int>();

        // Bọc toàn bộ checkout trong transaction để tránh race condition
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
                    Status            = OrderStatus.Pending,
                    ApplicationUserId = appUser?.Id,
                    OriginalAmount    = itemTotal,
                    DiscountAmount    = itemDiscount,
                    TotalAmount       = itemTotal - itemDiscount,
                    CouponId          = coupon?.Id
                };

                if (item.HasSeats)
                {
                    // Load lại ghế trong transaction để đảm bảo dữ liệu mới nhất
                    var seats = await db.Seats
                        .Where(s => item.SeatIds.Contains(s.Id) && s.EventId == item.EventId)
                        .ToListAsync();

                    var now = DateTime.UtcNow;

                    // Ghế bị chiếm khi:
                    //  - Đã Sold hoặc Disabled
                    //  - Reserved nhưng hết hạn
                    //  - Reserved bởi session khác (người khác đang giữ)
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
                        // Reserved → Sold: xóa toàn bộ thông tin giữ ghế
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

        // Store order IDs in TempData for confirmation page
        TempData["CartOrderIds"] = string.Join(",", orderIds);
        return RedirectToPage("/Cart/Confirmation");
    }
}
