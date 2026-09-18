using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.CommercialIntelligence;

public static class CommercialQuotationRiskSalesEvidenceReader
{
    public static async Task<CommercialQuotationRiskSalesEvidenceReadResult> ReadAsync(
        NevaxisZentraDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialQuotationRiskProfile profile,
        CommercialQuotationRiskEvidenceObservation quotationEvidence,
        DateTimeOffset referenceWindowStartUtc,
        DateTimeOffset referenceWindowEndUtc,
        CancellationToken cancellationToken)
    {
        var observations = await dbContext.CommercialSalesPriceEvidenceObservations.AsNoTracking()
            .Where(o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.MaterialId == quotationEvidence.MaterialId
                        && o.CommercialEventAtUtc >= referenceWindowStartUtc
                        && o.CommercialEventAtUtc <= referenceWindowEndUtc)
            .OrderBy(o => o.CommercialEventAtUtc)
            .ThenBy(o => o.SourceDocumentReference)
            .ToListAsync(cancellationToken);

        var allowedStatuses = ParseAllowedStatuses(profile.AllowedEvidenceStatuses);
        var allowedKinds = ParseAllowedKinds(profile.AllowedSalesEvidenceKinds);
        var comparable = new List<CommercialQuotationRiskComparableSalesObservation>();
        var excluded = new List<CommercialQuotationRiskExcludedSalesObservation>();
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);
        var staleCutoff = quotationEvidence.QuotedAtUtc.AddDays(-profile.MaximumEvidenceAgeDays);

        foreach (var observation in observations)
        {
            var exclusion = EvaluateCompatibility(
                observation,
                quotationEvidence,
                profile,
                allowedStatuses,
                allowedKinds,
                seenFingerprints,
                staleCutoff);

            if (exclusion is not null)
            {
                excluded.Add(new CommercialQuotationRiskExcludedSalesObservation(observation.Id, exclusion));
                continue;
            }

            seenFingerprints.Add(observation.SourceFingerprint);
            comparable.Add(new CommercialQuotationRiskComparableSalesObservation(
                observation.Id,
                observation.ComparableUnitPrice));
        }

        return new CommercialQuotationRiskSalesEvidenceReadResult(observations.Count, comparable, excluded);
    }

    public static Dictionary<string, int> SummarizeExclusions(
        IReadOnlyList<CommercialQuotationRiskExcludedSalesObservation> excluded)
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
        CommercialQuotationRiskEvidenceObservation quotationEvidence,
        CommercialQuotationRiskProfile profile,
        IReadOnlySet<string> allowedStatuses,
        IReadOnlySet<string> allowedKinds,
        HashSet<string> seenFingerprints,
        DateTimeOffset staleCutoff)
    {
        if (observation.Quantity <= 0m || observation.ComparableUnitPrice <= 0m || observation.OriginalUnitPrice <= 0m)
        {
            return CommercialQuotationRiskExclusionReasonCodes.InvalidQuantityOrPrice;
        }

        if (observation.IsCancelledOrReturned)
        {
            return CommercialQuotationRiskExclusionReasonCodes.CancelledOrReturned;
        }

        if (!allowedStatuses.Contains(observation.EvidenceStatus))
        {
            return CommercialQuotationRiskExclusionReasonCodes.DisallowedEvidenceStatus;
        }

        if (!allowedKinds.Contains(observation.SourceEvidenceKind))
        {
            return CommercialQuotationRiskExclusionReasonCodes.IncompatibleEvidenceKind;
        }

        if (!string.Equals(observation.CurrencyCode, quotationEvidence.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialQuotationRiskExclusionReasonCodes.IncompatibleCurrency;
        }

        var observationUnit = observation.NormalizedUnit ?? observation.SourceUnit;
        if (!string.Equals(observationUnit, quotationEvidence.UnitOfMeasure, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialQuotationRiskExclusionReasonCodes.IncompatibleUnit;
        }

        if (!string.Equals(observation.PriceBasisTaxInclusion, profile.RequiredTaxInclusion, StringComparison.Ordinal)
            || !string.Equals(observation.PriceBasisDiscountInclusion, profile.RequiredDiscountInclusion, StringComparison.Ordinal)
            || !string.Equals(observation.PriceBasisFreightInclusion, profile.RequiredFreightInclusion, StringComparison.Ordinal)
            || !string.Equals(observation.PriceBasisTaxInclusion, quotationEvidence.TaxBasis, StringComparison.Ordinal)
            || !string.Equals(observation.PriceBasisDiscountInclusion, quotationEvidence.DiscountBasis, StringComparison.Ordinal)
            || !string.Equals(observation.PriceBasisFreightInclusion, quotationEvidence.FreightBasis, StringComparison.Ordinal))
        {
            return CommercialQuotationRiskExclusionReasonCodes.IncompatiblePriceBasis;
        }

        if (!string.Equals(observation.PriceBasisGrossNet, profile.RequiredGrossNetBasis, StringComparison.Ordinal)
            || !string.Equals(observation.PriceBasisGrossNet, quotationEvidence.GrossNetBasis, StringComparison.Ordinal))
        {
            return CommercialQuotationRiskExclusionReasonCodes.IncompatibleGrossNetBasis;
        }

        if (observation.CommercialEventAtUtc < staleCutoff)
        {
            return CommercialQuotationRiskExclusionReasonCodes.StaleEvidence;
        }

        if (seenFingerprints.Contains(observation.SourceFingerprint))
        {
            return CommercialQuotationRiskExclusionReasonCodes.DuplicateSourceEvidence;
        }

        return null;
    }

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

    private static IReadOnlySet<string> ParseAllowedKinds(string allowedSalesEvidenceKinds)
    {
        if (string.IsNullOrWhiteSpace(allowedSalesEvidenceKinds))
        {
            return CommercialSalesPriceEvidenceKinds.All;
        }

        return allowedSalesEvidenceKinds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }
}
