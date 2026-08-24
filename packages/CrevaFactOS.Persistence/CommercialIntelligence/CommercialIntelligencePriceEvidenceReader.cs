using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialIntelligencePriceEvidenceReader
{
    public static async Task<CommercialPurchasePriceEvidenceReadResult> ReadAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialPurchasePriceProfile profile,
        EvaluateCommercialPurchasePriceAssessmentCommand command,
        CancellationToken cancellationToken)
    {
        var observations = await dbContext.CommercialPurchasePriceEvidenceObservations.AsNoTracking()
            .Where(o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.MaterialId == command.MaterialId
                        && o.CommercialEventAtUtc >= command.WindowStartUtc
                        && o.CommercialEventAtUtc <= command.WindowEndUtc)
            .OrderBy(o => o.CommercialEventAtUtc)
            .ThenBy(o => o.SourceDocumentReference)
            .ToListAsync(cancellationToken);

        var allowedStatuses = ParseAllowedStatuses(profile.AllowedEvidenceStatuses);
        var comparable = new List<CommercialPurchasePriceComparableObservation>();
        var excluded = new List<CommercialPurchasePriceExcludedObservation>();
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in observations)
        {
            var exclusion = EvaluateCompatibility(
                observation,
                command,
                profile,
                allowedStatuses,
                seenFingerprints);

            if (exclusion is not null)
            {
                excluded.Add(new CommercialPurchasePriceExcludedObservation(observation.Id, exclusion));
                continue;
            }

            seenFingerprints.Add(observation.SourceFingerprint);
            comparable.Add(new CommercialPurchasePriceComparableObservation(
                observation.Id,
                observation.UnitPrice,
                observation.SupplierReference));
        }

        return new CommercialPurchasePriceEvidenceReadResult(comparable, excluded);
    }

    public static async Task<CommercialPurchasePriceEvidenceReadResult> ReadForComparisonAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialPurchasePriceProfile profile,
        Guid materialId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        string currencyCode,
        string normalizedUnit,
        string priceBasisTaxInclusion,
        string priceBasisDiscountInclusion,
        string priceBasisFreightInclusion,
        CancellationToken cancellationToken)
    {
        var observations = await dbContext.CommercialPurchasePriceEvidenceObservations.AsNoTracking()
            .Where(o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.MaterialId == materialId
                        && o.CommercialEventAtUtc >= windowStartUtc
                        && o.CommercialEventAtUtc <= windowEndUtc)
            .OrderBy(o => o.CommercialEventAtUtc)
            .ThenBy(o => o.SourceDocumentReference)
            .ToListAsync(cancellationToken);

        var allowedStatuses = ParseAllowedStatuses(profile.AllowedEvidenceStatuses);
        var comparable = new List<CommercialPurchasePriceComparableObservation>();
        var excluded = new List<CommercialPurchasePriceExcludedObservation>();
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in observations)
        {
            var exclusion = EvaluateComparisonCompatibility(
                observation,
                currencyCode,
                normalizedUnit,
                priceBasisTaxInclusion,
                priceBasisDiscountInclusion,
                priceBasisFreightInclusion,
                allowedStatuses,
                seenFingerprints);

            if (exclusion is not null)
            {
                excluded.Add(new CommercialPurchasePriceExcludedObservation(observation.Id, exclusion));
                continue;
            }

            seenFingerprints.Add(observation.SourceFingerprint);
            comparable.Add(new CommercialPurchasePriceComparableObservation(
                observation.Id,
                observation.UnitPrice,
                observation.SupplierReference));
        }

        return new CommercialPurchasePriceEvidenceReadResult(comparable, excluded);
    }

    public static Dictionary<string, int> SummarizeExclusions(
        IReadOnlyList<CommercialPurchasePriceExcludedObservation> excluded)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in excluded)
        {
            counts[item.ExclusionReasonCode] = counts.GetValueOrDefault(item.ExclusionReasonCode) + 1;
        }

        return counts;
    }

    private static string? EvaluateCompatibility(
        CommercialPurchasePriceEvidenceObservation observation,
        EvaluateCommercialPurchasePriceAssessmentCommand command,
        CommercialPurchasePriceProfile profile,
        IReadOnlySet<string> allowedStatuses,
        HashSet<string> seenFingerprints)
    {
        if (observation.Quantity <= 0m || observation.UnitPrice <= 0m)
        {
            return CommercialIntelligenceExclusionReasonCodes.InvalidQuantityOrPrice;
        }

        if (!allowedStatuses.Contains(observation.EvidenceStatus))
        {
            return CommercialIntelligenceExclusionReasonCodes.DisallowedEvidenceStatus;
        }

        if (!string.Equals(observation.CurrencyCode, command.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialIntelligenceExclusionReasonCodes.IncompatibleCurrency;
        }

        var observationUnit = observation.NormalizedUnit ?? observation.SourceUnit;
        if (!string.Equals(observationUnit, command.NormalizedUnit, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialIntelligenceExclusionReasonCodes.IncompatibleUnit;
        }

        if (!PriceBasisMatches(observation.PriceBasisTaxInclusion, command.PriceBasisTaxInclusion)
            || !PriceBasisMatches(observation.PriceBasisDiscountInclusion, command.PriceBasisDiscountInclusion)
            || !PriceBasisMatches(observation.PriceBasisFreightInclusion, command.PriceBasisFreightInclusion))
        {
            return CommercialIntelligenceExclusionReasonCodes.IncompatiblePriceBasis;
        }

        if (string.Equals(command.ComparisonDimension, CommercialIntelligenceComparisonDimensions.MaterialSupplier, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(command.SupplierReference)
            && !string.Equals(observation.SupplierReference, command.SupplierReference, StringComparison.Ordinal))
        {
            return CommercialIntelligenceExclusionReasonCodes.SupplierMismatch;
        }

        if (seenFingerprints.Contains(observation.SourceFingerprint))
        {
            return CommercialIntelligenceExclusionReasonCodes.DuplicateSourceEvidence;
        }

        return null;
    }

    private static string? EvaluateComparisonCompatibility(
        CommercialPurchasePriceEvidenceObservation observation,
        string currencyCode,
        string normalizedUnit,
        string priceBasisTaxInclusion,
        string priceBasisDiscountInclusion,
        string priceBasisFreightInclusion,
        IReadOnlySet<string> allowedStatuses,
        HashSet<string> seenFingerprints)
    {
        if (observation.Quantity <= 0m || observation.UnitPrice <= 0m)
        {
            return CommercialIntelligenceExclusionReasonCodes.InvalidQuantityOrPrice;
        }

        if (!allowedStatuses.Contains(observation.EvidenceStatus))
        {
            return CommercialIntelligenceExclusionReasonCodes.DisallowedEvidenceStatus;
        }

        if (!string.Equals(observation.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialIntelligenceExclusionReasonCodes.IncompatibleCurrency;
        }

        var observationUnit = observation.NormalizedUnit ?? observation.SourceUnit;
        if (!string.Equals(observationUnit, normalizedUnit, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialIntelligenceExclusionReasonCodes.IncompatibleUnit;
        }

        if (!PriceBasisMatches(observation.PriceBasisTaxInclusion, priceBasisTaxInclusion)
            || !PriceBasisMatches(observation.PriceBasisDiscountInclusion, priceBasisDiscountInclusion)
            || !PriceBasisMatches(observation.PriceBasisFreightInclusion, priceBasisFreightInclusion))
        {
            return CommercialIntelligenceExclusionReasonCodes.IncompatiblePriceBasis;
        }

        if (seenFingerprints.Contains(observation.SourceFingerprint))
        {
            return CommercialIntelligenceExclusionReasonCodes.DuplicateSourceEvidence;
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
}
