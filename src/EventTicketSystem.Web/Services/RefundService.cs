using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Services;

public class RefundService(AppDbContext db, EmailService emailService, ILogger<RefundService> logger)
{
    public async Task<(RefundRequest? request, string? errorMessage)> SubmitAsync(
        string confirmationCode,
        string email,
        RefundReason reason,
        string? description,
        string? userId)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems).ThenInclude(i => i.TicketType).ThenInclude(t => t!.Event)
            .Include(o => o.OrderItems).ThenInclude(i => i.Seat)
            .FirstOrDefaultAsync(o => o.ConfirmationCode == confirmationCode.Trim().ToUpper());

        if (order == null)
            return (null, "Mã đơn hàng không tồn tại.");

        if (!string.Equals(order.CustomerEmail, email.Trim(), StringComparison.OrdinalIgnoreCase))
            return (null, "Email không khớp với đơn hàng này.");

        if (order.Status == OrderStatus.Cancelled)
            return (null, "Đơn hàng này đã bị huỷ, không thể yêu cầu hoàn vé.");

        if (order.Status == OrderStatus.Refunded)
            return (null, "Đơn hàng này đã được hoàn vé.");

        var existingPending = await db.RefundRequests
            .AnyAsync(r => r.OrderId == order.Id && (r.Status == RefundStatus.Pending || r.Status == RefundStatus.AutoApproved));
        if (existingPending)
            return (null, "Đã có yêu cầu hoàn vé đang chờ xử lý cho đơn hàng này.");

        var refund = new RefundRequest
        {
            OrderId     = order.Id,
            UserId      = userId,
            Email       = email.Trim(),
            Reason      = reason,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt   = DateTime.UtcNow
        };

        await ApplyAIDecisionAsync(refund, order);

        db.RefundRequests.Add(refund);

        if (refund.Status == RefundStatus.AutoApproved)
        {
            order.Status       = OrderStatus.Refunded;
            refund.ProcessedAt = DateTime.UtcNow;
            ReleaseSeatsAndQty(order);
        }

        await db.SaveChangesAsync();
        await SendEmailAsync(refund, order);

        return (refund, null);
    }

    public async Task<bool> ApproveAsync(int refundId, string adminId, string? note)
    {
        var refund = await db.RefundRequests
            .Include(r => r.Order).ThenInclude(o => o.OrderItems).ThenInclude(i => i.TicketType).ThenInclude(t => t!.Event)
            .Include(r => r.Order).ThenInclude(o => o.OrderItems).ThenInclude(i => i.Seat)
            .FirstOrDefaultAsync(r => r.Id == refundId);

        if (refund == null || refund.Status != RefundStatus.Pending) return false;

        await using var tx = await db.Database.BeginTransactionAsync();
        refund.Status      = RefundStatus.Approved;
        refund.ProcessedAt = DateTime.UtcNow;
        refund.ProcessedBy = adminId;
        refund.AdminNote   = note;
        refund.Order.Status = OrderStatus.Refunded;
        ReleaseSeatsAndQty(refund.Order);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await SendEmailAsync(refund, refund.Order);
        return true;
    }

    public async Task<bool> RejectAsync(int refundId, string adminId, string? note)
    {
        var refund = await db.RefundRequests.Include(r => r.Order).ThenInclude(o => o.OrderItems)
            .ThenInclude(i => i.TicketType).ThenInclude(t => t!.Event)
            .FirstOrDefaultAsync(r => r.Id == refundId);

        if (refund == null || refund.Status != RefundStatus.Pending) return false;

        refund.Status      = RefundStatus.Rejected;
        refund.ProcessedAt = DateTime.UtcNow;
        refund.ProcessedBy = adminId;
        refund.AdminNote   = note;

        await db.SaveChangesAsync();
        await SendEmailAsync(refund, refund.Order);
        return true;
    }

    // ── AI decision — reads threshold from AIConfig ───────────────────────────
    private async Task ApplyAIDecisionAsync(RefundRequest refund, Order order)
    {
        var config    = await db.AIConfigs.FindAsync(1) ?? new AIConfig();
        var threshold = config.AutoRefundThresholdMinutes;
        var minutes   = (DateTime.UtcNow - order.OrderDate).TotalMinutes;

        if (minutes <= threshold)
        {
            refund.Status        = RefundStatus.AutoApproved;
            refund.AutoProcessed = true;
            refund.AIDecisionReason = refund.Reason == RefundReason.WrongTicket
                ? $"Mua nhầm vé và yêu cầu trong vòng {threshold} phút kể từ khi mua ({minutes:F0} phút) – duyệt tự động."
                : $"Yêu cầu hoàn vé trong vòng {threshold} phút kể từ khi mua ({minutes:F0} phút) – duyệt tự động theo chính sách hoàn 100%.";
        }
        else
        {
            refund.Status = RefundStatus.Pending;
        }
    }

    private static void ReleaseSeatsAndQty(Order order)
    {
        foreach (var item in order.OrderItems)
        {
            item.TicketType.SoldQuantity -= item.Quantity;
            if (item.Seat != null)
            {
                item.Seat.Status = SeatStatus.Available;
                item.Seat.ReservedBySessionId = null;
                item.Seat.ReservedUntil = null;
            }
        }
    }

    private async Task SendEmailAsync(RefundRequest refund, Order order)
    {
        try { await emailService.SendRefundNotificationAsync(refund, order); }
        catch (Exception ex) { logger.LogError(ex, "Failed to send refund email for RefundRequest {Id}.", refund.Id); }
    }
}
