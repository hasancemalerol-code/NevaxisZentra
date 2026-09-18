namespace NevaxisZentra.Persistence.CommercialIntelligence;

public sealed record CommercialProfitabilityCostComponentCandidate(
    Guid ObservationId,
    string ComponentKind,
    decimal UnitCost,
    string CurrencyCode,
    string UnitOfMeasure,
    string CostBasis,
    string? AllocationBasis,
    DateTimeOffset EvidenceTimestampUtc,
    string SourceKind,
    string SourceReference,
    string EvidenceStatus,
    string InclusionStatus,
    string? ExclusionReason);

public sealed record CommercialProfitabilityCostEvidenceReadResult(
    int CandidateCostComponentCount,
    IReadOnlyList<CommercialProfitabilityCostComponentCandidate> Included,
    IReadOnlyList<CommercialProfitabilityCostComponentCandidate> Excluded);

public sealed record CommercialProfitabilityDriverDraft(
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedComponentCount,
    string SupportingText);

public sealed record CommercialProfitabilityEvaluationOutput(
    string Outcome,
    string EvidenceStatus,
    string CostCoverageStatus,
    string? LeadingDriverCode,
    decimal ComparableNetSalesUnitPrice,
    decimal TotalIncludedUnitCost,
    decimal UnitContribution,
    decimal? ContributionMarginRatio,
    decimal? MarkupRatio,
    int CandidateCostComponentCount,
    int IncludedCostComponentCount,
    int ExcludedCostComponentCount,
    int RequiredCostComponentCount,
    int AvailableRequiredCostComponentCount,
    int MissingRequiredCostComponentCount,
    string MissingRequiredComponentKinds,
    IReadOnlyList<CommercialProfitabilityCostComponentCandidate> ComponentSnapshots,
    IReadOnlyList<CommercialProfitabilityDriverDraft> Drivers);

public static class CommercialProfitabilityEvaluator
{
    private static readonly string[] DriverOrder =
    [
        CommercialProfitabilityDriverReasonCodes.SourceConflict,
        CommercialProfitabilityDriverReasonCodes.InvalidAllocationBasis,
        CommercialProfitabilityDriverReasonCodes.IncompatiblePriceBasis,
        CommercialProfitabilityDriverReasonCodes.IncompatibleCurrency,
        CommercialProfitabilityDriverReasonCodes.IncompatibleUnit,
        CommercialProfitabilityDriverReasonCodes.StaleSalesEvidence,
        CommercialProfitabilityDriverReasonCodes.StaleCostEvidence,
        CommercialProfitabilityDriverReasonCodes.MissingDirectMaterialCost,
        CommercialProfitabilityDriverReasonCodes.MissingDirectLaborCost,
        CommercialProfitabilityDriverReasonCodes.MissingMachineCost,
        CommercialProfitabilityDriverReasonCodes.MissingEnergyCost,
        CommercialProfitabilityDriverReasonCodes.MissingOtherRequiredCost,
        CommercialProfitabilityDriverReasonCodes.IncompleteCostCoverage,
        CommercialProfitabilityDriverReasonCodes.NegativeUnitContribution,
        CommercialProfitabilityDriverReasonCodes.ContributionMarginBelowThreshold,
        CommercialProfitabilityDriverReasonCodes.WithinProfile,
    ];

    public static decimal ComputeTotalIncludedUnitCost(IReadOnlyList<CommercialProfitabilityCostComponentCandidate> included) =>
        included.Sum(c => c.UnitCost);

    public static decimal ComputeUnitContribution(decimal comparableNetSalesUnitPrice, decimal totalIncludedUnitCost) =>
        comparableNetSalesUnitPrice - totalIncludedUnitCost;

    public static decimal? ComputeContributionMarginRatio(decimal unitContribution, decimal comparableNetSalesUnitPrice)
    {
        if (comparableNetSalesUnitPrice <= 0m)
        {
            return null;
        }

        var ratio = unitContribution / comparableNetSalesUnitPrice;
        return ratio >= 1m ? null : ratio;
    }

    public static decimal? ComputeMarkupRatio(decimal unitContribution, decimal totalIncludedUnitCost)
    {
        if (totalIncludedUnitCost <= 0m)
        {
            return null;
        }

        return unitContribution / totalIncludedUnitCost;
    }

