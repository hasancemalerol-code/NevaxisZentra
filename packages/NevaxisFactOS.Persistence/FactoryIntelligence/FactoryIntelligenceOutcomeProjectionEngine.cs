namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public sealed record FactoryDeliveryProjectionResult(
    string RiskState,
    string AffectedOutcome,
    string ConfidenceBand,
    string TimeToImpactLabel,
    int ProjectedGapQuantity,
    IReadOnlyList<string> ContributingSignals,
    IReadOnlyList<string> Explanation,
    IReadOnlyList<string> Assumptions,
    string DataQuality);

public static class FactoryIntelligenceOutcomeProjectionEngine
{
    private const int RequiredQuantity = 1200;
    private const decimal BaselineCycleSec = 27.5m;

    public static FactoryDeliveryProjectionResult? ProjectDelivery(
        FactorySignalWindowInput? cycle,
        FactorySignalWindowInput? material,
        FactorySignalWindowInput? scrap,
        string orderReference)
    {
        if (cycle is null && material is null && scrap is null)
        {
            return null;
        }

        if (cycle?.DataQuality is FactoryIntelligenceDataQualityStates.Insufficient
            && material?.DataQuality is FactoryIntelligenceDataQualityStates.Insufficient)
        {
            return null;
        }

        var cycleSec = cycle?.MeasuredValue ?? BaselineCycleSec;
        var throughputFactor = BaselineCycleSec / Math.Max(cycleSec, 1m);
        var projectedQuantity = (int)Math.Round(RequiredQuantity * throughputFactor);
        if (scrap is not null && scrap.MeasuredValue > 1m)
        {
            projectedQuantity -= (int)Math.Round(scrap.MeasuredValue * 12m);
        }

        var gap = Math.Max(0, RequiredQuantity - projectedQuantity);
        if (gap < 40)
        {
            return null;
        }

        var materialHours = material?.MeasuredValue;
        var riskState = ResolveDeliveryRiskState(gap, cycleSec, materialHours);
        var confidence = ResolveConfidence(cycle, material, scrap, gap);
        var timeToImpact = ResolveTimeToImpact(materialHours, cycleSec);

        var explanation = new List<string>
        {
            cycle is not null
                ? $"OBSERVED: cycle = {cycle.MeasuredValue:F1} sec (baseline {BaselineCycleSec:F1})"
                : "OBSERVED: cycle telemetry unavailable",
            $"PREDICTED: {orderReference} may miss target by {gap} parts if trend continues",
        };

        if (material is not null)
        {
            explanation.Add($"OBSERVED: material coverage {material.MeasuredValue:F1}h");
        }

        if (scrap is not null && scrap.MeasuredValue > 1m)
        {
            explanation.Add($"OBSERVED: reject trend index {scrap.MeasuredValue:F1}");
        }

        var signals = new List<string>();
        if (cycle is not null) signals.Add(FactoryIntelligenceSignalTypes.CycleTimeDrift);
        if (material is not null) signals.Add(FactoryIntelligenceSignalTypes.MaterialCoverageHours);
        if (scrap is not null) signals.Add(FactoryIntelligenceSignalTypes.ScrapRejectTrend);

        return new FactoryDeliveryProjectionResult(
            riskState,
            $"Order {orderReference} delivery commitment",
            confidence,
            timeToImpact,
            gap,
            signals,
            explanation,
            [
                "Throughput projection uses current cycle trend",
                "Scrap trend reduces projected good quantity",
                material?.DataQuality is FactoryIntelligenceDataQualityStates.Partial
                    ? "Inbound replenishment data is partial"
                    : "Material coverage assumes no unplanned inbound",
            ],
            MergeQuality(cycle?.DataQuality, material?.DataQuality, scrap?.DataQuality));
    }

    private static string ResolveDeliveryRiskState(int gap, decimal cycleSec, decimal? materialHours)
    {
        if (gap >= 120 || (materialHours is <= 3m && cycleSec >= 30m))
        {
            return FactoryIntelligenceRiskStates.Critical;
        }

        if (gap >= 70 || cycleSec >= 29m)
        {
            return FactoryIntelligenceRiskStates.Warning;
        }

        return FactoryIntelligenceRiskStates.Watch;
    }

    private static string ResolveConfidence(
        FactorySignalWindowInput? cycle,
        FactorySignalWindowInput? material,
        FactorySignalWindowInput? scrap,
        int gap)
    {
        var quality = MergeQuality(cycle?.DataQuality, material?.DataQuality, scrap?.DataQuality);
        if (quality is FactoryIntelligenceDataQualityStates.Stale
            or FactoryIntelligenceDataQualityStates.Insufficient
            or FactoryIntelligenceDataQualityStates.Partial)
        {
            return FactoryIntelligenceConfidenceBands.Low;
        }

        var signalCount = new[] { cycle, material, scrap }.Count(s => s is not null);
        if (gap >= 90 && signalCount >= 2)
        {
            return FactoryIntelligenceConfidenceBands.High;
        }

        return FactoryIntelligenceConfidenceBands.Medium;
    }

    private static string ResolveTimeToImpact(decimal? materialHours, decimal cycleSec)
    {
        if (materialHours is <= 4.5m)
        {
            return $"{materialHours:F1}h";
        }

        var hours = Math.Max(1.5m, (cycleSec - BaselineCycleSec) * 0.8m + 2m);
        return $"{hours:F1}h";
    }

    private static string MergeQuality(params string?[] qualities)
    {
        var values = qualities.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (values.Contains(FactoryIntelligenceDataQualityStates.Insufficient))
        {
            return FactoryIntelligenceDataQualityStates.Insufficient;
        }

        if (values.Contains(FactoryIntelligenceDataQualityStates.Conflicting))
        {
            return FactoryIntelligenceDataQualityStates.Conflicting;
        }

        if (values.Contains(FactoryIntelligenceDataQualityStates.Stale))
        {
            return FactoryIntelligenceDataQualityStates.Stale;
        }

        if (values.Contains(FactoryIntelligenceDataQualityStates.Partial))
        {
            return FactoryIntelligenceDataQualityStates.Partial;
        }

        return FactoryIntelligenceDataQualityStates.Good;
    }
}
