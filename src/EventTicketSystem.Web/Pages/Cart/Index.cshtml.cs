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

    public IActionResult OnPostUpdateQty(int ticketTypeId, int qty)
    {
        if (qty < 1)
            cartService.RemoveFromCart(HttpContext.Session, ticketTypeId);
        else
            cartService.UpdateQuantity(HttpContext.Session, ticketTypeId, qty);
        return RedirectToPage();
    }

    public IActionResult OnPostRemove(int ticketTypeId)
    {
        cartService.RemoveFromCart(HttpContext.Session, ticketTypeId);
        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        cartService.ClearCart(HttpContext.Session);
        return RedirectToPage();
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

        var appUser = signInManager.IsSignedIn(User) ? await userManager.GetUserAsync(User) : null;

        // Per-item discount ratio
        decimal discountRatio = subtotal > 0 ? discountAmount / subtotal : 0;

        var orderIds = new List<int>();

        foreach (var item in cart)
        {
            var ticketType = await db.TicketTypes
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Id == item.TicketTypeId);

            if (ticketType == null || ticketType.AvailableQuantity < item.Quantity)
            {
                TempData["Error"] = $"Vé '{item.TicketTypeName}' không còn đủ số lượng.";
                return RedirectToPage();
            }

            var itemTotal = item.Price * item.Quantity;
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

            order.OrderItems.Add(new OrderItem
            {
                TicketTypeId = ticketType.Id,
                Quantity     = item.Quantity,
                UnitPrice    = item.Price
            });

            ticketType.SoldQuantity += item.Quantity;
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderIds.Add(order.Id);
        }

        cartService.ClearCart(HttpContext.Session);

        // Store order IDs in TempData for confirmation page
        TempData["CartOrderIds"] = string.Join(",", orderIds);
        return RedirectToPage("/Cart/Confirmation");
    }
}
