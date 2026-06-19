using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Controllers;

[ApiController]
[Route("api/events")]
public class EventsApiController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? category)
    {
        var query = db.Events.Include(e => e.TicketTypes).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Title.Contains(search) || e.Venue.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        var events = await query
            .Where(e => e.IsActive)
            .OrderBy(e => e.StartDate)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.StartDate,
                e.EndDate,
                e.Venue,
                e.Category,
                e.ImageUrl,
                TicketTypes = e.TicketTypes.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Price,
                    t.AvailableQuantity
                })
            })
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var evt = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null) return NotFound();

        return Ok(new
        {
            evt.Id,
            evt.Title,
            evt.Description,
            evt.StartDate,
            evt.EndDate,
            evt.Venue,
            evt.Category,
            evt.IsActive,
            evt.HasSeatMap,
            TicketTypes = evt.TicketTypes.Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.Price,
                t.TotalQuantity,
                t.SoldQuantity,
                t.AvailableQuantity
            })
        });
    }

    [HttpGet("{id:int}/seats")]
    public async Task<IActionResult> GetSeats(int id)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt == null) return NotFound();
        if (!evt.HasSeatMap) return Ok(new { eventId = id, hasMap = false, zones = Array.Empty<object>() });

        var seats = await db.Seats
            .Include(s => s.TicketType)
            .Where(s => s.EventId == id)
            .OrderBy(s => s.Zone).ThenBy(s => s.RowLabel).ThenBy(s => s.SeatNumber)
            .ToListAsync();

        var zones = seats
            .GroupBy(s => s.Zone)
            .Select(zg => new
            {
                zone          = zg.Key,
                ticketTypeId  = zg.First().TicketTypeId,
                ticketTypeName= zg.First().TicketType?.Name,
                price         = zg.First().TicketType?.Price ?? 0,
                rows          = zg.GroupBy(s => s.RowLabel)
                    .Select(rg => new
                    {
                        row   = rg.Key,
                        seats = rg.Select(s => new
                        {
                            s.Id,
                            s.SeatNumber,
                            label  = s.Label,
                            status = s.Status.ToString()
                        })
                    })
            });

        return Ok(new { eventId = id, hasMap = true, zones });
    }
}
