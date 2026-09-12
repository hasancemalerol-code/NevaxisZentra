namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialSalesPriceComparableObservation(
    Guid ObservationId,
    decimal ComparableUnitPrice,
    string? CustomerReference);

public sealed record CommercialSalesPriceExcludedObservation(
    Guid ObservationId,
    string ExclusionReasonCode);

public sealed record CommercialSalesPriceEvidenceReadResult(
    int CandidateObservationCount,
    IReadOnlyList<CommercialSalesPriceComparableObservation> Comparable,
    IReadOnlyList<CommercialSalesPriceExcludedObservation> Excluded);

public sealed record CommercialSalesPriceDriverDraft(
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedObservationCount,
    string SupportingText);

public sealed record CommercialSalesPriceEvaluationOutput(
    string AssessmentOutcome,
    string EvidenceStatus,
    string? LeadingDriverCode,
    decimal? ReferenceMedianUnitPrice,
    decimal? AbsoluteVariance,
    decimal? VarianceRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    IReadOnlyList<CommercialSalesPriceDriverDraft> Drivers);

public static class CommercialSalesPriceEvaluator
{
    private static readonly string[] DriverOrder =
    [
        CommercialSalesPriceDriverReasonCodes.SourceConflict,
        CommercialSalesPriceDriverReasonCodes.IncompatiblePriceBasis,
        CommercialSalesPriceDriverReasonCodes.IncompatibleGrossNetBasis,
        CommercialSalesPriceDriverReasonCodes.IncompatibleEvidenceKind,
        CommercialSalesPriceDriverReasonCodes.IncompatibleCurrency,
        CommercialSalesPriceDriverReasonCodes.IncompatibleUnit,
        CommercialSalesPriceDriverReasonCodes.CustomerMismatch,
        CommercialSalesPriceDriverReasonCodes.CancelledOrReturned,
        CommercialSalesPriceDriverReasonCodes.DuplicateSourceEvidence,
        CommercialSalesPriceDriverReasonCodes.SalesPriceIncrease,
        CommercialSalesPriceDriverReasonCodes.InsufficientComparableObservations,
        CommercialSalesPriceDriverReasonCodes.MissingReferencePrice,
        CommercialSalesPriceDriverReasonCodes.FavorablePriceVariance,
        CommercialSalesPriceDriverReasonCodes.WithinReferenceRange,
    ];

    public static decimal? ComputeMedian(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var ordered = values.OrderBy(v => v).ToList();
        var middle = ordered.Count / 2;
        return ordered.Count % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2m
            : ordered[middle];
    }

    public static decimal? ComputeSignedVarianceRatio(decimal currentUnitPrice, decimal? referenceMedianUnitPrice)
    {
        if (!referenceMedianUnitPrice.HasValue || referenceMedianUnitPrice.Value == 0m)
        {
            return null;
        }

        return (currentUnitPrice - referenceMedianUnitPrice.Value) / referenceMedianUnitPrice.Value;
    }

