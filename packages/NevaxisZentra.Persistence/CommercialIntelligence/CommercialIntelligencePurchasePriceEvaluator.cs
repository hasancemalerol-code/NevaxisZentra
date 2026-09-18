namespace NevaxisZentra.Persistence.CommercialIntelligence;

public sealed record CommercialPurchasePriceComparableObservation(
    Guid ObservationId,
    decimal UnitPrice,
    string? SupplierReference);

public sealed record CommercialPurchasePriceExcludedObservation(
    Guid ObservationId,
    string ExclusionReasonCode);

public sealed record CommercialPurchasePriceEvidenceReadResult(
    IReadOnlyList<CommercialPurchasePriceComparableObservation> Comparable,
    IReadOnlyList<CommercialPurchasePriceExcludedObservation> Excluded);

public sealed record CommercialPurchasePriceDriverDraft(
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedObservationCount,
    string SupportingText);

public sealed record CommercialPurchasePriceEvaluationOutput(
    string AssessmentOutcome,
    string EvidenceStatus,
    string? LeadingDriverCode,
    decimal? ReferenceMedianUnitPrice,
    decimal? AbsoluteVariance,
    decimal? VarianceRatio,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    IReadOnlyList<CommercialPurchasePriceDriverDraft> Drivers);

public static class CommercialPurchasePriceEvaluator
{
    private static readonly string[] DriverOrder =
    [
        CommercialIntelligenceDriverReasonCodes.SourceConflict,
        CommercialIntelligenceDriverReasonCodes.IncompatiblePriceBasis,
        CommercialIntelligenceDriverReasonCodes.IncompatibleCurrency,
        CommercialIntelligenceDriverReasonCodes.IncompatibleUnit,
        CommercialIntelligenceDriverReasonCodes.DuplicateSourceEvidence,
        CommercialIntelligenceDriverReasonCodes.PurchasePriceIncrease,
        CommercialIntelligenceDriverReasonCodes.SupplierPriceSpread,
        CommercialIntelligenceDriverReasonCodes.InsufficientComparableObservations,
        CommercialIntelligenceDriverReasonCodes.MissingReferencePrice,
        CommercialIntelligenceDriverReasonCodes.FavorablePriceVariance,
        CommercialIntelligenceDriverReasonCodes.WithinReferenceRange,
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

    public static CommercialPurchasePriceEvaluationOutput Evaluate(
        CommercialPurchasePriceProfile profile,
        decimal currentUnitPrice,
        string normalizedUnit,
        CommercialPurchasePriceEvidenceReadResult evidence,
        IReadOnlyDictionary<string, int> exclusionCounts)
    {
        var drivers = new List<CommercialPurchasePriceDriverDraft>();
        var comparableCount = evidence.Comparable.Count;
        var excludedCount = evidence.Excluded.Count;

        AppendExclusionDrivers(drivers, exclusionCounts, normalizedUnit);

        if (comparableCount < profile.MinimumComparableObservations && HasConflictExclusions(exclusionCounts))
        {
            drivers.Add(Draft(
                CommercialIntelligenceDriverReasonCodes.SourceConflict,
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
                comparableCount,
                excludedCount);
        }

        if (comparableCount < profile.MinimumComparableObservations)
        {
            drivers.Add(Draft(
                CommercialIntelligenceDriverReasonCodes.InsufficientComparableObservations,
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
                comparableCount,
                excludedCount);
        }

        var median = ComputeMedian(evidence.Comparable.Select(o => o.UnitPrice).ToList());
        if (!median.HasValue || median.Value == 0m)
        {
            drivers.Add(Draft(
                CommercialIntelligenceDriverReasonCodes.MissingReferencePrice,
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
                comparableCount,
                excludedCount);
        }

        var varianceRatio = ComputeSignedVarianceRatio(currentUnitPrice, median);
        var absoluteVariance = currentUnitPrice - median.Value;

        if (profile.SupplierSpreadThreshold.HasValue)
        {
            var spreadRatio = ComputeSupplierSpreadRatio(evidence.Comparable);
            if (spreadRatio.HasValue && spreadRatio.Value > profile.SupplierSpreadThreshold.Value)
            {
                drivers.Add(Draft(
                    CommercialIntelligenceDriverReasonCodes.SupplierPriceSpread,
                    "moderate",
                    spreadRatio,
                    profile.SupplierSpreadThreshold,
                    normalizedUnit,
                    spreadRatio,
                    CountDistinctSuppliers(evidence.Comparable),
                    "Supplier comparable prices diverge beyond the approved spread threshold."));
            }
        }

        if (varianceRatio.HasValue && varianceRatio.Value > profile.MaximumPositiveVarianceRatio)
        {
            drivers.Add(Draft(
                CommercialIntelligenceDriverReasonCodes.PurchasePriceIncrease,
                "high",
                currentUnitPrice,
                median,
                normalizedUnit,
                varianceRatio,
                comparableCount,
                "Current comparable unit price exceeds the approved reference variance band."));
        }
        else if (varianceRatio.HasValue && varianceRatio.Value < 0m)
        {
            drivers.Add(Draft(
                CommercialIntelligenceDriverReasonCodes.FavorablePriceVariance,
                "low",
                currentUnitPrice,
                median,
                normalizedUnit,
                varianceRatio,
                comparableCount,
                "Current comparable unit price is lower than the reference median."));
        }
        else
        {
            drivers.Add(Draft(
                CommercialIntelligenceDriverReasonCodes.WithinReferenceRange,
                "low",
                currentUnitPrice,
                median,
                normalizedUnit,
                varianceRatio,
                comparableCount,
                "Current comparable unit price is within the approved reference range."));
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
            comparableCount,
            excludedCount);
    }

    public static CommercialPurchasePriceDriverDraft SelectLeadingDriver(
        IReadOnlyList<CommercialPurchasePriceDriverDraft> drivers) =>
        OrderDrivers(drivers)[0];

    public static IReadOnlyList<CommercialPurchasePriceDriverDraft> OrderDrivers(
        IReadOnlyList<CommercialPurchasePriceDriverDraft> drivers) =>
        drivers
            .OrderByDescending(d => OutcomeSeverityRank(d.Severity))
            .ThenByDescending(d => Math.Abs(d.NormalizedVariance ?? 0m))
            .ThenByDescending(d => d.AffectedObservationCount)
            .ThenBy(d => Array.IndexOf(DriverOrder, d.ReasonCode))
            .ToList();

    private static string ResolveOutcome(
        IReadOnlyList<CommercialPurchasePriceDriverDraft> drivers,
        decimal? varianceRatio,
        decimal maximumPositiveVarianceRatio)
    {
        if (drivers.Any(d => d.ReasonCode == CommercialIntelligenceDriverReasonCodes.SourceConflict))
        {
            return CommercialIntelligenceAssessmentOutcomes.SourceConflict;
        }

        if (drivers.Any(d => d.ReasonCode == CommercialIntelligenceDriverReasonCodes.InsufficientComparableObservations
                              || d.ReasonCode == CommercialIntelligenceDriverReasonCodes.MissingReferencePrice))
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
        List<CommercialPurchasePriceDriverDraft> drivers,
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
                    CommercialIntelligenceDriverReasonCodes.IncompatibleCurrency,
                CommercialIntelligenceExclusionReasonCodes.IncompatibleUnit =>
                    CommercialIntelligenceDriverReasonCodes.IncompatibleUnit,
                CommercialIntelligenceExclusionReasonCodes.IncompatiblePriceBasis =>
                    CommercialIntelligenceDriverReasonCodes.IncompatiblePriceBasis,
                CommercialIntelligenceExclusionReasonCodes.DuplicateSourceEvidence =>
                    CommercialIntelligenceDriverReasonCodes.DuplicateSourceEvidence,
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
            CommercialIntelligenceExclusionReasonCodes.IncompatibleCurrency,
            CommercialIntelligenceExclusionReasonCodes.IncompatibleUnit,
            CommercialIntelligenceExclusionReasonCodes.IncompatiblePriceBasis,
        };

        return incompatibleTypes.Count(key => exclusionCounts.GetValueOrDefault(key) > 0) >= 2;
    }

    private static decimal? ComputeSupplierSpreadRatio(IReadOnlyList<CommercialPurchasePriceComparableObservation> comparable)
    {
        var supplierMedians = comparable
            .Where(o => !string.IsNullOrWhiteSpace(o.SupplierReference))
            .GroupBy(o => o.SupplierReference!, StringComparer.Ordinal)
            .Select(g => ComputeMedian(g.Select(o => o.UnitPrice).ToList()))
            .Where(m => m.HasValue)
            .Select(m => m!.Value)
            .ToList();

        if (supplierMedians.Count < 2)
        {
            return null;
        }

        var min = supplierMedians.Min();
        var max = supplierMedians.Max();
        if (min == 0m)
        {
            return null;
        }

        return (max - min) / min;
    }

    private static int CountDistinctSuppliers(IReadOnlyList<CommercialPurchasePriceComparableObservation> comparable) =>
        comparable
            .Select(o => o.SupplierReference)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.Ordinal)
            .Count();

    private static CommercialPurchasePriceEvaluationOutput Output(
        string outcome,
        string evidenceStatus,
        List<CommercialPurchasePriceDriverDraft> drivers,
        decimal? median,
        decimal? absoluteVariance,
        decimal? varianceRatio,
        int comparableCount,
        int excludedCount)
    {
        var ordered = OrderDrivers(drivers);
        var leading = ordered[0];
        return new CommercialPurchasePriceEvaluationOutput(
            outcome,
            evidenceStatus,
            leading.ReasonCode,
            median,
            absoluteVariance,
            varianceRatio,
            comparableCount,
            excludedCount,
            ordered);
    }

    private static CommercialPurchasePriceDriverDraft Draft(
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
