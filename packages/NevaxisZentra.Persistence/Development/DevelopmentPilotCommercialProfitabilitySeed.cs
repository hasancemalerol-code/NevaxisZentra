using NevaxisZentra.Persistence.CommercialIntelligence;
using NevaxisZentra.Persistence.Material;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotCommercialProfitabilitySeed
{
    private const string SourceKind = CommercialProfitabilitySourceKinds.ManualDevelopmentSimulator;
    private const string CostBasis = CommercialSalesPriceGrossNetBasis.Net;

    internal static async Task EnsureCommercialProfitabilityPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = CommercialIntelligenceConstants.ProfitabilityDevelopmentPilotProvenance;
        var evaluatedAt = DevelopmentPilotIds.CommercialProfitabilityPilotSeedTimeUtc;
        var freshEvidenceTimestamp = evaluatedAt.AddDays(-30);
        var staleEvidenceTimestamp = evaluatedAt.AddDays(
            -(CommercialIntelligenceConstants.DefaultProfitabilityMaximumEvidenceAgeDays + 1));

        var profile = await EnsurePilotProfileAsync(
            dbContext,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        foreach (var scenario in PilotScenarios)
        {
            await EnsurePilotMaterialAsync(
                dbContext,
                scenario,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);

            await EnsureSalesEvidenceAsync(
                dbContext,
                scenario,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                freshEvidenceTimestamp,
                cancellationToken);

            await EnsureCostEvidenceAsync(
                dbContext,
                scenario,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                scenario.UseStaleCostEvidence ? staleEvidenceTimestamp : freshEvidenceTimestamp,
                cancellationToken);
        }

        await EnsurePilotAssessmentsAsync(
            dbContext,
            profile,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            evaluatedAt,
            cancellationToken);
    }

    private static readonly PilotScenario[] PilotScenarios =
    [
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentWithinThresholdId,
            PilotGuid(0x311),
            "COMMPROFIT1-MAT-WITHIN",
            PilotGuid(0x321),
            "COMMPROFIT1-CUST-WITHIN",
            "COMMPROFIT1-ASM-WITHIN",
            "commprofit1-pilot-within-threshold",
            "COMMPROFIT1-SET-WITHIN",
            100m,
            false,
            0,
            [
                Cost(0x341, CommercialProfitabilityCostComponentKinds.DirectMaterial, 35m),
                Cost(0x342, CommercialProfitabilityCostComponentKinds.DirectLabor, 15m),
                Cost(0x343, CommercialProfitabilityCostComponentKinds.Machine, 10m),
                Cost(0x344, CommercialProfitabilityCostComponentKinds.Energy, 10m),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentExactThresholdId,
            PilotGuid(0x312),
            "COMMPROFIT1-MAT-EXACT",
            PilotGuid(0x322),
            "COMMPROFIT1-CUST-EXACT",
            "COMMPROFIT1-ASM-EXACT",
            "commprofit1-pilot-exact-threshold",
            "COMMPROFIT1-SET-EXACT",
            100m,
            false,
            1,
            [
                Cost(0x345, CommercialProfitabilityCostComponentKinds.DirectMaterial, 43m),
                Cost(0x346, CommercialProfitabilityCostComponentKinds.DirectLabor, 17m),
                Cost(0x347, CommercialProfitabilityCostComponentKinds.Machine, 12m),
                Cost(0x348, CommercialProfitabilityCostComponentKinds.Energy, 13m),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentBelowThresholdId,
            PilotGuid(0x313),
            "COMMPROFIT1-MAT-BELOW",
            PilotGuid(0x323),
            "COMMPROFIT1-CUST-BELOW",
            "COMMPROFIT1-ASM-BELOW",
            "commprofit1-pilot-below-threshold",
            "COMMPROFIT1-SET-BELOW",
            100m,
            false,
            2,
            [
                Cost(0x349, CommercialProfitabilityCostComponentKinds.DirectMaterial, 43m),
                Cost(0x34a, CommercialProfitabilityCostComponentKinds.DirectLabor, 17m),
                Cost(0x34b, CommercialProfitabilityCostComponentKinds.Machine, 13m),
                Cost(0x34c, CommercialProfitabilityCostComponentKinds.Energy, 13m),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentZeroContributionId,
            PilotGuid(0x314),
            "COMMPROFIT1-MAT-ZERO",
            PilotGuid(0x324),
            "COMMPROFIT1-CUST-ZERO",
            "COMMPROFIT1-ASM-ZERO",
            "commprofit1-pilot-zero-contribution",
            "COMMPROFIT1-SET-ZERO",
            100m,
            false,
            3,
            [
                Cost(0x34d, CommercialProfitabilityCostComponentKinds.DirectMaterial, 25m),
                Cost(0x34e, CommercialProfitabilityCostComponentKinds.DirectLabor, 25m),
                Cost(0x34f, CommercialProfitabilityCostComponentKinds.Machine, 25m),
                Cost(0x350, CommercialProfitabilityCostComponentKinds.Energy, 25m),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentNegativeContributionId,
            PilotGuid(0x315),
            "COMMPROFIT1-MAT-NEGATIVE",
            PilotGuid(0x325),
            "COMMPROFIT1-CUST-NEGATIVE",
            "COMMPROFIT1-ASM-NEGATIVE",
            "commprofit1-pilot-negative-contribution",
            "COMMPROFIT1-SET-NEGATIVE",
            100m,
            false,
            4,
            [
                Cost(0x351, CommercialProfitabilityCostComponentKinds.DirectMaterial, 30m),
                Cost(0x352, CommercialProfitabilityCostComponentKinds.DirectLabor, 30m),
                Cost(0x353, CommercialProfitabilityCostComponentKinds.Machine, 25m),
                Cost(0x354, CommercialProfitabilityCostComponentKinds.Energy, 25m),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentMissingCostId,
            PilotGuid(0x316),
            "COMMPROFIT1-MAT-MISSING",
            PilotGuid(0x326),
            "COMMPROFIT1-CUST-MISSING",
            "COMMPROFIT1-ASM-MISSING",
            "commprofit1-pilot-missing-energy",
            "COMMPROFIT1-SET-MISSING",
            100m,
            false,
            5,
            [
                Cost(0x355, CommercialProfitabilityCostComponentKinds.DirectMaterial, 30m),
                Cost(0x356, CommercialProfitabilityCostComponentKinds.DirectLabor, 20m),
                Cost(0x357, CommercialProfitabilityCostComponentKinds.Machine, 20m),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentSourceConflictId,
            PilotGuid(0x317),
            "COMMPROFIT1-MAT-CONFLICT",
            PilotGuid(0x327),
            "COMMPROFIT1-CUST-CONFLICT",
            "COMMPROFIT1-ASM-CONFLICT",
            "commprofit1-pilot-source-conflict",
            "COMMPROFIT1-SET-CONFLICT",
            100m,
            false,
            6,
            [
                Cost(0x358, CommercialProfitabilityCostComponentKinds.DirectMaterial, 35m),
                Cost(0x359, CommercialProfitabilityCostComponentKinds.DirectLabor, 15m),
                Cost(
                    0x35a,
                    CommercialProfitabilityCostComponentKinds.Machine,
                    10m,
                    evidenceStatus: CommercialProfitabilityEvidenceStatuses.SourceConflict),
                Cost(0x35b, CommercialProfitabilityCostComponentKinds.Energy, 10m),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentIncompatibleId,
            PilotGuid(0x318),
            "COMMPROFIT1-MAT-INCOMPATIBLE",
            PilotGuid(0x328),
            "COMMPROFIT1-CUST-INCOMPATIBLE",
            "COMMPROFIT1-ASM-INCOMPATIBLE",
            "commprofit1-pilot-incompatible",
            "COMMPROFIT1-SET-INCOMPATIBLE",
            100m,
            false,
            7,
            [
                Cost(0x35c, CommercialProfitabilityCostComponentKinds.DirectMaterial, 35m),
                Cost(0x35d, CommercialProfitabilityCostComponentKinds.DirectLabor, 15m),
                Cost(0x35e, CommercialProfitabilityCostComponentKinds.Machine, 10m),
                Cost(0x35f, CommercialProfitabilityCostComponentKinds.Energy, 10m),
                Cost(0x360, CommercialProfitabilityCostComponentKinds.Tooling, 99m, currencyCode: "USD"),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentStaleEvidenceId,
            PilotGuid(0x319),
            "COMMPROFIT1-MAT-STALE",
            PilotGuid(0x329),
            "COMMPROFIT1-CUST-STALE",
            "COMMPROFIT1-ASM-STALE",
            "commprofit1-pilot-stale",
            "COMMPROFIT1-SET-STALE",
            100m,
            true,
            8,
            [
                Cost(0x361, CommercialProfitabilityCostComponentKinds.DirectMaterial, 35m),
                Cost(0x362, CommercialProfitabilityCostComponentKinds.DirectLabor, 15m),
                Cost(0x363, CommercialProfitabilityCostComponentKinds.Machine, 10m),
                Cost(0x364, CommercialProfitabilityCostComponentKinds.Energy, 10m),
            ]),
        new(
            DevelopmentPilotIds.CommercialProfitabilityPilotAssessmentMarginMarkupId,
            PilotGuid(0x31a),
            "COMMPROFIT1-MAT-MARGIN",
            PilotGuid(0x32a),
            "COMMPROFIT1-CUST-MARGIN",
            "COMMPROFIT1-ASM-MARGIN",
            "commprofit1-pilot-margin-vs-markup",
            "COMMPROFIT1-SET-MARGIN",
            100m,
            false,
            9,
            [
                Cost(0x365, CommercialProfitabilityCostComponentKinds.DirectMaterial, 35m),
                Cost(0x366, CommercialProfitabilityCostComponentKinds.DirectLabor, 15m),
                Cost(0x367, CommercialProfitabilityCostComponentKinds.Machine, 10m),
                Cost(0x368, CommercialProfitabilityCostComponentKinds.Energy, 10m),
            ]),
    ];

    private static async Task<CommercialProfitabilityProfile> EnsurePilotProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var requiredKinds = string.Join(
            ",",
            CommercialProfitabilityCostComponentKinds.DefaultRequiredKinds);

        var profile = await dbContext.CommercialProfitabilityProfiles
            .SingleOrDefaultAsync(
                p => p.Id == DevelopmentPilotIds.CommercialProfitabilityPilotProfileId,
                cancellationToken);

        if (profile is not null)
        {
            var changed = false;
            if (profile.MinimumContributionMarginRatio
                != CommercialIntelligenceConstants.DefaultProfitabilityMinimumContributionMarginRatio)
            {
                profile.MinimumContributionMarginRatio =
                    CommercialIntelligenceConstants.DefaultProfitabilityMinimumContributionMarginRatio;
                changed = true;
            }

            if (profile.MaximumEvidenceAgeDays
                != CommercialIntelligenceConstants.DefaultProfitabilityMaximumEvidenceAgeDays)
            {
                profile.MaximumEvidenceAgeDays =
                    CommercialIntelligenceConstants.DefaultProfitabilityMaximumEvidenceAgeDays;
                changed = true;
            }

            if (!string.Equals(profile.RequiredCostComponentKinds, requiredKinds, StringComparison.Ordinal))
            {
                profile.RequiredCostComponentKinds = requiredKinds;
                changed = true;
            }

            if (!string.Equals(profile.RequiredCurrencyCode, "TRY", StringComparison.OrdinalIgnoreCase))
            {
                profile.RequiredCurrencyCode = "TRY";
                changed = true;
            }

            if (!string.Equals(profile.RequiredUnitOfMeasure, "kg", StringComparison.Ordinal))
            {
                profile.RequiredUnitOfMeasure = "kg";
                changed = true;
            }

            if (!string.Equals(profile.RequiredGrossNetBasis, CommercialSalesPriceGrossNetBasis.Net, StringComparison.Ordinal))
            {
                profile.RequiredGrossNetBasis = CommercialSalesPriceGrossNetBasis.Net;
                changed = true;
            }

            if (!string.Equals(profile.RequiredTaxInclusion, CommercialIntelligencePriceBasisInclusionStates.Excluded, StringComparison.Ordinal))
            {
                profile.RequiredTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded;
                changed = true;
            }

            if (!string.Equals(profile.RequiredDiscountInclusion, CommercialIntelligencePriceBasisInclusionStates.Excluded, StringComparison.Ordinal))
            {
                profile.RequiredDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded;
                changed = true;
            }

            if (!string.Equals(profile.RequiredFreightInclusion, CommercialIntelligencePriceBasisInclusionStates.Excluded, StringComparison.Ordinal))
            {
                profile.RequiredFreightInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded;
                changed = true;
            }

            if (!string.Equals(
                    profile.AllowedEvidenceStatuses,
                    CommercialProfitabilityEvidenceStatuses.Simulated,
                    StringComparison.Ordinal))
            {
                profile.AllowedEvidenceStatuses = CommercialProfitabilityEvidenceStatuses.Simulated;
                changed = true;
            }

            if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
            {
                profile.Status = CommercialIntelligenceProfileStatuses.Approved;
                changed = true;
            }

            if (changed)
            {
                profile.UpdatedAtUtc = seedTimestampUtc;
                profile.UpdatedBy = actorSubjectId;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return profile;
        }

        profile = new CommercialProfitabilityProfile
        {
            Id = DevelopmentPilotIds.CommercialProfitabilityPilotProfileId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProfileCode = DevelopmentPilotIds.CommercialProfitabilityPilotProfileCode,
            Name = "Development pilot commercial profitability profile",
            Status = CommercialIntelligenceProfileStatuses.Approved,
            MinimumContributionMarginRatio =
                CommercialIntelligenceConstants.DefaultProfitabilityMinimumContributionMarginRatio,
            MaximumEvidenceAgeDays = CommercialIntelligenceConstants.DefaultProfitabilityMaximumEvidenceAgeDays,
            RequiredCostComponentKinds = requiredKinds,
            RequiredCurrencyCode = "TRY",
            RequiredUnitOfMeasure = "kg",
            RequiredGrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            RequiredTaxInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredDiscountInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            RequiredFreightInclusion = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            AllowedEvidenceStatuses = CommercialProfitabilityEvidenceStatuses.Simulated,
            RuleSetVersion = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion,
            SourceProvenance = provenance,
            CreateIdempotencyKey = "commprofit1-pilot-profile",
            CreateIdempotencyPayloadFingerprint = "commprofit1-pilot-profile-fingerprint",
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        };

        dbContext.CommercialProfitabilityProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static async Task EnsurePilotMaterialAsync(
        NevaxisZentraDbContext dbContext,
        PilotScenario scenario,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Materials
            .SingleOrDefaultAsync(m => m.Id == scenario.MaterialId, cancellationToken);
        if (existing is not null)
        {
            var changed = false;
            if (!string.Equals(existing.Code, scenario.MaterialCode, StringComparison.Ordinal))
            {
                existing.Code = scenario.MaterialCode;
                existing.CodeNormalized = scenario.MaterialCode;
                changed = true;
            }

            if (!string.Equals(existing.Name, scenario.MaterialName, StringComparison.Ordinal))
            {
                existing.Name = scenario.MaterialName;
                changed = true;
            }

            if (changed)
            {
                existing.UpdatedAtUtc = seedTimestampUtc;
                existing.UpdatedBySubjectId = actorSubjectId;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.Materials.Add(new MaterialRegistryEntry
        {
            Id = scenario.MaterialId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = scenario.MaterialCode,
            CodeNormalized = scenario.MaterialCode,
            Name = scenario.MaterialName,
            Description = "Deterministic development pilot material for commercial-profitability scenarios.",
            MaterialCategory = MaterialCategory.VirginPolymer,
            PolymerFamily = PolymerFamily.Pp,
            Manufacturer = "Pilot Polimer A.Ş.",
            TradeNameOrGrade = scenario.MaterialCode,
            ColorName = "Doğal",
            PhysicalForm = PhysicalForm.Granule,
            LifecycleStatus = MaterialLifecycleStatus.Active,
            UnitOfMeasure = "kg",
            IsActive = true,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceSystem = "development-pilot",
            SourceReference = scenario.MaterialCode.ToLowerInvariant(),
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSalesEvidenceAsync(
        NevaxisZentraDbContext dbContext,
        PilotScenario scenario,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset evidenceTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.CommercialProfitabilityEvidenceObservations
            .SingleOrDefaultAsync(e => e.Id == scenario.SalesEvidenceId, cancellationToken);
        if (existing is not null)
        {
            var changed = false;
            if (existing.ComparableNetSalesUnitPrice != scenario.SalesUnitPrice)
            {
                existing.ComparableNetSalesUnitPrice = scenario.SalesUnitPrice;
                changed = true;
            }

            if (existing.EvidenceTimestampUtc != evidenceTimestampUtc)
            {
                existing.EvidenceTimestampUtc = evidenceTimestampUtc;
                existing.ReceivedAtUtc = evidenceTimestampUtc;
                changed = true;
            }

            if (!string.Equals(existing.EvidenceSetReference, scenario.EvidenceSetReference, StringComparison.Ordinal))
            {
                existing.EvidenceSetReference = scenario.EvidenceSetReference;
                changed = true;
            }

            if (!string.Equals(existing.CustomerReference, scenario.CustomerReference, StringComparison.Ordinal))
            {
                existing.CustomerReference = scenario.CustomerReference;
                changed = true;
            }

            if (existing.MaterialId != scenario.MaterialId)
            {
                existing.MaterialId = scenario.MaterialId;
                changed = true;
            }

            if (!string.Equals(existing.EvidenceStatus, CommercialProfitabilityEvidenceStatuses.Simulated, StringComparison.Ordinal))
            {
                existing.EvidenceStatus = CommercialProfitabilityEvidenceStatuses.Simulated;
                changed = true;
            }

            if (changed)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.CommercialProfitabilityEvidenceObservations.Add(new CommercialProfitabilityEvidenceObservation
        {
            Id = scenario.SalesEvidenceId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ObservationKind = CommercialProfitabilityObservationKinds.Sales,
            EvidenceSetReference = scenario.EvidenceSetReference,
            CustomerReference = scenario.CustomerReference,
            MaterialId = scenario.MaterialId,
            QuotationReference = $"COMMPROFIT1-QTE-{scenario.AssessmentReferenceSuffix}",
            ComparableNetSalesUnitPrice = scenario.SalesUnitPrice,
            CurrencyCode = "TRY",
            UnitOfMeasure = "kg",
            GrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            TaxBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            DiscountBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            FreightBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            SourceKind = SourceKind,
            EvidenceStatus = CommercialProfitabilityEvidenceStatuses.Simulated,
            EvidenceTimestampUtc = evidenceTimestampUtc,
            IsCancelledOrReturned = false,
            SourceReference = $"COMMPROFIT1-SALES-{scenario.SalesEvidenceId:N}",
            SourceProvenance = provenance,
            SourceFingerprint = $"commprofit1-sales-{scenario.SalesEvidenceId:N}",
            IngestIdempotencyKey = $"commprofit1-sales-{scenario.SalesEvidenceId:N}",
            IngestIdempotencyPayloadFingerprint = $"commprofit1-sales-{scenario.SalesEvidenceId:N}",
            ReceivedAtUtc = evidenceTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCostEvidenceAsync(
        NevaxisZentraDbContext dbContext,
        PilotScenario scenario,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset evidenceTimestampUtc,
        CancellationToken cancellationToken)
    {
        foreach (var cost in scenario.CostComponents)
        {
            var existing = await dbContext.CommercialProfitabilityEvidenceObservations
                .SingleOrDefaultAsync(e => e.Id == cost.EvidenceId, cancellationToken);
            if (existing is not null)
            {
                var changed = false;
                if (existing.UnitCost != cost.UnitCost)
                {
                    existing.UnitCost = cost.UnitCost;
                    changed = true;
                }

                if (!string.Equals(existing.ComponentKind, cost.ComponentKind, StringComparison.Ordinal))
                {
                    existing.ComponentKind = cost.ComponentKind;
                    changed = true;
                }

                if (!string.Equals(existing.CurrencyCode, cost.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                {
                    existing.CurrencyCode = cost.CurrencyCode;
                    changed = true;
                }

                if (!string.Equals(existing.EvidenceStatus, cost.EvidenceStatus, StringComparison.Ordinal))
                {
                    existing.EvidenceStatus = cost.EvidenceStatus;
                    changed = true;
                }

                if (existing.EvidenceTimestampUtc != evidenceTimestampUtc)
                {
                    existing.EvidenceTimestampUtc = evidenceTimestampUtc;
                    existing.ReceivedAtUtc = evidenceTimestampUtc;
                    changed = true;
                }

                if (!string.Equals(existing.EvidenceSetReference, scenario.EvidenceSetReference, StringComparison.Ordinal))
                {
                    existing.EvidenceSetReference = scenario.EvidenceSetReference;
                    changed = true;
                }

                if (existing.MaterialId != scenario.MaterialId)
                {
                    existing.MaterialId = scenario.MaterialId;
                    changed = true;
                }

                if (changed)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                continue;
            }

            dbContext.CommercialProfitabilityEvidenceObservations.Add(new CommercialProfitabilityEvidenceObservation
            {
                Id = cost.EvidenceId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ObservationKind = CommercialProfitabilityObservationKinds.CostComponent,
                EvidenceSetReference = scenario.EvidenceSetReference,
                CustomerReference = scenario.CustomerReference,
                MaterialId = scenario.MaterialId,
                ComponentKind = cost.ComponentKind,
                UnitCost = cost.UnitCost,
                CurrencyCode = cost.CurrencyCode,
                UnitOfMeasure = "kg",
                GrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
                TaxBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
                DiscountBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
                FreightBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
                CostBasis = CostBasis,
                SourceKind = SourceKind,
                EvidenceStatus = cost.EvidenceStatus,
                EvidenceTimestampUtc = evidenceTimestampUtc,
                IsCancelledOrReturned = false,
                SourceReference = $"COMMPROFIT1-COST-{cost.EvidenceId:N}",
                SourceProvenance = provenance,
                SourceFingerprint = $"commprofit1-cost-{cost.EvidenceId:N}",
                IngestIdempotencyKey = $"commprofit1-cost-{cost.EvidenceId:N}",
                IngestIdempotencyPayloadFingerprint = $"commprofit1-cost-{cost.EvidenceId:N}",
                ReceivedAtUtc = evidenceTimestampUtc,
                CreatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsurePilotAssessmentsAsync(
        NevaxisZentraDbContext dbContext,
        CommercialProfitabilityProfile profile,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken)
    {
        var context = new CommercialIntelligenceOperationContext(
            tenantId,
            facilityId,
            Guid.Empty,
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            CommercialIntelligencePermissionKeys.Manage,
            null);

        foreach (var scenario in PilotScenarios)
        {
            await EnsureSeededAssessmentAsync(
                dbContext,
                scenario,
                profile,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                evaluatedAt,
                context,
                cancellationToken);
        }
    }

    private static async Task EnsureSeededAssessmentAsync(
        NevaxisZentraDbContext dbContext,
        PilotScenario scenario,
        CommercialProfitabilityProfile profile,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        DateTimeOffset evaluatedAt,
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        var salesEvidence = await dbContext.CommercialProfitabilityEvidenceObservations
            .AsNoTracking()
            .SingleAsync(e => e.Id == scenario.SalesEvidenceId, cancellationToken);

        var costEvidence = await CommercialProfitabilityCostEvidenceReader.ReadAsync(
            dbContext,
            context,
            profile,
            salesEvidence,
            evaluatedAt,
            cancellationToken);
        var requiredKinds = CommercialProfitabilityEvaluator.ParseRequiredKinds(profile.RequiredCostComponentKinds);
        var evaluation = CommercialProfitabilityEvaluator.Evaluate(
            profile,
            salesEvidence,
            costEvidence,
            requiredKinds,
            evaluatedAt);

        var command = new EvaluateCommercialProfitabilityAssessmentCommand(
            profile.Id,
            profile.Version,
            scenario.SalesEvidenceId,
            provenance,
            scenario.IdempotencyKey);
        var fingerprint = CommercialProfitabilityIdempotencyFingerprint.ComputeAssessmentFingerprint(context, command);

        var existing = await dbContext.CommercialProfitabilityAssessments
            .Include(a => a.CostComponents)
            .Include(a => a.Drivers)
            .SingleOrDefaultAsync(a => a.Id == scenario.AssessmentId, cancellationToken);

        if (existing is not null)
        {
            var changed = ApplyAssessmentFields(
                existing,
                scenario,
                profile,
                salesEvidence,
                evaluation,
                provenance,
                evaluatedAt,
                fingerprint);

            var componentsChanged = await SyncAssessmentCostComponentsAsync(
                dbContext,
                existing,
                evaluation,
                provenance,
                seedTimestampUtc,
                tenantId,
                facilityId,
                cancellationToken);

            var driversChanged = await SyncAssessmentDriversAsync(
                dbContext,
                existing,
                evaluation,
                provenance,
                seedTimestampUtc,
                tenantId,
                facilityId,
                cancellationToken);

            if (changed || componentsChanged || driversChanged)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var assessment = new CommercialProfitabilityAssessment
        {
            Id = scenario.AssessmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            AssessmentReference = scenario.AssessmentReference,
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SalesEvidenceObservationId = salesEvidence.Id,
            CustomerReference = salesEvidence.CustomerReference,
            MaterialId = salesEvidence.MaterialId,
            QuotationReference = salesEvidence.QuotationReference,
            EvidenceSetReference = salesEvidence.EvidenceSetReference,
            ComparableNetSalesUnitPrice = evaluation.ComparableNetSalesUnitPrice,
            TotalIncludedUnitCost = evaluation.TotalIncludedUnitCost,
            UnitContribution = evaluation.UnitContribution,
            ContributionMarginRatio = evaluation.ContributionMarginRatio,
            MarkupRatio = evaluation.MarkupRatio,
            CandidateCostComponentCount = evaluation.CandidateCostComponentCount,
            IncludedCostComponentCount = evaluation.IncludedCostComponentCount,
            ExcludedCostComponentCount = evaluation.ExcludedCostComponentCount,
            RequiredCostComponentCount = evaluation.RequiredCostComponentCount,
            AvailableRequiredCostComponentCount = evaluation.AvailableRequiredCostComponentCount,
            MissingRequiredCostComponentCount = evaluation.MissingRequiredCostComponentCount,
            MissingRequiredComponentKinds = evaluation.MissingRequiredComponentKinds,
            CostCoverageStatus = evaluation.CostCoverageStatus,
            CurrencyCode = salesEvidence.CurrencyCode,
            UnitOfMeasure = salesEvidence.UnitOfMeasure,
            GrossNetBasis = salesEvidence.GrossNetBasis,
            TaxBasis = salesEvidence.TaxBasis,
            DiscountBasis = salesEvidence.DiscountBasis,
            FreightBasis = salesEvidence.FreightBasis,
            EvidenceStatus = evaluation.EvidenceStatus,
            Outcome = evaluation.Outcome,
            LeadingDriverCode = evaluation.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion,
            IdempotencyKey = scenario.IdempotencyKey,
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = provenance,
            EvaluatedAtUtc = evaluatedAt,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        };

        AddCostComponents(assessment, evaluation, provenance, seedTimestampUtc, tenantId, facilityId);
        AddDrivers(assessment, evaluation, provenance, seedTimestampUtc, tenantId, facilityId);
        dbContext.CommercialProfitabilityAssessments.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool ApplyAssessmentFields(
        CommercialProfitabilityAssessment assessment,
        PilotScenario scenario,
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityEvidenceObservation salesEvidence,
        CommercialProfitabilityEvaluationOutput evaluation,
        string provenance,
        DateTimeOffset evaluatedAt,
        string fingerprint)
    {
        var changed = false;

        void Set<T>(Func<T> getter, Action<T> setter, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(getter(), value))
            {
                setter(value);
                changed = true;
            }
        }

        Set(() => assessment.AssessmentReference, v => assessment.AssessmentReference = v, scenario.AssessmentReference);
        Set(() => assessment.ProfileVersion, v => assessment.ProfileVersion = v,
            profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Set(() => assessment.ComparableNetSalesUnitPrice, v => assessment.ComparableNetSalesUnitPrice = v, evaluation.ComparableNetSalesUnitPrice);
        Set(() => assessment.TotalIncludedUnitCost, v => assessment.TotalIncludedUnitCost = v, evaluation.TotalIncludedUnitCost);
        Set(() => assessment.UnitContribution, v => assessment.UnitContribution = v, evaluation.UnitContribution);
        Set(() => assessment.ContributionMarginRatio, v => assessment.ContributionMarginRatio = v, evaluation.ContributionMarginRatio);
        Set(() => assessment.MarkupRatio, v => assessment.MarkupRatio = v, evaluation.MarkupRatio);
        Set(() => assessment.CandidateCostComponentCount, v => assessment.CandidateCostComponentCount = v, evaluation.CandidateCostComponentCount);
        Set(() => assessment.IncludedCostComponentCount, v => assessment.IncludedCostComponentCount = v, evaluation.IncludedCostComponentCount);
        Set(() => assessment.ExcludedCostComponentCount, v => assessment.ExcludedCostComponentCount = v, evaluation.ExcludedCostComponentCount);
        Set(() => assessment.RequiredCostComponentCount, v => assessment.RequiredCostComponentCount = v, evaluation.RequiredCostComponentCount);
        Set(() => assessment.AvailableRequiredCostComponentCount, v => assessment.AvailableRequiredCostComponentCount = v, evaluation.AvailableRequiredCostComponentCount);
        Set(() => assessment.MissingRequiredCostComponentCount, v => assessment.MissingRequiredCostComponentCount = v, evaluation.MissingRequiredCostComponentCount);
        Set(() => assessment.MissingRequiredComponentKinds, v => assessment.MissingRequiredComponentKinds = v, evaluation.MissingRequiredComponentKinds);
        Set(() => assessment.CostCoverageStatus, v => assessment.CostCoverageStatus = v, evaluation.CostCoverageStatus);
        Set(() => assessment.EvidenceStatus, v => assessment.EvidenceStatus = v, evaluation.EvidenceStatus);
        Set(() => assessment.Outcome, v => assessment.Outcome = v, evaluation.Outcome);
        Set(() => assessment.LeadingDriverCode, v => assessment.LeadingDriverCode = v, evaluation.LeadingDriverCode);
        Set(() => assessment.EvidenceSetReference, v => assessment.EvidenceSetReference = v, salesEvidence.EvidenceSetReference);
        Set(() => assessment.SourceProvenance, v => assessment.SourceProvenance = v, provenance);
        Set(() => assessment.EvaluatedAtUtc, v => assessment.EvaluatedAtUtc = v, evaluatedAt);
        Set(() => assessment.IdempotencyPayloadFingerprint, v => assessment.IdempotencyPayloadFingerprint = v, fingerprint);

        return changed;
    }

    private static async Task<bool> SyncAssessmentCostComponentsAsync(
        NevaxisZentraDbContext dbContext,
        CommercialProfitabilityAssessment assessment,
        CommercialProfitabilityEvaluationOutput evaluation,
        string provenance,
        DateTimeOffset seedTimestampUtc,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var expected = evaluation.ComponentSnapshots
            .OrderBy(c => c.ComponentKind, StringComparer.Ordinal)
            .ThenBy(c => c.ObservationId)
            .ToList();
        var existing = assessment.CostComponents
            .OrderBy(c => c.SortOrder)
            .ToList();

        if (existing.Count == expected.Count)
        {
            var changed = false;
            for (var index = 0; index < expected.Count; index++)
            {
                var expectedComponent = expected[index];
                var actual = existing[index];
                if (string.Equals(actual.ComponentKind, expectedComponent.ComponentKind, StringComparison.Ordinal)
                    && actual.UnitCost == expectedComponent.UnitCost
                    && string.Equals(actual.CurrencyCode, expectedComponent.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(actual.InclusionStatus, expectedComponent.InclusionStatus, StringComparison.Ordinal)
                    && string.Equals(actual.ExclusionReason, expectedComponent.ExclusionReason, StringComparison.Ordinal)
                    && actual.SourceEvidenceObservationId == (expectedComponent.ObservationId == Guid.Empty ? null : expectedComponent.ObservationId)
                    && actual.SortOrder == index + 1)
                {
                    continue;
                }

                changed = true;
                actual.ComponentKind = expectedComponent.ComponentKind;
                actual.UnitCost = expectedComponent.UnitCost;
                actual.CurrencyCode = expectedComponent.CurrencyCode;
                actual.UnitOfMeasure = expectedComponent.UnitOfMeasure;
                actual.CostBasis = expectedComponent.CostBasis;
                actual.AllocationBasis = expectedComponent.AllocationBasis;
                actual.EvidenceTimestampUtc = expectedComponent.EvidenceTimestampUtc;
                actual.SourceKind = expectedComponent.SourceKind;
                actual.SourceReference = expectedComponent.SourceReference;
                actual.EvidenceStatus = expectedComponent.EvidenceStatus;
                actual.InclusionStatus = expectedComponent.InclusionStatus;
                actual.ExclusionReason = expectedComponent.ExclusionReason;
                actual.SourceProvenance = provenance;
                actual.SourceEvidenceObservationId =
                    expectedComponent.ObservationId == Guid.Empty ? null : expectedComponent.ObservationId;
                actual.SortOrder = index + 1;
            }

            return changed;
        }

        if (existing.Count > 0)
        {
            dbContext.CommercialProfitabilityAssessmentCostComponents.RemoveRange(existing);
            assessment.CostComponents.Clear();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        AddCostComponents(assessment, evaluation, provenance, seedTimestampUtc, tenantId, facilityId);
        return true;
    }

    private static async Task<bool> SyncAssessmentDriversAsync(
        NevaxisZentraDbContext dbContext,
        CommercialProfitabilityAssessment assessment,
        CommercialProfitabilityEvaluationOutput evaluation,
        string provenance,
        DateTimeOffset seedTimestampUtc,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var orderedDrivers = CommercialProfitabilityEvaluator.OrderDrivers(evaluation.Drivers);
        var existingDrivers = assessment.Drivers.OrderBy(d => d.SortOrder).ToList();
        if (existingDrivers.Count == orderedDrivers.Count)
        {
            var changed = false;
            for (var index = 0; index < orderedDrivers.Count; index++)
            {
                var expected = orderedDrivers[index];
                var actual = existingDrivers[index];
                if (string.Equals(actual.ReasonCode, expected.ReasonCode, StringComparison.Ordinal)
                    && string.Equals(actual.Severity, expected.Severity, StringComparison.Ordinal)
                    && actual.ActualValue == expected.ActualValue
                    && actual.ThresholdValue == expected.ThresholdValue
                    && actual.NormalizedVariance == expected.NormalizedVariance
                    && actual.AffectedComponentCount == expected.AffectedComponentCount
                    && actual.IsLeadingDriver == (index == 0)
                    && actual.SortOrder == index + 1)
                {
                    continue;
                }

                changed = true;
                actual.ReasonCode = expected.ReasonCode;
                actual.Severity = expected.Severity;
                actual.ActualValue = expected.ActualValue;
                actual.ThresholdValue = expected.ThresholdValue;
                actual.Unit = expected.Unit;
                actual.NormalizedVariance = expected.NormalizedVariance;
                actual.AffectedComponentCount = expected.AffectedComponentCount;
                actual.SupportingText = expected.SupportingText;
                actual.SourceProvenance = provenance;
                actual.SortOrder = index + 1;
                actual.IsLeadingDriver = index == 0;
            }

            return changed;
        }

        if (existingDrivers.Count > 0)
        {
            dbContext.CommercialProfitabilityAssessmentDrivers.RemoveRange(existingDrivers);
            assessment.Drivers.Clear();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        AddDrivers(assessment, evaluation, provenance, seedTimestampUtc, tenantId, facilityId);
        return true;
    }

    private static void AddCostComponents(
        CommercialProfitabilityAssessment assessment,
        CommercialProfitabilityEvaluationOutput evaluation,
        string provenance,
        DateTimeOffset seedTimestampUtc,
        Guid tenantId,
        Guid facilityId)
    {
        var componentOrder = 0;
        foreach (var component in evaluation.ComponentSnapshots)
        {
            componentOrder++;
            assessment.CostComponents.Add(new CommercialProfitabilityAssessmentCostComponent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                AssessmentId = assessment.Id,
                SourceEvidenceObservationId = component.ObservationId == Guid.Empty ? null : component.ObservationId,
                ComponentKind = component.ComponentKind,
                UnitCost = component.UnitCost,
                CurrencyCode = component.CurrencyCode,
                UnitOfMeasure = component.UnitOfMeasure,
                CostBasis = component.CostBasis,
                AllocationBasis = component.AllocationBasis,
                EvidenceTimestampUtc = component.EvidenceTimestampUtc,
                SourceKind = component.SourceKind,
                SourceReference = component.SourceReference,
                EvidenceStatus = component.EvidenceStatus,
                InclusionStatus = component.InclusionStatus,
                ExclusionReason = component.ExclusionReason,
                SourceProvenance = provenance,
                SortOrder = componentOrder,
                CreatedAtUtc = seedTimestampUtc,
            });
        }
    }

    private static void AddDrivers(
        CommercialProfitabilityAssessment assessment,
        CommercialProfitabilityEvaluationOutput evaluation,
        string provenance,
        DateTimeOffset seedTimestampUtc,
        Guid tenantId,
        Guid facilityId)
    {
        var orderedDrivers = CommercialProfitabilityEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            assessment.Drivers.Add(new CommercialProfitabilityAssessmentDriver
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                AssessmentId = assessment.Id,
                ReasonCode = driver.ReasonCode,
                Severity = driver.Severity,
                ActualValue = driver.ActualValue,
                ThresholdValue = driver.ThresholdValue,
                Unit = driver.Unit,
                NormalizedVariance = driver.NormalizedVariance,
                AffectedComponentCount = driver.AffectedComponentCount,
                SupportingText = driver.SupportingText,
                SourceProvenance = provenance,
                SortOrder = index + 1,
                IsLeadingDriver = index == 0,
                CreatedAtUtc = seedTimestampUtc,
            });
        }
    }

    private static Guid PilotGuid(int value) =>
        Guid.Parse($"bb000001-{value:x4}-4000-8000-{value:x012}");

    private static CostComponentDefinition Cost(
        int evidenceIdValue,
        string componentKind,
        decimal unitCost,
        string currencyCode = "TRY",
        string evidenceStatus = CommercialProfitabilityEvidenceStatuses.Simulated) =>
        new(PilotGuid(evidenceIdValue), componentKind, unitCost, currencyCode, evidenceStatus);

    private sealed record CostComponentDefinition(
        Guid EvidenceId,
        string ComponentKind,
        decimal UnitCost,
        string CurrencyCode,
        string EvidenceStatus);

    private sealed record PilotScenario(
        Guid AssessmentId,
        Guid MaterialId,
        string MaterialCode,
        Guid SalesEvidenceId,
        string CustomerReference,
        string AssessmentReference,
        string IdempotencyKey,
        string EvidenceSetReference,
        decimal SalesUnitPrice,
        bool UseStaleCostEvidence,
        int ScenarioIndex,
        IReadOnlyList<CostComponentDefinition> CostComponents)
    {
        public string MaterialName => $"Pilot Commercial Profitability Material ({MaterialCode})";

        public string AssessmentReferenceSuffix =>
            AssessmentReference["COMMPROFIT1-ASM-".Length..];
    }
}
