namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialQuotationRiskComparableSalesObservation(
    Guid ObservationId,
    decimal ComparableUnitPrice);

public sealed record CommercialQuotationRiskExcludedSalesObservation(
    Guid ObservationId,
    string ExclusionReasonCode);

public sealed record CommercialQuotationRiskSalesEvidenceReadResult(
    int CandidateObservationCount,
    IReadOnlyList<CommercialQuotationRiskComparableSalesObservation> Comparable,
    IReadOnlyList<CommercialQuotationRiskExcludedSalesObservation> Excluded);

public sealed record CommercialQuotationRiskDriverDraft(
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedObservationCount,
    string SupportingText);

public sealed record CommercialQuotationRiskEvaluationOutput(
    string Outcome,
    string EvidenceStatus,
    string? LeadingDriverCode,
    decimal? ReferenceMedianUnitPrice,
    decimal? SignedPriceVarianceRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    int? RemainingValidityDays,
    IReadOnlyList<CommercialQuotationRiskDriverDraft> Drivers);

public static class CommercialQuotationRiskEvaluator
{
    private static readonly string[] DriverOrder =
    [
        CommercialQuotationRiskDriverReasonCodes.SourceConflict,
        CommercialQuotationRiskDriverReasonCodes.IncompatiblePriceBasis,
        CommercialQuotationRiskDriverReasonCodes.IncompatibleCurrency,
        CommercialQuotationRiskDriverReasonCodes.IncompatibleUnit,
        CommercialQuotationRiskDriverReasonCodes.QuotationExpired,
        CommercialQuotationRiskDriverReasonCodes.QuotationExpiringSoon,
        CommercialQuotationRiskDriverReasonCodes.PaymentTermExceeded,
        CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference,
        CommercialQuotationRiskDriverReasonCodes.QuotedPriceAboveReference,
        CommercialQuotationRiskDriverReasonCodes.StaleReferenceEvidence,
        CommercialQuotationRiskDriverReasonCodes.InsufficientComparableSalesEvidence,
        CommercialQuotationRiskDriverReasonCodes.WithinProfile,
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

    public static decimal? ComputeSignedPriceVarianceRatio(decimal quotedComparableUnitPrice, decimal? referenceMedianUnitPrice)
    {
        if (!referenceMedianUnitPrice.HasValue || referenceMedianUnitPrice.Value <= 0m)
        {
            return null;
        }

        return (quotedComparableUnitPrice - referenceMedianUnitPrice.Value) / referenceMedianUnitPrice.Value;
    }

    public static int? ComputeRemainingValidityDays(DateTimeOffset? expiresAtUtc, DateTimeOffset evaluatedAtUtc)
    {
        if (!expiresAtUtc.HasValue)
        {
            return null;
        }

        return (int)Math.Floor((expiresAtUtc.Value - evaluatedAtUtc).TotalDays);
    }

    public static CommercialQuotationRiskEvaluationOutput Evaluate(
        CommercialQuotationRiskProfile profile,
        CommercialQuotationRiskEvidenceObservation quotationEvidence,
        CommercialQuotationRiskSalesEvidenceReadResult salesEvidence,
        IReadOnlyDictionary<string, int> exclusionCounts,
        DateTimeOffset evaluatedAtUtc)
    {
        var drivers = new List<CommercialQuotationRiskDriverDraft>();
        var comparableCount = salesEvidence.Comparable.Count;
        var excludedCount = salesEvidence.Excluded.Count;
        var candidateCount = salesEvidence.CandidateObservationCount;
        var unit = quotationEvidence.UnitOfMeasure;

        if (quotationEvidence.EvidenceStatus == CommercialQuotationRiskEvidenceStatuses.SourceConflict)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskDriverReasonCodes.SourceConflict,
                "critical",
                quotationEvidence.QuotedUnitPrice,
                null,
                unit,
                null,
                0,
                "Quotation evidence is marked source-conflict and cannot be evaluated."));
            return Output(
                CommercialQuotationRiskAssessmentOutcomes.SourceConflict,
                CommercialQuotationRiskEvidenceStatuses.SourceConflict,
                drivers,
                null,
                null,
                candidateCount,
                comparableCount,
                excludedCount,
                ComputeRemainingValidityDays(quotationEvidence.ExpiresAtUtc, evaluatedAtUtc));
        }

        AppendExclusionDrivers(drivers, exclusionCounts, unit);

        if (comparableCount < profile.MinimumComparableObservations && HasConflictExclusions(exclusionCounts))
        {
            drivers.Add(Draft(
                CommercialQuotationRiskDriverReasonCodes.SourceConflict,
                "critical",
                quotationEvidence.QuotedUnitPrice,
                null,
                unit,
                null,
                excludedCount,
                "Comparable sales evidence could not be normalized without mixing incompatible commercial bases."));
            return Output(
                CommercialQuotationRiskAssessmentOutcomes.SourceConflict,
                CommercialQuotationRiskEvidenceStatuses.SourceConflict,
                drivers,
                null,
                null,
                candidateCount,
                comparableCount,
                excludedCount,
                ComputeRemainingValidityDays(quotationEvidence.ExpiresAtUtc, evaluatedAtUtc));
        }

        decimal? median = null;
        decimal? varianceRatio = null;

        if (comparableCount >= profile.MinimumComparableObservations)
        {
            median = ComputeMedian(salesEvidence.Comparable.Select(o => o.ComparableUnitPrice).ToList());
            if (median.HasValue && median.Value > 0m)
            {
                varianceRatio = ComputeSignedPriceVarianceRatio(quotationEvidence.QuotedUnitPrice, median);
            }
        }

        if (comparableCount < profile.MinimumComparableObservations)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskDriverReasonCodes.InsufficientComparableSalesEvidence,
                "info",
                quotationEvidence.QuotedUnitPrice,
                profile.MinimumComparableObservations,
                unit,
                null,
                comparableCount,
                $"Comparable sales observation count {comparableCount} is below profile minimum {profile.MinimumComparableObservations}."));
        }

        if (exclusionCounts.GetValueOrDefault(CommercialQuotationRiskExclusionReasonCodes.StaleEvidence) > 0)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskDriverReasonCodes.StaleReferenceEvidence,
                "moderate",
                exclusionCounts[CommercialQuotationRiskExclusionReasonCodes.StaleEvidence],
                profile.MaximumEvidenceAgeDays,
                "days",
                null,
                exclusionCounts[CommercialQuotationRiskExclusionReasonCodes.StaleEvidence],
                "Reference sales evidence exceeded the profile maximum evidence age."));
        }

        var remainingValidityDays = ComputeRemainingValidityDays(quotationEvidence.ExpiresAtUtc, evaluatedAtUtc);
        if (quotationEvidence.ExpiresAtUtc.HasValue)
        {
            if (quotationEvidence.ExpiresAtUtc.Value <= evaluatedAtUtc)
            {
                drivers.Add(Draft(
                    CommercialQuotationRiskDriverReasonCodes.QuotationExpired,
                    "high",
                    remainingValidityDays,
                    0,
                    "days",
                    null,
                    0,
                    "Quotation validity has expired."));
            }
            else if (remainingValidityDays.HasValue
                     && remainingValidityDays.Value > 0
                     && remainingValidityDays.Value <= profile.ExpiryWarningDays)
            {
                drivers.Add(Draft(
                    CommercialQuotationRiskDriverReasonCodes.QuotationExpiringSoon,
                    "moderate",
                    remainingValidityDays,
                    profile.ExpiryWarningDays,
                    "days",
                    null,
                    0,
                    "Quotation validity is within the expiry warning window."));
            }
        }

        if (quotationEvidence.PaymentTermDays > profile.MaximumPaymentTermDays)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskDriverReasonCodes.PaymentTermExceeded,
                "high",
                quotationEvidence.PaymentTermDays,
                profile.MaximumPaymentTermDays,
                "days",
                null,
                0,
                "Quotation payment term exceeds the profile maximum."));
        }

        if (varianceRatio.HasValue && varianceRatio.Value < -profile.MaximumNegativePriceVarianceRatio)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference,
                "high",
                quotationEvidence.QuotedUnitPrice,
                median,
                unit,
                varianceRatio,
                comparableCount,
                "Quoted unit price is below the approved negative variance threshold."));
        }
        else if (varianceRatio.HasValue && varianceRatio.Value > profile.MaximumPositivePriceVarianceRatio)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskDriverReasonCodes.QuotedPriceAboveReference,
                "high",
                quotationEvidence.QuotedUnitPrice,
                median,
                unit,
                varianceRatio,
                comparableCount,
                "Quoted unit price is above the approved positive variance threshold."));
        }

        if (drivers.Count == 0
            || drivers.All(d => d.ReasonCode is CommercialQuotationRiskDriverReasonCodes.IncompatibleCurrency
                or CommercialQuotationRiskDriverReasonCodes.IncompatibleUnit
                or CommercialQuotationRiskDriverReasonCodes.IncompatiblePriceBasis))
        {
            if (comparableCount >= profile.MinimumComparableObservations && median.HasValue)
            {
                drivers.Add(Draft(
                    CommercialQuotationRiskDriverReasonCodes.WithinProfile,
                    "low",
                    quotationEvidence.QuotedUnitPrice,
                    median,
                    unit,
                    varianceRatio,
                    comparableCount,
                    "Quoted price, validity, and payment terms are within profile thresholds."));
            }
        }

        var outcome = ResolveOutcome(drivers, comparableCount, profile.MinimumComparableObservations);
        var evidenceStatus = ResolveEvidenceStatus(quotationEvidence, outcome, comparableCount);

        return Output(
            outcome,
            evidenceStatus,
            drivers,
            median,
            varianceRatio,
            candidateCount,
            comparableCount,
            excludedCount,
            remainingValidityDays);
    }

    public static IReadOnlyList<CommercialQuotationRiskDriverDraft> OrderDrivers(
        IReadOnlyList<CommercialQuotationRiskDriverDraft> drivers) =>
        drivers
            .OrderByDescending(d => OutcomeSeverityRank(d.Severity))
            .ThenByDescending(d => Math.Abs(d.NormalizedVariance ?? 0m))
            .ThenByDescending(d => d.AffectedObservationCount)
            .ThenBy(d => Array.IndexOf(DriverOrder, d.ReasonCode))
            .ToList();

    private static string ResolveOutcome(
        IReadOnlyList<CommercialQuotationRiskDriverDraft> drivers,
        int comparableCount,
        int minimumComparableObservations)
    {
        if (drivers.Any(d => d.ReasonCode == CommercialQuotationRiskDriverReasonCodes.SourceConflict))
        {
            return CommercialQuotationRiskAssessmentOutcomes.SourceConflict;
        }

        if (comparableCount < minimumComparableObservations
            || drivers.Any(d => d.ReasonCode == CommercialQuotationRiskDriverReasonCodes.InsufficientComparableSalesEvidence))
        {
            return CommercialQuotationRiskAssessmentOutcomes.InsufficientData;
        }

        var attentionCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference,
            CommercialQuotationRiskDriverReasonCodes.QuotedPriceAboveReference,
            CommercialQuotationRiskDriverReasonCodes.QuotationExpired,
            CommercialQuotationRiskDriverReasonCodes.QuotationExpiringSoon,
            CommercialQuotationRiskDriverReasonCodes.PaymentTermExceeded,
        };

        if (drivers.Any(d => attentionCodes.Contains(d.ReasonCode)))
        {
            return CommercialQuotationRiskAssessmentOutcomes.Attention;
        }

        return CommercialQuotationRiskAssessmentOutcomes.WithinThreshold;
    }

    private static string ResolveEvidenceStatus(
        CommercialQuotationRiskEvidenceObservation quotationEvidence,
        string outcome,
        int comparableCount)
    {
        if (outcome == CommercialQuotationRiskAssessmentOutcomes.SourceConflict)
        {
            return CommercialQuotationRiskEvidenceStatuses.SourceConflict;
        }

        if (outcome == CommercialQuotationRiskAssessmentOutcomes.InsufficientData)
        {
            return CommercialQuotationRiskEvidenceStatuses.InsufficientData;
        }

        if (quotationEvidence.EvidenceStatus == CommercialQuotationRiskEvidenceStatuses.Simulated)
        {
            return CommercialQuotationRiskEvidenceStatuses.Simulated;
        }

        return comparableCount > 0
            ? quotationEvidence.EvidenceStatus
            : CommercialQuotationRiskEvidenceStatuses.InsufficientData;
    }

    private static void AppendExclusionDrivers(
        List<CommercialQuotationRiskDriverDraft> drivers,
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
                CommercialQuotationRiskExclusionReasonCodes.IncompatibleCurrency =>
                    CommercialQuotationRiskDriverReasonCodes.IncompatibleCurrency,
                CommercialQuotationRiskExclusionReasonCodes.IncompatibleUnit =>
                    CommercialQuotationRiskDriverReasonCodes.IncompatibleUnit,
                CommercialQuotationRiskExclusionReasonCodes.IncompatiblePriceBasis
                    or CommercialQuotationRiskExclusionReasonCodes.IncompatibleGrossNetBasis =>
                    CommercialQuotationRiskDriverReasonCodes.IncompatiblePriceBasis,
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
                $"Excluded {count} sales observation(s) for {reason}."));
        }
    }

    private static bool HasConflictExclusions(IReadOnlyDictionary<string, int> exclusionCounts)
    {
        var incompatibleTypes = new[]
        {
            CommercialQuotationRiskExclusionReasonCodes.IncompatibleCurrency,
            CommercialQuotationRiskExclusionReasonCodes.IncompatibleUnit,
            CommercialQuotationRiskExclusionReasonCodes.IncompatiblePriceBasis,
            CommercialQuotationRiskExclusionReasonCodes.IncompatibleGrossNetBasis,
            CommercialQuotationRiskExclusionReasonCodes.IncompatibleEvidenceKind,
        };

        return incompatibleTypes.Count(key => exclusionCounts.GetValueOrDefault(key) > 0) >= 2;
    }

    private static CommercialQuotationRiskEvaluationOutput Output(
        string outcome,
        string evidenceStatus,
        List<CommercialQuotationRiskDriverDraft> drivers,
        decimal? median,
        decimal? varianceRatio,
        int candidateCount,
        int comparableCount,
        int excludedCount,
        int? remainingValidityDays)
    {
        if (drivers.Count == 0)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskDriverReasonCodes.WithinProfile,
                "low",
                null,
                null,
                null,
                null,
                comparableCount,
                "Assessment completed within profile."));
        }

        var ordered = OrderDrivers(drivers);
        var leading = ordered[0];
        return new CommercialQuotationRiskEvaluationOutput(
            outcome,
            evidenceStatus,
            leading.ReasonCode,
            median,
            varianceRatio,
            candidateCount,
            comparableCount,
            excludedCount,
            remainingValidityDays,
            ordered);
    }

    private static CommercialQuotationRiskDriverDraft Draft(
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
