using EventTicketSystem.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersApiController(AppDbContext db) : ControllerBase
{
    [HttpGet("{confirmationCode}")]
    public async Task<IActionResult> GetByCode(string confirmationCode)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.TicketType)
                    .ThenInclude(tt => tt.Event)
            .FirstOrDefaultAsync(o => o.ConfirmationCode == confirmationCode.ToUpper());

        if (order == null) return NotFound();

        return Ok(new
        {
            order.Id,
            order.ConfirmationCode,
            order.CustomerName,
            order.CustomerEmail,
            order.OrderDate,
            Status = order.Status.ToString(),
            order.TotalAmount,
            Items = order.OrderItems.Select(i => new
            {
                Event = i.TicketType.Event.Title,
                TicketType = i.TicketType.Name,
                i.Quantity,
                i.UnitPrice,
                i.Subtotal
            })
        });
    }
}
