namespace EventTicketSystem.Web.Models;

public static class OrderStatusExtensions
{
    public static string ToVietnamese(this OrderStatus status) => status switch
    {
        OrderStatus.Confirmed => "Đã Xác Nhận",
        OrderStatus.Cancelled => "Đã Hủy",
        OrderStatus.Pending   => "Chờ Xử Lý",
        _                     => status.ToString()
    };

    public static string ToBadgeClass(this OrderStatus status) => status switch
    {
        OrderStatus.Confirmed => "bg-success",
        OrderStatus.Cancelled => "bg-danger",
        _                     => "bg-warning text-dark"
    };
}
