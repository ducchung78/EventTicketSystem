using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.AI;

[Authorize(Roles = "Admin")]
public class AIPredictionModel(AppDbContext db, TicketPredictionService predictionService) : PageModel
{
    public bool                     IsTrained    { get; set; }
    public double                   R2           { get; set; }
    public double                   Mae          { get; set; }
    public double                   Rmse         { get; set; }
    public List<EventPrediction>    Predictions  { get; set; } = [];

    public async Task OnGetAsync()
    {
        await predictionService.EnsureTrainedAsync();
        IsTrained = predictionService.IsTrained;

        if (predictionService.ModelMetrics is { } m)
        {
            R2   = m.RSquared;
            Mae  = m.MeanAbsoluteError;
            Rmse = m.RootMeanSquaredError;
        }

        var events = await db.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.IsActive)
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        foreach (var evt in events)
        {
            var ticketPreds = evt.TicketTypes.Select(tt => new TicketPrediction
            {
                TicketName    = tt.Name,
                Price         = tt.Price,
                TotalQuantity = tt.TotalQuantity,
                SoldQuantity  = tt.SoldQuantity,
                Predicted     = (int)predictionService.PredictForEvent(evt, tt)
            }).ToList();

            Predictions.Add(new EventPrediction
            {
                EventTitle = evt.Title,
                StartDate  = evt.StartDate,
                Category   = evt.Category,
                Tickets    = ticketPreds
            });
        }
    }

    public class EventPrediction
    {
        public string               EventTitle { get; set; } = string.Empty;
        public DateTime             StartDate  { get; set; }
        public string               Category   { get; set; } = string.Empty;
        public List<TicketPrediction> Tickets  { get; set; } = [];
    }

    public class TicketPrediction
    {
        public string  TicketName    { get; set; } = string.Empty;
        public decimal Price         { get; set; }
        public int     TotalQuantity { get; set; }
        public int     SoldQuantity  { get; set; }
        public int     Predicted     { get; set; }
    }
}
