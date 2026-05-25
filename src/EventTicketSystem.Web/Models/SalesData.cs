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
