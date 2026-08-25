using CrevaFactOS.Persistence.CommercialIntelligence;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotCommercialSalesPriceSeed
{
    internal static async Task EnsureCommercialSalesPricePilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = CommercialIntelligenceConstants.SalesDevelopmentPilotProvenance;
        var eventTime = DevelopmentPilotIds.CommercialSalesPricePilotSeedTimeUtc;
        var windowStart = eventTime.AddDays(-120);
        var kind = CommercialSalesPriceEvidenceKinds.ManualDevelopmentSimulator;

        await EnsurePilotProfileAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        foreach (var (id, daysAgo, price) in new[]
        {
            (DevelopmentPilotIds.CommercialSalesPricePilotWithinRangeObservation1Id, 60, 100m),
            (DevelopmentPilotIds.CommercialSalesPricePilotWithinRangeObservation2Id, 55, 100m),
            (DevelopmentPilotIds.CommercialSalesPricePilotWithinRangeObservation3Id, 50, 101m),
            (DevelopmentPilotIds.CommercialSalesPricePilotWithinRangeObservation4Id, 45, 101m),
            (DevelopmentPilotIds.CommercialSalesPricePilotWithinRangeObservation5Id, 40, 102m),
            (DevelopmentPilotIds.CommercialSalesPricePilotWithinRangeObservation6Id, 35, 103m),
        })
        {
            await EnsureObservationAsync(
                dbContext, id, tenantId, facilityId, DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
                kind, "COMMSALE1-SET-WITHIN-RANGE", $"SO-SIM-WR-{id:N}".Substring(0, 20),
                "LINE-001", eventTime.AddDays(-daysAgo), 10m, price, price, provenance,
                actorSubjectId, seedTimestampUtc, cancellationToken);
        }

        foreach (var (id, daysAgo, price) in new[]
        {
            (DevelopmentPilotIds.CommercialSalesPricePilotInsufficientObservation1Id, 30, 100m),
            (DevelopmentPilotIds.CommercialSalesPricePilotInsufficientObservation2Id, 25, 101m),
            (DevelopmentPilotIds.CommercialSalesPricePilotInsufficientObservation3Id, 20, 102m),
        })
        {
            await EnsureObservationAsync(
                dbContext, id, tenantId, facilityId, DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
                kind, "COMMSALE1-SET-INSUFFICIENT-HISTORY", $"SO-SIM-INS-{id:N}".Substring(0, 20),
                "LINE-001", eventTime.AddDays(-daysAgo), 10m, price, price, provenance,
                actorSubjectId, seedTimestampUtc, cancellationToken);
        }

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPricePilotConflictCurrencyObservationId,
            tenantId, facilityId, DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
            kind, "COMMSALE1-SET-SOURCE-CONFLICT", "SO-SIM-SC-CUR-001", "LINE-001",
            eventTime.AddDays(-10), 10m, 100m, 100m, provenance, actorSubjectId, seedTimestampUtc,
            cancellationToken, currencyCode: "USD");

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPricePilotConflictUnitObservationId,
            tenantId, facilityId, DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
            kind, "COMMSALE1-SET-SOURCE-CONFLICT", "SO-SIM-SC-UOM-001", "LINE-001",
            eventTime.AddDays(-9), 10m, 100m, 100m, provenance, actorSubjectId, seedTimestampUtc,
            cancellationToken, normalizedUnit: "ton");

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPricePilotConflictBasisObservationId,
            tenantId, facilityId, DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
            kind, "COMMSALE1-SET-SOURCE-CONFLICT", "SO-SIM-SC-BASIS-001", "LINE-001",
            eventTime.AddDays(-8), 10m, 100m, 100m, provenance, actorSubjectId, seedTimestampUtc,
            cancellationToken, grossNet: CommercialSalesPriceGrossNetBasis.Gross);

        _ = windowStart;
    }

    private static async Task EnsurePilotProfileAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.CommercialSalesPriceProfiles
            .SingleOrDefaultAsync(p => p.Id == DevelopmentPilotIds.CommercialSalesPricePilotProfileId, cancellationToken);
        if (profile is not null)
        {
            return;
        }

        dbContext.CommercialSalesPriceProfiles.Add(new CommercialSalesPriceProfile
        {
            Id = DevelopmentPilotIds.CommercialSalesPricePilotProfileId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProfileCode = DevelopmentPilotIds.CommercialSalesPricePilotProfileCode,
            DisplayName = "Development pilot sales price profile",
            Status = CommercialIntelligenceProfileStatuses.Approved,
            ComparisonDimension = CommercialSalesPriceComparisonDimensions.Material,
            ReferenceWindowDurationDays = 120,
            MinimumComparableObservations = CommercialIntelligenceConstants.DefaultMinimumComparableObservations,
            MaximumPositiveVarianceRatio = CommercialIntelligenceConstants.DefaultMaximumPositiveVarianceRatio,
            AllowedEvidenceStatuses = CommercialIntelligenceEvidenceStatuses.Simulated,
            AllowedSourceEvidenceKinds = CommercialSalesPriceEvidenceKinds.ManualDevelopmentSimulator,
            RequiredTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredFreightInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredGrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            ReferenceCenter = CommercialIntelligenceConstants.ReferenceCenterMedian,
            RuleSetVersion = CommercialIntelligenceConstants.SalesRuleSetVersion,
            SourceProvenance = provenance,
            CreateIdempotencyKey = "commsale1-pilot-profile-bootstrap",
            CreateIdempotencyPayloadFingerprint = "commsale1-pilot-profile-bootstrap-fingerprint",
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureObservationAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        string customerReference,
        string sourceEvidenceKind,
        string observationSetCode,
        string sourceDocumentReference,
        string sourceLineReference,
        DateTimeOffset commercialEventAtUtc,
        decimal quantity,
        decimal originalUnitPrice,
        decimal comparableUnitPrice,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken,
        string currencyCode = "TRY",
        string normalizedUnit = "kg",
        string grossNet = CommercialSalesPriceGrossNetBasis.Net,
        string? freightInclusion = null)
    {
        var exists = await dbContext.CommercialSalesPriceEvidenceObservations
            .AnyAsync(o => o.Id == id, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.CommercialSalesPriceEvidenceObservations.Add(new CommercialSalesPriceEvidenceObservation
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = materialId,
            CustomerReference = customerReference,
            SourceEvidenceKind = sourceEvidenceKind,
            SourceDocumentReference = sourceDocumentReference,
            SourceLineReference = sourceLineReference,
            CommercialEventAtUtc = commercialEventAtUtc,
            ReceivedAtUtc = commercialEventAtUtc,
            Quantity = quantity,
            SourceUnit = normalizedUnit,
            NormalizedQuantity = quantity,
            NormalizedUnit = normalizedUnit,
            OriginalUnitPrice = originalUnitPrice,
            ComparableUnitPrice = comparableUnitPrice,
            CurrencyCode = currencyCode,
            PriceBasisGrossNet = grossNet,
            PriceBasisTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisFreightInclusion = freightInclusion ?? CommercialIntelligencePriceBasisInclusionStates.Excluded,
            IsCancelledOrReturned = false,
            EvidenceStatus = CommercialIntelligenceEvidenceStatuses.Simulated,
            SourceFingerprint = $"commsale1-{id:N}",
            SourceProvenance = provenance,
            ObservationSetCode = observationSetCode,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
