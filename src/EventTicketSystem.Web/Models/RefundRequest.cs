using System.ComponentModel.DataAnnotations;

namespace EventTicketSystem.Web.Models;

public enum RefundStatus
{
    Pending,
    AutoApproved,
    Approved,
    Rejected
}

public enum RefundReason
{
    WrongTicket,
    ScheduleConflict,
    EventChanged,
    HealthReason,
    PersonalReason,
    Other
}

public class RefundRequest
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public RefundReason Reason { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    public bool AutoProcessed { get; set; }

    [MaxLength(1000)]
    public string? AIDecisionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    [MaxLength(450)]
    public string? ProcessedBy { get; set; }

    // Admin rejection reason
    [MaxLength(1000)]
    public string? AdminNote { get; set; }
}
