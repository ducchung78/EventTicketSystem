using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventTicketSystem.Web.Pages.Events.SeatMap;

[Authorize(Roles = "Admin,SuperAdmin")]
public class SeatMapIndexModel(AppDbContext db) : PageModel
{
    public Event?   Event        { get; set; }
    public List<Seat> Seats      { get; set; } = [];
    public bool     HasSoldSeats { get; set; }

    // Grid dimensions (State B)
    public int GridRows { get; private set; }
    public int GridCols { get; private set; }
    public Dictionary<(int row, int col), Seat> SeatGrid { get; private set; } = [];

    // ── GET ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        Event = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (Event == null) return NotFound();
        await LoadSeatsAsync(eventId);
        return Page();
    }

    // ── Save grid (new visual editor) ────────────────────────────────────────
    public async Task<IActionResult> OnPostSaveGridAsync(int eventId, string gridJson)
    {
        var evt = await db.Events.Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (evt == null) return NotFound();

        if (await db.Seats.AnyAsync(s => s.EventId == eventId && s.Status == SeatStatus.Sold))
        {
            TempData["Error"] = "Không thể tạo lại sơ đồ khi đã có ghế được bán.";
            return RedirectToPage(new { eventId });
        }

        GridSaveDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GridSaveDto>(gridJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { payload = null; }

        if (payload == null || payload.Cells.Count == 0)
        {
            TempData["Error"] = "Dữ liệu sơ đồ không hợp lệ.";
            return RedirectToPage(new { eventId });
        }

        // Remove existing non-sold seats
        var old = await db.Seats
            .Where(s => s.EventId == eventId && s.Status != SeatStatus.Sold)
            .ToListAsync();
        db.Seats.RemoveRange(old);

        const string RowAlpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        int? normalTtId = payload.NormalTicketTypeId > 0 ? payload.NormalTicketTypeId : null;
        int? vipTtId    = payload.VipTicketTypeId    > 0 ? payload.VipTicketTypeId    : null;
        var now = DateTime.UtcNow;
        var newSeats = new List<Seat>();

        foreach (var cell in payload.Cells)
        {
            int r = cell.Row, c = cell.Col;
            if (r < 0 || r > 25 || c < 0) continue;

            var seatType = cell.Type.ToLower() switch
            {
                "vip"    => SeatType.VIP,
                "couple" => SeatType.Couple,
                _        => SeatType.Normal
            };

            int? ttId = seatType == SeatType.VIP ? vipTtId : normalTtId;

            newSeats.Add(new Seat
            {
                EventId      = eventId,
                TicketTypeId = ttId,
                Zone         = "MAIN",
                RowLabel     = RowAlpha[r].ToString(),
                SeatNumber   = c + 1,
                SeatType     = seatType,
                GridRow      = r,
                GridCol      = c,
                Status       = SeatStatus.Available,
                CreatedAt    = now
            });
        }

        db.Seats.AddRange(newSeats);
        evt.HasSeatMap = true;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã lưu sơ đồ với {newSeats.Count} ghế.";
        return RedirectToPage(new { eventId });
    }

    // ── Toggle single seat Available ↔ Disabled (AJAX) ───────────────────────
    public async Task<IActionResult> OnPostToggleSeatAsync(int eventId, int seatId)
    {
        var seat = await db.Seats.FirstOrDefaultAsync(s => s.Id == seatId && s.EventId == eventId);
        if (seat == null) return NotFound();
        if (seat.Status == SeatStatus.Sold || seat.Status == SeatStatus.Reserved)
            return new JsonResult(new { error = "Không thể thay đổi ghế này." }) { StatusCode = 400 };

        seat.Status = seat.Status == SeatStatus.Disabled
            ? SeatStatus.Available
            : SeatStatus.Disabled;
        await db.SaveChangesAsync();

        return new JsonResult(new { status = seat.Status.ToString() });
    }

    // ── Reset all seats (only if no Sold) ────────────────────────────────────
    public async Task<IActionResult> OnPostResetAsync(int eventId)
    {
        if (await db.Seats.AnyAsync(s => s.EventId == eventId && s.Status == SeatStatus.Sold))
        {
            TempData["Error"] = "Không thể xóa sơ đồ khi đã có ghế đã bán.";
            return RedirectToPage(new { eventId });
        }

        db.Seats.RemoveRange(await db.Seats.Where(s => s.EventId == eventId).ToListAsync());

        var evt = await db.Events.FindAsync(eventId);
        if (evt != null) evt.HasSeatMap = false;

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa toàn bộ sơ đồ ghế.";
        return RedirectToPage(new { eventId });
    }

    // ── Load seats + build grid ───────────────────────────────────────────────
    private async Task LoadSeatsAsync(int eventId)
    {
        Seats = await db.Seats
            .Include(s => s.TicketType)
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.GridRow).ThenBy(s => s.GridCol)
            .ToListAsync();

        HasSoldSeats = Seats.Any(s => s.Status == SeatStatus.Sold);

        if (Seats.Count > 0)
        {
            GridRows = Seats.Max(s => s.GridRow) + 1;
            GridCols = Seats.Max(s => s.GridCol + (s.SeatType == SeatType.Couple ? 2 : 1));
            SeatGrid = Seats.ToDictionary(s => (s.GridRow, s.GridCol));
        }
    }

    public record GridSaveDto(int EventId, int Rows, int Cols,
        int NormalTicketTypeId, int VipTicketTypeId,
        List<GridCellDto> Cells);

    public record GridCellDto(int Row, int Col, string Type);
}
