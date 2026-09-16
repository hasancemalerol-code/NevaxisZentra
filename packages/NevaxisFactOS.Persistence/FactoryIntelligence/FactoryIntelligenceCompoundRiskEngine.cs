namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public static class FactoryIntelligenceCompoundRiskEngine
{
    public static FactoryRiskDetectionResult? Compose(
        IReadOnlyList<FactoryRiskDetectionResult> singles,
        string machineCode,
        string orderReference,
        DateTimeOffset evaluatedAtUtc)
    {
        var machine = singles.FirstOrDefault(r =>
            r.RiskType == FactoryIntelligenceRiskTypes.MachineDegradation);
        var material = singles.FirstOrDefault(r =>
            r.RiskType == FactoryIntelligenceRiskTypes.MaterialShortage);
        var maintenance = singles.FirstOrDefault(r =>
            r.RiskType == FactoryIntelligenceRiskTypes.MaintenanceReliability);
        var delivery = singles.FirstOrDefault(r =>
            r.RiskType == FactoryIntelligenceRiskTypes.CriticalOrderDelivery);

        if (machine is null && material is null && maintenance is null)
        {
            return null;
        }

        var machineState = machine?.RiskState ?? FactoryIntelligenceRiskStates.Watch;
        var materialState = material?.RiskState ?? FactoryIntelligenceRiskStates.Watch;
        var maintenanceState = maintenance?.RiskState ?? FactoryIntelligenceRiskStates.Watch;

        var compoundScore = StateScore(machineState) + StateScore(materialState) + StateScore(maintenanceState);
        if (compoundScore < 4)
        {
            return null;
        }

        var compoundState = compoundScore >= 8
            ? FactoryIntelligenceRiskStates.Critical
            : compoundScore >= 5
                ? FactoryIntelligenceRiskStates.Warning
                : FactoryIntelligenceRiskStates.Watch;

        if (delivery is not null && StateScore(delivery.RiskState) > StateScore(compoundState))
        {
            return null;
        }

        var contributing = singles
            .SelectMany(r => r.ContributingSignals)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var explanation = new List<string>
        {
            $"INFERRED: compound delivery outcome risk on {orderReference}",
            machine is not null ? $"Machine degradation alone: {machine.RiskState}" : "Machine degradation alone: none",
            material is not null ? $"Material risk alone: {material.RiskState}" : "Material risk alone: none",
            maintenance is not null ? $"Maintenance risk alone: {maintenance.RiskState}" : "Maintenance risk alone: none",
            $"Together: {compoundState} delivery outcome risk",
        };

        return new FactoryRiskDetectionResult(
            RiskType: FactoryIntelligenceRiskTypes.CompoundDelivery,
            RiskState: compoundState,
            AffectedOutcome: $"Compound delivery risk for {orderReference}",
            ConfidenceBand: ResolveCompoundConfidence(machine, material, maintenance),
            Horizon: FactoryIntelligencePredictionHorizons.Delivery,
            Severity: compoundState,
            TimeToImpactLabel: delivery?.TimeToImpactLabel ?? material?.TimeToImpactLabel ?? "remaining shift",
            ProjectedGapQuantity: delivery?.ProjectedGapQuantity ?? machine?.ProjectedGapQuantity,
            ContributingSignals: contributing,
            Explanation: explanation,
            Assumptions:
            [
                "Correlated machine, material and maintenance signals worsen together",
                "Order criticality elevates combined impact",
            ],
            DataQuality: MergeQuality(machine?.DataQuality, material?.DataQuality, maintenance?.DataQuality),
            CorrelationKey: $"compound-delivery:{machineCode}:{orderReference}");
    }

    private static int StateScore(string state) => state switch
    {
        FactoryIntelligenceRiskStates.Critical => 4,
        FactoryIntelligenceRiskStates.Warning => 2,
        FactoryIntelligenceRiskStates.Watch => 1,
        _ => 0,
    };

    private static string ResolveCompoundConfidence(
        FactoryRiskDetectionResult? machine,
        FactoryRiskDetectionResult? material,
        FactoryRiskDetectionResult? maintenance)
    {
        var bands = new[] { machine?.ConfidenceBand, material?.ConfidenceBand, maintenance?.ConfidenceBand }
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .ToList();

        if (bands.Count >= 2 && bands.All(b => b == FactoryIntelligenceConfidenceBands.High))
        {
            return FactoryIntelligenceConfidenceBands.High;
        }

        if (bands.Any(b => b == FactoryIntelligenceConfidenceBands.Low))
        {
            return FactoryIntelligenceConfidenceBands.Low;
        }

        return FactoryIntelligenceConfidenceBands.Medium;
    }

    private static string MergeQuality(params string?[] qualities)
    {
        var values = qualities.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (values.Contains(FactoryIntelligenceDataQualityStates.Insufficient))
        {
            return FactoryIntelligenceDataQualityStates.Insufficient;
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
