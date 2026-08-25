namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialSalesPriceComparisonDriverDraft(
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

public sealed record CommercialSalesPriceComparisonResultDraft(
    string CustomerReference,
    int ComparableObservationCount,
    decimal? CustomerMedianUnitPrice,
    decimal? LowestComparableMedianUnitPrice,
    decimal? RelativePricePositionRatio,
    int PriceRank,
    bool IsTied,
    string PositionState,
    string EvidenceStatus);

public sealed record CommercialSalesPriceComparisonEvaluationOutput(
    string ComparisonOutcome,
    string EvidenceStatus,
    string? LeadingDriverCode,
    decimal? CurrentMedianUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? SignedVarianceRatio,
    decimal? CustomerSpreadRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    IReadOnlyList<CommercialSalesPriceComparisonResultDraft> CustomerResults,
    IReadOnlyList<CommercialSalesPriceComparisonDriverDraft> Drivers);

public static class CommercialSalesPriceComparisonEvaluator
{
    private static readonly string[] DriverOrder =
    [
        CommercialSalesPriceComparisonDriverReasonCodes.SourceConflict,
        CommercialSalesPriceComparisonDriverReasonCodes.IncompatiblePriceBasis,
        CommercialSalesPriceComparisonDriverReasonCodes.IncompatibleGrossNetBasis,
        CommercialSalesPriceComparisonDriverReasonCodes.IncompatibleEvidenceKind,
        CommercialSalesPriceComparisonDriverReasonCodes.IncompatibleCurrency,
        CommercialSalesPriceComparisonDriverReasonCodes.IncompatibleUnit,
        CommercialSalesPriceComparisonDriverReasonCodes.DuplicateSourceEvidence,
        CommercialSalesPriceComparisonDriverReasonCodes.InsufficientCurrentWindow,
        CommercialSalesPriceComparisonDriverReasonCodes.InsufficientReferenceWindow,
        CommercialSalesPriceComparisonDriverReasonCodes.InsufficientCustomerObservations,
        CommercialSalesPriceComparisonDriverReasonCodes.SingleComparableCustomer,
        CommercialSalesPriceComparisonDriverReasonCodes.MissingReferenceMedian,
        CommercialSalesPriceComparisonDriverReasonCodes.PositivePriceVariance,
        CommercialSalesPriceComparisonDriverReasonCodes.SalesPriceDecrease,
        CommercialSalesPriceComparisonDriverReasonCodes.CurrentWindowPriceIncrease,
        CommercialSalesPriceComparisonDriverReasonCodes.CustomerPriceSpread,
        CommercialSalesPriceComparisonDriverReasonCodes.CustomerHigherComparableMedian,
        CommercialSalesPriceComparisonDriverReasonCodes.CustomerMedianTie,
        CommercialSalesPriceComparisonDriverReasonCodes.CustomerLowestComparableMedian,
        CommercialSalesPriceComparisonDriverReasonCodes.CurrentWindowFavorableVariance,
        CommercialSalesPriceComparisonDriverReasonCodes.WithinReferenceRange,
    ];

    public static CommercialSalesPriceComparisonEvaluationOutput EvaluateTemporalDeviation(
        CommercialSalesPriceProfile profile,
        string normalizedUnit,
        CommercialSalesPriceEvidenceReadResult currentEvidence,
        CommercialSalesPriceEvidenceReadResult referenceEvidence,
        IReadOnlyDictionary<string, int> currentExclusionCounts,
        IReadOnlyDictionary<string, int> referenceExclusionCounts)
    {
        var drivers = new List<CommercialSalesPriceComparisonDriverDraft>();
        var excludedCount = currentEvidence.Excluded.Count + referenceEvidence.Excluded.Count;
        var candidateCount = currentEvidence.CandidateObservationCount + referenceEvidence.CandidateObservationCount;
        var combinedExclusions = MergeExclusionCounts(currentExclusionCounts, referenceExclusionCounts);
        AppendExclusionDrivers(drivers, combinedExclusions, normalizedUnit);

        if (HasConflictExclusions(combinedExclusions))
        {
            drivers.Add(Draft(
                CommercialSalesPriceComparisonDriverReasonCodes.SourceConflict,
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
                candidateCount,
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
                CommercialSalesPriceComparisonDriverReasonCodes.InsufficientCurrentWindow,
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
                CommercialSalesPriceComparisonDriverReasonCodes.InsufficientReferenceWindow,
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
                candidateCount,
                currentCount + referenceCount,
                excludedCount,
                []);
        }

        var currentMedian = CommercialSalesPriceEvaluator.ComputeMedian(
            currentEvidence.Comparable.Select(o => o.ComparableUnitPrice).ToList());
        var referenceMedian = CommercialSalesPriceEvaluator.ComputeMedian(
            referenceEvidence.Comparable.Select(o => o.ComparableUnitPrice).ToList());

        if (!referenceMedian.HasValue || referenceMedian.Value == 0m)
        {
            drivers.Add(Draft(
                CommercialSalesPriceComparisonDriverReasonCodes.MissingReferenceMedian,
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
                candidateCount,
                currentCount + referenceCount,
                excludedCount,
                []);
        }

        var signedVarianceRatio = CommercialSalesPriceEvaluator.ComputeSignedVarianceRatio(
            currentMedian!.Value,
            referenceMedian);

        if (signedVarianceRatio.HasValue && signedVarianceRatio.Value > profile.MaximumPositiveVarianceRatio)
        {
            drivers.Add(Draft(
                CommercialSalesPriceComparisonDriverReasonCodes.PositivePriceVariance,
                "high",
                currentMedian,
                referenceMedian,
                profile.MaximumPositiveVarianceRatio,
                normalizedUnit,
                signedVarianceRatio,
                currentCount,
                "Current window median exceeds the approved positive sales-price variance band."));
        }
        else if (signedVarianceRatio.HasValue && signedVarianceRatio.Value < -profile.MaximumPositiveVarianceRatio)
        {
            drivers.Add(Draft(
                CommercialSalesPriceComparisonDriverReasonCodes.SalesPriceDecrease,
                "high",
                currentMedian,
                referenceMedian,
                profile.MaximumPositiveVarianceRatio,
                normalizedUnit,
                signedVarianceRatio,
                currentCount,
                "Current window median decreased beyond the approved sales-price variance band."));
        }
        else
        {
            drivers.Add(Draft(
                CommercialSalesPriceComparisonDriverReasonCodes.WithinReferenceRange,
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
            candidateCount,
            currentCount + referenceCount,
            excludedCount,
            []);
    }

    public static CommercialSalesPriceComparisonEvaluationOutput EvaluateCustomerPricePosition(
        CommercialSalesPriceProfile profile,
        string normalizedUnit,
        CommercialSalesPriceEvidenceReadResult evidence,
        IReadOnlyDictionary<string, int> exclusionCounts)
    {
        var drivers = new List<CommercialSalesPriceComparisonDriverDraft>();
        var excludedCount = evidence.Excluded.Count;
        var candidateCount = evidence.CandidateObservationCount;
        AppendExclusionDrivers(drivers, exclusionCounts, normalizedUnit);

        if (HasConflictExclusions(exclusionCounts))
        {
            drivers.Add(Draft(
                CommercialSalesPriceComparisonDriverReasonCodes.SourceConflict,
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
                candidateCount,
                evidence.Comparable.Count,
                excludedCount,
                []);
        }

        var customerGroups = evidence.Comparable
            .Where(o => !string.IsNullOrWhiteSpace(o.CustomerReference))
            .GroupBy(o => o.CustomerReference!, StringComparer.Ordinal)
            .Select(g => new
            {
                CustomerReference = g.Key,
                Observations = g.ToList(),
                Median = CommercialSalesPriceEvaluator.ComputeMedian(g.Select(o => o.ComparableUnitPrice).ToList()),
            })
            .Where(g => g.Median.HasValue)
            .ToList();

        var eligibleCustomers = customerGroups
            .Where(g => g.Observations.Count >= profile.MinimumComparableObservations)
            .ToList();

        if (eligibleCustomers.Count < 2)
        {
            if (customerGroups.Count == 1)
            {
                drivers.Add(Draft(
                    CommercialSalesPriceComparisonDriverReasonCodes.SingleComparableCustomer,
                    "info",
                    1,
                    2,
                    null,
                    normalizedUnit,
                    null,
                    customerGroups[0].Observations.Count,
                    "Only one customer has comparable observations in the analysis window."));
            }
            else
            {
                drivers.Add(Draft(
                    CommercialSalesPriceComparisonDriverReasonCodes.InsufficientCustomerObservations,
                    "info",
                    eligibleCustomers.Count,
                    profile.MinimumComparableObservations,
                    null,
                    normalizedUnit,
                    null,
                    evidence.Comparable.Count,
                    "Fewer than two customers meet the minimum comparable observation threshold."));
            }

            return Output(
                CommercialIntelligenceComparisonOutcomes.InsufficientData,
                CommercialIntelligenceEvidenceStatuses.InsufficientData,
                drivers,
                null,
                null,
                null,
                null,
                candidateCount,
                evidence.Comparable.Count,
                excludedCount,
                []);
        }

        var lowestMedian = eligibleCustomers.Min(g => g.Median!.Value);
        var highestMedian = eligibleCustomers.Max(g => g.Median!.Value);
        decimal? customerSpreadRatio = lowestMedian == 0m
            ? null
            : (highestMedian - lowestMedian) / lowestMedian;

        if (profile.CustomerSpreadThreshold.HasValue
            && customerSpreadRatio.HasValue
            && customerSpreadRatio.Value > profile.CustomerSpreadThreshold.Value)
        {
            drivers.Add(Draft(
                CommercialSalesPriceComparisonDriverReasonCodes.CustomerPriceSpread,
                "moderate",
                customerSpreadRatio,
                lowestMedian,
                profile.CustomerSpreadThreshold,
                normalizedUnit,
                customerSpreadRatio,
                eligibleCustomers.Count,
                "Customer comparable medians diverge beyond the approved spread threshold."));
        }

        var rankedCustomers = RankCustomers(eligibleCustomers.Select(g => (
            g.CustomerReference,
            g.Median!.Value,
            g.Observations.Count)).ToList());

        var tieGroups = rankedCustomers
            .GroupBy(c => c.Median)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Select(c => c.CustomerReference)
            .ToHashSet(StringComparer.Ordinal);

        var customerResults = new List<CommercialSalesPriceComparisonResultDraft>();
        foreach (var ranked in rankedCustomers)
        {
            var relativePosition = lowestMedian == 0m || highestMedian == 0m
                ? (decimal?)null
                : (ranked.Median - lowestMedian) / highestMedian;
            var isTied = tieGroups.Contains(ranked.CustomerReference);
            var positionState = ResolveCustomerPositionState(
                ranked.Median,
                lowestMedian,
                isTied,
                profile.CustomerSpreadThreshold);

            customerResults.Add(new CommercialSalesPriceComparisonResultDraft(
                ranked.CustomerReference,
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
                    CommercialSalesPriceComparisonDriverReasonCodes.CustomerMedianTie,
                    "low",
                    ranked.Median,
                    lowestMedian,
                    profile.CustomerSpreadThreshold,
                    normalizedUnit,
                    relativePosition,
                    ranked.ObservationCount,
                    $"Customer {ranked.CustomerReference} has a tied comparable median price."));
            }
            else if (ranked.Median == lowestMedian)
            {
                drivers.Add(Draft(
                    CommercialSalesPriceComparisonDriverReasonCodes.CustomerLowestComparableMedian,
                    "low",
                    ranked.Median,
                    lowestMedian,
                    profile.CustomerSpreadThreshold,
                    normalizedUnit,
                    relativePosition,
                    ranked.ObservationCount,
                    $"Customer {ranked.CustomerReference} has the lowest comparable observed median price."));
            }
            else if (positionState == CommercialSalesPriceCustomerPositionStates.HigherComparablePrice)
            {
                drivers.Add(Draft(
                    CommercialSalesPriceComparisonDriverReasonCodes.CustomerHigherComparableMedian,
                    "moderate",
                    ranked.Median,
                    lowestMedian,
                    profile.CustomerSpreadThreshold,
                    normalizedUnit,
                    relativePosition,
                    ranked.ObservationCount,
                    $"Customer {ranked.CustomerReference} has a higher comparable median beyond the spread threshold."));
            }
        }

        var outcome = customerSpreadRatio.HasValue
                       && profile.CustomerSpreadThreshold.HasValue
                       && customerSpreadRatio.Value > profile.CustomerSpreadThreshold.Value
            ? CommercialIntelligenceComparisonOutcomes.Attention
            : CommercialIntelligenceComparisonOutcomes.WithinReferenceRange;

        return Output(
            outcome,
            CommercialIntelligenceEvidenceStatuses.Simulated,
            drivers,
            null,
            null,
            null,
            customerSpreadRatio,
            candidateCount,
            evidence.Comparable.Count,
            excludedCount,
            customerResults);
    }

    public static IReadOnlyList<CommercialSalesPriceComparisonDriverDraft> OrderDrivers(
        IReadOnlyList<CommercialSalesPriceComparisonDriverDraft> drivers) =>
        drivers
            .OrderByDescending(d => OutcomeSeverityRank(d.Severity))
            .ThenByDescending(d => Math.Abs(d.NormalizedVariance ?? 0m))
            .ThenByDescending(d => d.AffectedObservationCount)
            .ThenBy(d => Array.IndexOf(DriverOrder, d.ReasonCode))
            .ToList();

    private static List<(string CustomerReference, decimal Median, int ObservationCount, int Rank)> RankCustomers(
        IReadOnlyList<(string CustomerReference, decimal Median, int ObservationCount)> customers)
    {
        var ordered = customers
            .OrderBy(c => c.Median)
            .ThenByDescending(c => c.ObservationCount)
            .ThenBy(c => c.CustomerReference, StringComparer.Ordinal)
            .ToList();

        var results = new List<(string CustomerReference, decimal Median, int ObservationCount, int Rank)>();
        var rank = 0;
        decimal? previousMedian = null;
        for (var index = 0; index < ordered.Count; index++)
        {
            var item = ordered[index];
            if (previousMedian is null || item.Median != previousMedian.Value)
            {
                rank = index + 1;
            }

            results.Add((item.CustomerReference, item.Median, item.ObservationCount, rank));
            previousMedian = item.Median;
        }

        return results;
    }

    private static string ResolveCustomerPositionState(
        decimal customerMedian,
        decimal lowestMedian,
        bool isTied,
        decimal? spreadThreshold)
    {
        if (isTied)
        {
            return CommercialSalesPriceCustomerPositionStates.TiedComparablePrice;
        }

        if (customerMedian == lowestMedian)
        {
            return CommercialSalesPriceCustomerPositionStates.LowestComparablePrice;
        }

        if (spreadThreshold.HasValue
            && lowestMedian > 0m
            && (customerMedian - lowestMedian) / lowestMedian > spreadThreshold.Value)
        {
            return CommercialSalesPriceCustomerPositionStates.HigherComparablePrice;
        }

        return CommercialSalesPriceCustomerPositionStates.WithinSpreadThreshold;
    }

    private static string ResolveTemporalOutcome(
        IReadOnlyList<CommercialSalesPriceComparisonDriverDraft> drivers,
        decimal? signedVarianceRatio,
        decimal maximumPositiveVarianceRatio)
    {
        if (drivers.Any(d => d.ReasonCode == CommercialSalesPriceComparisonDriverReasonCodes.SourceConflict))
        {
            return CommercialIntelligenceComparisonOutcomes.SourceConflict;
        }

        if (drivers.Any(d =>
                d.ReasonCode == CommercialSalesPriceComparisonDriverReasonCodes.InsufficientCurrentWindow
                || d.ReasonCode == CommercialSalesPriceComparisonDriverReasonCodes.InsufficientReferenceWindow
                || d.ReasonCode == CommercialSalesPriceComparisonDriverReasonCodes.MissingReferenceMedian))
        {
            return CommercialIntelligenceComparisonOutcomes.InsufficientData;
        }

        if (drivers.Any(d => d.ReasonCode == CommercialSalesPriceComparisonDriverReasonCodes.SalesPriceDecrease))
        {
            return CommercialSalesPriceComparisonOutcomes.Attention;
        }

        if (signedVarianceRatio.HasValue && signedVarianceRatio.Value > maximumPositiveVarianceRatio)
        {
            return CommercialSalesPriceComparisonOutcomes.PositivePriceVariance;
        }

        return CommercialSalesPriceComparisonOutcomes.WithinReferenceRange;
    }

    private static void AppendExclusionDrivers(
        List<CommercialSalesPriceComparisonDriverDraft> drivers,
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
                CommercialSalesPriceExclusionReasonCodes.IncompatibleCurrency =>
                    CommercialSalesPriceComparisonDriverReasonCodes.IncompatibleCurrency,
                CommercialSalesPriceExclusionReasonCodes.IncompatibleUnit =>
                    CommercialSalesPriceComparisonDriverReasonCodes.IncompatibleUnit,
                CommercialSalesPriceExclusionReasonCodes.IncompatiblePriceBasis =>
                    CommercialSalesPriceComparisonDriverReasonCodes.IncompatiblePriceBasis,
                CommercialSalesPriceExclusionReasonCodes.IncompatibleGrossNetBasis =>
                    CommercialSalesPriceComparisonDriverReasonCodes.IncompatibleGrossNetBasis,
                CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind =>
                    CommercialSalesPriceComparisonDriverReasonCodes.IncompatibleEvidenceKind,
                CommercialSalesPriceExclusionReasonCodes.DuplicateSourceEvidence =>
                    CommercialSalesPriceComparisonDriverReasonCodes.DuplicateSourceEvidence,
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
            CommercialSalesPriceExclusionReasonCodes.IncompatibleCurrency,
            CommercialSalesPriceExclusionReasonCodes.IncompatibleUnit,
            CommercialSalesPriceExclusionReasonCodes.IncompatiblePriceBasis,
            CommercialSalesPriceExclusionReasonCodes.IncompatibleGrossNetBasis,
            CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind,
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

    private static CommercialSalesPriceComparisonEvaluationOutput Output(
        string outcome,
        string evidenceStatus,
        List<CommercialSalesPriceComparisonDriverDraft> drivers,
        decimal? currentMedian,
        decimal? referenceMedian,
        decimal? signedVarianceRatio,
        decimal? customerSpreadRatio,
        int candidateCount,
        int comparableCount,
        int excludedCount,
        IReadOnlyList<CommercialSalesPriceComparisonResultDraft> customerResults)
    {
        if (drivers.Count == 0)
        {
            return new CommercialSalesPriceComparisonEvaluationOutput(
                outcome,
                evidenceStatus,
                null,
                currentMedian,
                referenceMedian,
                signedVarianceRatio,
                customerSpreadRatio,
                candidateCount,
                comparableCount,
                excludedCount,
                customerResults,
                []);
        }

        var ordered = OrderDrivers(drivers);
        return new CommercialSalesPriceComparisonEvaluationOutput(
            outcome,
            evidenceStatus,
            ordered[0].ReasonCode,
            currentMedian,
            referenceMedian,
            signedVarianceRatio,
            customerSpreadRatio,
            candidateCount,
            comparableCount,
            excludedCount,
            customerResults,
            ordered);
    }

    private static CommercialSalesPriceComparisonDriverDraft Draft(
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
