using NevaxisZentra.Persistence.CommercialIntelligence;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotCommercialSalesPriceComparisonSeed
{
    private const string Kind = CommercialSalesPriceEvidenceKinds.ManualDevelopmentSimulator;

    internal static async Task EnsureCommercialSalesPriceComparisonPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = CommercialIntelligenceConstants.SalesDevelopmentPilotProvenance;
        var profile = await dbContext.CommercialSalesPriceProfiles
            .SingleOrDefaultAsync(p => p.Id == DevelopmentPilotIds.CommercialSalesPricePilotProfileId, cancellationToken);
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

        await SeedCustomerSpreadObservationsAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        await SeedCustomerTieObservationsAsync(
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
        NevaxisZentraDbContext dbContext,
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
                Guid.Parse($"bb000001-030{index:x}-4000-8000-00000000030{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
                "COMMSALE2-SET-TEMPORAL-REFERENCE",
                $"SO-SIM-TREF-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotReferenceWindowStartUtc.AddDays(index * 12),
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
                Guid.Parse($"bb000001-031{index:x}-4000-8000-00000000031{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
                "COMMSALE2-SET-TEMPORAL-STABLE",
                $"SO-SIM-TSTB-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCurrentWindowStartUtc.AddDays(index * 3),
                stableCurrentPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }

        var decreaseCurrentPrices = new[] { 80m, 80m, 80m, 80m, 80m, 80m };
        for (var index = 0; index < decreaseCurrentPrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-032{index:x}-4000-8000-00000000032{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
                "COMMSALE2-SET-TEMPORAL-DECREASE",
                $"SO-SIM-TDEC-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCurrentWindowStartUtc.AddDays(index * 3),
                decreaseCurrentPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }

        var positiveCurrentPrices = new[] { 115m, 115m, 115m, 115m, 115m, 115m };
        for (var index = 0; index < positiveCurrentPrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-033{index:x}-4000-8000-00000000033{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
                "COMMSALE2-SET-TEMPORAL-POSITIVE",
                $"SO-SIM-TPOS-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCurrentWindowStartUtc.AddDays(index * 3),
                positiveCurrentPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task SeedCustomerSpreadObservationsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var spreadLowPrices = new[] { 100m, 100m, 101m, 101m, 99m, 99m };
        for (var index = 0; index < spreadLowPrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-034{index:x}-4000-8000-00000000034{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferenceSpreadPrimary,
                "COMMSALE2-SET-CUSTOMER-SPREAD",
                $"SO-SIM-SPR-A-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerWindowStartUtc.AddDays(index * 5),
                spreadLowPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }

        var spreadHighPrices = new[] { 140m, 140m, 141m, 141m, 139m, 139m };
        for (var index = 0; index < spreadHighPrices.Length; index++)
        {
            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-035{index:x}-4000-8000-00000000035{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferenceSpreadSecondary,
                "COMMSALE2-SET-CUSTOMER-SPREAD",
                $"SO-SIM-SPR-B-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerWindowStartUtc.AddDays(index * 5 + 1),
                spreadHighPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task SeedCustomerTieObservationsAsync(
        NevaxisZentraDbContext dbContext,
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
                Guid.Parse($"bb000001-036{index:x}-4000-8000-00000000036{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferenceTiePrimary,
                "COMMSALE2-SET-CUSTOMER-TIE",
                $"SO-SIM-TIE-A-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerWindowStartUtc.AddDays(index * 5),
                tiePrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);

            await EnsureObservationAsync(
                dbContext,
                Guid.Parse($"bb000001-037{index:x}-4000-8000-00000000037{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferenceTieSecondary,
                "COMMSALE2-SET-CUSTOMER-TIE",
                $"SO-SIM-TIE-B-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerWindowStartUtc.AddDays(index * 5 + 1),
                tiePrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task SeedInsufficientCurrentWindowObservationsAsync(
        NevaxisZentraDbContext dbContext,
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
                Guid.Parse($"bb000001-038{index:x}-4000-8000-00000000038{index:x}"),
                tenantId,
                facilityId,
                DevelopmentPilotIds.MaterialPilotPpId,
                DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
                "COMMSALE2-SET-TEMPORAL-INSUFFICIENT",
                $"SO-SIM-TINS-{index + 1:00}",
                "LINE-001",
                DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCurrentWindowStartUtc.AddDays(index),
                insufficientPrices[index],
                provenance,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task SeedSourceConflictObservationsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        await EnsureObservationAsync(
            dbContext,
            Guid.Parse("bb000001-0390-4000-8000-000000000390"),
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
            "COMMSALE2-SET-SOURCE-CONFLICT",
            "SO-SIM-SC2-EUR-001",
            "LINE-001",
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerWindowStartUtc.AddDays(10),
            4.5m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            currencyCode: "EUR");

        await EnsureObservationAsync(
            dbContext,
            Guid.Parse("bb000001-0391-4000-8000-000000000391"),
            tenantId,
            facilityId,
            DevelopmentPilotIds.MaterialPilotPpId,
            DevelopmentPilotIds.CommercialSalesPricePilotCustomerReferencePrimary,
            "COMMSALE2-SET-SOURCE-CONFLICT",
            "SO-SIM-SC2-TON-001",
            "LINE-001",
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerWindowStartUtc.AddDays(11),
            98000m,
            provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken,
            normalizedUnit: "ton");
    }

    private static async Task EnsurePilotComparisonRunsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        CommercialSalesPriceProfile profile,
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

        var basis = CommercialIntelligencePriceBasisInclusionStates.Excluded;
        var temporalCommandBase = new EvaluateCommercialSalesPriceComparisonCommand(
            profile.Id,
            profile.Version,
            CommercialSalesPriceComparisonTypes.TemporalSalesPriceDeviation,
            DevelopmentPilotIds.MaterialPilotPpId,
            null,
            Kind,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCurrentWindowStartUtc,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCurrentWindowEndUtc,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotReferenceWindowStartUtc,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotReferenceWindowEndUtc,
            "TRY",
            "kg",
            CommercialSalesPriceGrossNetBasis.Net,
            basis,
            basis,
            basis,
            provenance,
            string.Empty);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotStableTemporalRunId,
            "COMMSALE2-CMP-PILOT-STABLE",
            "commsale2-pilot-stable-temporal",
            CommercialSalesPriceComparisonTypes.TemporalSalesPriceDeviation,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with { IdempotencyKey = "commsale2-pilot-stable-temporal" },
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotDecreaseTemporalRunId,
            "COMMSALE2-CMP-PILOT-DECREASE",
            "commsale2-pilot-decrease-temporal",
            CommercialSalesPriceComparisonTypes.TemporalSalesPriceDeviation,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with { IdempotencyKey = "commsale2-pilot-decrease-temporal" },
            cancellationToken,
            observationSetCode: "COMMSALE2-SET-TEMPORAL-DECREASE");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotPositiveVarianceTemporalRunId,
            "COMMSALE2-CMP-PILOT-POSITIVE",
            "commsale2-pilot-positive-variance-temporal",
            CommercialSalesPriceComparisonTypes.TemporalSalesPriceDeviation,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with { IdempotencyKey = "commsale2-pilot-positive-variance-temporal" },
            cancellationToken,
            observationSetCode: "COMMSALE2-SET-TEMPORAL-POSITIVE");

        var customerCommandBase = new EvaluateCommercialSalesPriceComparisonCommand(
            profile.Id,
            profile.Version,
            CommercialSalesPriceComparisonTypes.CustomerPricePosition,
            DevelopmentPilotIds.MaterialPilotPpId,
            null,
            Kind,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerWindowStartUtc,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerWindowEndUtc,
            null,
            null,
            "TRY",
            "kg",
            CommercialSalesPriceGrossNetBasis.Net,
            basis,
            basis,
            basis,
            provenance,
            string.Empty);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerSpreadRunId,
            "COMMSALE2-CMP-PILOT-SPREAD",
            "commsale2-pilot-customer-spread",
            CommercialSalesPriceComparisonTypes.CustomerPricePosition,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            customerCommandBase with { IdempotencyKey = "commsale2-pilot-customer-spread" },
            cancellationToken,
            observationSetCode: "COMMSALE2-SET-CUSTOMER-SPREAD");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotCustomerTieRunId,
            "COMMSALE2-CMP-PILOT-TIE",
            "commsale2-pilot-customer-tie",
            CommercialSalesPriceComparisonTypes.CustomerPricePosition,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            customerCommandBase with { IdempotencyKey = "commsale2-pilot-customer-tie" },
            cancellationToken,
            observationSetCode: "COMMSALE2-SET-CUSTOMER-TIE");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotInsufficientTemporalRunId,
            "COMMSALE2-CMP-PILOT-INSUFFICIENT",
            "commsale2-pilot-insufficient-temporal",
            CommercialSalesPriceComparisonTypes.TemporalSalesPriceDeviation,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with { IdempotencyKey = "commsale2-pilot-insufficient-temporal" },
            cancellationToken,
            observationSetCode: "COMMSALE2-SET-TEMPORAL-INSUFFICIENT");

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialSalesPriceComparisonPilotSourceConflictRunId,
            "COMMSALE2-CMP-PILOT-CONFLICT",
            "commsale2-pilot-source-conflict",
            CommercialSalesPriceComparisonTypes.CustomerPricePosition,
            tenantId,
            facilityId,
            profile,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            customerCommandBase with { IdempotencyKey = "commsale2-pilot-source-conflict" },
            cancellationToken,
            observationSetCode: "COMMSALE2-SET-SOURCE-CONFLICT");
    }

    private static async Task EnsureSeededRunAsync(
        NevaxisZentraDbContext dbContext,
        Guid runId,
        string comparisonReference,
        string idempotencyKey,
        string comparisonType,
        Guid tenantId,
        Guid facilityId,
        CommercialSalesPriceProfile profile,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceComparisonCommand command,
        CancellationToken cancellationToken,
        string? observationSetCode = null)
    {
        var exists = await dbContext.CommercialSalesPriceComparisonRuns
            .AnyAsync(r => r.Id == runId, cancellationToken);
        if (exists)
        {
            return;
        }

        CommercialSalesPriceComparisonEvaluationOutput evaluation;
        if (string.Equals(comparisonType, CommercialSalesPriceComparisonTypes.TemporalSalesPriceDeviation, StringComparison.Ordinal))
        {
            var currentQuery = dbContext.CommercialSalesPriceEvidenceObservations.AsNoTracking()
                .Where(o => o.TenantId == tenantId
                            && o.FacilityId == facilityId
                            && o.MaterialId == command.MaterialId
                            && o.CommercialEventAtUtc >= command.CurrentWindowStartUtc
                            && o.CommercialEventAtUtc <= command.CurrentWindowEndUtc);
            if (!string.IsNullOrWhiteSpace(observationSetCode))
            {
                currentQuery = currentQuery.Where(o => o.ObservationSetCode == observationSetCode);
            }

            var referenceQuery = dbContext.CommercialSalesPriceEvidenceObservations.AsNoTracking()
                .Where(o => o.TenantId == tenantId
                            && o.FacilityId == facilityId
                            && o.MaterialId == command.MaterialId
                            && o.CommercialEventAtUtc >= command.ReferenceWindowStartUtc
                            && o.CommercialEventAtUtc <= command.ReferenceWindowEndUtc);
            if (!string.IsNullOrWhiteSpace(observationSetCode) && observationSetCode != "COMMSALE2-SET-TEMPORAL-INSUFFICIENT")
            {
                referenceQuery = referenceQuery.Where(o => o.ObservationSetCode == observationSetCode || o.ObservationSetCode == "COMMSALE2-SET-TEMPORAL-REFERENCE");
            }
            else if (observationSetCode == "COMMSALE2-SET-TEMPORAL-INSUFFICIENT")
            {
                currentQuery = currentQuery.Where(o => o.ObservationSetCode == observationSetCode);
                referenceQuery = referenceQuery.Where(o => o.ObservationSetCode == "COMMSALE2-SET-TEMPORAL-REFERENCE");
            }
            else
            {
                referenceQuery = referenceQuery.Where(o => o.ObservationSetCode == "COMMSALE2-SET-TEMPORAL-REFERENCE");
                currentQuery = currentQuery.Where(o => o.ObservationSetCode == "COMMSALE2-SET-TEMPORAL-STABLE");
            }

            var currentEvidence = BuildEvidenceFromObservations(
                await currentQuery.ToListAsync(cancellationToken),
                command,
                profile);
            var referenceEvidence = BuildEvidenceFromObservations(
                await referenceQuery.ToListAsync(cancellationToken),
                command,
                profile);

            evaluation = CommercialSalesPriceComparisonEvaluator.EvaluateTemporalDeviation(
                profile,
                command.NormalizedUnit,
                currentEvidence.Evidence,
                referenceEvidence.Evidence,
                currentEvidence.ExclusionCounts,
                referenceEvidence.ExclusionCounts);
        }
        else
        {
            var query = dbContext.CommercialSalesPriceEvidenceObservations.AsNoTracking()
                .Where(o => o.TenantId == tenantId
                            && o.FacilityId == facilityId
                            && o.MaterialId == command.MaterialId
                            && o.CommercialEventAtUtc >= command.CurrentWindowStartUtc
                            && o.CommercialEventAtUtc <= command.CurrentWindowEndUtc);
            if (!string.IsNullOrWhiteSpace(observationSetCode))
            {
                query = query.Where(o => o.ObservationSetCode == observationSetCode);
            }

            var built = BuildEvidenceFromObservations(
                await query.ToListAsync(cancellationToken),
                command,
                profile);
            evaluation = CommercialSalesPriceComparisonEvaluator.EvaluateCustomerPricePosition(
                profile,
                command.NormalizedUnit,
                built.Evidence,
                built.ExclusionCounts);
        }

        var fingerprint = CommercialSalesPriceIdempotencyFingerprint.ComputeComparisonFingerprint(context, command with { IdempotencyKey = idempotencyKey });
        var now = seedTimestampUtc;
        var run = new CommercialSalesPriceComparisonRun
        {
            Id = runId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ComparisonReference = comparisonReference,
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ComparisonType = comparisonType,
            MaterialId = command.MaterialId,
            CustomerReference = command.CustomerReference,
            SourceEvidenceKind = command.SourceEvidenceKind,
            CurrentWindowStartUtc = command.CurrentWindowStartUtc,
            CurrentWindowEndUtc = command.CurrentWindowEndUtc,
            ReferenceWindowStartUtc = command.ReferenceWindowStartUtc,
            ReferenceWindowEndUtc = command.ReferenceWindowEndUtc,
            CurrencyCode = command.CurrencyCode,
            NormalizedUnit = command.NormalizedUnit,
            PriceBasisGrossNet = command.PriceBasisGrossNet,
            PriceBasisTaxInclusion = command.PriceBasisTaxInclusion,
            PriceBasisDiscountInclusion = command.PriceBasisDiscountInclusion,
            PriceBasisFreightInclusion = command.PriceBasisFreightInclusion,
            EvidenceStatus = evaluation.EvidenceStatus,
            ComparisonOutcome = evaluation.ComparisonOutcome,
            CurrentMedianUnitPrice = evaluation.CurrentMedianUnitPrice,
            ReferenceMedianUnitPrice = evaluation.ReferenceMedianUnitPrice,
            SignedVarianceRatio = evaluation.SignedVarianceRatio,
            CustomerSpreadRatio = evaluation.CustomerSpreadRatio,
            CandidateObservationCount = evaluation.CandidateObservationCount,
            ComparableObservationCount = evaluation.ComparableObservationCount,
            ExcludedObservationCount = evaluation.ExcludedObservationCount,
            LeadingDriverCode = evaluation.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.SalesComparisonRuleSetVersion,
            IdempotencyKey = idempotencyKey,
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = provenance,
            EvaluatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actorSubjectId,
        };

        foreach (var customerResult in evaluation.CustomerResults)
        {
            run.Results.Add(new CommercialSalesPriceComparisonResult
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ComparisonRunId = run.Id,
                CustomerReference = customerResult.CustomerReference,
                ComparableObservationCount = customerResult.ComparableObservationCount,
                CustomerMedianUnitPrice = customerResult.CustomerMedianUnitPrice,
                LowestComparableMedianUnitPrice = customerResult.LowestComparableMedianUnitPrice,
                RelativePricePositionRatio = customerResult.RelativePricePositionRatio,
                PriceRank = customerResult.PriceRank,
                IsTied = customerResult.IsTied,
                PositionState = customerResult.PositionState,
                EvidenceStatus = customerResult.EvidenceStatus,
                CreatedAtUtc = now,
            });
        }

        var orderedDrivers = CommercialSalesPriceComparisonEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            run.Drivers.Add(new CommercialSalesPriceComparisonDriver
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ComparisonRunId = run.Id,
                ComparisonResultId = driver.ComparisonResultId,
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

        dbContext.CommercialSalesPriceComparisonRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (CommercialSalesPriceEvidenceReadResult Evidence, Dictionary<string, int> ExclusionCounts)
        BuildEvidenceFromObservations(
            List<CommercialSalesPriceEvidenceObservation> observations,
            EvaluateCommercialSalesPriceComparisonCommand command,
            CommercialSalesPriceProfile profile)
    {
        var allowedStatuses = ParseAllowedStatuses(profile.AllowedEvidenceStatuses);
        var allowedKinds = ParseAllowedKinds(profile.AllowedSourceEvidenceKinds);
        var comparable = new List<CommercialSalesPriceComparableObservation>();
        var excluded = new List<CommercialSalesPriceExcludedObservation>();
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in observations)
        {
            if (observation.Quantity <= 0m || observation.ComparableUnitPrice <= 0m || observation.OriginalUnitPrice <= 0m)
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(
                    observation.Id,
                    CommercialSalesPriceExclusionReasonCodes.InvalidQuantityOrPrice));
                continue;
            }

            if (!allowedStatuses.Contains(observation.EvidenceStatus))
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(
                    observation.Id,
                    CommercialSalesPriceExclusionReasonCodes.DisallowedEvidenceStatus));
                continue;
            }

            if (!allowedKinds.Contains(observation.SourceEvidenceKind))
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(
                    observation.Id,
                    CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind));
                continue;
            }

            if (!string.Equals(observation.SourceEvidenceKind, command.SourceEvidenceKind.Trim(), StringComparison.Ordinal))
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(
                    observation.Id,
                    CommercialSalesPriceExclusionReasonCodes.IncompatibleEvidenceKind));
                continue;
            }

            if (!string.Equals(observation.CurrencyCode, command.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(
                    observation.Id,
                    CommercialSalesPriceExclusionReasonCodes.IncompatibleCurrency));
                continue;
            }

            var observationUnit = observation.NormalizedUnit ?? observation.SourceUnit;
            if (!string.Equals(observationUnit, command.NormalizedUnit, StringComparison.OrdinalIgnoreCase))
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(
                    observation.Id,
                    CommercialSalesPriceExclusionReasonCodes.IncompatibleUnit));
                continue;
            }

            if (!string.Equals(observation.PriceBasisTaxInclusion, command.PriceBasisTaxInclusion, StringComparison.Ordinal)
                || !string.Equals(observation.PriceBasisDiscountInclusion, command.PriceBasisDiscountInclusion, StringComparison.Ordinal)
                || !string.Equals(observation.PriceBasisFreightInclusion, command.PriceBasisFreightInclusion, StringComparison.Ordinal)
                || !string.Equals(observation.PriceBasisGrossNet, command.PriceBasisGrossNet, StringComparison.Ordinal))
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(
                    observation.Id,
                    CommercialSalesPriceExclusionReasonCodes.IncompatiblePriceBasis));
                continue;
            }

            if (seenFingerprints.Contains(observation.SourceFingerprint))
            {
                excluded.Add(new CommercialSalesPriceExcludedObservation(
                    observation.Id,
                    CommercialSalesPriceExclusionReasonCodes.DuplicateSourceEvidence));
                continue;
            }

            seenFingerprints.Add(observation.SourceFingerprint);
            comparable.Add(new CommercialSalesPriceComparableObservation(
                observation.Id,
                observation.ComparableUnitPrice,
                observation.CustomerReference));
        }

        return (
            new CommercialSalesPriceEvidenceReadResult(observations.Count, comparable, excluded),
            CommercialSalesPriceEvidenceReader.SummarizeExclusions(excluded));
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

    private static async Task EnsureObservationAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        string customerReference,
        string observationSetCode,
        string sourceDocumentReference,
        string sourceLineReference,
        DateTimeOffset commercialEventAtUtc,
        decimal comparableUnitPrice,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken,
        string currencyCode = "TRY",
        string normalizedUnit = "kg",
        string grossNet = CommercialSalesPriceGrossNetBasis.Net)
    {
        var existing = await dbContext.CommercialSalesPriceEvidenceObservations
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (existing is not null)
        {
            var changed = false;
            if (existing.CommercialEventAtUtc != commercialEventAtUtc)
            {
                existing.CommercialEventAtUtc = commercialEventAtUtc;
                existing.ReceivedAtUtc = commercialEventAtUtc;
                changed = true;
            }

            if (!string.Equals(existing.ObservationSetCode, observationSetCode, StringComparison.Ordinal))
            {
                existing.ObservationSetCode = observationSetCode;
                changed = true;
            }

            if (existing.ComparableUnitPrice != comparableUnitPrice)
            {
                existing.ComparableUnitPrice = comparableUnitPrice;
                existing.OriginalUnitPrice = comparableUnitPrice;
                changed = true;
            }

            if (!string.Equals(existing.CustomerReference, customerReference, StringComparison.Ordinal))
            {
                existing.CustomerReference = customerReference;
                changed = true;
            }

            if (changed)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.CommercialSalesPriceEvidenceObservations.Add(new CommercialSalesPriceEvidenceObservation
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = materialId,
            CustomerReference = customerReference,
            SourceEvidenceKind = Kind,
            SourceDocumentReference = sourceDocumentReference,
            SourceLineReference = sourceLineReference,
            CommercialEventAtUtc = commercialEventAtUtc,
            ReceivedAtUtc = commercialEventAtUtc,
            Quantity = 10m,
            SourceUnit = normalizedUnit,
            NormalizedQuantity = 10m,
            NormalizedUnit = normalizedUnit,
            OriginalUnitPrice = comparableUnitPrice,
            ComparableUnitPrice = comparableUnitPrice,
            CurrencyCode = currencyCode,
            PriceBasisGrossNet = grossNet,
            PriceBasisTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            PriceBasisFreightInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            IsCancelledOrReturned = false,
            EvidenceStatus = CommercialIntelligenceEvidenceStatuses.Simulated,
            SourceFingerprint = $"commsale2-{id:N}",
            SourceProvenance = provenance,
            ObservationSetCode = observationSetCode,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
