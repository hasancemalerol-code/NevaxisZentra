using NevaxisFactOS.Persistence.ThroughputIntelligence;

namespace NevaxisFactOS.Persistence.ThroughputOptimizer;

public static class ThroughputObserveEngine
{
    public static ThroughputGapObservation? Observe(ThroughputIntelligenceOverview overview, decimal gapThresholdPerHour = 20m)
    {
        var current = overview.Measurement.GoodUnitsPerHour;
        var target = overview.ProductionGap.RequiredGoodPerHour;
        var gap = target - current;
        var currentConstraint = overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT");

        var meaningful = gap >= gapThresholdPerHour && overview.DataQuality != "LOW";
        if (!meaningful)
        {
            return null;
        }

        return new ThroughputGapObservation(
            overview.LineId,
            overview.LineCode,
            current,
            target,
            gap,
            currentConstraint?.StationCode,
            currentConstraint?.ConstraintType,
            overview.DataQuality,
            true);
    }

    public static string BuildCorrelationKey(Guid lineId) =>
        $"{ThroughputOptimizerCorrelationPrefixes.LineGap}:{lineId:N}";
}

public static class ThroughputDiagnoseEngine
{
    public static ThroughputDiagnosisResult Diagnose(
        ThroughputGapObservation observation,
        ThroughputIntelligenceOverview overview,
        string intelligencePath)
    {
        var constraint = overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT");
        var factors = new List<string>();

        if (overview.Microstops.Count > 0)
        {
            factors.Add("microstop-accumulation");
        }

        if (overview.StarvedBlocked.Any(s => s.State == "STARVED" || s.State == "BLOCKED"))
        {
            factors.Add("flow-interruption");
        }

        if (overview.LossTree.Nodes.Any(n => n.Category.Contains("OPERATOR", StringComparison.OrdinalIgnoreCase)))
        {
            factors.Add("operator-wait");
        }

        if (overview.BufferIntelligence.Any(b =>
            b.UtilizationPercent > 80m || !string.Equals(b.FlowImbalance, "BALANCED", StringComparison.OrdinalIgnoreCase)))
        {
            factors.Add("buffer-instability");
        }

        return new ThroughputDiagnosisResult(
            constraint?.StationCode ?? observation.CurrentConstraintStationCode ?? "UNKNOWN",
            constraint?.ConstraintType ?? observation.CurrentConstraintType ?? "UNKNOWN",
            constraint?.Explanation ?? "Throughput constrained by station capacity imbalance.",
            factors,
            ["mp2-constraint", "mp2-loss-tree", "canonical-events"],
            intelligencePath,
            constraint?.DataQuality == "HIGH" ? 0.9m : 0.75m);
    }
}

public static class ThroughputInterventionSelectionEngine
{
    public static ThroughputInterventionSelection Select(
        IReadOnlyList<ThroughputInterventionCandidate> candidates,
        ThroughputGapObservation observation)
    {
        var eligible = candidates
            .Where(c => c.Eligibility == "ELIGIBLE" || c.Eligibility == "ELIGIBLE_WITH_ASSUMPTIONS")
            .OrderByDescending(c => Score(c, observation))
            .ToList();

        if (eligible.Count == 0)
        {
            throw new ThroughputOptimizerException(
                ThroughputOptimizerFailureCodes.ValidationFailed,
                "No eligible intervention candidates.");
        }

        var selected = eligible[0];
        return new ThroughputInterventionSelection(
            selected,
            eligible.Skip(1).Take(3).ToList(),
            $"Highest scored intervention balancing throughput gain, quality, and risk.");
    }

    private static decimal Score(ThroughputInterventionCandidate candidate, ThroughputGapObservation observation)
    {
        var gapRecovery = Math.Min(candidate.DeltaGoodThroughputPerHour, observation.GapPerHour);
        return gapRecovery
            - (candidate.QualityImpactScore * 10m)
            - (candidate.OperationalRiskScore * 15m)
            - (candidate.RequiresApproval ? 2m : 0m);
    }
}

public static class VerifiedImprovementEngine
{
    public static string Evaluate(
        VerifiedImprovementBaseline baseline,
        decimal targetImprovementPerHour,
        VerifiedImprovementActual? actual,
        bool observationComplete,
        bool qualityGuardPassed,
        bool safetyGuardPassed)
    {
        if (!observationComplete)
        {
            return ThroughputOptimizerVerificationOutcomes.InsufficientObservation;
        }

        if (!qualityGuardPassed)
        {
            return ThroughputOptimizerVerificationOutcomes.QualityGuardFailed;
        }

        if (!safetyGuardPassed)
        {
            return ThroughputOptimizerVerificationOutcomes.SafetyGuardFailed;
        }

        if (actual is null)
        {
            return ThroughputOptimizerVerificationOutcomes.InsufficientObservation;
        }

        var improvement = actual.GoodThroughputPerHour - baseline.GoodThroughputPerHour;
        if (actual.GoodThroughputPerHour < baseline.GoodThroughputPerHour - 5m)
        {
            return ThroughputOptimizerVerificationOutcomes.Regression;
        }

        if (improvement >= targetImprovementPerHour * 0.9m)
        {
            return ThroughputOptimizerVerificationOutcomes.VerifiedImprovement;
        }

        if (improvement > 0)
        {
            return ThroughputOptimizerVerificationOutcomes.PartialImprovement;
        }

        return ThroughputOptimizerVerificationOutcomes.NoImprovement;
    }

    public static VerifiedImprovementBaseline FromOverview(ThroughputIntelligenceOverview overview)
    {
        var constraint = overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT");
        var wip = overview.BufferIntelligence.Sum(b => b.CurrentWip);
        return new VerifiedImprovementBaseline(
            overview.Measurement.GoodUnitsPerHour,
            overview.Measurement.GoodCount,
            overview.Measurement.RejectCount,
            overview.Measurement.ReworkCount,
            wip,
            constraint?.StationCode ?? "UNKNOWN",
            constraint?.ConstraintType ?? "UNKNOWN");
    }

    public static VerifiedImprovementActual FromOverviewAfter(ThroughputIntelligenceOverview overview) =>
        new(
            overview.Measurement.GoodUnitsPerHour,
            overview.Measurement.GoodCount,
            overview.Measurement.RejectCount,
            overview.Measurement.ReworkCount,
            overview.BufferIntelligence.Sum(b => b.CurrentWip),
            overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT")?.StationCode ?? "UNKNOWN",
            overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT")?.ConstraintType ?? "UNKNOWN");
}

public static class VerifiedDatasetGovernanceEngine
{
    public static (string Status, string Reason, bool ExportEligible) Evaluate(
        string verificationOutcome,
        bool evidenceComplete,
        bool tenantPolicyAllows,
        bool hasUnresolvedConflict)
    {
        if (!tenantPolicyAllows)
        {
            return (VerifiedDatasetEligibilityStatuses.Ineligible, "tenant-policy-denied", false);
        }

        if (hasUnresolvedConflict)
        {
            return (VerifiedDatasetEligibilityStatuses.Ineligible, "unresolved-conflict", false);
        }

        if (!evidenceComplete)
        {
            return (VerifiedDatasetEligibilityStatuses.Ineligible, "insufficient-evidence", false);
        }

        if (verificationOutcome != ThroughputOptimizerVerificationOutcomes.VerifiedImprovement)
        {
            return (VerifiedDatasetEligibilityStatuses.Ineligible, "unverified-outcome", false);
        }

        return (VerifiedDatasetEligibilityStatuses.Eligible, string.Empty, true);
    }
}
