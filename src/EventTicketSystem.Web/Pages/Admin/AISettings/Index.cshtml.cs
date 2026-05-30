using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Pages.Admin.AISettings;

[Authorize(Roles = "SuperAdmin")]
public class AISettingsModel(AppDbContext db, TicketPredictionService predictionService) : PageModel
{
    public AIConfig     Config         { get; set; } = new();
    public int          RealSamples    { get; set; }
    public int          TotalSamples   { get; set; }

    [BindProperty] public ConfigInputModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var cfg = await db.AIConfigs.FindAsync(1);
        if (cfg == null) { cfg = new AIConfig { Id = 1 }; db.AIConfigs.Add(cfg); }

        cfg.AutoRefundThresholdMinutes = Math.Clamp(Input.AutoRefundThresholdMinutes, 5, 1440);
        cfg.MinTrainingSamples         = Math.Clamp(Input.MinTrainingSamples, 10, 2000);
        cfg.Iterations                 = Math.Clamp(Input.Iterations, 10, 1000);
        cfg.L1Regularization           = Math.Clamp(Input.L1Regularization, 0f, 1f);
        cfg.L2Regularization           = Math.Clamp(Input.L2Regularization, 0f, 1f);

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu cấu hình AI.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Config = await db.AIConfigs.FindAsync(1) ?? new AIConfig { Id = 1 };

        await predictionService.EnsureTrainedAsync();
        TotalSamples = predictionService.TrainingSampleCount;

        RealSamples = await db.TicketTypes
            .CountAsync(tt => tt.SoldQuantity > 0 && tt.TotalQuantity > 0);

        Input = new ConfigInputModel
        {
            AutoRefundThresholdMinutes = Config.AutoRefundThresholdMinutes,
            MinTrainingSamples         = Config.MinTrainingSamples,
            Iterations                 = Config.Iterations,
            L1Regularization           = Config.L1Regularization,
            L2Regularization           = Config.L2Regularization,
        };
    }

    public class ConfigInputModel
    {
        public int   AutoRefundThresholdMinutes { get; set; } = 60;
        public int   MinTrainingSamples         { get; set; } = 60;
        public int   Iterations                 { get; set; } = 100;
        public float L1Regularization           { get; set; } = 0f;
        public float L2Regularization           { get; set; } = 0.1f;
    }
}
