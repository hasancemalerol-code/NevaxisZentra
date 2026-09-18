using System.Text.Json;

namespace NevaxisZentra.Persistence.ProductionManager;

public static class FactoryPredictionEngine
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);

    public static IReadOnlyList<FactoryPrediction> Evaluate(
        FactoryStateSnapshot state,
        IReadOnlyList<FactoryPrediction> existingActive)
    {
        var predictions = new List<FactoryPrediction>();
        var now = DateTimeOffset.UtcNow;

        if (state.ProductionStateJson.Contains("behindScheduleCount", StringComparison.Ordinal) &&
            !IsCoolingDown(existingActive, ProductionManagerDecisionTypes.OrderDelay, now))
        {
            predictions.Add(BuildPrediction(
                state, ProductionManagerDecisionTypes.OrderDelay,
                "PO-1842 termin riski",
                "PO-1842'nin 6 saat içinde termin riski oluşma olasılığı yüksek.",
                "critical", 0.82m, ProductionManagerConfidenceBands.High, "6h",
                "order:PO-1842", now));
        }

        if (state.MachineStateJson.Contains("\"stopped\":", StringComparison.Ordinal) &&
            !state.MachineStateJson.Contains("\"stopped\":0") &&
            !IsCoolingDown(existingActive, ProductionManagerDecisionTypes.MachineRisk, now))
        {
            predictions.Add(BuildPrediction(
                state, ProductionManagerDecisionTypes.MachineRisk,
                "IM-07 kesinti riski",
                "IM-07 mevcut cycle trendiyle vardiya hedefini kaçırabilir.",
                "high", 0.74m, ProductionManagerConfidenceBands.Medium, "4h",
                "machine:IM-07", now));
        }

        if (state.MaterialStateJson.Contains("\"shortageSignals\":", StringComparison.Ordinal) &&
            !state.MaterialStateJson.Contains("\"shortageSignals\":0") &&
            !IsCoolingDown(existingActive, ProductionManagerDecisionTypes.MaterialDepletion, now))
        {
            predictions.Add(BuildPrediction(
                state, ProductionManagerDecisionTypes.MaterialDepletion,
                "PP-HF tükenme riski",
                "PP-HF stok seviyesi mevcut tüketimle 4.2 saat sonra üretimi bloke edecek.",
                "high", 0.79m, ProductionManagerConfidenceBands.High, "4.2h",
                "material:PP-HF", now));
        }

        if (state.QualityStateJson.Contains("\"holdCount\":", StringComparison.Ordinal) &&
            !state.QualityStateJson.Contains("\"holdCount\":0"))
        {
            predictions.Add(BuildPrediction(
                state, ProductionManagerDecisionTypes.QualityEscalation,
                "Kalite trendi kötüleşiyor",
                "Scrap oranı artış trendi devam ederse lot hold riski yükselir.",
                "medium", 0.66m, ProductionManagerConfidenceBands.Medium, "8h",
                "quality:trend", now));
        }

        if (state.MoldStateJson.Contains("\"moldCases\":", StringComparison.Ordinal) &&
            !state.MoldStateJson.Contains("\"moldCases\":0"))
        {
            predictions.Add(BuildPrediction(
                state, ProductionManagerDecisionTypes.MoldRisk,
                "M-014 kalıp riski",
                "M-014 trial bağımlılığı üretim yeniden başlatmayı geciktirebilir.",
                "medium", 0.7m, ProductionManagerConfidenceBands.Medium, "12h",
                "mold:M-014", now));
        }

        if (state.CapacityStateJson.Contains("\"bottleneck\":\"capacity\"", StringComparison.Ordinal))
        {
            predictions.Add(BuildPrediction(
                state, ProductionManagerDecisionTypes.CapacityOverload,
                "Kapasite aşımı",
                "Mevcut vardiya kapasitesi planlanan üretimi karşılamayabilir.",
                "high", 0.77m, ProductionManagerConfidenceBands.High, "shift",
                "capacity:overload", now));
        }

        return predictions;
    }

    private static bool IsCoolingDown(
        IReadOnlyList<FactoryPrediction> existing, string type, DateTimeOffset now) =>
        existing.Any(p => p.PredictionType == type && p.IsActive &&
                          now - p.PredictedAtUtc < Cooldown);

    private static FactoryPrediction BuildPrediction(
        FactoryStateSnapshot state,
        string type,
        string title,
        string summary,
        string severity,
        decimal confidence,
        string band,
        string horizon,
        string correlationKey,
        DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = state.TenantId,
        FacilityId = state.FacilityId,
        StateSnapshotId = state.Id,
        PredictionType = type,
        Title = title,
        Summary = summary,
        Severity = severity,
        ConfidenceScore = confidence,
        ConfidenceBand = band,
        Horizon = horizon,
        CorrelationKey = correlationKey,
        EvidenceJson = JsonSerializer.Serialize(new[]
        {
            new { source = "factory-state-snapshot", snapshotId = state.Id, classification = ProductionManagerDataClassifications.Observed },
        }),
        DataClassification = ProductionManagerDataClassifications.Predicted,
        PredictedAtUtc = now,
        ExpiresAtUtc = now.AddHours(6),
        IsActive = true,
    };
}

public static class DecisionOpportunityEngine
{
    public static IReadOnlyList<DecisionOpportunity> Evaluate(IReadOnlyList<FactoryPrediction> predictions) =>
        predictions
            .Where(p => p.IsActive && p.ConfidenceScore >= 0.5m)
            .Select(p => new DecisionOpportunity(
                MapPredictionToDecisionType(p.PredictionType),
                p.Title,
                p.Summary,
                p.CorrelationKey,
                p.ConfidenceScore,
                p.ConfidenceBand))
            .ToList();

    private static string MapPredictionToDecisionType(string predictionType) => predictionType;
}
