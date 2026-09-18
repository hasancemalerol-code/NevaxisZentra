using NevaxisZentra.Persistence.CommercialIntelligence;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotCommercialIntelligenceSeed
{
    internal static async Task EnsureCommercialIntelligencePilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = CommercialIntelligenceConstants.DevelopmentPilotProvenance;
        var eventTime = DevelopmentPilotIds.CommercialIntelligencePilotSeedTimeUtc;
        var windowStart = eventTime.AddDays(-120);

        await EnsurePilotProfileAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotWithinRangeObservation1Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-WITHIN-RANGE",
            "PO-SIM-WR-001",
            "LINE-001",
            eventTime.AddDays(-60),
            100m,
            100m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotWithinRangeObservation2Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-WITHIN-RANGE",
            "PO-SIM-WR-002",
            "LINE-001",
            eventTime.AddDays(-55),
            100m,
            100m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotWithinRangeObservation3Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-WITHIN-RANGE",
            "PO-SIM-WR-003",
            "LINE-001",
            eventTime.AddDays(-50),
            101m,
            101m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotWithinRangeObservation4Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-WITHIN-RANGE",
            "PO-SIM-WR-004",
            "LINE-001",
            eventTime.AddDays(-45),
            101m,
            101m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotWithinRangeObservation5Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-WITHIN-RANGE",
            "PO-SIM-WR-005",
            "LINE-001",
            eventTime.AddDays(-40),
            102m,
            102m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotWithinRangeObservation6Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-WITHIN-RANGE",
            "PO-SIM-WR-006",
            "LINE-001",
            eventTime.AddDays(-35),
            102m,
            102m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotInsufficientObservation1Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-INSUFFICIENT-HISTORY",
            "PO-SIM-IH-001",
            "LINE-001",
            eventTime.AddDays(-3),
            99m,
            99m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotInsufficientObservation2Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-INSUFFICIENT-HISTORY",
            "PO-SIM-IH-002",
            "LINE-001",
            eventTime.AddDays(-2),
            100m,
            100m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotInsufficientObservation3Id,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-INSUFFICIENT-HISTORY",
            "PO-SIM-IH-003",
            "LINE-001",
            eventTime.AddDays(-1),
            101m,
            101m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        var supplierSpreadPricesSup001 = new[] { 95m, 96m, 96m, 97m, 97m, 98m };
        var supplierSpreadIdsSup001 = new[]
        {
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup001Observation1Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup001Observation2Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup001Observation3Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup001Observation4Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup001Observation5Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup001Observation6Id,
        };

        for (var index = 0; index < supplierSpreadPricesSup001.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                supplierSpreadIdsSup001[index],
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
                "COMMINT1-SET-SUPPLIER-SPREAD",
                $"PO-SIM-SS-SUP001-{index + 1:00}",
                "LINE-001",
                eventTime.AddDays(-20 - index),
                supplierSpreadPricesSup001[index],
                supplierSpreadPricesSup001[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }

        var supplierSpreadPricesSup002 = new[] { 110m, 111m, 111m, 112m, 112m, 113m };
        var supplierSpreadIdsSup002 = new[]
        {
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup002Observation1Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup002Observation2Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup002Observation3Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup002Observation4Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup002Observation5Id,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierSpreadSup002Observation6Id,
        };

        for (var index = 0; index < supplierSpreadPricesSup002.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                supplierSpreadIdsSup002[index],
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferenceSecondary,
                "COMMINT1-SET-SUPPLIER-SPREAD",
                $"PO-SIM-SS-SUP002-{index + 1:00}",
                "LINE-001",
                eventTime.AddDays(-20 - index),
                supplierSpreadPricesSup002[index],
                supplierSpreadPricesSup002[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotConflictCurrencyObservationId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-SOURCE-CONFLICT",
            "PO-SIM-SC-EUR-001",
            "LINE-001",
            eventTime.AddDays(-10),
            4.5m,
            4.5m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            currencyCode: "EUR");

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotConflictUnitObservationId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-SOURCE-CONFLICT",
            "PO-SIM-SC-TON-001",
            "LINE-001",
            eventTime.AddDays(-9),
            98000m,
            98000m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            normalizedUnit: "ton");

        await EnsureObservationAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligencePilotConflictBasisObservationId,
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT1-SET-SOURCE-CONFLICT",
            "PO-SIM-SC-BASIS-001",
            "LINE-001",
            eventTime.AddDays(-8),
            100m,
            100m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            freightInclusion: CommercialIntelligencePriceBasisInclusionStates.Included);

        _ = windowStart;
    }

    private static async Task EnsurePilotProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.CommercialPurchasePriceProfiles
            .SingleOrDefaultAsync(p => p.Id == DevelopmentPilotIds.CommercialIntelligencePilotProfileId, cancellationToken);
        if (profile is not null)
        {
            var requiresReconciliation =
                profile.SupplierSpreadThreshold != CommercialIntelligenceConstants.DefaultSupplierSpreadThreshold
                || profile.MaximumPositiveVarianceRatio != CommercialIntelligenceConstants.DefaultMaximumPositiveVarianceRatio;
            if (requiresReconciliation)
            {
                profile.SupplierSpreadThreshold = CommercialIntelligenceConstants.DefaultSupplierSpreadThreshold;
                profile.MaximumPositiveVarianceRatio = CommercialIntelligenceConstants.DefaultMaximumPositiveVarianceRatio;
                profile.UpdatedAtUtc = seedTimestampUtc;
                profile.UpdatedBy = actorSubjectId;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.CommercialPurchasePriceProfiles.Add(new CommercialPurchasePriceProfile
        {
            Id = DevelopmentPilotIds.CommercialIntelligencePilotProfileId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProfileCode = DevelopmentPilotIds.CommercialIntelligencePilotProfileCode,
            DisplayName = "Development pilot purchase price profile",
            Status = CommercialIntelligenceProfileStatuses.Approved,
            ComparisonDimension = CommercialIntelligenceComparisonDimensions.Material,
            ReferenceWindowDurationDays = 120,
            MinimumComparableObservations = CommercialIntelligenceConstants.DefaultMinimumComparableObservations,
            MaximumPositiveVarianceRatio = CommercialIntelligenceConstants.DefaultMaximumPositiveVarianceRatio,
            SupplierSpreadThreshold = CommercialIntelligenceConstants.DefaultSupplierSpreadThreshold,
            AllowedEvidenceStatuses = CommercialIntelligenceEvidenceStatuses.Simulated,
            RequiredTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredFreightInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            ReferenceCenter = CommercialIntelligenceConstants.ReferenceCenterMedian,
            RuleSetVersion = CommercialIntelligenceConstants.RuleSetVersion,
            SourceProvenance = provenance,
            CreateIdempotencyKey = "commint1-pilot-profile-bootstrap",
            CreateIdempotencyPayloadFingerprint = "commint1-pilot-profile-bootstrap-fingerprint",
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureObservationAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        string supplierReference,
        string observationSetCode,
        string sourceDocumentReference,
        string sourceLineReference,
        DateTimeOffset commercialEventAtUtc,
        decimal quantity,
        decimal unitPrice,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken,
        string currencyCode = "TRY",
        string normalizedUnit = "kg",
        string? freightInclusion = null)
    {
        var exists = await dbContext.CommercialPurchasePriceEvidenceObservations
            .AnyAsync(o => o.Id == id, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.CommercialPurchasePriceEvidenceObservations.Add(new CommercialPurchasePriceEvidenceObservation
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = materialId,
            SupplierReference = supplierReference,
            SourceDocumentType = "simulated-purchase-order",
            SourceDocumentReference = sourceDocumentReference,
            SourceLineReference = sourceLineReference,
            CommercialEventAtUtc = commercialEventAtUtc,
            Quantity = quantity,
            SourceUnit = normalizedUnit,
            NormalizedQuantity = quantity,
            NormalizedUnit = normalizedUnit,
            UnitPrice = unitPrice,
            CurrencyCode = currencyCode,
            PriceBasisTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisFreightInclusion = freightInclusion ?? CommercialIntelligencePriceBasisInclusionStates.Excluded,
            EvidenceStatus = CommercialIntelligenceEvidenceStatuses.Simulated,
            SourceFingerprint = $"commint1-{id:N}",
            SourceProvenance = provenance,
            ObservationSetCode = observationSetCode,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
