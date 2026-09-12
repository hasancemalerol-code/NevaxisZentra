using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public static class CommercialSalesPriceEvidenceReader
{
    public static async Task<CommercialSalesPriceEvidenceReadResult> ReadAsync(
        NevaxisFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceProfile profile,
        EvaluateCommercialSalesPriceAssessmentCommand command,
        CancellationToken cancellationToken)
    {
        var observations = await dbContext.CommercialSalesPriceEvidenceObservations.AsNoTracking()
            .Where(o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.MaterialId == command.MaterialId
                        && o.CommercialEventAtUtc >= command.WindowStartUtc
                        && o.CommercialEventAtUtc <= command.WindowEndUtc)
            .OrderBy(o => o.CommercialEventAtUtc)
            .ThenBy(o => o.SourceDocumentReference)
            .ToListAsync(cancellationToken);

        var allowedStatuses = ParseAllowedStatuses(profile.AllowedEvidenceStatuses);
        var allowedKinds = ParseAllowedKinds(profile.AllowedSourceEvidenceKinds);
        var comparable = new List<CommercialSalesPriceComparableObservation>();
        var excluded = new List<CommercialSalesPriceExcludedObservation>();
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in observations)
        {
            var exclusion = EvaluateCompatibility(
                observation,
                command,
                profile,
                allowedStatuses,
                allowedKinds,
                seenFingerprints);

            if (exclusion is not null)
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(observation.Id, exclusion));
                continue;
            }

            seenFingerprints.Add(observation.SourceFingerprint);
            comparable.Add(new CommercialSalesPriceComparableObservation(
                observation.Id,
                observation.ComparableUnitPrice,
                observation.CustomerReference));
        }

        return new CommercialSalesPriceEvidenceReadResult(observations.Count, comparable, excluded);
    }

    public static async Task<CommercialSalesPriceEvidenceReadResult> ReadForComparisonAsync(
        NevaxisFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceProfile profile,
        Guid materialId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        string sourceEvidenceKind,
        string currencyCode,
        string normalizedUnit,
        string priceBasisGrossNet,
        string priceBasisTaxInclusion,
        string priceBasisDiscountInclusion,
        string priceBasisFreightInclusion,
        string? customerReference,
        CancellationToken cancellationToken)
    {
        var observations = await dbContext.CommercialSalesPriceEvidenceObservations.AsNoTracking()
            .Where(o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.MaterialId == materialId
                        && o.CommercialEventAtUtc >= windowStartUtc
                        && o.CommercialEventAtUtc <= windowEndUtc)
            .OrderBy(o => o.CommercialEventAtUtc)
            .ThenBy(o => o.SourceDocumentReference)
            .ToListAsync(cancellationToken);

        var allowedStatuses = ParseAllowedStatuses(profile.AllowedEvidenceStatuses);
        var allowedKinds = ParseAllowedKinds(profile.AllowedSourceEvidenceKinds);
        var comparable = new List<CommercialSalesPriceComparableObservation>();
        var excluded = new List<CommercialSalesPriceExcludedObservation>();
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in observations)
        {
            var exclusion = EvaluateComparisonCompatibility(
                observation,
                sourceEvidenceKind,
                currencyCode,
                normalizedUnit,
                priceBasisGrossNet,
                priceBasisTaxInclusion,
                priceBasisDiscountInclusion,
                priceBasisFreightInclusion,
                customerReference,
                allowedStatuses,
                allowedKinds,
                seenFingerprints);

            if (exclusion is not null)
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(observation.Id, exclusion));
                continue;
            }

            seenFingerprints.Add(observation.SourceFingerprint);
            comparable.Add(new CommercialSalesPriceComparableObservation(
                observation.Id,
                observation.ComparableUnitPrice,
                observation.CustomerReference));
        }

        return new CommercialSalesPriceEvidenceReadResult(observations.Count, comparable, excluded);
    }

    public static Dictionary<string, int> SummarizeExclusions(
        IReadOnlyList<CommercialSalesPriceExcludedObservation> excluded)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in excluded)
        {
            counts[item.ExclusionReasonCode] = counts.GetValueOrDefault(item.ExclusionReasonCode) + 1;
        }

        return counts;
    }

    private static string? EvaluateCompatibility(
        CommercialSalesPriceEvidenceObservation observation,
        EvaluateCommercialSalesPriceAssessmentCommand command,
        CommercialSalesPriceProfile profile,
        IReadOnlySet<string> allowedStatuses,
        IReadOnlySet<string> allowedKinds,
        HashSet<string> seenFingerprints)
    {
        if (observation.Quantity <= 0m || observation.ComparableUnitPrice <= 0m || observation.OriginalUnitPrice <= 0m)
        {
            return CommercialSalesPriceExclusionReasonCodes.InvalidQuantityOrPrice;
        }

        if (observation.IsCancelledOrReturned)
        {
            return CommercialSalesPriceExclusionReasonCodes.CancelledOrReturned;
        }

        if (!allowedStatuses.Contains(observation.EvidenceStatus))
        {
            return CommercialSalesPriceExclusionReasonCodes.DisallowedEvidenceStatus;
        }

        if (!allowedKinds.Contains(observation.SourceEvidenceKind))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind;
        }

        if (!string.Equals(observation.SourceEvidenceKind, command.SourceEvidenceKind.Trim(), StringComparison.Ordinal))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind;
        }

        if (!string.Equals(observation.CurrencyCode, command.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleCurrency;
        }

        var observationUnit = observation.NormalizedUnit ?? observation.SourceUnit;
        if (!string.Equals(observationUnit, command.NormalizedUnit, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleUnit;
        }

        if (!PriceBasisMatches(observation.PriceBasisTaxInclusion, command.PriceBasisTaxInclusion)
            || !PriceBasisMatches(observation.PriceBasisDiscountInclusion, command.PriceBasisDiscountInclusion)
            || !PriceBasisMatches(observation.PriceBasisFreightInclusion, command.PriceBasisFreightInclusion))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatiblePriceBasis;
        }

        if (!string.Equals(observation.PriceBasisGrossNet, command.PriceBasisGrossNet, StringComparison.Ordinal))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleGrossNetBasis;
        }

        if (string.Equals(command.ComparisonDimension, CommercialSalesPriceComparisonDimensions.MaterialCustomer, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(command.CustomerReference)
            && !string.Equals(observation.CustomerReference, command.CustomerReference, StringComparison.Ordinal))
        {
            return CommercialSalesPriceExclusionReasonCodes.CustomerMismatch;
        }

        if (seenFingerprints.Contains(observation.SourceFingerprint))
        {
            return CommercialSalesPriceExclusionReasonCodes.DuplicateSourceEvidence;
        }

        return null;
    }

    private static string? EvaluateComparisonCompatibility(
        CommercialSalesPriceEvidenceObservation observation,
        string sourceEvidenceKind,
        string currencyCode,
        string normalizedUnit,
        string priceBasisGrossNet,
        string priceBasisTaxInclusion,
        string priceBasisDiscountInclusion,
        string priceBasisFreightInclusion,
        string? customerReference,
        IReadOnlySet<string> allowedStatuses,
        IReadOnlySet<string> allowedKinds,
        HashSet<string> seenFingerprints)
    {
        if (observation.Quantity <= 0m || observation.ComparableUnitPrice <= 0m || observation.OriginalUnitPrice <= 0m)
        {
            return CommercialSalesPriceExclusionReasonCodes.InvalidQuantityOrPrice;
        }

        if (observation.IsCancelledOrReturned)
        {
            return CommercialSalesPriceExclusionReasonCodes.CancelledOrReturned;
        }

        if (!allowedStatuses.Contains(observation.EvidenceStatus))
        {
            return CommercialSalesPriceExclusionReasonCodes.DisallowedEvidenceStatus;
        }

        if (!allowedKinds.Contains(observation.SourceEvidenceKind))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind;
        }

        if (!string.Equals(observation.SourceEvidenceKind, sourceEvidenceKind.Trim(), StringComparison.Ordinal))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind;
        }

        if (!string.Equals(observation.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleCurrency;
        }

        var observationUnit = observation.NormalizedUnit ?? observation.SourceUnit;
        if (!string.Equals(observationUnit, normalizedUnit, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleUnit;
        }

        if (!PriceBasisMatches(observation.PriceBasisTaxInclusion, priceBasisTaxInclusion)
            || !PriceBasisMatches(observation.PriceBasisDiscountInclusion, priceBasisDiscountInclusion)
            || !PriceBasisMatches(observation.PriceBasisFreightInclusion, priceBasisFreightInclusion))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatiblePriceBasis;
        }

        if (!string.Equals(observation.PriceBasisGrossNet, priceBasisGrossNet, StringComparison.Ordinal))
        {
            return CommercialSalesPriceExclusionReasonCodes.IncompatibleGrossNetBasis;
        }

        if (!string.IsNullOrWhiteSpace(customerReference)
            && !string.Equals(observation.CustomerReference, customerReference.Trim(), StringComparison.Ordinal))
        {
            return CommercialSalesPriceExclusionReasonCodes.CustomerMismatch;
        }

        if (seenFingerprints.Contains(observation.SourceFingerprint))
        {
            return CommercialSalesPriceExclusionReasonCodes.DuplicateSourceEvidence;
        }

        return null;
    }

    private static bool PriceBasisMatches(string observationValue, string requestedValue) =>
        string.Equals(observationValue, requestedValue, StringComparison.Ordinal);

    private static IReadOnlySet<string> ParseAllowedStatuses(string allowedEvidenceStatuses)
    {
        if (string.IsNullOrWhiteSpace(allowedEvidenceStatuses))
        {
            return CommercialIntelligenceEvidenceStatuses.All;
        }

        return allowedEvidenceStatuses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IReadOnlySet<string> ParseAllowedKinds(string allowedSourceEvidenceKinds)
    {
        if (string.IsNullOrWhiteSpace(allowedSourceEvidenceKinds))
        {
            return CommercialSalesPriceEvidenceKinds.All;
        }

        return allowedSourceEvidenceKinds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }
}
