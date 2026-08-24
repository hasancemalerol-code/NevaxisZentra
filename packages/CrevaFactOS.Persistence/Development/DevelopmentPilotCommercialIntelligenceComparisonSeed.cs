using CrevaFactOS.Persistence.CommercialIntelligence;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotCommercialIntelligenceComparisonSeed
{
    internal static async Task EnsureCommercialIntelligenceComparisonPilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = CommercialIntelligenceConstants.DevelopmentPilotProvenance;
        var profile = await dbContext.CommercialPurchasePriceProfiles
            .SingleOrDefaultAsync(p => p.Id == DevelopmentPilotIds.CommercialIntelligencePilotProfileId, cancellationToken);
        if (profile is null)
        {
            return;
        }

        await SeedTemporalObservationsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        await SeedSupplierTieObservationsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        await SeedInsufficientCurrentWindowObservationsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        await SeedSourceConflictObservationsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        await EnsurePilotComparisonRunsAsync(
            dbContext,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);
    }

    private static async Task SeedTemporalObservationsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var referencePrices = new[] { 100m, 100m, 100m, 100m, 100m, 100m };
        for (var index = 0; index < referencePrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-027{index:x}-4000-8000-00000000027{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
                "COMMINT2-SET-TEMPORAL-REFERENCE",
                $"PO-SIM-TREF-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowStartUtc.AddDays(index * 10),
                referencePrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }

        var stableCurrentPrices = new[] { 105m, 105m, 105m, 105m, 105m, 105m };
        for (var index = 0; index < stableCurrentPrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-028{index:x}-4000-8000-00000000028{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
                "COMMINT2-SET-TEMPORAL-STABLE",
                $"PO-SIM-TSTB-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowStartUtc.AddDays(index * 3),
                stableCurrentPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }

        var upwardCurrentPrices = new[] { 115m, 115m, 115m, 115m, 115m, 115m };
        for (var index = 0; index < upwardCurrentPrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-029{index:x}-4000-8000-00000000029{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
                "COMMINT2-SET-TEMPORAL-UPWARD",
                $"PO-SIM-TUP-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowStartUtc.AddDays(index * 3),
                upwardCurrentPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }

        var favorableCurrentPrices = new[] { 90m, 90m, 90m, 90m, 90m, 90m };
        for (var index = 0; index < favorableCurrentPrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-02a{index:x}-4000-8000-0000000002a{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
                "COMMINT2-SET-TEMPORAL-FAVORABLE",
                $"PO-SIM-TFAV-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowStartUtc.AddDays(index * 3),
                favorableCurrentPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task SeedSupplierTieObservationsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var tiePrices = new[] { 100m, 100m, 101m, 101m, 99m, 99m };
        for (var index = 0; index < tiePrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-02b{index:x}-4000-8000-0000000002b{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
                "COMMINT2-SET-SUPPLIER-TIE",
                $"PO-SIM-TIE-SUP001-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowStartUtc.AddDays(index * 5),
                tiePrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);

            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-02c{index:x}-4000-8000-0000000002c{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferenceTie,
                "COMMINT2-SET-SUPPLIER-TIE",
                $"PO-SIM-TIE-SUP003-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowStartUtc.AddDays(index * 5 + 1),
                tiePrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task SeedInsufficientCurrentWindowObservationsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var insufficientPrices = new[] { 102m, 103m, 104m };
        for (var index = 0; index < insufficientPrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-02d{index:x}-4000-8000-0000000002d{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
                "COMMINT2-SET-TEMPORAL-INSUFFICIENT",
                $"PO-SIM-TINS-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowStartUtc.AddDays(index),
                insufficientPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task SeedSourceConflictObservationsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        await EnsureObservationAsync(
            dbContext,
            Guid.Parse("bb000001-02e0-4000-8000-0000000002e0"),
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT2-SET-SOURCE-CONFLICT",
            "PO-SIM-SC2-EUR-001",
            "LINE-001",
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowStartUtc.AddDays(10),
            4.5m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            currencyCode: "EUR");

        await EnsureObservationAsync(
            dbContext,
            Guid.Parse("bb000001-02e1-4000-8000-0000000002e1"),
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialIntelligencePilotSupplierReferencePrimary,
            "COMMINT2-SET-SOURCE-CONFLICT",
            "PO-SIM-SC2-TON-001",
            "LINE-001",
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowStartUtc.AddDays(11),
            98000m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            normalizedUnit: "ton");
    }

    private static async Task EnsurePilotComparisonRunsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        CommercialPurchasePriceProfile profile,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var context = new CommercialIntelligenceOperationContext(
            tenantId,
            facilityId,
            Guid.Empty,
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            CommercialIntelligencePermissionKeys.Manage,
            null);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotStableTemporalRunId,
            "CI-CMP-PILOT-STABLE",
            "commint2-pilot-stable-temporal",
            CommercialIntelligenceComparisonTypes.TemporalPriceDeviation,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            new EvaluateCommercialPurchasePriceComparisonCommand(
                profile.Id,
                profile.Version,
                CommercialIntelligenceComparisonTypes.TemporalPriceDeviation,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowEndUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowEndUtc,
                "TRY",
                "kg",
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                provenance,
                "commint2-pilot-stable-temporal"),
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotUpwardTemporalRunId,
            "CI-CMP-PILOT-UPWARD",
            "commint2-pilot-upward-temporal",
            CommercialIntelligenceComparisonTypes.TemporalPriceDeviation,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            new EvaluateCommercialPurchasePriceComparisonCommand(
                profile.Id,
                profile.Version,
                CommercialIntelligenceComparisonTypes.TemporalPriceDeviation,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowEndUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowEndUtc,
                "TRY",
                "kg",
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                provenance,
                "commint2-pilot-upward-temporal"),
            cancellationToken,
            observationSetCode: "COMMINT2-SET-TEMPORAL-UPWARD");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotFavorableTemporalRunId,
            "CI-CMP-PILOT-FAVORABLE",
            "commint2-pilot-favorable-temporal",
            CommercialIntelligenceComparisonTypes.TemporalPriceDeviation,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            new EvaluateCommercialPurchasePriceComparisonCommand(
                profile.Id,
                profile.Version,
                CommercialIntelligenceComparisonTypes.TemporalPriceDeviation,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowEndUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowEndUtc,
                "TRY",
                "kg",
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                provenance,
                "commint2-pilot-favorable-temporal"),
            cancellationToken,
            observationSetCode: "COMMINT2-SET-TEMPORAL-FAVORABLE");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierSpreadRunId,
            "CI-CMP-PILOT-SPREAD",
            "commint2-pilot-supplier-spread",
            CommercialIntelligenceComparisonTypes.SupplierPricePosition,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            new EvaluateCommercialPurchasePriceComparisonCommand(
                profile.Id,
                profile.Version,
                CommercialIntelligenceComparisonTypes.SupplierPricePosition,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowEndUtc,
                null,
                null,
                "TRY",
                "kg",
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                provenance,
                "commint2-pilot-supplier-spread"),
            cancellationToken,
            observationSetCode: "COMMINT1-SET-SUPPLIER-SPREAD");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierTieRunId,
            "CI-CMP-PILOT-TIE",
            "commint2-pilot-supplier-tie",
            CommercialIntelligenceComparisonTypes.SupplierPricePosition,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            new EvaluateCommercialPurchasePriceComparisonCommand(
                profile.Id,
                profile.Version,
                CommercialIntelligenceComparisonTypes.SupplierPricePosition,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowEndUtc,
                null,
                null,
                "TRY",
                "kg",
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                provenance,
                "commint2-pilot-supplier-tie"),
            cancellationToken,
            observationSetCode: "COMMINT2-SET-SUPPLIER-TIE");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotInsufficientTemporalRunId,
            "CI-CMP-PILOT-INSUFFICIENT",
            "commint2-pilot-insufficient-temporal",
            CommercialIntelligenceComparisonTypes.TemporalPriceDeviation,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            new EvaluateCommercialPurchasePriceComparisonCommand(
                profile.Id,
                profile.Version,
                CommercialIntelligenceComparisonTypes.TemporalPriceDeviation,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotCurrentWindowEndUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotReferenceWindowEndUtc,
                "TRY",
                "kg",
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                provenance,
                "commint2-pilot-insufficient-temporal"),
            cancellationToken,
            observationSetCode: "COMMINT2-SET-TEMPORAL-INSUFFICIENT");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSourceConflictRunId,
            "CI-CMP-PILOT-CONFLICT",
            "commint2-pilot-source-conflict",
            CommercialIntelligenceComparisonTypes.SupplierPricePosition,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            new EvaluateCommercialPurchasePriceComparisonCommand(
                profile.Id,
                profile.Version,
                CommercialIntelligenceComparisonTypes.SupplierPricePosition,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowStartUtc,
                DevelopmentPilotIds.CommercialIntelligenceComparisonPilotSupplierWindowEndUtc,
                null,
                null,
                "TRY",
                "kg",
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CommercialIntelligencePriceBasisInclusionStates.Excluded,
                provenance,
                "commint2-pilot-source-conflict"),
            cancellationToken,
            observationSetCode: "COMMINT2-SET-SOURCE-CONFLICT");
    }

    private static async Task EnsureSeededRunAsync(
        CrevaFactOSDbContext dbContext,
        Guid runId,
        string comparisonReference,
        string idempotencyKey,
        string comparisonType,
        Guid tenantId,
        Guid facilityId,
        CommercialPurchasePriceProfile profile,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceComparisonCommand command,
        CancellationToken cancellationToken,
        string? observationSetCode = null)
    {
        var exists = await dbContext.CommercialPurchasePriceComparisonRuns
            .AnyAsync(r => r.Id == runId, cancellationToken);
        if (exists)
        {
            return;
        }

        CommercialPurchasePriceComparisonEvaluationOutput evaluation;
        if (string.Equals(comparisonType, CommercialIntelligenceComparisonTypes.TemporalPriceDeviation, StringComparison.Ordinal))
        {
            var currentQuery = dbContext.CommercialPurchasePriceEvidenceObservations.AsNoTracking()
                .Where(o => o.TenantId == tenantId
                            && o.FacilityId == facilityId
                            && o.MaterialId == command.MaterialId
                            && o.CommercialEventAtUtc >= command.CurrentWindowStartUtc
                            && o.CommercialEventAtUtc <= command.CurrentWindowEndUtc);
            if (!string.IsNullOrWhiteSpace(observationSetCode))
            {
                currentQuery = currentQuery.Where(o => o.ObservationSetCode == observationSetCode);
            }

            var referenceQuery = dbContext.CommercialPurchasePriceEvidenceObservations.AsNoTracking()
                .Where(o => o.TenantId == tenantId
                            && o.FacilityId == facilityId
                            && o.MaterialId == command.MaterialId
                            && o.CommercialEventAtUtc >= command.ReferenceWindowStartUtc
                            && o.CommercialEventAtUtc <= command.ReferenceWindowEndUtc);
            if (!string.IsNullOrWhiteSpace(observationSetCode) && observationSetCode != "COMMINT2-SET-TEMPORAL-INSUFFICIENT")
            {
                referenceQuery = referenceQuery.Where(o => o.ObservationSetCode == observationSetCode || o.ObservationSetCode == "COMMINT2-SET-TEMPORAL-REFERENCE");
            }
            else if (observationSetCode == "COMMINT2-SET-TEMPORAL-INSUFFICIENT")
            {
                currentQuery = currentQuery.Where(o => o.ObservationSetCode == observationSetCode);
                referenceQuery = referenceQuery.Where(o => o.ObservationSetCode == "COMMINT2-SET-TEMPORAL-REFERENCE");
            }
            else
            {
                referenceQuery = referenceQuery.Where(o => o.ObservationSetCode == "COMMINT2-SET-TEMPORAL-REFERENCE");
                currentQuery = currentQuery.Where(o => o.ObservationSetCode == "COMMINT2-SET-TEMPORAL-STABLE");
            }

            var currentEvidence = await BuildEvidenceFromObservationsAsync(
                await currentQuery.ToListAsync(cancellationToken),
                command,
                profile);
            var referenceEvidence = await BuildEvidenceFromObservationsAsync(
                await referenceQuery.ToListAsync(cancellationToken),
                command,
                profile);

            evaluation = CommercialPurchasePriceComparisonEvaluator.EvaluateTemporalDeviation(
                profile,
                command.NormalizedUnit,
                currentEvidence.Evidence,
                referenceEvidence.Evidence,
                currentEvidence.ExclusionCounts,
                referenceEvidence.ExclusionCounts);
        }
        else
        {
            var query = dbContext.CommercialPurchasePriceEvidenceObservations.AsNoTracking()
                .Where(o => o.TenantId == tenantId
                            && o.FacilityId == facilityId
                            && o.MaterialId == command.MaterialId
                            && o.CommercialEventAtUtc >= command.CurrentWindowStartUtc
                            && o.CommercialEventAtUtc <= command.CurrentWindowEndUtc);
            if (!string.IsNullOrWhiteSpace(observationSetCode))
            {
                query = query.Where(o => o.ObservationSetCode == observationSetCode);
            }

            var built = await BuildEvidenceFromObservationsAsync(
                await query.ToListAsync(cancellationToken),
                command,
                profile);
            evaluation = CommercialPurchasePriceComparisonEvaluator.EvaluateSupplierPricePosition(
                profile,
                command.NormalizedUnit,
                built.Evidence,
                built.ExclusionCounts);
        }

        var fingerprint = CommercialIntelligenceIdempotencyFingerprint.ComputeComparisonFingerprint(context, command);
        var now = seedTimestampUtc;
        var run = new CommercialPurchasePriceComparisonRun
        {
            Id = runId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ComparisonReference = comparisonReference,
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ComparisonType = comparisonType,
            MaterialId = command.MaterialId,
            CurrentWindowStartUtc = command.CurrentWindowStartUtc,
            CurrentWindowEndUtc = command.CurrentWindowEndUtc,
            ReferenceWindowStartUtc = command.ReferenceWindowStartUtc,
            ReferenceWindowEndUtc = command.ReferenceWindowEndUtc,
            CurrencyCode = command.CurrencyCode,
            NormalizedUnit = command.NormalizedUnit,
            PriceBasisTaxInclusion = command.PriceBasisTaxInclusion,
            PriceBasisDiscountInclusion = command.PriceBasisDiscountInclusion,
            PriceBasisFreightInclusion = command.PriceBasisFreightInclusion,
            EvidenceStatus = evaluation.EvidenceStatus,
            ComparisonOutcome = evaluation.ComparisonOutcome,
            CurrentMedianUnitPrice = evaluation.CurrentMedianUnitPrice,
            ReferenceMedianUnitPrice = evaluation.ReferenceMedianUnitPrice,
            SignedVarianceRatio = evaluation.SignedVarianceRatio,
            SupplierSpreadRatio = evaluation.SupplierSpreadRatio,
            ComparableObservationCount = evaluation.ComparableObservationCount,
            ExcludedObservationCount = evaluation.ExcludedObservationCount,
            LeadingDriverCode = evaluation.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.ComparisonRuleSetVersion,
            IdempotencyKey = idempotencyKey,
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = provenance,
            EvaluatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actorSubjectId,
        };

        foreach (var supplierResult in evaluation.SupplierResults)
        {
            var resultId = Guid.NewGuid();
            run.Results.Add(new CommercialPurchasePriceComparisonResult
            {
                Id = resultId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ComparisonRunId = run.Id,
                SupplierReference = supplierResult.SupplierReference,
                ComparableObservationCount = supplierResult.ComparableObservationCount,
                SupplierMedianUnitPrice = supplierResult.SupplierMedianUnitPrice,
                LowestComparableMedianUnitPrice = supplierResult.LowestComparableMedianUnitPrice,
                RelativePricePositionRatio = supplierResult.RelativePricePositionRatio,
                PriceRank = supplierResult.PriceRank,
                IsTied = supplierResult.IsTied,
                PositionState = supplierResult.PositionState,
                EvidenceStatus = supplierResult.EvidenceStatus,
                CreatedAtUtc = now,
            });
        }

        var orderedDrivers = CommercialPurchasePriceComparisonEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            run.Drivers.Add(new CommercialPurchasePriceComparisonDriver
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ComparisonRunId = run.Id,
                ReasonCode = driver.ReasonCode,
                Severity = driver.Severity,
                ActualValue = driver.ActualValue,
                ReferenceValue = driver.ReferenceValue,
                ThresholdValue = driver.ThresholdValue,
                Unit = driver.Unit,
                NormalizedVariance = driver.NormalizedVariance,
                AffectedObservationCount = driver.AffectedObservationCount,
                SupportingText = driver.SupportingText,
                SourceProvenance = provenance,
                SortOrder = index + 1,
                IsLeadingDriver = index == 0,
                CreatedAtUtc = now,
            });
        }

        dbContext.CommercialPurchasePriceComparisonRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Task<(CommercialPurchasePriceEvidenceReadResult Evidence, Dictionary<string, int> ExclusionCounts)>
        BuildEvidenceFromObservationsAsync(
            IReadOnlyList<CommercialPurchasePriceEvidenceObservation> observations,
            EvaluateCommercialPurchasePriceComparisonCommand command,
            CommercialPurchasePriceProfile profile)
    {
        var allowedStatuses = ParseAllowedStatuses(profile.AllowedEvidenceStatuses);
        var comparable = new List<CommercialPurchasePriceComparableObservation>();
        var excluded = new List<CommercialPurchasePriceExcludedObservation>();
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in observations)
        {
            if (observation.Quantity <= 0m || observation.UnitPrice <= 0m)
            {
                excluded.Add(new CommercialPurchasePriceExcludedObservation(
                    observation.Id,
                    CommercialIntelligenceExclusionReasonCodes.InvalidQuantityOrPrice));
                continue;
            }

            if (!allowedStatuses.Contains(observation.EvidenceStatus))
            {
                excluded.Add(new CommercialPurchasePriceExcludedObservation(
                    observation.Id,
                    CommercialIntelligenceExclusionReasonCodes.DisallowedEvidenceStatus));
                continue;
            }

            if (!string.Equals(observation.CurrencyCode, command.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                excluded.Add(new CommercialPurchasePriceExcludedObservation(
                    observation.Id,
                    CommercialIntelligenceExclusionReasonCodes.IncompatibleCurrency));
                continue;
            }

            var observationUnit = observation.NormalizedUnit ?? observation.SourceUnit;
            if (!string.Equals(observationUnit, command.NormalizedUnit, StringComparison.OrdinalIgnoreCase))
            {
                excluded.Add(new CommercialPurchasePriceExcludedObservation(
                    observation.Id,
                    CommercialIntelligenceExclusionReasonCodes.IncompatibleUnit));
                continue;
            }

            if (!string.Equals(observation.PriceBasisTaxInclusion, command.PriceBasisTaxInclusion, StringComparison.Ordinal)
                || !string.Equals(observation.PriceBasisDiscountInclusion, command.PriceBasisDiscountInclusion, StringComparison.Ordinal)
                || !string.Equals(observation.PriceBasisFreightInclusion, command.PriceBasisFreightInclusion, StringComparison.Ordinal))
            {
                excluded.Add(new CommercialPurchasePriceExcludedObservation(
                    observation.Id,
                    CommercialIntelligenceExclusionReasonCodes.IncompatiblePriceBasis));
                continue;
            }

            if (seenFingerprints.Contains(observation.SourceFingerprint))
            {
                excluded.Add(new CommercialPurchasePriceExcludedObservation(
                    observation.Id,
                    CommercialIntelligenceExclusionReasonCodes.DuplicateSourceEvidence));
                continue;
            }

            seenFingerprints.Add(observation.SourceFingerprint);
            comparable.Add(new CommercialPurchasePriceComparableObservation(
                observation.Id,
                observation.UnitPrice,
                observation.SupplierReference));
        }

        return Task.FromResult((
            new CommercialPurchasePriceEvidenceReadResult(comparable, excluded),
            CommercialIntelligencePriceEvidenceReader.SummarizeExclusions(excluded)));
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

    private static async Task EnsureObservationAsync(
        CrevaFactOSDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        string supplierReference,
        string observationSetCode,
        string sourceDocumentReference,
        string sourceLineReference,
        DateTimeOffset commercialEventAtUtc,
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
            Quantity = 100m,
            SourceUnit = normalizedUnit,
            NormalizedQuantity = 100m,
            NormalizedUnit = normalizedUnit,
            UnitPrice = unitPrice,
            CurrencyCode = currencyCode,
            PriceBasisTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisFreightInclusion = freightInclusion ?? CommercialIntelligencePriceBasisInclusionStates.Excluded,
            EvidenceStatus = CommercialIntelligenceEvidenceStatuses.Simulated,
            SourceFingerprint = $"commint2-{id:N}",
            SourceProvenance = provenance,
            ObservationSetCode = observationSetCode,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
