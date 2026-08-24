namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialPurchasePriceComparisonDriverDraft(
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ReferenceValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedObservationCount,
    string SupportingText,
    Guid? ComparisonResultId = null);

public sealed record CommercialPurchasePriceComparisonResultDraft(
    string SupplierReference,
    int ComparableObservationCount,
    decimal? SupplierMedianUnitPrice,
    decimal? LowestComparableMedianUnitPrice,
    decimal? RelativePricePositionRatio,
    int PriceRank,
    bool IsTied,
    string PositionState,
    string EvidenceStatus);

public sealed record CommercialPurchasePriceComparisonEvaluationOutput(
    string ComparisonOutcome,
    string EvidenceStatus,
    string? LeadingDriverCode,
    decimal? CurrentMedianUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? SignedVarianceRatio,
    decimal? SupplierSpreadRatio,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    IReadOnlyList<CommercialPurchasePriceComparisonResultDraft> SupplierResults,
    IReadOnlyList<CommercialPurchasePriceComparisonDriverDraft> Drivers);

public static class CommercialPurchasePriceComparisonEvaluator
{
    private static readonly string[] DriverOrder =
    [
        CommercialIntelligenceComparisonDriverReasonCodes.SourceConflict,
        CommercialIntelligenceComparisonDriverReasonCodes.IncompatiblePriceBasis,
        CommercialIntelligenceComparisonDriverReasonCodes.IncompatibleCurrency,
        CommercialIntelligenceComparisonDriverReasonCodes.IncompatibleUnit,
        CommercialIntelligenceComparisonDriverReasonCodes.DuplicateSourceEvidence,
        CommercialIntelligenceComparisonDriverReasonCodes.InsufficientCurrentWindow,
        CommercialIntelligenceComparisonDriverReasonCodes.InsufficientReferenceWindow,
        CommercialIntelligenceComparisonDriverReasonCodes.InsufficientSupplierObservations,
        CommercialIntelligenceComparisonDriverReasonCodes.SingleComparableSupplier,
        CommercialIntelligenceComparisonDriverReasonCodes.MissingReferenceMedian,
        CommercialIntelligenceComparisonDriverReasonCodes.CurrentWindowPriceIncrease,
        CommercialIntelligenceComparisonDriverReasonCodes.SupplierPriceSpread,
        CommercialIntelligenceComparisonDriverReasonCodes.SupplierHigherComparableMedian,
        CommercialIntelligenceComparisonDriverReasonCodes.SupplierMedianTie,
        CommercialIntelligenceComparisonDriverReasonCodes.SupplierLowestComparableMedian,
        CommercialIntelligenceComparisonDriverReasonCodes.CurrentWindowFavorableVariance,
        CommercialIntelligenceComparisonDriverReasonCodes.WithinReferenceRange,
    ];

