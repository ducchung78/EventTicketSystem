using Microsoft.ML.Data;

namespace EventTicketSystem.Web.Models;

public class SalesData
{
    public float ThangSuKien { get; set; }
    public float NgayTrongTuan { get; set; }
    public float SoNgayCon { get; set; }
    public float GiaVe { get; set; }
    public float TongSoChoNgoi { get; set; }
    public float DanhMucId { get; set; }

    [ColumnName("Label")]
    public float SoVeBan { get; set; }
}

public class SalesPrediction
{
    [ColumnName("Score")]
    public float SoVeDuBao { get; set; }
}

public class PricingSuggestion
{
    public int     EventId           { get; set; }
    public int     TicketTypeId      { get; set; }
    public string  TicketTypeName    { get; set; } = string.Empty;
    public decimal CurrentPrice      { get; set; }
    public decimal SuggestedPrice    { get; set; }
    public float   ChangePercent     { get; set; }
    public string  Reason            { get; set; } = string.Empty;
    public float   Confidence        { get; set; }
    public float   PredictedFillRate { get; set; }
    public int     PredictedSold     { get; set; }
}
