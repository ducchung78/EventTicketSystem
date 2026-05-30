using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Services;

public class RefundService(AppDbContext db, EmailService emailService, ILogger<RefundService> logger)
{
    // Returns the created RefundRequest, or null + sets errorMessage on failure
    public async Task<(RefundRequest? request, string? errorMessage)> SubmitAsync(
        string confirmationCode,
        string email,
        RefundReason reason,
        string? description,
        string? userId)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems).ThenInclude(i => i.TicketType).ThenInclude(t => t!.Event)
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

        ApplyAIDecision(refund, order);

        db.RefundRequests.Add(refund);

        if (refund.Status == RefundStatus.AutoApproved)
        {
            order.Status = OrderStatus.Refunded;
            refund.ProcessedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        await SendEmailAsync(refund, order);

        return (refund, null);
    }

    private static void ApplyAIDecision(RefundRequest refund, Order order)
    {
        var minutesSincePurchase = (DateTime.UtcNow - order.OrderDate).TotalMinutes;

        if (minutesSincePurchase <= 60)
        {
            refund.Status = RefundStatus.AutoApproved;
            refund.AutoProcessed = true;
            refund.AIDecisionReason = refund.Reason == RefundReason.WrongTicket
                ? $"Mua nhầm vé và yêu cầu trong vòng 1 giờ kể từ khi mua ({minutesSincePurchase:F0} phút) – duyệt tự động theo chính sách."
                : $"Yêu cầu hoàn vé trong vòng 1 giờ kể từ khi mua ({minutesSincePurchase:F0} phút) – duyệt tự động theo chính sách hoàn 100%.";
        }
        else
        {
            refund.Status = RefundStatus.Pending;
        }
    }

    public async Task<bool> ApproveAsync(int refundId, string adminId, string? note)
    {
        var refund = await db.RefundRequests.Include(r => r.Order).ThenInclude(o => o.OrderItems)
            .ThenInclude(i => i.TicketType).ThenInclude(t => t!.Event)
            .FirstOrDefaultAsync(r => r.Id == refundId);

        if (refund == null || refund.Status != RefundStatus.Pending) return false;

        refund.Status      = RefundStatus.Approved;
        refund.ProcessedAt = DateTime.UtcNow;
        refund.ProcessedBy = adminId;
        refund.AdminNote   = note;

        refund.Order.Status = OrderStatus.Refunded;

        await db.SaveChangesAsync();
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

    private async Task SendEmailAsync(RefundRequest refund, Order order)
    {
        try
        {
            await emailService.SendRefundNotificationAsync(refund, order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send refund notification email for RefundRequest {Id}.", refund.Id);
        }
    }
}
