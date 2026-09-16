namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public static class FactoryIntelligenceRiskDetectionEngine
{
    public static IReadOnlyList<FactoryRiskDetectionResult> Detect(FactoryRiskDetectionInput input)
    {
        var results = new List<FactoryRiskDetectionResult>();
        var machine = input.MachineCode;
        var order = input.OrderReference ?? "ORD-8742";

        var cycle = FindSignal(input, FactoryIntelligenceSignalTypes.CycleTimeDrift);
        var microStops = FindSignal(input, FactoryIntelligenceSignalTypes.MicroStopFrequency);
        var material = FindSignal(input, FactoryIntelligenceSignalTypes.MaterialCoverageHours);
        var scrap = FindSignal(input, FactoryIntelligenceSignalTypes.ScrapRejectTrend);
        var maintenance = FindSignal(input, FactoryIntelligenceSignalTypes.MaintenanceWarning);

        if (cycle is not null)
        {
            var baseline = FindBaseline(input, FactoryIntelligenceSignalTypes.CycleTimeDrift);
            var state = ResolveMachineDegradationState(cycle, baseline);
            if (state is not null)
            {
                results.Add(state);
            }
        }

        if (material is not null)
        {
            var materialRisk = DetectMaterialRisk(material, machine, order);
            if (materialRisk is not null)
            {
                results.Add(materialRisk);
            }
        }

        if (cycle is not null || material is not null || scrap is not null)
        {
            var delivery = DetectDeliveryRisk(input, cycle, material, scrap, machine, order);
            if (delivery is not null)
            {
                results.Add(delivery);
            }
        }

        if (maintenance is not null)
        {
            var maintenanceRisk = DetectMaintenanceRisk(maintenance, machine);
            if (maintenanceRisk is not null)
            {
                results.Add(maintenanceRisk);
            }
        }

        if (scrap is not null)
        {
            var qualityRisk = DetectQualityRisk(scrap, machine);
            if (qualityRisk is not null)
            {
                results.Add(qualityRisk);
            }
        }

        var compound = FactoryIntelligenceCompoundRiskEngine.Compose(
            results,
            machine,
            order,
            input.EvaluatedAtUtc);
        if (compound is not null)
        {
            results.Add(compound);
        }

        return results;
    }

    private static FactoryRiskDetectionResult? ResolveMachineDegradationState(
        FactorySignalWindowInput cycle,
        FactoryBaselineResult? baseline)
    {
        if (cycle.DataQuality is FactoryIntelligenceDataQualityStates.Insufficient)
        {
            return null;
        }

        var deviation = baseline?.DeviationPercent ?? 0;
        if (deviation < 3m)
        {
            return null;
        }

        var state = deviation switch
        {
            >= 12m => FactoryIntelligenceRiskStates.Critical,
            >= 7m => FactoryIntelligenceRiskStates.Warning,
            _ => FactoryIntelligenceRiskStates.Watch
        };

        var confidence = ResolveConfidence(cycle.DataQuality, baseline?.DataQuality, deviation);
        return new FactoryRiskDetectionResult(
            RiskType: FactoryIntelligenceRiskTypes.MachineDegradation,
            RiskState: state,
            AffectedOutcome: $"{cycle.ResourceId} cycle-time drift",
            ConfidenceBand: confidence,
            Horizon: FactoryIntelligencePredictionHorizons.ShortTerm,
            Severity: state,
            TimeToImpactLabel: state == FactoryIntelligenceRiskStates.Critical ? "2h 45m" : "4h",
            ProjectedGapQuantity: null,
            ContributingSignals: [FactoryIntelligenceSignalTypes.CycleTimeDrift],
            Explanation:
            [
                $"OBSERVED: {cycle.ResourceId} cycle = {cycle.MeasuredValue:F1} sec",
                $"BASELINE: {baseline?.Reference:F1} sec ({deviation:F1}% deviation)",
                "PREDICTED: throughput loss if trend continues"
            ],
            Assumptions: ["Current cycle trend continues without intervention"],
            DataQuality: MergeQuality(cycle.DataQuality, baseline?.DataQuality),
            CorrelationKey: $"machine-degradation:{cycle.ResourceId}");
    }

    private static FactoryRiskDetectionResult? DetectMaterialRisk(
        FactorySignalWindowInput material,
        string machine,
        string order)
    {
        if (material.MeasuredValue > 6m)
        {
            return null;
        }

        var state = material.MeasuredValue switch
        {
            <= 3m => FactoryIntelligenceRiskStates.Critical,
            <= 4.5m => FactoryIntelligenceRiskStates.Warning,
            _ => FactoryIntelligenceRiskStates.Watch
        };

        return new FactoryRiskDetectionResult(
            RiskType: FactoryIntelligenceRiskTypes.MaterialShortage,
            RiskState: state,
            AffectedOutcome: $"Material coverage pressure on {order}",
            ConfidenceBand: material.MeasuredValue <= 4m
                ? FactoryIntelligenceConfidenceBands.High
                : FactoryIntelligenceConfidenceBands.Medium,
            Horizon: FactoryIntelligencePredictionHorizons.Immediate,
            Severity: state,
            TimeToImpactLabel: $"{material.MeasuredValue:F1}h coverage",
            ProjectedGapQuantity: null,
            ContributingSignals: [FactoryIntelligenceSignalTypes.MaterialCoverageHours],
            Explanation:
            [
                $"OBSERVED: material coverage {material.MeasuredValue:F1}h",
                "PREDICTED: production stop risk before replenishment"
            ],
            Assumptions: ["No inbound replenishment within coverage window"],
            DataQuality: material.DataQuality,
            CorrelationKey: $"material-shortage:{machine}:{order}");
    }

    private static FactoryRiskDetectionResult? DetectDeliveryRisk(
        FactoryRiskDetectionInput input,
        FactorySignalWindowInput? cycle,
        FactorySignalWindowInput? material,
        FactorySignalWindowInput? scrap,
        string machine,
        string order)
    {
        var projection = FactoryIntelligenceOutcomeProjectionEngine.ProjectDelivery(
            cycle,
            material,
            scrap,
            order);

        if (projection is null || projection.RiskState is null)
        {
            return null;
        }

        return new FactoryRiskDetectionResult(
            RiskType: FactoryIntelligenceRiskTypes.CriticalOrderDelivery,
            RiskState: projection.RiskState,
            AffectedOutcome: projection.AffectedOutcome,
            ConfidenceBand: projection.ConfidenceBand,
            Horizon: FactoryIntelligencePredictionHorizons.Delivery,
            Severity: projection.RiskState,
            TimeToImpactLabel: projection.TimeToImpactLabel,
            ProjectedGapQuantity: projection.ProjectedGapQuantity,
            ContributingSignals: projection.ContributingSignals,
            Explanation: projection.Explanation,
            Assumptions: projection.Assumptions,
            DataQuality: projection.DataQuality,
            CorrelationKey: $"delivery-risk:{machine}:{order}");
    }

    private static FactoryRiskDetectionResult? DetectMaintenanceRisk(
        FactorySignalWindowInput maintenance,
        string machine)
    {
        if (maintenance.MeasuredValue < 1m)
        {
            return null;
        }

        var state = maintenance.MeasuredValue switch
        {
            >= 3m => FactoryIntelligenceRiskStates.Critical,
            >= 2m => FactoryIntelligenceRiskStates.Warning,
            _ => FactoryIntelligenceRiskStates.Watch
        };

        return new FactoryRiskDetectionResult(
            RiskType: FactoryIntelligenceRiskTypes.MaintenanceReliability,
            RiskState: state,
            AffectedOutcome: $"{machine} reliability warning",
            ConfidenceBand: FactoryIntelligenceConfidenceBands.Medium,
            Horizon: FactoryIntelligencePredictionHorizons.Shift,
            Severity: state,
            TimeToImpactLabel: "remaining shift",
            ProjectedGapQuantity: null,
            ContributingSignals: [FactoryIntelligenceSignalTypes.MaintenanceWarning],
            Explanation:
            [
                "OBSERVED: rising maintenance reliability warnings",
                "INFERRED: elevated downtime exposure"
            ],
            Assumptions: ["Maintenance pattern continues without preventive action"],
            DataQuality: maintenance.DataQuality,
            CorrelationKey: $"maintenance-risk:{machine}");
    }

    private static FactoryRiskDetectionResult? DetectQualityRisk(
        FactorySignalWindowInput scrap,
        string machine)
    {
        if (scrap.MeasuredValue < 1.5m)
        {
            return null;
        }

        var state = scrap.MeasuredValue switch
        {
            >= 4m => FactoryIntelligenceRiskStates.Critical,
            >= 2.5m => FactoryIntelligenceRiskStates.Warning,
            _ => FactoryIntelligenceRiskStates.Watch
        };

        return new FactoryRiskDetectionResult(
            RiskType: FactoryIntelligenceRiskTypes.QualityDeterioration,
            RiskState: state,
            AffectedOutcome: $"{machine} reject-rate drift",
            ConfidenceBand: FactoryIntelligenceConfidenceBands.Medium,
            Horizon: FactoryIntelligencePredictionHorizons.ShortTerm,
            Severity: state,
            TimeToImpactLabel: "3h",
            ProjectedGapQuantity: null,
            ContributingSignals: [FactoryIntelligenceSignalTypes.ScrapRejectTrend],
            Explanation:
            [
                $"OBSERVED: reject trend index {scrap.MeasuredValue:F1}",
                "ASSOCIATED: machine/mold pattern — root cause not asserted"
            ],
            Assumptions: ["Reject trend continues at current slope"],
            DataQuality: scrap.DataQuality,
            CorrelationKey: $"quality-risk:{machine}");
    }

    private static FactorySignalWindowInput? FindSignal(FactoryRiskDetectionInput input, string signalType) =>
        input.Signals.FirstOrDefault(s => s.SignalType == signalType);

    private static FactoryBaselineResult? FindBaseline(FactoryRiskDetectionInput input, string signalType) =>
        input.Baselines.FirstOrDefault(b =>
            string.Equals(b.SignalType, signalType, StringComparison.OrdinalIgnoreCase));

    private static string ResolveConfidence(string? primary, string? secondary, decimal deviation)
    {
        var quality = MergeQuality(primary ?? FactoryIntelligenceDataQualityStates.Good, secondary);
        if (quality is FactoryIntelligenceDataQualityStates.Partial or FactoryIntelligenceDataQualityStates.Stale)
        {
            return FactoryIntelligenceConfidenceBands.Low;
        }

        return deviation >= 10m
            ? FactoryIntelligenceConfidenceBands.High
            : FactoryIntelligenceConfidenceBands.Medium;
    }

    private static string MergeQuality(string? a, string? b)
    {
        var values = new[] { a, b }.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
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
