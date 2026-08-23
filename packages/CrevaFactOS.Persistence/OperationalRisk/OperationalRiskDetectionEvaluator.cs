using CrevaFactOS.Persistence.Energy;
using CrevaFactOS.Persistence.IotPlc;
using CrevaFactOS.Persistence.Maintenance;

namespace CrevaFactOS.Persistence.OperationalRisk;

public sealed record OperationalRiskDetectionEvaluationInput(
    OperationalRiskSourceSnapshot Snapshot,
    OperationalRiskDetectionRule? Rule);

public sealed record OperationalRiskDetectionEvaluationOutput(
    string DetectionOutcome,
    string EvidenceState,
    string? LeadingDriverCode,
    int DriverSeverity,
    decimal? NormalizedVariance,
    int? LikelihoodLevel,
    int? ImpactLevel,
    int? RiskScore,
    string? SeverityBand,
    IReadOnlyList<OperationalRiskDetectionDriverDraft> Drivers);

public sealed record OperationalRiskDetectionDriverDraft(
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    string SupportingText,
    int SortOrder);

public static class OperationalRiskDetectionEvaluator
{
    private static readonly string[] DriverOrder =
    [
        OperationalRiskDetectionDriverReasonCodes.SourceConflict,
        OperationalRiskDetectionDriverReasonCodes.SourceStale,
        OperationalRiskDetectionDriverReasonCodes.MaintenanceRisk,
        OperationalRiskDetectionDriverReasonCodes.ConnectivityRisk,
        OperationalRiskDetectionDriverReasonCodes.EnergyRisk,
        OperationalRiskDetectionDriverReasonCodes.CostDeviationExceeded,
        OperationalRiskDetectionDriverReasonCodes.QualityRisk,
        OperationalRiskDetectionDriverReasonCodes.ProductionContinuityRisk,
        OperationalRiskDetectionDriverReasonCodes.OrderDeliveryDelay,
        OperationalRiskDetectionDriverReasonCodes.SupplierDelay,
        OperationalRiskDetectionDriverReasonCodes.MaterialShortfall,
        OperationalRiskDetectionDriverReasonCodes.MissingThreshold,
        OperationalRiskDetectionDriverReasonCodes.MissingDenominator,
        OperationalRiskDetectionDriverReasonCodes.InsufficientEvidence,
        OperationalRiskDetectionDriverReasonCodes.WithinThreshold,
    ];

    public static OperationalRiskDetectionEvaluationOutput Evaluate(OperationalRiskDetectionEvaluationInput input)
    {
        var snapshot = input.Snapshot;
        var drivers = new List<OperationalRiskDetectionDriverDraft>();

        if (string.Equals(snapshot.EvidenceState, OperationalRiskEvidenceStates.SourceConflict, StringComparison.Ordinal)
            || string.Equals(snapshot.SourceOutcome, EnergyIntelligenceOutcomes.SourceConflict, StringComparison.Ordinal)
            || string.Equals(snapshot.SourceOutcome, IotPlcIntelligenceOutcomes.SourceConflict, StringComparison.Ordinal))
        {
            drivers.Add(Draft(OperationalRiskDetectionDriverReasonCodes.SourceConflict, "critical", snapshot, "Source evidence conflict."));
            return Output(OperationalRiskDetectionOutcomes.SourceConflict, snapshot.EvidenceState, drivers);
        }

        if (string.Equals(snapshot.EvidenceState, OperationalRiskEvidenceStates.InsufficientData, StringComparison.Ordinal)
            || string.Equals(snapshot.SourceOutcome, EnergyIntelligenceOutcomes.InsufficientData, StringComparison.Ordinal)
            || string.Equals(snapshot.SourceOutcome, IotPlcIntelligenceOutcomes.InsufficientData, StringComparison.Ordinal)
            || string.Equals(snapshot.SourceOutcome, MaintenancePredictiveRiskBands.InsufficientData, StringComparison.Ordinal))
        {
            drivers.Add(Draft(OperationalRiskDetectionDriverReasonCodes.InsufficientEvidence, "info", snapshot, "Insufficient source evidence."));
            return Output(OperationalRiskDetectionOutcomes.InsufficientData, OperationalRiskEvidenceStates.InsufficientData, drivers);
        }

        if (snapshot.IsStale)
        {
            drivers.Add(Draft(OperationalRiskDetectionDriverReasonCodes.SourceStale, "moderate", snapshot, "Source evidence is stale."));
        }

        if (input.Rule is null)
        {
            drivers.Add(Draft(OperationalRiskDetectionDriverReasonCodes.MissingThreshold, "info", snapshot, "No approved detection rule matched."));
            return Output(OperationalRiskDetectionOutcomes.InsufficientData, snapshot.EvidenceState, drivers);
        }

        if (IsWithinThreshold(snapshot))
        {
            drivers.Add(Draft(OperationalRiskDetectionDriverReasonCodes.WithinThreshold, "low", snapshot, "Source within approved threshold."));
            return Output(OperationalRiskDetectionOutcomes.WithinThreshold, snapshot.EvidenceState, drivers);
        }

        var variance = ComputeVariance(snapshot);
        drivers.Add(Draft(input.Rule.DriverReasonCode, SeverityFromRule(input.Rule), snapshot, $"Source outcome {snapshot.SourceOutcome}.", variance));

        var assessment = OperationalRiskAssessor.Assess(
            snapshot.EvidenceState,
            input.Rule.DefaultLikelihood,
            input.Rule.DefaultImpact);

        var leading = SelectLeadingDriver(drivers, assessment.SeverityBand);
        return new OperationalRiskDetectionEvaluationOutput(
            OperationalRiskDetectionOutcomes.RiskDetected,
            snapshot.EvidenceState,
            leading.ReasonCode,
            DriverSeverityRank(leading.Severity),
            variance,
            assessment.LikelihoodLevel,
            assessment.ImpactLevel,
            assessment.RiskScore,
            assessment.SeverityBand,
            drivers);
    }

