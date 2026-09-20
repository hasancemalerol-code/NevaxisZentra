using System.Globalization;
using System.Text.Json;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.ContinuousOptimization;

public static class ContinuousOptimizationQualificationEngine
{
    public static ContinuousOptimizationPolicyThresholds ResolveThresholds(ContinuousOptimizationPolicyRecord? policy) =>
        policy is null
            ? new ContinuousOptimizationPolicyThresholds()
            : JsonSerializer.Deserialize<ContinuousOptimizationPolicyThresholds>(policy.ThresholdsJson)
              ?? new ContinuousOptimizationPolicyThresholds();

    public static ContinuousOptimizationQualificationResult QualifyTrigger(
        ContinuousOptimizationTriggerRequest trigger,
        ContinuousOptimizationPolicyThresholds thresholds,
        bool policyEnabled,
        bool policyPaused,
        ContinuousOptimizationRunRecord? activeRun,
        DateTimeOffset? lastRunAtUtc,
        int runsToday)
    {
        if (!policyEnabled || policyPaused)
        {
            return new ContinuousOptimizationQualificationResult(
                false,
                ContinuousOptimizationFailureCodes.PolicyBlocked,
                "Optimization policy disabled or paused.",
                thresholds);
        }

        if (activeRun is not null && !IsTerminalPhase(activeRun.CurrentPhase))
        {
            return new ContinuousOptimizationQualificationResult(
                false,
                ContinuousOptimizationFailureCodes.DuplicateOpportunity,
                "Active optimization run already exists for this condition.",
                thresholds);
        }

        if (lastRunAtUtc.HasValue &&
            DateTimeOffset.UtcNow - lastRunAtUtc.Value < TimeSpan.FromMinutes(thresholds.CooldownMinutes))
        {
            return new ContinuousOptimizationQualificationResult(
                false,
                ContinuousOptimizationFailureCodes.CooldownActive,
                $"Cooldown active for {thresholds.CooldownMinutes} minutes.",
                thresholds);
        }

        if (runsToday >= thresholds.MaxRunsPerLinePerDay)
        {
            return new ContinuousOptimizationQualificationResult(
                false,
                ContinuousOptimizationFailureCodes.BudgetExceeded,
                "Daily optimization run budget exceeded.",
                thresholds);
        }

        if (trigger.TriggerType == ContinuousOptimizationTriggerTypes.InsignificantVariation)
        {
            return new ContinuousOptimizationQualificationResult(
                false,
                ContinuousOptimizationOutcomes.NoSignificantOpportunity,
                "Variation below significance threshold.",
                thresholds);
        }

        if (trigger.OptimizationType == ContinuousOptimizationTypes.Throughput)
        {
            var gap = trigger.DeltaValue ?? 0m;
            var baseline = trigger.BaselineValue ?? 0m;
            var percent = baseline > 0 ? gap / baseline * 100m : 0m;

            if (gap < thresholds.MinimumGapPerHour && percent < thresholds.GapThresholdPercent)
            {
                return new ContinuousOptimizationQualificationResult(
                    false,
                    ContinuousOptimizationOutcomes.NoSignificantOpportunity,
                    "Throughput deviation below configured threshold.",
                    thresholds);
            }

            if (gap < thresholds.MinimumValueThreshold)
            {
                return new ContinuousOptimizationQualificationResult(
                    false,
                    ContinuousOptimizationOutcomes.NoSignificantOpportunity,
                    "Expected benefit below minimum value threshold.",
                    thresholds);
            }
        }

        return new ContinuousOptimizationQualificationResult(true, "qualified", null, thresholds);
    }

    public static OptimizationOpportunity BuildThroughputOpportunity(
        ContinuousOptimizationTriggerRequest trigger,
        ThroughputIntelligenceOverview overview,
        ContinuousOptimizationPolicyThresholds thresholds)
    {
        var gap = overview.ProductionGap.GapPerHour;
        var constraint = overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT");
        var evidence = new List<string>
        {
            $"line:{overview.LineCode}",
            $"gap:{gap:0.#}/h",
            $"data-quality:{overview.DataQuality}",
        };

        if (constraint?.StationCode is not null)
        {
            evidence.Add($"constraint:{constraint.StationCode}");
        }

        return new OptimizationOpportunity(
            ContinuousOptimizationTypes.Throughput,
            "line",
            overview.LineId.ToString(),
            trigger.TriggerType,
            $"{overview.LineCode} throughput opportunity",
            $"Potential +{Math.Max(gap, trigger.DeltaValue ?? 0m):0.#} good units/hour via registered interventions.",
            overview.ProductionGap.CurrentGoodPerHour,
            overview.ProductionGap.RequiredGoodPerHour,
            Math.Max(gap, thresholds.MinimumValueThreshold),
            constraint?.Confidence == "HIGH" ? 0.9m : 0.75m,
            ContinuousOptimizationActionRiskClasses.Medium,
            evidence,
            ["Evaluate scenario options", "Link DigitalWorkCase if approved"],
            ["Policy", "Autonomy", "Verification window"],
            DateTimeOffset.UtcNow.AddHours(4));
    }

    public static string BuildCorrelationKey(Guid lineId, string triggerType) =>
        $"{ContinuousOptimizationCorrelationPrefixes.ThroughputGap}:{lineId:N}:{triggerType}";

    public static bool IsTerminalPhase(string phase) =>
        phase is ContinuousOptimizationPhases.Verified
            or ContinuousOptimizationPhases.NotVerified
            or ContinuousOptimizationPhases.NoAction
            or ContinuousOptimizationPhases.Failed
            or ContinuousOptimizationPhases.Cancelled;

    public static ContinuousOptimizationExplainability BuildExplainability(
        ContinuousOptimizationTriggerRequest trigger,
        OptimizationOpportunity opportunity,
        string? approvalState) =>
        new(
            trigger.TriggerType,
            trigger.Detail ?? opportunity.Summary,
            opportunity.BaselineValue.ToString("0.#", CultureInfo.InvariantCulture),
            opportunity.EstimatedBenefit.ToString("0.#", CultureInfo.InvariantCulture),
            opportunity.Constraints,
            opportunity.RequiredActions.Count > 0 ? opportunity.RequiredActions[0] : null,
            opportunity.RiskClass,
            approvalState,
            opportunity.Evidence);
}

public sealed class ContinuousOptimizationException : Exception
{
    public ContinuousOptimizationException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;

    public string FailureCode { get; }
}
