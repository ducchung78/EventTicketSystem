using EventTicketSystem.Web.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace EventTicketSystem.Web.Services;

public class TicketPredictionService
{
    private readonly MLContext _mlContext = new(seed: 42);
    private ITransformer? _model;
    private PredictionEngine<SalesData, SalesPrediction>? _predictionEngine;
    private RegressionMetrics? _metrics;
    private readonly object _lock = new();
    private bool _trained;

    public RegressionMetrics? ModelMetrics => _metrics;
    public bool IsTrained => _trained;

    private static readonly Dictionary<string, float> CategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Âm Nhạc"]    = 1f,
        ["Công Nghệ"]  = 2f,
        ["Hài Kịch"]   = 3f,
        ["Nghệ Thuật"] = 4f,
        ["Thể Thao"]   = 5f,
        ["Music"]       = 1f,
        ["Technology"]  = 2f,
        ["Comedy"]      = 3f,
        ["Arts"]        = 4f,
        ["Sports"]      = 5f,
    };

    public static float MapCategory(string category) =>
        CategoryMap.TryGetValue(category, out var v) ? v : 0f;

    public void EnsureTrained()
    {
        if (_trained) return;
        lock (_lock)
        {
            if (_trained) return;
            TrainAndEvaluate();
            _trained = true;
        }
    }

    private void TrainAndEvaluate()
    {
        var trainData = GenerateSyntheticData(800, seed: 42);
        var testData  = GenerateSyntheticData(200, seed: 99);

        var trainView = _mlContext.Data.LoadFromEnumerable(trainData);
        var testView  = _mlContext.Data.LoadFromEnumerable(testData);

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

        _model = pipeline.Fit(trainView);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesData, SalesPrediction>(_model);

        var transformed = _model.Transform(testView);
        _metrics = _mlContext.Regression.Evaluate(transformed, labelColumnName: "Label");
    }

    public float Predict(SalesData input)
    {
        EnsureTrained();
        lock (_lock)
        {
            var result = _predictionEngine?.Predict(input).SoVeDuBao ?? 0f;
            return MathF.Max(0f, result);
        }
    }

    public float PredictForEvent(EventTicketSystem.Web.Models.Event evt, TicketType tt)
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
        var raw = Predict(input);
        return MathF.Min(raw, tt.TotalQuantity);
    }

    private static IEnumerable<SalesData> GenerateSyntheticData(int count, int seed)
    {
        var rng = new Random(seed);
        float[] cats      = { 1f, 2f, 3f, 4f, 5f };
        float[] baseRates = { 0.78f, 0.65f, 0.72f, 0.80f, 0.58f };

        for (int i = 0; i < count; i++)
        {
            int ci       = rng.Next(cats.Length);
            float month  = rng.Next(1, 13);
            float dow    = rng.Next(0, 7);
            float days   = rng.Next(7, 180);
            float price  = rng.Next(1, 11) * 50f;
            float cap    = rng.Next(5, 21) * 50f;

            float rate = baseRates[ci]
                + (dow >= 5 ? 0.10f : 0f)
                + ((month >= 6 && month <= 8) || month == 12 ? 0.08f : 0f)
                + MathF.Max(0f, (500f - price) / 500f * 0.12f);

            rate = MathF.Min(0.97f, rate);
            float sold = cap * rate * (0.70f + (float)rng.NextDouble() * 0.50f);
            sold = MathF.Min(cap, MathF.Max(0f, sold));

            yield return new SalesData
            {
                DanhMucId     = cats[ci],
                ThangSuKien   = month,
                NgayTrongTuan = dow,
                SoNgayCon     = days,
                GiaVe         = price,
                TongSoChoNgoi = cap,
                SoVeBan       = MathF.Round(sold)
            };
        }
    }
}