    public static CommercialPurchasePriceComparisonEvaluationOutput EvaluateTemporalDeviation(
        CommercialPurchasePriceProfile profile,
        string normalizedUnit,
        CommercialPurchasePriceEvidenceReadResult currentEvidence,
        CommercialPurchasePriceEvidenceReadResult referenceEvidence,
        IReadOnlyDictionary<string, int> currentExclusionCounts,
        IReadOnlyDictionary<string, int> referenceExclusionCounts)
    {
        var drivers = new List<CommercialPurchasePriceComparisonDriverDraft>();
        var excludedCount = currentEvidence.Excluded.Count + referenceEvidence.Excluded.Count;
        var combinedExclusions = MergeExclusionCounts(currentExclusionCounts, referenceExclusionCounts);
        AppendExclusionDrivers(drivers, combinedExclusions, normalizedUnit);

        if (HasConflictExclusions(combinedExclusions))
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.SourceConflict,
                "critical",
                null,
                null,
                null,
                normalizedUnit,
                null,
                excludedCount,
                "Comparable evidence could not be normalized without mixing incompatible commercial bases."));
            return Output(
                CommercialIntelligenceComparisonOutcomes.SourceConflict,
                CommercialIntelligenceEvidenceStatuses.SourceConflict,
                drivers,
                null,
                null,
                null,
                null,
                currentEvidence.Comparable.Count + referenceEvidence.Comparable.Count,
                excludedCount,
                []);
        }

        var currentCount = currentEvidence.Comparable.Count;
        var referenceCount = referenceEvidence.Comparable.Count;
        var minimum = profile.MinimumComparableObservations;

        if (currentCount < minimum)
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.InsufficientCurrentWindow,
                "info",
                currentCount,
                minimum,
                null,
                normalizedUnit,
                null,
                currentCount,
                $"Current window comparable observation count {currentCount} is below profile minimum {minimum}."));
        }

        if (referenceCount < minimum)
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.InsufficientReferenceWindow,
                "info",
                referenceCount,
                minimum,
                null,
                normalizedUnit,
                null,
                referenceCount,
                $"Reference window comparable observation count {referenceCount} is below profile minimum {minimum}."));
        }

        if (currentCount < minimum || referenceCount < minimum)
        {
            return Output(
                CommercialIntelligenceComparisonOutcomes.InsufficientData,
                CommercialIntelligenceEvidenceStatuses.InsufficientData,
                drivers,
                null,
                null,
                null,
                null,
                currentCount + referenceCount,
                excludedCount,
                []);
        }

        var currentMedian = CommercialPurchasePriceEvaluator.ComputeMedian(
            currentEvidence.Comparable.Select(o => o.UnitPrice).ToList());
        var referenceMedian = CommercialPurchasePriceEvaluator.ComputeMedian(
            referenceEvidence.Comparable.Select(o => o.UnitPrice).ToList());

        if (!referenceMedian.HasValue || referenceMedian.Value == 0m)
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.MissingReferenceMedian,
                "moderate",
                currentMedian,
                referenceMedian,
                null,
                normalizedUnit,
                null,
                referenceCount,
                "Reference window median unit price is unavailable."));
            return Output(
                CommercialIntelligenceComparisonOutcomes.InsufficientData,
                CommercialIntelligenceEvidenceStatuses.InsufficientData,
                drivers,
                currentMedian,
                referenceMedian,
                null,
                null,
                currentCount + referenceCount,
                excludedCount,
                []);
        }

        var signedVarianceRatio = CommercialPurchasePriceEvaluator.ComputeSignedVarianceRatio(
            currentMedian!.Value,
            referenceMedian);

        if (signedVarianceRatio.HasValue && signedVarianceRatio.Value > profile.MaximumPositiveVarianceRatio)
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.CurrentWindowPriceIncrease,
                "high",
                currentMedian,
                referenceMedian,
                profile.MaximumPositiveVarianceRatio,
                normalizedUnit,
                signedVarianceRatio,
                currentCount,
                "Current window median exceeds the approved reference variance band."));
        }
        else if (signedVarianceRatio.HasValue && signedVarianceRatio.Value < 0m)
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.CurrentWindowFavorableVariance,
                "low",
                currentMedian,
                referenceMedian,
                profile.MaximumPositiveVarianceRatio,
                normalizedUnit,
                signedVarianceRatio,
                currentCount,
                "Current window median is lower than the reference window median."));
        }
        else
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.WithinReferenceRange,
                "low",
                currentMedian,
                referenceMedian,
                profile.MaximumPositiveVarianceRatio,
                normalizedUnit,
                signedVarianceRatio,
                currentCount + referenceCount,
                "Current window median is within the approved reference range."));
        }

        var outcome = ResolveTemporalOutcome(drivers, signedVarianceRatio, profile.MaximumPositiveVarianceRatio);
        return Output(
            outcome,
            CommercialIntelligenceEvidenceStatuses.Simulated,
            drivers,
            currentMedian,
            referenceMedian,
            signedVarianceRatio,
            null,
            currentCount + referenceCount,
            excludedCount,
            []);
    }

    public static CommercialPurchasePriceComparisonEvaluationOutput EvaluateSupplierPricePosition(
        CommercialPurchasePriceProfile profile,
        string normalizedUnit,
        CommercialPurchasePriceEvidenceReadResult evidence,
        IReadOnlyDictionary<string, int> exclusionCounts)
    {
        var drivers = new List<CommercialPurchasePriceComparisonDriverDraft>();
        var excludedCount = evidence.Excluded.Count;
        AppendExclusionDrivers(drivers, exclusionCounts, normalizedUnit);

        if (HasConflictExclusions(exclusionCounts))
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.SourceConflict,
                "critical",
                null,
                null,
                null,
                normalizedUnit,
                null,
                excludedCount,
                "Comparable evidence could not be normalized without mixing incompatible commercial bases."));
            return Output(
                CommercialIntelligenceComparisonOutcomes.SourceConflict,
                CommercialIntelligenceEvidenceStatuses.SourceConflict,
                drivers,
                null,
                null,
                null,
                null,
                evidence.Comparable.Count,
                excludedCount,
                []);
        }

        var supplierGroups = evidence.Comparable
            .Where(o => !string.IsNullOrWhiteSpace(o.SupplierReference))
            .GroupBy(o => o.SupplierReference!, StringComparer.Ordinal)
            .Select(g => new
            {
                SupplierReference = g.Key,
                Observations = g.ToList(),
                Median = CommercialPurchasePriceEvaluator.ComputeMedian(g.Select(o => o.UnitPrice).ToList()),
            })
            .Where(g => g.Median.HasValue)
            .ToList();

        var eligibleSuppliers = supplierGroups
            .Where(g => g.Observations.Count >= profile.MinimumComparableObservations)
            .ToList();

        if (eligibleSuppliers.Count < 2)
        {
            if (supplierGroups.Count == 1)
            {
                drivers.Add(Draft(
                    CommercialIntelligenceComparisonDriverReasonCodes.SingleComparableSupplier,
                    "info",
                    1,
                    2,
                    null,
                    normalizedUnit,
                    null,
                    supplierGroups[0].Observations.Count,
                    "Only one supplier has comparable observations in the analysis window."));
            }
            else
            {
                drivers.Add(Draft(
                    CommercialIntelligenceComparisonDriverReasonCodes.InsufficientSupplierObservations,
                    "info",
                    eligibleSuppliers.Count,
                    profile.MinimumComparableObservations,
                    null,
                    normalizedUnit,
                    null,
                    evidence.Comparable.Count,
                    "Fewer than two suppliers meet the minimum comparable observation threshold."));
            }

            return Output(
                CommercialIntelligenceComparisonOutcomes.InsufficientData,
                CommercialIntelligenceEvidenceStatuses.InsufficientData,
                drivers,
                null,
                null,
                null,
                null,
                evidence.Comparable.Count,
                excludedCount,
                []);
        }

        var lowestMedian = eligibleSuppliers.Min(g => g.Median!.Value);
        var highestMedian = eligibleSuppliers.Max(g => g.Median!.Value);
        decimal? supplierSpreadRatio = lowestMedian == 0m
            ? null
            : (highestMedian - lowestMedian) / lowestMedian;

        if (profile.SupplierSpreadThreshold.HasValue
            && supplierSpreadRatio.HasValue
            && supplierSpreadRatio.Value > profile.SupplierSpreadThreshold.Value)
        {
            drivers.Add(Draft(
                CommercialIntelligenceComparisonDriverReasonCodes.SupplierPriceSpread,
                "moderate",
                supplierSpreadRatio,
                lowestMedian,
                profile.SupplierSpreadThreshold,
                normalizedUnit,
                supplierSpreadRatio,
                eligibleSuppliers.Count,
                "Supplier comparable medians diverge beyond the approved spread threshold."));
        }

        var rankedSuppliers = RankSuppliers(eligibleSuppliers.Select(g => (
            g.SupplierReference,
            g.Median!.Value,
            g.Observations.Count)).ToList());

        var tieGroups = rankedSuppliers
            .GroupBy(s => s.Median)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Select(s => s.SupplierReference)
            .ToHashSet(StringComparer.Ordinal);

        var supplierResults = new List<CommercialPurchasePriceComparisonResultDraft>();
        foreach (var ranked in rankedSuppliers)
        {
            var relativePosition = lowestMedian == 0m
                ? (decimal?)null
                : (ranked.Median - lowestMedian) / lowestMedian;
            var isTied = tieGroups.Contains(ranked.SupplierReference);
            var positionState = ResolveSupplierPositionState(
                ranked.Median,
                lowestMedian,
                isTied,
                profile.SupplierSpreadThreshold,
                relativePosition);

            supplierResults.Add(new CommercialPurchasePriceComparisonResultDraft(
                ranked.SupplierReference,
                ranked.ObservationCount,
                ranked.Median,
                lowestMedian,
                relativePosition,
                ranked.Rank,
                isTied,
                positionState,
                CommercialIntelligenceEvidenceStatuses.Simulated));

            if (isTied)
            {
                drivers.Add(Draft(
                    CommercialIntelligenceComparisonDriverReasonCodes.SupplierMedianTie,
                    "low",
                    ranked.Median,
                    lowestMedian,
                    profile.SupplierSpreadThreshold,
                    normalizedUnit,
                    relativePosition,
                    ranked.ObservationCount,
                    $"Supplier {ranked.SupplierReference} has a tied comparable median price."));
            }
            else if (ranked.Median == lowestMedian)
            {
                drivers.Add(Draft(
                    CommercialIntelligenceComparisonDriverReasonCodes.SupplierLowestComparableMedian,
                    "low",
                    ranked.Median,
                    lowestMedian,
                    profile.SupplierSpreadThreshold,
                    normalizedUnit,
                    relativePosition,
                    ranked.ObservationCount,
                    $"Supplier {ranked.SupplierReference} has the lowest comparable observed median price."));
            }
            else if (positionState == CommercialIntelligenceSupplierPositionStates.HigherComparablePrice)
            {
                drivers.Add(Draft(
                    CommercialIntelligenceComparisonDriverReasonCodes.SupplierHigherComparableMedian,
                    "moderate",
                    ranked.Median,
                    lowestMedian,
                    profile.SupplierSpreadThreshold,
                    normalizedUnit,
                    relativePosition,
                    ranked.ObservationCount,
                    $"Supplier {ranked.SupplierReference} has a higher comparable median beyond the spread threshold."));
            }
        }

        var outcome = supplierSpreadRatio.HasValue
                       && profile.SupplierSpreadThreshold.HasValue
                       && supplierSpreadRatio.Value > profile.SupplierSpreadThreshold.Value
            ? CommercialIntelligenceComparisonOutcomes.Attention
            : CommercialIntelligenceComparisonOutcomes.WithinReferenceRange;

        return Output(
            outcome,
            CommercialIntelligenceEvidenceStatuses.Simulated,
            drivers,
            null,
            null,
            null,
            supplierSpreadRatio,
            evidence.Comparable.Count,
            excludedCount,
            supplierResults);
    }

    public static IReadOnlyList<CommercialPurchasePriceComparisonDriverDraft> OrderDrivers(
        IReadOnlyList<CommercialPurchasePriceComparisonDriverDraft> drivers) =>
        drivers
            .OrderByDescending(d => OutcomeSeverityRank(d.Severity))
            .ThenByDescending(d => Math.Abs(d.NormalizedVariance ?? 0m))
            .ThenByDescending(d => d.AffectedObservationCount)
            .ThenBy(d => Array.IndexOf(DriverOrder, d.ReasonCode))
            .ToList();

    private static List<(string SupplierReference, decimal Median, int ObservationCount, int Rank)> RankSuppliers(
        IReadOnlyList<(string SupplierReference, decimal Median, int ObservationCount)> suppliers)
    {
        var ordered = suppliers
            .OrderBy(s => s.Median)
            .ThenByDescending(s => s.ObservationCount)
            .ThenBy(s => s.SupplierReference, StringComparer.Ordinal)
            .ToList();

        var results = new List<(string SupplierReference, decimal Median, int ObservationCount, int Rank)>();
        var rank = 0;
        decimal? previousMedian = null;
        for (var index = 0; index < ordered.Count; index++)
        {
            var item = ordered[index];
            if (previousMedian is null || item.Median != previousMedian.Value)
            {
                rank = index + 1;
            }

            results.Add((item.SupplierReference, item.Median, item.ObservationCount, rank));
            previousMedian = item.Median;
        }

        return results;
    }

    private static string ResolveSupplierPositionState(
        decimal supplierMedian,
        decimal lowestMedian,
        bool isTied,
        decimal? spreadThreshold,
        decimal? relativePosition)
    {
        if (isTied)
        {
            return CommercialIntelligenceSupplierPositionStates.TiedComparablePrice;
        }

        if (supplierMedian == lowestMedian)
        {
            return CommercialIntelligenceSupplierPositionStates.LowestComparablePrice;
        }

        if (spreadThreshold.HasValue
            && relativePosition.HasValue
            && relativePosition.Value > spreadThreshold.Value)
        {
            return CommercialIntelligenceSupplierPositionStates.HigherComparablePrice;
        }

        return CommercialIntelligenceSupplierPositionStates.WithinSpreadThreshold;
    }

    private static string ResolveTemporalOutcome(
        IReadOnlyList<CommercialPurchasePriceComparisonDriverDraft> drivers,
        decimal? signedVarianceRatio,
        decimal maximumPositiveVarianceRatio)
    {
        if (drivers.Any(d => d.ReasonCode == CommercialIntelligenceComparisonDriverReasonCodes.SourceConflict))
        {
            return CommercialIntelligenceComparisonOutcomes.SourceConflict;
        }

        if (drivers.Any(d =>
                d.ReasonCode == CommercialIntelligenceComparisonDriverReasonCodes.InsufficientCurrentWindow
                || d.ReasonCode == CommercialIntelligenceComparisonDriverReasonCodes.InsufficientReferenceWindow
                || d.ReasonCode == CommercialIntelligenceComparisonDriverReasonCodes.MissingReferenceMedian))
        {
            return CommercialIntelligenceComparisonOutcomes.InsufficientData;
        }

        if (signedVarianceRatio.HasValue && signedVarianceRatio.Value > maximumPositiveVarianceRatio)
        {
            return CommercialIntelligenceComparisonOutcomes.Attention;
        }

        if (signedVarianceRatio.HasValue && signedVarianceRatio.Value < 0m)
        {
            return CommercialIntelligenceComparisonOutcomes.FavorableVariance;
        }

        return CommercialIntelligenceComparisonOutcomes.WithinReferenceRange;
    }

    private static void AppendExclusionDrivers(
        List<CommercialPurchasePriceComparisonDriverDraft> drivers,
        IReadOnlyDictionary<string, int> exclusionCounts,
        string unit)
    {
        foreach (var (reason, count) in exclusionCounts.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (count <= 0)
            {
                continue;
            }

            var driverCode = reason switch
            {
                CommercialIntelligenceExclusionReasonCodes.IncompatibleCurrency =>
                    CommercialIntelligenceComparisonDriverReasonCodes.IncompatibleCurrency,
                CommercialIntelligenceExclusionReasonCodes.IncompatibleUnit =>
                    CommercialIntelligenceComparisonDriverReasonCodes.IncompatibleUnit,
                CommercialIntelligenceExclusionReasonCodes.IncompatiblePriceBasis =>
                    CommercialIntelligenceComparisonDriverReasonCodes.IncompatiblePriceBasis,
                CommercialIntelligenceExclusionReasonCodes.DuplicateSourceEvidence =>
                    CommercialIntelligenceComparisonDriverReasonCodes.DuplicateSourceEvidence,
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
                null,
                unit,
                null,
                count,
                $"Excluded {count} observation(s) for {reason}."));
        }
    }

    private static bool HasConflictExclusions(IReadOnlyDictionary<string, int> exclusionCounts)
    {
        var incompatibleTypes = new[]
        {
            CommercialIntelligenceExclusionReasonCodes.IncompatibleCurrency,
            CommercialIntelligenceExclusionReasonCodes.IncompatibleUnit,
            CommercialIntelligenceExclusionReasonCodes.IncompatiblePriceBasis,
        };

        return incompatibleTypes.Count(key => exclusionCounts.GetValueOrDefault(key) > 0) >= 2;
    }

    private static Dictionary<string, int> MergeExclusionCounts(
        IReadOnlyDictionary<string, int> left,
        IReadOnlyDictionary<string, int> right)
    {
        var merged = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (key, value) in left)
        {
            merged[key] = value;
        }

        foreach (var (key, value) in right)
        {
            merged[key] = merged.GetValueOrDefault(key) + value;
        }

        return merged;
    }

    private static CommercialPurchasePriceComparisonEvaluationOutput Output(
        string outcome,
        string evidenceStatus,
        List<CommercialPurchasePriceComparisonDriverDraft> drivers,
        decimal? currentMedian,
        decimal? referenceMedian,
        decimal? signedVarianceRatio,
        decimal? supplierSpreadRatio,
        int comparableCount,
        int excludedCount,
        IReadOnlyList<CommercialPurchasePriceComparisonResultDraft> supplierResults)
    {
        if (drivers.Count == 0)
        {
            return new CommercialPurchasePriceComparisonEvaluationOutput(
                outcome,
                evidenceStatus,
                null,
                currentMedian,
                referenceMedian,
                signedVarianceRatio,
                supplierSpreadRatio,
                comparableCount,
                excludedCount,
                supplierResults,
                []);
        }

        var ordered = OrderDrivers(drivers);
        return new CommercialPurchasePriceComparisonEvaluationOutput(
            outcome,
            evidenceStatus,
            ordered[0].ReasonCode,
            currentMedian,
            referenceMedian,
            signedVarianceRatio,
            supplierSpreadRatio,
            comparableCount,
            excludedCount,
            supplierResults,
            ordered);
    }

    private static CommercialPurchasePriceComparisonDriverDraft Draft(
        string reasonCode,
        string severity,
        decimal? actualValue,
        decimal? referenceValue,
        decimal? thresholdValue,
        string? unit,
        decimal? normalizedVariance,
        int affectedObservationCount,
        string supportingText) =>
        new(reasonCode, severity, actualValue, referenceValue, thresholdValue, unit, normalizedVariance, affectedObservationCount, supportingText);

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