    public static CommercialSalesPriceEvaluationOutput Evaluate(
        CommercialSalesPriceProfile profile,
        decimal currentUnitPrice,
        string normalizedUnit,
        CommercialSalesPriceEvidenceReadResult evidence,
        IReadOnlyDictionary<string, int> exclusionCounts)
    {
        var drivers = new List<CommercialSalesPriceDriverDraft>();
        var comparableCount = evidence.Comparable.Count;
        var excludedCount = evidence.Excluded.Count;
        var candidateCount = evidence.CandidateObservationCount;

        AppendExclusionDrivers(drivers, exclusionCounts, normalizedUnit);

        if (comparableCount < profile.MinimumComparableObservations && HasConflictExclusions(exclusionCounts))
        {
            drivers.Add(Draft(
                CommercialSalesPriceDriverReasonCodes.SourceConflict,
                "critical",
                currentUnitPrice,
                null,
                normalizedUnit,
                null,
                excludedCount,
                "Comparable evidence could not be normalized without mixing incompatible commercial bases."));
            return Output(
                CommercialIntelligenceAssessmentOutcomes.SourceConflict,
                CommercialIntelligenceEvidenceStatuses.SourceConflict,
                drivers,
                null,
                null,
                null,
                candidateCount,
                comparableCount,
                excludedCount);
        }

        if (comparableCount < profile.MinimumComparableObservations)
        {
            drivers.Add(Draft(
                CommercialSalesPriceDriverReasonCodes.InsufficientComparableObservations,
                "info",
                currentUnitPrice,
                null,
                normalizedUnit,
                null,
                comparableCount,
                $"Comparable observation count {comparableCount} is below profile minimum {profile.MinimumComparableObservations}."));
            return Output(
                CommercialIntelligenceAssessmentOutcomes.InsufficientData,
                CommercialIntelligenceEvidenceStatuses.InsufficientData,
                drivers,
                null,
                null,
                null,
                candidateCount,
                comparableCount,
                excludedCount);
        }

        var median = ComputeMedian(evidence.Comparable.Select(o => o.ComparableUnitPrice).ToList());
        if (!median.HasValue || median.Value == 0m)
        {
            drivers.Add(Draft(
                CommercialSalesPriceDriverReasonCodes.MissingReferencePrice,
                "moderate",
                currentUnitPrice,
                median,
                normalizedUnit,
                null,
                comparableCount,
                "Reference median unit price is unavailable."));
            return Output(
                CommercialIntelligenceAssessmentOutcomes.InsufficientData,
                CommercialIntelligenceEvidenceStatuses.InsufficientData,
                drivers,
                median,
                null,
                null,
                candidateCount,
                comparableCount,
                excludedCount);
        }

        var varianceRatio = ComputeSignedVarianceRatio(currentUnitPrice, median);
        var absoluteVariance = currentUnitPrice - median.Value;

        if (varianceRatio.HasValue && varianceRatio.Value > profile.MaximumPositiveVarianceRatio)
        {
            drivers.Add(Draft(
                CommercialSalesPriceDriverReasonCodes.SalesPriceIncrease,
                "high",
                currentUnitPrice,
                median,
                normalizedUnit,
                varianceRatio,
                comparableCount,
                "Current comparable sales unit price exceeds the approved reference variance band."));
        }
        else if (varianceRatio.HasValue && varianceRatio.Value < 0m)
        {
            drivers.Add(Draft(
                CommercialSalesPriceDriverReasonCodes.FavorablePriceVariance,
                "low",
                currentUnitPrice,
                median,
                normalizedUnit,
                varianceRatio,
                comparableCount,
                "Current comparable sales unit price is lower than the reference median."));
        }
        else
        {
            drivers.Add(Draft(
                CommercialSalesPriceDriverReasonCodes.WithinReferenceRange,
                "low",
                currentUnitPrice,
                median,
                normalizedUnit,
                varianceRatio,
                comparableCount,
                "Current comparable sales unit price is within the approved reference range."));
        }

        var outcome = ResolveOutcome(drivers, varianceRatio, profile.MaximumPositiveVarianceRatio);
        var evidenceStatus = comparableCount > 0
            ? CommercialIntelligenceEvidenceStatuses.Simulated
            : CommercialIntelligenceEvidenceStatuses.InsufficientData;

        return Output(
            outcome,
            evidenceStatus,
            drivers,
            median,
            absoluteVariance,
            varianceRatio,
            candidateCount,
            comparableCount,
            excludedCount);
    }

    public static IReadOnlyList<CommercialSalesPriceDriverDraft> OrderDrivers(
        IReadOnlyList<CommercialSalesPriceDriverDraft> drivers) =>
        drivers
            .OrderByDescending(d => OutcomeSeverityRank(d.Severity))
            .ThenByDescending(d => Math.Abs(d.NormalizedVariance ?? 0m))
            .ThenByDescending(d => d.AffectedObservationCount)
            .ThenBy(d => Array.IndexOf(DriverOrder, d.ReasonCode))
            .ToList();