    public static OperationalRiskDetectionDriverDraft SelectLeadingDriver(
        IReadOnlyList<OperationalRiskDetectionDriverDraft> drivers,
        string? severityBand)
    {
        return drivers
            .OrderByDescending(d => DriverSeverityRank(d.Severity))
            .ThenByDescending(d => d.NormalizedVariance ?? 0m)
            .ThenBy(d => Array.IndexOf(DriverOrder, d.ReasonCode))
            .First();
    }

    private static bool IsWithinThreshold(OperationalRiskSourceSnapshot snapshot) =>
        string.Equals(snapshot.SourceOutcome, EnergyIntelligenceOutcomes.WithinTarget, StringComparison.Ordinal)
        || string.Equals(snapshot.SourceOutcome, IotPlcIntelligenceOutcomes.Healthy, StringComparison.Ordinal)
        || string.Equals(snapshot.SourceOutcome, MaintenancePredictiveRiskBands.Low, StringComparison.Ordinal)
        || string.Equals(snapshot.SourceOutcome, MaintenancePredictiveRiskBands.Watch, StringComparison.Ordinal);

    private static decimal? ComputeVariance(OperationalRiskSourceSnapshot snapshot)
    {
        if (!snapshot.ActualValue.HasValue || !snapshot.ThresholdValue.HasValue || snapshot.ThresholdValue == 0)
        {
            return null;
        }

        return (snapshot.ActualValue.Value - snapshot.ThresholdValue.Value) / Math.Abs(snapshot.ThresholdValue.Value);
    }

    private static OperationalRiskDetectionEvaluationOutput Output(
        string outcome,
        string evidenceState,
        List<OperationalRiskDetectionDriverDraft> drivers)
    {
        var leading = SelectLeadingDriver(drivers, null);
        return new OperationalRiskDetectionEvaluationOutput(
            outcome,
            evidenceState,
            leading.ReasonCode,
            DriverSeverityRank(leading.Severity),
            leading.NormalizedVariance,
            null,
            null,
            null,
            null,
            drivers);
    }

    private static OperationalRiskDetectionDriverDraft Draft(
        string reason,
        string severity,
        OperationalRiskSourceSnapshot snapshot,
        string text,
        decimal? variance = null) =>
        new(reason, severity, snapshot.ActualValue, snapshot.ThresholdValue, snapshot.Unit, variance, text, 0);

    private static string SeverityFromRule(OperationalRiskDetectionRule rule)
    {
        var score = rule.DefaultLikelihood * rule.DefaultImpact;
        return OperationalRiskAssessor.ResolveSeverityBand(score) ?? OperationalRiskSeverityBands.Moderate;
    }

    private static int DriverSeverityRank(string severity) =>
        severity switch
        {
            OperationalRiskSeverityBands.Critical => 4,
            OperationalRiskSeverityBands.High => 3,
            OperationalRiskSeverityBands.Moderate => 2,
            OperationalRiskSeverityBands.Low => 1,
            _ => 0,
        };
}
