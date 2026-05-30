namespace EventTicketSystem.Web.Models;

public class PredictionLog
{
    public int     Id                   { get; set; }

    public int     EventId              { get; set; }
    public Event   Event                { get; set; } = null!;

    public DateTime  PredictedAt         { get; set; } = DateTime.UtcNow;
    public float     PredictedTicketsSold { get; set; }
    public decimal   PredictedRevenue     { get; set; }

    // Filled in after the event ends
    public int?      ActualTicketsSold    { get; set; }
    public decimal?  ActualRevenue        { get; set; }
    public DateTime? UpdatedAt            { get; set; }
}