    private static string ResolveOutcome(
        IReadOnlyList<CommercialSalesPriceDriverDraft> drivers,
        decimal? varianceRatio,
        decimal maximumPositiveVarianceRatio)
    {
        if (drivers.Any(d => d.ReasonCode == CommercialSalesPriceDriverReasonCodes.SourceConflict))
        {
            return CommercialIntelligenceAssessmentOutcomes.SourceConflict;
        }

        if (drivers.Any(d => d.ReasonCode == CommercialSalesPriceDriverReasonCodes.InsufficientComparableObservations
                              || d.ReasonCode == CommercialSalesPriceDriverReasonCodes.MissingReferencePrice))
        {
            return CommercialIntelligenceAssessmentOutcomes.InsufficientData;
        }

        if (varianceRatio.HasValue && varianceRatio.Value > maximumPositiveVarianceRatio)
        {
            return CommercialIntelligenceAssessmentOutcomes.Attention;
        }

        if (varianceRatio.HasValue && varianceRatio.Value < 0m)
        {
            return CommercialIntelligenceAssessmentOutcomes.FavorableVariance;
        }

        return CommercialIntelligenceAssessmentOutcomes.WithinReferenceRange;
    }

    private static void AppendExclusionDrivers(
        List<CommercialSalesPriceDriverDraft> drivers,
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
                    CommercialSalesPriceDriverReasonCodes.IncompatibleCurrency,
                CommercialSalesPriceExclusionReasonCodes.IncompatibleUnit =>
                    CommercialSalesPriceDriverReasonCodes.IncompatibleUnit,
                CommercialSalesPriceExclusionReasonCodes.IncompatiblePriceBasis =>
                    CommercialSalesPriceDriverReasonCodes.IncompatiblePriceBasis,
                CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind =>
                    CommercialSalesPriceDriverReasonCodes.IncompatibleEvidenceKind,
                CommercialSalesPriceExclusionReasonCodes.IncompatibleGrossNetBasis =>
                    CommercialSalesPriceDriverReasonCodes.IncompatibleGrossNetBasis,
                CommercialSalesPriceExclusionReasonCodes.CustomerMismatch =>
                    CommercialSalesPriceDriverReasonCodes.CustomerMismatch,
                CommercialSalesPriceExclusionReasonCodes.CancelledOrReturned =>
                    CommercialSalesPriceDriverReasonCodes.CancelledOrReturned,
                CommercialSalesPriceExclusionReasonCodes.DuplicateSourceEvidence =>
                    CommercialSalesPriceDriverReasonCodes.DuplicateSourceEvidence,
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

    private static CommercialSalesPriceEvaluationOutput Output(
        string outcome,
        string evidenceStatus,
        List<CommercialSalesPriceDriverDraft> drivers,
        decimal? median,
        decimal? absoluteVariance,
        decimal? varianceRatio,
        int candidateCount,
        int comparableCount,
        int excludedCount)
    {
        var ordered = OrderDrivers(drivers);
        var leading = ordered[0];
        return new CommercialSalesPriceEvaluationOutput(
            outcome,
            evidenceStatus,
            leading.ReasonCode,
            median,
            absoluteVariance,
            varianceRatio,
            candidateCount,
            comparableCount,
            excludedCount,
            ordered);
    }

    private static CommercialSalesPriceDriverDraft Draft(
        string reasonCode,
        string severity,
        decimal? actualValue,
        decimal? thresholdValue,
        string? unit,
        decimal? normalizedVariance,
        int affectedObservationCount,
        string supportingText) =>
        new(reasonCode, severity, actualValue, thresholdValue, unit, normalizedVariance, affectedObservationCount, supportingText);

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
