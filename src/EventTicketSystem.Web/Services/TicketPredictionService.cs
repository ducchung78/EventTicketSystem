using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace EventTicketSystem.Web.Services;

public class TicketPredictionService(IServiceScopeFactory scopeFactory)
{
    private readonly MLContext _mlContext = new(seed: 42);
    private ITransformer? _model;
    private PredictionEngine<SalesData, SalesPrediction>? _predictionEngine;
    private RegressionMetrics? _metrics;
    private readonly object _lock = new();
    private volatile bool _trained;

    public RegressionMetrics? ModelMetrics    => _metrics;
    public bool               IsTrained       => _trained;
    public int                TrainingSampleCount { get; private set; }

    // Maps category strings (Vietnamese or English, any case) → float ID
    private static readonly Dictionary<string, float> CategoryMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Âm Nhạc"]    = 1f, ["Music"]       = 1f,
            ["Công Nghệ"]  = 2f, ["Technology"]  = 2f,
            ["Hài Kịch"]   = 3f, ["Comedy"]      = 3f,
            ["Nghệ Thuật"] = 4f, ["Arts"]        = 4f,
            ["Thể Thao"]   = 5f, ["Sports"]      = 5f,
            ["Hội Thảo"]   = 6f, ["Seminar"]     = 6f,
            ["Tham Quan"]  = 7f, ["Tour"]        = 7f,
        };

    public static float MapCategory(string category) =>
        CategoryMap.TryGetValue(category.Trim(), out var v) ? v : 0f;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Primary training entry — must be awaited before calling Predict.</summary>
    public async Task EnsureTrainedAsync()
    {
        if (_trained) return;

        var allData = await LoadDataAsync();

        lock (_lock)
        {
            if (_trained) return;   // double-check after acquiring lock
            TrainingSampleCount = allData.Count;
            FitModel(allData);
            _trained = true;
        }
    }

    /// <summary>Sync guard used inside Predict(). Assumes EnsureTrainedAsync was already called.</summary>
    public void EnsureTrained()
    {
        // No-op when the model is already trained (the normal path after EnsureTrainedAsync).
        // If called cold (e.g. unit tests), fall back to synchronous execution.
        if (!_trained)
            EnsureTrainedAsync().GetAwaiter().GetResult();
    }

    public float Predict(SalesData input)
    {
        if (!_trained) return 0f;
        lock (_lock)
        {
            var score = _predictionEngine?.Predict(input).SoVeDuBao ?? 0f;
            return MathF.Max(0f, score);
        }
    }

    public float PredictForEvent(Event evt, TicketType tt)
    {
        var daysUntil = MathF.Max(0f, (float)(evt.StartDate - DateTime.UtcNow).TotalDays);
        var input = new SalesData
        {
            ThangSuKien   = evt.StartDate.Month,
            NgayTrongTuan = (float)evt.StartDate.DayOfWeek,
            SoNgayCon     = daysUntil,
            GiaVe         = (float)tt.Price,
            TongSoChoNgoi = tt.TotalQuantity,
            DanhMucId     = MapCategory(evt.Category)
        };
        return MathF.Min(Predict(input), tt.TotalQuantity);
    }

    // ── Dynamic Pricing ───────────────────────────────────────────────────────

    /// <summary>Sync variant — call after EnsureTrainedAsync, event already loaded.</summary>
    public List<PricingSuggestion> SuggestPricingForEvent(Event evt)
    {
        float confidence = TrainingSampleCount >= 60 ? 0.8f : 0.4f;
        return ComputeSuggestions(evt, confidence);
    }

    /// <summary>Async variant for API use — loads event from DB internally.</summary>
    public async Task<List<PricingSuggestion>> SuggestPricingAsync(int eventId)
    {
        await EnsureTrainedAsync();

        using var scope = scopeFactory.CreateScope();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var evt = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (evt == null) return [];

        float confidence = TrainingSampleCount >= 60 ? 0.8f : 0.4f;
        return ComputeSuggestions(evt, confidence);
    }

    private List<PricingSuggestion> ComputeSuggestions(Event evt, float confidence)
    {
        var suggestions = new List<PricingSuggestion>();

        foreach (var tt in evt.TicketTypes)
        {
            if (tt.TotalQuantity == 0) continue;

            // Free tickets: skip pricing logic
            if (tt.Price == 0)
            {
                suggestions.Add(new PricingSuggestion
                {
                    EventId           = evt.Id,
                    TicketTypeId      = tt.Id,
                    TicketTypeName    = tt.Name,
                    CurrentPrice      = 0,
                    SuggestedPrice    = 0,
                    ChangePercent     = 0,
                    Reason            = "Vé miễn phí – không áp dụng điều chỉnh giá.",
                    Confidence        = confidence,
                    PredictedFillRate = 0,
                    PredictedSold     = 0
                });
                continue;
            }

            var predicted = PredictForEvent(evt, tt);
            var fillRate  = MathF.Min(predicted / tt.TotalQuantity, 1f);
            int predictedInt = (int)MathF.Round(predicted);

            decimal factor;
            string  reason;

            if (fillRate < 0.30f)
            {
                factor = 0.85m;
                reason = $"Dự đoán lấp đầy {fillRate * 100:F0}% ({predictedInt}/{tt.TotalQuantity} ghế) — nhu cầu thấp, giảm 15% để kích cầu.";
            }
            else if (fillRate < 0.70f)
            {
                factor = 1.00m;
                reason = $"Dự đoán lấp đầy {fillRate * 100:F0}% ({predictedInt}/{tt.TotalQuantity} ghế) — nhu cầu ổn định, giữ nguyên giá.";
            }
            else if (fillRate < 0.90f)
            {
                factor = 1.10m;
                reason = $"Dự đoán lấp đầy {fillRate * 100:F0}% ({predictedInt}/{tt.TotalQuantity} ghế) — nhu cầu cao, tăng 10%.";
            }
            else
            {
                factor = 1.20m;
                reason = $"Dự đoán lấp đầy {fillRate * 100:F0}% ({predictedInt}/{tt.TotalQuantity} ghế) — nhu cầu rất cao, tăng 20%.";
            }

            // Round to nearest 1 000 VND
            var suggested = Math.Round(tt.Price * factor / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
            if (suggested <= 0) suggested = 1000m;

            var changePct = (float)((suggested - tt.Price) / tt.Price * 100m);

            suggestions.Add(new PricingSuggestion
            {
                EventId           = evt.Id,
                TicketTypeId      = tt.Id,
                TicketTypeName    = tt.Name,
                CurrentPrice      = tt.Price,
                SuggestedPrice    = suggested,
                ChangePercent     = changePct,
                Reason            = reason,
                Confidence        = confidence,
                PredictedFillRate = fillRate,
                PredictedSold     = predictedInt
            });
        }

        return suggestions;
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private async Task<List<SalesData>> LoadDataAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // One row per TicketType that has at least one confirmed sale.
        // Label = SoldQuantity so the model learns "total tickets sold for a type".
        var rows = await db.TicketTypes
            .Include(tt => tt.Event)
            .Where(tt => tt.SoldQuantity > 0 && tt.TotalQuantity > 0)
            .Select(tt => new
            {
                tt.Event.StartDate,
                Price    = tt.Price,
                TotalQty = tt.TotalQuantity,
                SoldQty  = tt.SoldQuantity,
                Category = tt.Event.Category
            })
            .ToListAsync();

        var real = rows.Select(r => new SalesData
        {
            ThangSuKien   = r.StartDate.Month,
            NgayTrongTuan = (float)r.StartDate.DayOfWeek,
            SoNgayCon     = 0f,            // historical records: event already completed
            GiaVe         = (float)r.Price,
            TongSoChoNgoi = r.TotalQty,
            DanhMucId     = MapCategory(r.Category),
            SoVeBan       = r.SoldQty
        }).ToList();

        // Supplement with event-based samples when real data is too sparse
        // for the regression to generalise across the SoNgayCon dimension.
        const int MinSamples = 60;
        if (real.Count < MinSamples)
        {
            var events = await db.Events
                .Include(e => e.TicketTypes)
                .Where(e => e.TicketTypes.Any(tt => tt.TotalQuantity > 0))
                .ToListAsync();

            int needed = Math.Max(0, 200 - real.Count);
            real.AddRange(GenerateEventBasedData(events, needed, seed: 42));
        }

        return real;
    }

    /// <summary>
    /// Generates training samples rooted in real DB events.
    /// Uses each event's actual category / price / capacity; varies SoNgayCon to
    /// teach the model how purchase timing affects expected sales.
    /// </summary>
    private static IEnumerable<SalesData> GenerateEventBasedData(
        IList<Event> events, int count, int seed)
    {
        if (events.Count == 0 || count <= 0) yield break;

        // Category-specific base fill rates (empirical estimates per genre)
        var baseRates = new Dictionary<float, float>
        {
            [1f] = 0.78f, [2f] = 0.65f, [3f] = 0.72f,
            [4f] = 0.80f, [5f] = 0.58f, [6f] = 0.62f,
            [7f] = 0.55f, [0f] = 0.60f
        };

        int[] daysOptions = [7, 14, 21, 30, 45, 60, 90, 120, 150];
        var rng = new Random(seed);
        int produced = 0;

        while (produced < count)
        {
            var evt = events[rng.Next(events.Count)];
            var tickets = evt.TicketTypes.Where(t => t.TotalQuantity > 0).ToList();
            if (tickets.Count == 0) continue;

            var tt   = tickets[rng.Next(tickets.Count)];
            float days  = daysOptions[rng.Next(daysOptions.Length)];
            float cat   = MapCategory(evt.Category);
            float @base = baseRates.GetValueOrDefault(cat, 0.60f);

            bool isWeekend = (int)evt.StartDate.DayOfWeek is 0 or 6;
            bool isPeakMonth = evt.StartDate.Month is >= 6 and <= 8 or 12;

            // Demand factors
            float rate = @base
                + (isWeekend   ? 0.10f : 0f)
                + (isPeakMonth ? 0.08f : 0f)
                + MathF.Max(0f, (500_000f - (float)tt.Price) / 500_000f * 0.10f)
                - (days < 14   ? 0.05f : 0f);    // late purchase → lower total

            rate = Math.Clamp(rate, 0.15f, 0.97f);
            float noise = 0.70f + (float)rng.NextDouble() * 0.50f;
            float sold  = MathF.Round(
                Math.Clamp(tt.TotalQuantity * rate * noise, 0f, tt.TotalQuantity));

            yield return new SalesData
            {
                ThangSuKien   = evt.StartDate.Month,
                NgayTrongTuan = (float)evt.StartDate.DayOfWeek,
                SoNgayCon     = days,
                GiaVe         = (float)tt.Price,
                TongSoChoNgoi = tt.TotalQuantity,
                DanhMucId     = cat,
                SoVeBan       = sold
            };
            produced++;
        }
    }

    // ── ML.NET training pipeline ──────────────────────────────────────────────

    private void FitModel(List<SalesData> allData)
    {
        var rng = new Random(42);
        var shuffled  = allData.OrderBy(_ => rng.Next()).ToList();
        int trainSize = Math.Max(1, (int)(shuffled.Count * 0.8));

        var trainList = shuffled.Take(trainSize).ToList();
        // For tiny datasets keep test = train to avoid empty evaluation
        var testList  = shuffled.Count > 5 ? shuffled.Skip(trainSize).ToList() : trainList;

        var trainView = _mlContext.Data.LoadFromEnumerable(trainList);
        var testView  = _mlContext.Data.LoadFromEnumerable(testList);

        var pipeline = _mlContext.Transforms
            .Concatenate("Features",
                nameof(SalesData.ThangSuKien),
                nameof(SalesData.NgayTrongTuan),
                nameof(SalesData.SoNgayCon),
                nameof(SalesData.GiaVe),
                nameof(SalesData.TongSoChoNgoi),
                nameof(SalesData.DanhMucId))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.Sdca(
                labelColumnName: "Label",
                featureColumnName: "Features"));

        _model            = pipeline.Fit(trainView);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesData, SalesPrediction>(_model);

        var evaluated = _model.Transform(testView);
        _metrics = _mlContext.Regression.Evaluate(evaluated, labelColumnName: "Label");
    }
}
