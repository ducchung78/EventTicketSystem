namespace EventTicketSystem.Web.Models;

public class AIConfig
{
    public int Id { get; set; } = 1;

    // ── Refund policy ─────────────────────────────────────────────────────────
    public int AutoRefundThresholdMinutes { get; set; } = 60;

    // ── ML.NET training parameters ────────────────────────────────────────────
    public int   MinTrainingSamples { get; set; } = 60;
    public int   Iterations         { get; set; } = 100;
    public float L1Regularization   { get; set; } = 0f;
    public float L2Regularization   { get; set; } = 0.1f;

    // ── Model performance (updated after each training run) ───────────────────
    public DateTime? LastTrainedAt { get; set; }
    public double?   LastModelMAE  { get; set; }
    public double?   LastModelRMSE { get; set; }
    public double?   LastModelR2   { get; set; }
}