    public static CommercialProfitabilityEvaluationOutput Evaluate(
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityEvidenceObservation salesEvidence,
        CommercialProfitabilityCostEvidenceReadResult costEvidence,
        IReadOnlyList<string> requiredKinds,
        DateTimeOffset evaluatedAtUtc)
    {
        var drivers = new List<CommercialProfitabilityDriverDraft>();
        var candidateCount = costEvidence.CandidateCostComponentCount;
        var included = costEvidence.Included;
        var excluded = costEvidence.Excluded;
        var includedCount = included.Count;
        var excludedCount = excluded.Count;
        var requiredCount = requiredKinds.Count;
        var unit = salesEvidence.UnitOfMeasure;
        var currency = salesEvidence.CurrencyCode;

        if (salesEvidence.EvidenceStatus == CommercialProfitabilityEvidenceStatuses.SourceConflict)
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.SourceConflict,
                "critical",
                salesEvidence.ComparableNetSalesUnitPrice,
                null,
                unit,
                null,
                0,
                "Sales evidence is marked source-conflict and cannot be evaluated."));
            return BuildOutput(
                CommercialProfitabilityAssessmentOutcomes.SourceConflict,
                CommercialProfitabilityEvidenceStatuses.SourceConflict,
                CommercialProfitabilityCostCoverageStatuses.SourceConflict,
                drivers,
                salesEvidence.ComparableNetSalesUnitPrice ?? 0m,
                0m,
                requiredCount,
                0,
                requiredCount,
                string.Join(",", requiredKinds),
                included,
                excluded);
        }

        if (!salesEvidence.ComparableNetSalesUnitPrice.HasValue || salesEvidence.ComparableNetSalesUnitPrice.Value <= 0m)
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.IncompleteCostCoverage,
                "critical",
                salesEvidence.ComparableNetSalesUnitPrice,
                0m,
                unit,
                null,
                0,
                "Comparable net sales unit price must be positive."));
            return BuildOutput(
                CommercialProfitabilityAssessmentOutcomes.InsufficientData,
                CommercialProfitabilityEvidenceStatuses.InsufficientData,
                CommercialProfitabilityCostCoverageStatuses.InsufficientCostEvidence,
                drivers,
                0m,
                0m,
                requiredCount,
                0,
                requiredCount,
                string.Join(",", requiredKinds),
                included,
                excluded);
        }

        var salesPrice = salesEvidence.ComparableNetSalesUnitPrice.Value;
        AppendExclusionDrivers(drivers, excluded, unit);

        if (IsSalesEvidenceStale(salesEvidence, profile, evaluatedAtUtc))
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.StaleSalesEvidence,
                "high",
                (decimal)(evaluatedAtUtc - salesEvidence.EvidenceTimestampUtc).TotalDays,
                profile.MaximumEvidenceAgeDays,
                "days",
                null,
                0,
                "Sales evidence exceeded the profile maximum evidence age."));
        }

        var staleCostCount = excluded.Count(c =>
            c.ExclusionReason == CommercialProfitabilityExclusionReasonCodes.StaleEvidence);
        if (staleCostCount > 0)
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.StaleCostEvidence,
                "high",
                staleCostCount,
                profile.MaximumEvidenceAgeDays,
                "days",
                null,
                staleCostCount,
                "One or more cost components exceeded the profile maximum evidence age."));
        }

        if (excluded.Any(c => c.EvidenceStatus == CommercialProfitabilityEvidenceStatuses.SourceConflict)
            || excluded.Any(c => c.ExclusionReason == CommercialProfitabilityExclusionReasonCodes.SourceConflict))
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.SourceConflict,
                "critical",
                salesPrice,
                null,
                unit,
                null,
                excluded.Count(c => c.ExclusionReason == CommercialProfitabilityExclusionReasonCodes.SourceConflict),
                "Cost evidence contains source-conflict that prevents trustworthy calculation."));
            return BuildOutput(
                CommercialProfitabilityAssessmentOutcomes.SourceConflict,
                CommercialProfitabilityEvidenceStatuses.SourceConflict,
                CommercialProfitabilityCostCoverageStatuses.SourceConflict,
                drivers,
                salesPrice,
                ComputeTotalIncludedUnitCost(included),
                requiredCount,
                CountAvailableRequiredKinds(included, requiredKinds),
                CountMissingRequiredKinds(included, requiredKinds),
                string.Join(",", GetMissingRequiredKinds(included, requiredKinds)),
                included,
                excluded);
        }

        var availableRequired = CountAvailableRequiredKinds(included, requiredKinds);
        var missingKinds = GetMissingRequiredKinds(included, requiredKinds);
        var missingCount = missingKinds.Count;
        AppendMissingKindDrivers(drivers, missingKinds);

        var totalCost = ComputeTotalIncludedUnitCost(included);
        var contribution = ComputeUnitContribution(salesPrice, totalCost);
        var margin = ComputeContributionMarginRatio(contribution, salesPrice);
        var markup = ComputeMarkupRatio(contribution, totalCost);

        var coverageStatus = ResolveCoverageStatus(requiredCount, availableRequired, missingCount, excludedCount);

        if (missingCount > 0 || coverageStatus != CommercialProfitabilityCostCoverageStatuses.CompleteRequiredCostCoverage)
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.IncompleteCostCoverage,
                "high",
                availableRequired,
                requiredCount,
                "components",
                null,
                missingCount,
                "Required cost component coverage is incomplete."));
        }

        if (contribution < 0m)
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.NegativeUnitContribution,
                "high",
                contribution,
                0m,
                unit,
                margin,
                includedCount,
                "Unit contribution is negative."));
        }
        else if (margin.HasValue && margin.Value < profile.MinimumContributionMarginRatio)
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.ContributionMarginBelowThreshold,
                "moderate",
                margin,
                profile.MinimumContributionMarginRatio,
                "ratio",
                margin - profile.MinimumContributionMarginRatio,
                includedCount,
                "Contribution margin ratio is below the profile minimum threshold."));
        }

        if (missingCount == 0
            && contribution >= 0m
            && margin.HasValue
            && margin.Value >= profile.MinimumContributionMarginRatio
            && !HasBlockingDrivers(drivers))
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.WithinProfile,
                "low",
                margin,
                profile.MinimumContributionMarginRatio,
                "ratio",
                margin - profile.MinimumContributionMarginRatio,
                includedCount,
                "Contribution margin and cost coverage are within profile thresholds."));
        }

        var outcome = ResolveOutcome(drivers, missingCount, contribution, margin, profile.MinimumContributionMarginRatio);
        var evidenceStatus = ResolveEvidenceStatus(salesEvidence, outcome);

        return BuildOutput(
            outcome,
            evidenceStatus,
            coverageStatus,
            drivers,
            salesPrice,
            totalCost,
            requiredCount,
            availableRequired,
            missingCount,
            string.Join(",", missingKinds),
            included,
            excluded,
            contribution,
            margin,
            markup);
    }

    public static IReadOnlyList<string> ParseRequiredKinds(string requiredCostComponentKinds) =>
        requiredCostComponentKinds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<CommercialProfitabilityDriverDraft> OrderDrivers(
        IReadOnlyList<CommercialProfitabilityDriverDraft> drivers) =>
        drivers
            .OrderByDescending(d => OutcomeSeverityRank(d.Severity))
            .ThenByDescending(d => Math.Abs(d.NormalizedVariance ?? 0m))
            .ThenByDescending(d => d.AffectedComponentCount)
            .ThenBy(d => Array.IndexOf(DriverOrder, d.ReasonCode))
            .ToList();

    private static string ResolveOutcome(
        IReadOnlyList<CommercialProfitabilityDriverDraft> drivers,
        int missingRequiredCount,
        decimal unitContribution,
        decimal? marginRatio,
        decimal minimumMarginRatio)
    {
        if (drivers.Any(d => d.ReasonCode == CommercialProfitabilityDriverReasonCodes.SourceConflict))
        {
            return CommercialProfitabilityAssessmentOutcomes.SourceConflict;
        }

        if (missingRequiredCount > 0
            || drivers.Any(d => d.ReasonCode == CommercialProfitabilityDriverReasonCodes.IncompleteCostCoverage
                && d.Severity == "critical"))
        {
            return CommercialProfitabilityAssessmentOutcomes.InsufficientData;
        }

        if (drivers.Any(d => d.ReasonCode is CommercialProfitabilityDriverReasonCodes.StaleSalesEvidence
                or CommercialProfitabilityDriverReasonCodes.StaleCostEvidence))
        {
            return CommercialProfitabilityAssessmentOutcomes.InsufficientData;
        }

        if (unitContribution < 0m)
        {
            return CommercialProfitabilityAssessmentOutcomes.NegativeContribution;
        }

        if (missingRequiredCount == 0
            && marginRatio.HasValue
            && marginRatio.Value < minimumMarginRatio)
        {
            return CommercialProfitabilityAssessmentOutcomes.BelowThreshold;
        }

        if (missingRequiredCount > 0)
        {
            return CommercialProfitabilityAssessmentOutcomes.InsufficientData;
        }

        return CommercialProfitabilityAssessmentOutcomes.WithinThreshold;
    }

    private static string ResolveCoverageStatus(
        int requiredCount,
        int availableRequired,
        int missingCount,
        int excludedCount)
    {
        if (missingCount > 0 && availableRequired == 0 && excludedCount > 0)
        {
            return CommercialProfitabilityCostCoverageStatuses.InsufficientCostEvidence;
        }

        if (missingCount > 0)
        {
            return CommercialProfitabilityCostCoverageStatuses.PartialCostCoverage;
        }

        return availableRequired >= requiredCount
            ? CommercialProfitabilityCostCoverageStatuses.CompleteRequiredCostCoverage
            : CommercialProfitabilityCostCoverageStatuses.PartialCostCoverage;
    }

    private static string ResolveEvidenceStatus(
        CommercialProfitabilityEvidenceObservation salesEvidence,
        string outcome)
    {
        if (outcome == CommercialProfitabilityAssessmentOutcomes.SourceConflict)
        {
            return CommercialProfitabilityEvidenceStatuses.SourceConflict;
        }

        if (outcome == CommercialProfitabilityAssessmentOutcomes.InsufficientData)
        {
            return CommercialProfitabilityEvidenceStatuses.InsufficientData;
        }

        return salesEvidence.EvidenceStatus == CommercialProfitabilityEvidenceStatuses.Simulated
            ? CommercialProfitabilityEvidenceStatuses.Simulated
            : salesEvidence.EvidenceStatus;
    }

    private static bool IsSalesEvidenceStale(
        CommercialProfitabilityEvidenceObservation salesEvidence,
        CommercialProfitabilityProfile profile,
        DateTimeOffset evaluatedAtUtc) =>
        (evaluatedAtUtc - salesEvidence.EvidenceTimestampUtc).TotalDays > profile.MaximumEvidenceAgeDays;

    private static int CountAvailableRequiredKinds(
        IReadOnlyList<CommercialProfitabilityCostComponentCandidate> included,
        IReadOnlyList<string> requiredKinds) =>
        requiredKinds.Count(kind => included.Any(c =>
            string.Equals(c.ComponentKind, kind, StringComparison.Ordinal)));

    private static int CountMissingRequiredKinds(
        IReadOnlyList<CommercialProfitabilityCostComponentCandidate> included,
        IReadOnlyList<string> requiredKinds) =>
        GetMissingRequiredKinds(included, requiredKinds).Count;

    private static List<string> GetMissingRequiredKinds(
        IReadOnlyList<CommercialProfitabilityCostComponentCandidate> included,
        IReadOnlyList<string> requiredKinds) =>
        requiredKinds
            .Where(kind => included.All(c => !string.Equals(c.ComponentKind, kind, StringComparison.Ordinal)))
            .ToList();

    private static void AppendMissingKindDrivers(List<CommercialProfitabilityDriverDraft> drivers, IReadOnlyList<string> missingKinds)
    {
        foreach (var kind in missingKinds)
        {
            var reasonCode = kind switch
            {
                CommercialProfitabilityCostComponentKinds.DirectMaterial =>
                    CommercialProfitabilityDriverReasonCodes.MissingDirectMaterialCost,
                CommercialProfitabilityCostComponentKinds.DirectLabor =>
                    CommercialProfitabilityDriverReasonCodes.MissingDirectLaborCost,
                CommercialProfitabilityCostComponentKinds.Machine =>
                    CommercialProfitabilityDriverReasonCodes.MissingMachineCost,
                CommercialProfitabilityCostComponentKinds.Energy =>
                    CommercialProfitabilityDriverReasonCodes.MissingEnergyCost,
                _ => CommercialProfitabilityDriverReasonCodes.MissingOtherRequiredCost,
            };

            drivers.Add(Draft(
                reasonCode,
                "high",
                0m,
                1m,
                "components",
                null,
                1,
                $"Required cost component kind '{kind}' has no compatible included evidence."));
        }
    }

    private static void AppendExclusionDrivers(
        List<CommercialProfitabilityDriverDraft> drivers,
        IReadOnlyList<CommercialProfitabilityCostComponentCandidate> excluded,
        string unit)
    {
        var grouped = excluded
            .Where(c => c.ExclusionReason is not null)
            .GroupBy(c => c.ExclusionReason!, StringComparer.Ordinal);

        foreach (var group in grouped.OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var driverCode = group.Key switch
            {
                CommercialProfitabilityExclusionReasonCodes.IncompatibleCurrency =>
                    CommercialProfitabilityDriverReasonCodes.IncompatibleCurrency,
                CommercialProfitabilityExclusionReasonCodes.IncompatibleUnit =>
                    CommercialProfitabilityDriverReasonCodes.IncompatibleUnit,
                CommercialProfitabilityExclusionReasonCodes.IncompatiblePriceBasis
                    or CommercialProfitabilityExclusionReasonCodes.IncompatibleGrossNetBasis =>
                    CommercialProfitabilityDriverReasonCodes.IncompatiblePriceBasis,
                CommercialProfitabilityExclusionReasonCodes.InvalidAllocationBasis
                    or CommercialProfitabilityExclusionReasonCodes.MissingAllocationBasis =>
                    CommercialProfitabilityDriverReasonCodes.InvalidAllocationBasis,
                _ => null,
            };

            if (driverCode is null)
            {
                continue;
            }

            drivers.Add(Draft(
                driverCode,
                "moderate",
                null,
                null,
                unit,
                null,
                group.Count(),
                $"Excluded {group.Count()} cost component(s) for {group.Key}."));
        }
    }

    private static bool HasBlockingDrivers(IReadOnlyList<CommercialProfitabilityDriverDraft> drivers) =>
        drivers.Any(d => d.ReasonCode is CommercialProfitabilityDriverReasonCodes.SourceConflict
            or CommercialProfitabilityDriverReasonCodes.IncompleteCostCoverage
            or CommercialProfitabilityDriverReasonCodes.NegativeUnitContribution
            or CommercialProfitabilityDriverReasonCodes.ContributionMarginBelowThreshold
            or CommercialProfitabilityDriverReasonCodes.StaleSalesEvidence);

    private static CommercialProfitabilityEvaluationOutput BuildOutput(
        string outcome,
        string evidenceStatus,
        string costCoverageStatus,
        List<CommercialProfitabilityDriverDraft> drivers,
        decimal salesPrice,
        decimal totalCost,
        int requiredCount,
        int availableRequired,
        int missingCount,
        string missingKinds,
        IReadOnlyList<CommercialProfitabilityCostComponentCandidate> included,
        IReadOnlyList<CommercialProfitabilityCostComponentCandidate> excluded,
        decimal? contribution = null,
        decimal? margin = null,
        decimal? markup = null)
    {
        var contributionValue = contribution ?? ComputeUnitContribution(salesPrice, totalCost);
        var marginValue = margin ?? ComputeContributionMarginRatio(contributionValue, salesPrice);
        var markupValue = markup ?? ComputeMarkupRatio(contributionValue, totalCost);
        var candidateCount = included.Count + excluded.Count;

        if (drivers.Count == 0)
        {
            drivers.Add(Draft(
                CommercialProfitabilityDriverReasonCodes.WithinProfile,
                "low",
                marginValue,
                null,
                null,
                marginValue,
                included.Count,
                "Assessment completed."));
        }

        var ordered = OrderDrivers(drivers);
        var snapshots = included
            .Select(c => c with { InclusionStatus = CommercialProfitabilityComponentInclusionStatuses.Included })
            .Concat(excluded.Select(c => c with
            {
                InclusionStatus = CommercialProfitabilityComponentInclusionStatuses.Excluded,
            }))
            .ToList();

        return new CommercialProfitabilityEvaluationOutput(
            outcome,
            evidenceStatus,
            costCoverageStatus,
            ordered[0].ReasonCode,
            salesPrice,
            totalCost,
            contributionValue,
            marginValue,
            markupValue,
            candidateCount,
            included.Count,
            excluded.Count,
            requiredCount,
            availableRequired,
            missingCount,
            missingKinds,
            snapshots,
            ordered);
    }

    private static CommercialProfitabilityDriverDraft Draft(
        string reasonCode,
        string severity,
        decimal? actualValue,
        decimal? thresholdValue,
        string? unit,
        decimal? normalizedVariance,
        int affectedComponentCount,
        string supportingText) =>
        new(reasonCode, severity, actualValue, thresholdValue, unit, normalizedVariance, affectedComponentCount, supportingText);

    private static int OutcomeSeverityRank(string severity) =>
        severity switch
        {
            "critical" => 4,
            "high" => 3,
            "moderate" => 2,
            "low" => 1,
            _ => 0,
        };
}
