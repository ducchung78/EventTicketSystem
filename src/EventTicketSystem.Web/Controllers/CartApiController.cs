using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Controllers;

[ApiController]
[Route("api/cart")]
public class CartApiController(CartService cartService, AppDbContext db) : ControllerBase
{
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
    {
        if (dto.Qty < 1 || dto.Qty > 10)
            return BadRequest(new { success = false, message = "Số lượng không hợp lệ (1–10)." });

        var ticket = await db.TicketTypes
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.Id == dto.TicketTypeId && t.EventId == dto.EventId);

        if (ticket == null)
            return BadRequest(new { success = false, message = "Loại vé không tồn tại." });

        if (ticket.AvailableQuantity < dto.Qty)
            return BadRequest(new { success = false, message = $"Chỉ còn {ticket.AvailableQuantity} vé loại này." });

        cartService.AddToCart(HttpContext.Session, new CartItem
        {
            EventId        = dto.EventId,
            TicketTypeId   = dto.TicketTypeId,
            Quantity       = dto.Qty,
            Price          = ticket.Price,
            EventName      = ticket.Event.Title,
            TicketTypeName = ticket.Name,
            EventImageUrl  = ticket.Event.ImageUrl
        });

        var count = cartService.GetCartCount(HttpContext.Session);
        return Ok(new
        {
            success = true,
            count,
            message = $"Đã thêm {dto.Qty} × {ticket.Name} vào giỏ hàng!"
        });
    }

    [HttpGet("count")]
    public IActionResult Count() =>
        Ok(new { count = cartService.GetCartCount(HttpContext.Session) });
}

public record AddToCartDto(int EventId, int TicketTypeId, int Qty);
