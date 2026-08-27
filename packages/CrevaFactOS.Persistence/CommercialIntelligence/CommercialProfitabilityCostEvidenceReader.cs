using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialProfitabilityCostEvidenceReader
{
    public static async Task<CommercialProfitabilityCostEvidenceReadResult> ReadAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityEvidenceObservation salesEvidence,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.CommercialProfitabilityEvidenceObservations.AsNoTracking()
            .Where(o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.ObservationKind == CommercialProfitabilityObservationKinds.CostComponent
                        && o.EvidenceSetReference == salesEvidence.EvidenceSetReference
                        && o.MaterialId == salesEvidence.MaterialId)
            .OrderBy(o => o.ComponentKind)
            .ThenBy(o => o.EvidenceTimestampUtc)
            .ToListAsync(cancellationToken);

        var included = new List<CommercialProfitabilityCostComponentCandidate>();
        var excluded = new List<CommercialProfitabilityCostComponentCandidate>();
        var seenKinds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in candidates)
        {
            var exclusionReason = ResolveExclusionReason(profile, salesEvidence, observation, evaluatedAtUtc, seenKinds);
            var candidate = MapCandidate(observation, exclusionReason);

            if (exclusionReason is null)
            {
                included.Add(candidate with
                {
                    InclusionStatus = CommercialProfitabilityComponentInclusionStatuses.Included,
                });
                if (!string.IsNullOrWhiteSpace(observation.ComponentKind))
                {
                    seenKinds.Add(observation.ComponentKind);
                }
            }
            else
            {
                excluded.Add(candidate with
                {
                    InclusionStatus = CommercialProfitabilityComponentInclusionStatuses.Excluded,
                    ExclusionReason = exclusionReason,
                });
            }
        }

        return new CommercialProfitabilityCostEvidenceReadResult(
            candidates.Count,
            included,
            excluded);
    }

    public static IReadOnlyDictionary<string, int> SummarizeExclusions(
        IReadOnlyList<CommercialProfitabilityCostComponentCandidate> excluded) =>
        excluded
            .Where(c => c.ExclusionReason is not null)
            .GroupBy(c => c.ExclusionReason!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

    private static string? ResolveExclusionReason(
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityEvidenceObservation salesEvidence,
        CommercialProfitabilityEvidenceObservation observation,
        DateTimeOffset evaluatedAtUtc,
        HashSet<string> seenKinds)
    {
        if (observation.EvidenceStatus == CommercialProfitabilityEvidenceStatuses.SourceConflict)
        {
            return CommercialProfitabilityExclusionReasonCodes.SourceConflict;
        }

        if (!CommercialProfitabilityCostComponentKinds.All.Contains(observation.ComponentKind ?? string.Empty))
        {
            return CommercialProfitabilityExclusionReasonCodes.IncompatibleEvidenceKind;
        }

        if (observation.IsCancelledOrReturned)
        {
            return CommercialProfitabilityExclusionReasonCodes.CancelledOrReturned;
        }

        if (!IsAllowedEvidenceStatus(profile, observation.EvidenceStatus))
        {
            return CommercialProfitabilityExclusionReasonCodes.DisallowedEvidenceStatus;
        }

        if (!observation.UnitCost.HasValue || observation.UnitCost.Value <= 0m)
        {
            return CommercialProfitabilityExclusionReasonCodes.InvalidQuantityOrPrice;
        }

        if (!string.Equals(observation.CurrencyCode, profile.RequiredCurrencyCode, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(observation.CurrencyCode, salesEvidence.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return CommercialProfitabilityExclusionReasonCodes.IncompatibleCurrency;
        }

        if (!string.Equals(observation.UnitOfMeasure, profile.RequiredUnitOfMeasure, StringComparison.Ordinal)
            || !string.Equals(observation.UnitOfMeasure, salesEvidence.UnitOfMeasure, StringComparison.Ordinal))
        {
            return CommercialProfitabilityExclusionReasonCodes.IncompatibleUnit;
        }

        if (!string.Equals(observation.GrossNetBasis, profile.RequiredGrossNetBasis, StringComparison.Ordinal)
            || !string.Equals(observation.TaxBasis, profile.RequiredTaxInclusion, StringComparison.Ordinal)
            || !string.Equals(observation.DiscountBasis, profile.RequiredDiscountInclusion, StringComparison.Ordinal)
            || !string.Equals(observation.FreightBasis, profile.RequiredFreightInclusion, StringComparison.Ordinal))
        {
            return CommercialProfitabilityExclusionReasonCodes.IncompatiblePriceBasis;
        }

        if (string.Equals(observation.ComponentKind, CommercialProfitabilityCostComponentKinds.AllocatedOverhead, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(observation.AllocationBasis))
        {
            return CommercialProfitabilityExclusionReasonCodes.MissingAllocationBasis;
        }

        if ((evaluatedAtUtc - observation.EvidenceTimestampUtc).TotalDays > profile.MaximumEvidenceAgeDays)
        {
            return CommercialProfitabilityExclusionReasonCodes.StaleEvidence;
        }

        if (seenKinds.Contains(observation.ComponentKind ?? string.Empty))
        {
            return CommercialProfitabilityExclusionReasonCodes.DuplicateSourceEvidence;
        }

        return null;
    }

    private static bool IsAllowedEvidenceStatus(CommercialProfitabilityProfile profile, string evidenceStatus)
    {
        var allowed = profile.AllowedEvidenceStatuses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return allowed.Contains(evidenceStatus, StringComparer.Ordinal);
    }

    private static CommercialProfitabilityCostComponentCandidate MapCandidate(
        CommercialProfitabilityEvidenceObservation observation,
        string? exclusionReason) =>
        new(
            observation.Id,
            observation.ComponentKind ?? string.Empty,
            observation.UnitCost ?? 0m,
            observation.CurrencyCode,
            observation.UnitOfMeasure,
            observation.CostBasis ?? string.Empty,
            observation.AllocationBasis,
            observation.EvidenceTimestampUtc,
            observation.SourceKind,
            observation.SourceReference,
            observation.EvidenceStatus,
            exclusionReason is null
                ? CommercialProfitabilityComponentInclusionStatuses.Included
                : CommercialProfitabilityComponentInclusionStatuses.Excluded,
            exclusionReason);
}
