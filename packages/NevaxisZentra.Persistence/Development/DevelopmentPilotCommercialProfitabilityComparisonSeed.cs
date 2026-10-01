using NevaxisZentra.Persistence.CommercialIntelligence;
using NevaxisZentra.Persistence.Material;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotCommercialProfitabilityComparisonSeed
{
    private const string SourceKind = CommercialProfitabilitySourceKinds.ManualDevelopmentSimulator;
    private const string CostBasis = CommercialSalesPriceGrossNetBasis.Net;
    private const decimal SalesUnitPrice = 100m;

    internal static async Task EnsureCommercialProfitabilityComparisonPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = CommercialIntelligenceConstants.ProfitabilityComparisonDevelopmentPilotProvenance;
        var profile = await dbContext.CommercialProfitabilityProfiles
            .SingleOrDefaultAsync(p => p.Id == DevelopmentPilotIds.CommercialProfitabilityPilotProfileId, cancellationToken);
        if (profile is null)
        {
            return;
        }

        await ReconcileProfileAsync(dbContext, profile, actorSubjectId, seedTimestampUtc, cancellationToken);

        foreach (var scenario in ComparisonScenarios)
        {
            await EnsurePilotMaterialAsync(
                dbContext,
                scenario.MaterialId,
                scenario.MaterialCode,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);

            if (scenario.SecondaryMaterialId.HasValue && scenario.SecondaryMaterialCode is not null)
            {
                await EnsurePilotMaterialAsync(
                    dbContext,
                    scenario.SecondaryMaterialId.Value,
                    scenario.SecondaryMaterialCode,
                    tenantId,
                    facilityId,
                    actorSubjectId,
                    seedTimestampUtc,
                    cancellationToken);
            }

            await SeedScenarioAssessmentsAsync(
                dbContext,
                scenario,
                profile,
                tenantId,
                facilityId,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                cancellationToken);
        }

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

    private static readonly CostMix StandardMargin30 = new(35m, 15m, 10m, 10m);
    private static readonly CostMix Margin24 = new(35m, 15m, 16m, 10m);
    private static readonly CostMix Margin36 = new(31m, 15m, 8m, 10m);
    private static readonly CostMix Margin25 = new(35m, 15m, 15m, 10m);
    private static readonly CostMix DirectMaterialIncreaseCurrent = new(41m, 9m, 10m, 10m);
    private static readonly CostMix EnergyIncreaseCurrent = new(35m, 9m, 10m, 16m);
    private static readonly CostMix PartialCoverage = new(35m, 15m, 10m, null);

    private static readonly ComparisonScenario[] ComparisonScenarios =
    [
        new(
            0,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialStableId,
            "COMMPROFIT2-MAT-STABLE",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 6, StandardMargin30),
            ]),
        new(
            1,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialDeteriorationId,
            "COMMPROFIT2-MAT-DETERIORATION",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 6, Margin24),
            ]),
        new(
            2,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialImprovementId,
            "COMMPROFIT2-MAT-IMPROVEMENT",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 6, Margin36),
            ]),
        new(
            3,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialExactThresholdId,
            "COMMPROFIT2-MAT-EXACT",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 6, Margin25),
            ]),
        new(
            4,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialDirectMaterialId,
            "COMMPROFIT2-MAT-DM",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 6, DirectMaterialIncreaseCurrent),
            ],
            IncludeCostComponents: true),
        new(
            5,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialEnergyId,
            "COMMPROFIT2-MAT-ENERGY",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 6, EnergyIncreaseCurrent),
            ],
            IncludeCostComponents: true),
        new(
            6,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialCustomerPositionId,
            "COMMPROFIT2-MAT-CUST-POS",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(1, 6, Margin36, DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary),
                new(1, 6, Margin24, DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerSecondary),
            ]),
        new(
            7,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialPositionAId,
            "COMMPROFIT2-MAT-POS-A",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerMaterialPosition,
            [
                new(1, 6, Margin36, MaterialId: DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialPositionAId),
                new(1, 6, Margin24, MaterialId: DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialPositionBId),
            ],
            SecondaryMaterialId: DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialPositionBId,
            SecondaryMaterialCode: "COMMPROFIT2-MAT-POS-B"),
        new(
            8,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialTieId,
            "COMMPROFIT2-MAT-TIE",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerTieA,
            [
                new(1, 3, StandardMargin30, DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerTieA),
                new(1, 3, StandardMargin30, DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerTieB),
            ]),
        new(
            9,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialInsufficientId,
            "COMMPROFIT2-MAT-INSUFFICIENT",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 2, StandardMargin30),
            ]),
        new(
            10,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialIncompleteId,
            "COMMPROFIT2-MAT-INCOMPLETE",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 2, StandardMargin30),
                new(1, 3, PartialCoverage, PartialCoverage: true),
            ]),
        new(
            11,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialSourceConflictId,
            "COMMPROFIT2-MAT-CONFLICT",
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPrimary,
            [
                new(0, 6, StandardMargin30),
                new(1, 6, StandardMargin30, SourceConflict: true),
            ]),
    ];

    private static async Task ReconcileProfileAsync(
        NevaxisZentraDbContext dbContext,
        CommercialProfitabilityProfile profile,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var changed = false;

        if (profile.MinimumComparableAssessmentCountPerWindow
            != CommercialIntelligenceConstants.DefaultProfitabilityMinimumComparableAssessmentCountPerWindow)
        {
            profile.MinimumComparableAssessmentCountPerWindow =
                CommercialIntelligenceConstants.DefaultProfitabilityMinimumComparableAssessmentCountPerWindow;
            changed = true;
        }

        if (profile.MaximumAdverseContributionMarginDelta
            != CommercialIntelligenceConstants.DefaultProfitabilityMaximumAdverseContributionMarginDelta)
        {
            profile.MaximumAdverseContributionMarginDelta =
                CommercialIntelligenceConstants.DefaultProfitabilityMaximumAdverseContributionMarginDelta;
            changed = true;
        }

        if (profile.MinimumFavorableContributionMarginDelta
            != CommercialIntelligenceConstants.DefaultProfitabilityMinimumFavorableContributionMarginDelta)
        {
            profile.MinimumFavorableContributionMarginDelta =
                CommercialIntelligenceConstants.DefaultProfitabilityMinimumFavorableContributionMarginDelta;
            changed = true;
        }

        if (profile.MaximumCostShareIncrease
            != CommercialIntelligenceConstants.DefaultProfitabilityMaximumCostShareIncrease)
        {
            profile.MaximumCostShareIncrease =
                CommercialIntelligenceConstants.DefaultProfitabilityMaximumCostShareIncrease;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        profile.UpdatedAtUtc = seedTimestampUtc;
        profile.UpdatedBy = actorSubjectId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePilotMaterialAsync(
        NevaxisZentraDbContext dbContext,
        Guid materialId,
        string materialCode,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Materials
            .SingleOrDefaultAsync(m => m.Id == materialId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        dbContext.Materials.Add(new MaterialRegistryEntry
        {
            Id = materialId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = materialCode,
            CodeNormalized = materialCode,
            Name = $"Pilot Profitability Comparison Material ({materialCode})",
            Description = "Deterministic development pilot material for profitability comparison scenarios.",
            MaterialCategory = MaterialCategory.VirginPolymer,
            PolymerFamily = PolymerFamily.Pp,
            Manufacturer = "Pilot Polimer A.Ş.",
            TradeNameOrGrade = materialCode,
            ColorName = "Doğal",
            PhysicalForm = PhysicalForm.Granule,
            LifecycleStatus = MaterialLifecycleStatus.Active,
            UnitOfMeasure = "kg",
            IsActive = true,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceSystem = "development-pilot",
            SourceReference = materialCode.ToLowerInvariant(),
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedScenarioAssessmentsAsync(
        NevaxisZentraDbContext dbContext,
        ComparisonScenario scenario,
        CommercialProfitabilityProfile profile,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var referenceStart = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotReferenceWindowStartUtc;
        var referenceEnd = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotReferenceWindowEndUtc;
        var currentStart = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCurrentWindowStartUtc;
        var currentEnd = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCurrentWindowEndUtc;
        var positionStart = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotPositionWindowStartUtc;
        var positionEnd = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotPositionWindowEndUtc;
        var assessmentIndex = 0;

        foreach (var batch in scenario.AssessmentBatches)
        {
            var windowStart = batch.WindowKind switch
            {
                0 => referenceStart,
                1 => currentStart,
                2 => positionStart,
                _ => currentStart,
            };
            var windowEnd = batch.WindowKind switch
            {
                0 => referenceEnd,
                1 => currentEnd,
                2 => positionEnd,
                _ => currentEnd,
            };

            for (var index = 0; index < batch.Count; index++)
            {
                var customerReference = batch.CustomerReference ?? scenario.DefaultCustomerReference;
                var materialId = batch.MaterialId ?? scenario.MaterialId;
                var evaluatedAt = windowStart.AddDays(index * 20 + scenario.ScenarioIndex + batch.WindowKind);
                await EnsureComparisonAssessmentAsync(
                    dbContext,
                    ComparisonAssessmentId(scenario.ScenarioIndex, assessmentIndex),
                    ComparisonSalesEvidenceId(scenario.ScenarioIndex, assessmentIndex),
                    scenario,
                    materialId,
                    profile,
                    tenantId,
                    facilityId,
                    customerReference,
                    batch.CostMix,
                    batch.PartialCoverage,
                    batch.SourceConflict,
                    evaluatedAt,
                    windowStart,
                    windowEnd,
                    assessmentIndex,
                    actorSubjectId,
                    seedTimestampUtc,
                    provenance,
                    cancellationToken);
                assessmentIndex++;
            }
        }
    }

    private static async Task EnsurePilotComparisonRunsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        CommercialProfitabilityProfile profile,
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

        var temporalCommandBase = new EvaluateCommercialProfitabilityComparisonCommand(
            profile.Id,
            profile.Version,
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend,
            null,
            null,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCurrentWindowStartUtc,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCurrentWindowEndUtc,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotReferenceWindowStartUtc,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotReferenceWindowEndUtc,
            provenance,
            string.Empty);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotStableRunId,
            "COMMPROFIT2-CMP-PILOT-STABLE",
            "commprofit2-pilot-stable",
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialStableId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialStableId,
                IdempotencyKey = "commprofit2-pilot-stable",
            },
            includeCostComponents: false,
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotDeteriorationRunId,
            "COMMPROFIT2-CMP-PILOT-DETERIORATION",
            "commprofit2-pilot-deterioration",
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialDeteriorationId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialDeteriorationId,
                IdempotencyKey = "commprofit2-pilot-deterioration",
            },
            includeCostComponents: false,
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotImprovementRunId,
            "COMMPROFIT2-CMP-PILOT-IMPROVEMENT",
            "commprofit2-pilot-improvement",
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialImprovementId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialImprovementId,
                IdempotencyKey = "commprofit2-pilot-improvement",
            },
            includeCostComponents: false,
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotExactThresholdRunId,
            "COMMPROFIT2-CMP-PILOT-EXACT",
            "commprofit2-pilot-exact-threshold",
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialExactThresholdId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialExactThresholdId,
                IdempotencyKey = "commprofit2-pilot-exact-threshold",
            },
            includeCostComponents: false,
            cancellationToken);

        var costMixCommandBase = new EvaluateCommercialProfitabilityComparisonCommand(
            profile.Id,
            profile.Version,
            CommercialProfitabilityComparisonTypes.CostComponentMixChange,
            null,
            null,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCurrentWindowStartUtc,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCurrentWindowEndUtc,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotReferenceWindowStartUtc,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotReferenceWindowEndUtc,
            provenance,
            string.Empty);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotDirectMaterialRunId,
            "COMMPROFIT2-CMP-PILOT-DM",
            "commprofit2-pilot-direct-material",
            CommercialProfitabilityComparisonTypes.CostComponentMixChange,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialDirectMaterialId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            costMixCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialDirectMaterialId,
                IdempotencyKey = "commprofit2-pilot-direct-material",
            },
            includeCostComponents: true,
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotEnergyRunId,
            "COMMPROFIT2-CMP-PILOT-ENERGY",
            "commprofit2-pilot-energy",
            CommercialProfitabilityComparisonTypes.CostComponentMixChange,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialEnergyId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            costMixCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialEnergyId,
                IdempotencyKey = "commprofit2-pilot-energy",
            },
            includeCostComponents: true,
            cancellationToken);

        var positionCommandBase = new EvaluateCommercialProfitabilityComparisonCommand(
            profile.Id,
            profile.Version,
            CommercialProfitabilityComparisonTypes.CustomerContributionPosition,
            null,
            null,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotPositionWindowStartUtc,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotPositionWindowEndUtc,
            null,
            null,
            provenance,
            string.Empty);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerPositionRunId,
            "COMMPROFIT2-CMP-PILOT-CUST-POS",
            "commprofit2-pilot-customer-position",
            CommercialProfitabilityComparisonTypes.CustomerContributionPosition,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialCustomerPositionId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            positionCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialCustomerPositionId,
                IdempotencyKey = "commprofit2-pilot-customer-position",
            },
            includeCostComponents: false,
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotTieRunId,
            "COMMPROFIT2-CMP-PILOT-TIE",
            "commprofit2-pilot-tie",
            CommercialProfitabilityComparisonTypes.CustomerContributionPosition,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialTieId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            positionCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialTieId,
                IdempotencyKey = "commprofit2-pilot-tie",
            },
            includeCostComponents: false,
            cancellationToken);

        var materialPositionCommandBase = new EvaluateCommercialProfitabilityComparisonCommand(
            profile.Id,
            profile.Version,
            CommercialProfitabilityComparisonTypes.MaterialContributionPosition,
            null,
            null,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotPositionWindowStartUtc,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotPositionWindowEndUtc,
            null,
            null,
            provenance,
            string.Empty);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialPositionRunId,
            "COMMPROFIT2-CMP-PILOT-MAT-POS",
            "commprofit2-pilot-material-position",
            CommercialProfitabilityComparisonTypes.MaterialContributionPosition,
            tenantId,
            facilityId,
            profile,
            null,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            materialPositionCommandBase with
            {
                CustomerReference = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotCustomerMaterialPosition,
                IdempotencyKey = "commprofit2-pilot-material-position",
            },
            includeCostComponents: false,
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotInsufficientRunId,
            "COMMPROFIT2-CMP-PILOT-INSUFFICIENT",
            "commprofit2-pilot-insufficient",
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialInsufficientId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialInsufficientId,
                IdempotencyKey = "commprofit2-pilot-insufficient",
            },
            includeCostComponents: false,
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotIncompleteCoverageRunId,
            "COMMPROFIT2-CMP-PILOT-INCOMPLETE",
            "commprofit2-pilot-incomplete-coverage",
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialIncompleteId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialIncompleteId,
                IdempotencyKey = "commprofit2-pilot-incomplete-coverage",
            },
            includeCostComponents: false,
            cancellationToken);

        await EnsureSeededRunAsync(
            dbContext,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotSourceConflictRunId,
            "COMMPROFIT2-CMP-PILOT-CONFLICT",
            "commprofit2-pilot-source-conflict",
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend,
            tenantId,
            facilityId,
            profile,
            DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialSourceConflictId,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            context,
            temporalCommandBase with
            {
                MaterialId = DevelopmentPilotIds.CommercialProfitabilityComparisonPilotMaterialSourceConflictId,
                IdempotencyKey = "commprofit2-pilot-source-conflict",
            },
            includeCostComponents: false,
            cancellationToken);
    }

    private static async Task EnsureSeededRunAsync(
        NevaxisZentraDbContext dbContext,
        Guid runId,
        string comparisonReference,
        string idempotencyKey,
        string comparisonType,
        Guid tenantId,
        Guid facilityId,
        CommercialProfitabilityProfile profile,
        Guid? materialId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialProfitabilityComparisonCommand command,
        bool includeCostComponents,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.CommercialProfitabilityComparisonRuns
            .Include(r => r.Results)
            .Include(r => r.Drivers)
            .SingleOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        CommercialProfitabilityComparisonEvaluationOutput evaluation;
        if (RequiresTemporalWindows(comparisonType))
        {
            var referenceAssessments = await CommercialProfitabilityAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.ReferenceWindowStartUtc!.Value,
                command.ReferenceWindowEndUtc!.Value,
                materialId,
                command.CustomerReference,
                includeCostComponents,
                cancellationToken);
            var currentAssessments = await CommercialProfitabilityAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                materialId,
                command.CustomerReference,
                includeCostComponents,
                cancellationToken);

            var referenceSlice = CommercialProfitabilityAssessmentReader.SummarizeWindow(referenceAssessments);
            var currentSlice = CommercialProfitabilityAssessmentReader.SummarizeWindow(currentAssessments);

            evaluation = string.Equals(comparisonType, CommercialProfitabilityComparisonTypes.TemporalContributionTrend, StringComparison.Ordinal)
                ? CommercialProfitabilityComparisonEvaluator.EvaluateTemporalContributionTrend(
                    profile,
                    referenceSlice,
                    currentSlice,
                    referenceAssessments,
                    currentAssessments)
                : CommercialProfitabilityComparisonEvaluator.EvaluateCostComponentMixChange(
                    profile,
                    referenceSlice,
                    currentSlice,
                    referenceAssessments,
                    currentAssessments);
        }
        else if (string.Equals(comparisonType, CommercialProfitabilityComparisonTypes.CustomerContributionPosition, StringComparison.Ordinal))
        {
            var assessments = await CommercialProfitabilityAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                materialId,
                null,
                includeCostComponents: false,
                cancellationToken);
            var customerReferences = assessments
                .Select(a => a.CustomerReference)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();

            evaluation = CommercialProfitabilityComparisonEvaluator.EvaluateCustomerContributionPosition(
                profile,
                assessments,
                customerReferences);
        }
        else
        {
            var assessments = await CommercialProfitabilityAssessmentReader.ReadAssessmentsInWindowAsync(
                dbContext,
                context,
                profile.Id,
                command.CurrentWindowStartUtc,
                command.CurrentWindowEndUtc,
                null,
                command.CustomerReference,
                includeCostComponents: false,
                cancellationToken);
            var materialIds = assessments
                .Select(a => a.MaterialId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            evaluation = CommercialProfitabilityComparisonEvaluator.EvaluateMaterialContributionPosition(
                profile,
                assessments,
                materialIds);
        }

        var fingerprint = CommercialProfitabilityIdempotencyFingerprint.ComputeComparisonFingerprint(
            context,
            command with { IdempotencyKey = idempotencyKey });
        var now = seedTimestampUtc;
        var run = new CommercialProfitabilityComparisonRun
        {
            Id = runId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ComparisonReference = comparisonReference,
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ComparisonType = comparisonType,
            MaterialId = materialId,
            CustomerReference = command.CustomerReference,
            ReferenceWindowStartUtc = command.ReferenceWindowStartUtc,
            ReferenceWindowEndUtc = command.ReferenceWindowEndUtc,
            CurrentWindowStartUtc = command.CurrentWindowStartUtc,
            CurrentWindowEndUtc = command.CurrentWindowEndUtc,
            AggregationMethod = evaluation.AggregationMethod,
            EvidenceStatus = evaluation.EvidenceStatus,
            ComparisonOutcome = evaluation.ComparisonOutcome,
            CandidateAssessmentCount = evaluation.CandidateAssessmentCount,
            ComparableAssessmentCount = evaluation.ComparableAssessmentCount,
            ExcludedAssessmentCount = evaluation.ExcludedAssessmentCount,
            CompleteCoverageCount = evaluation.CompleteCoverageCount,
            PartialCoverageCount = evaluation.PartialCoverageCount,
            InsufficientDataCount = evaluation.InsufficientDataCount,
            SourceConflictCount = evaluation.SourceConflictCount,
            NegativeContributionCount = evaluation.NegativeContributionCount,
            BelowThresholdCount = evaluation.BelowThresholdCount,
            ReferenceCandidateAssessmentCount = evaluation.ReferenceCandidateAssessmentCount,
            ReferenceComparableAssessmentCount = evaluation.ReferenceComparableAssessmentCount,
            ReferenceExcludedAssessmentCount = evaluation.ReferenceExcludedAssessmentCount,
            ReferenceCompleteCoverageCount = evaluation.ReferenceCompleteCoverageCount,
            ReferencePartialCoverageCount = evaluation.ReferencePartialCoverageCount,
            ReferenceInsufficientDataCount = evaluation.ReferenceInsufficientDataCount,
            ReferenceSourceConflictCount = evaluation.ReferenceSourceConflictCount,
            ReferenceNegativeContributionCount = evaluation.ReferenceNegativeContributionCount,
            ReferenceBelowThresholdCount = evaluation.ReferenceBelowThresholdCount,
            CurrentCandidateAssessmentCount = evaluation.CurrentCandidateAssessmentCount,
            CurrentComparableAssessmentCount = evaluation.CurrentComparableAssessmentCount,
            CurrentExcludedAssessmentCount = evaluation.CurrentExcludedAssessmentCount,
            CurrentCompleteCoverageCount = evaluation.CurrentCompleteCoverageCount,
            CurrentPartialCoverageCount = evaluation.CurrentPartialCoverageCount,
            CurrentInsufficientDataCount = evaluation.CurrentInsufficientDataCount,
            CurrentSourceConflictCount = evaluation.CurrentSourceConflictCount,
            CurrentNegativeContributionCount = evaluation.CurrentNegativeContributionCount,
            CurrentBelowThresholdCount = evaluation.CurrentBelowThresholdCount,
            ReferenceMedianContributionMarginRatio = evaluation.ReferenceMedianContributionMarginRatio,
            CurrentMedianContributionMarginRatio = evaluation.CurrentMedianContributionMarginRatio,
            ContributionMarginDelta = evaluation.ContributionMarginDelta,
            ReferenceMedianUnitContribution = evaluation.ReferenceMedianUnitContribution,
            CurrentMedianUnitContribution = evaluation.CurrentMedianUnitContribution,
            LeadingDriverCode = evaluation.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.ProfitabilityComparisonRuleSetVersion,
            IdempotencyKey = idempotencyKey,
            IdempotencyPayloadFingerprint = fingerprint,
            SourceProvenance = provenance,
            EvaluatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actorSubjectId,
        };

        var resultIdByKey = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var resultDraft in evaluation.Results)
        {
            var resultId = Guid.NewGuid();
            resultIdByKey[resultDraft.CanonicalResultReference] = resultId;
            run.Results.Add(new CommercialProfitabilityComparisonResult
            {
                Id = resultId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ComparisonRunId = run.Id,
                ResultDimension = resultDraft.ResultDimension,
                CanonicalResultReference = resultDraft.CanonicalResultReference,
                CustomerReference = resultDraft.CustomerReference,
                MaterialId = resultDraft.MaterialId,
                ComponentKind = resultDraft.ComponentKind,
                CandidateAssessmentCount = resultDraft.CandidateAssessmentCount,
                ComparableAssessmentCount = resultDraft.ComparableAssessmentCount,
                ExcludedAssessmentCount = resultDraft.ExcludedAssessmentCount,
                CompleteCoverageCount = resultDraft.CompleteCoverageCount,
                PartialCoverageCount = 0,
                NegativeContributionCount = resultDraft.NegativeContributionCount,
                BelowThresholdCount = resultDraft.BelowThresholdCount,
                ReferenceComparableAssessmentCount = resultDraft.ReferenceComparableAssessmentCount,
                CurrentComparableAssessmentCount = resultDraft.CurrentComparableAssessmentCount,
                ReferenceExcludedAssessmentCount = resultDraft.ReferenceExcludedAssessmentCount,
                CurrentExcludedAssessmentCount = resultDraft.CurrentExcludedAssessmentCount,
                ReferenceMedianContributionMarginRatio = resultDraft.ReferenceMedianContributionMarginRatio,
                CurrentMedianContributionMarginRatio = resultDraft.CurrentMedianContributionMarginRatio,
                ContributionMarginDelta = resultDraft.ContributionMarginDelta,
                ReferenceMedianUnitContribution = resultDraft.ReferenceMedianUnitContribution,
                CurrentMedianUnitContribution = resultDraft.CurrentMedianUnitContribution,
                ReferenceMedianUnitCost = resultDraft.ReferenceMedianUnitCost,
                CurrentMedianUnitCost = resultDraft.CurrentMedianUnitCost,
                SignedUnitCostChange = resultDraft.SignedUnitCostChange,
                SignedUnitCostChangeRatio = resultDraft.SignedUnitCostChangeRatio,
                ReferenceMedianCostShare = resultDraft.ReferenceMedianCostShare,
                CurrentMedianCostShare = resultDraft.CurrentMedianCostShare,
                CostShareDelta = resultDraft.CostShareDelta,
                PositionState = resultDraft.PositionState,
                ContributionRank = resultDraft.ContributionRank,
                IsTied = resultDraft.IsTied,
                EvidenceStatus = resultDraft.EvidenceStatus,
                SourceProvenance = provenance,
                SortOrder = resultDraft.ContributionRank ?? 0,
                CreatedAtUtc = now,
            });
        }

        var orderedDrivers = CommercialProfitabilityComparisonEvaluator.OrderDrivers(evaluation.Drivers);
        for (var index = 0; index < orderedDrivers.Count; index++)
        {
            var driver = orderedDrivers[index];
            run.Drivers.Add(new CommercialProfitabilityComparisonDriver
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
                AffectedAssessmentCount = driver.AffectedAssessmentCount,
                SupportingText = driver.SupportingText,
                SourceProvenance = provenance,
                SortOrder = index + 1,
                IsLeadingDriver = index == 0,
                CreatedAtUtc = now,
            });
        }

        dbContext.CommercialProfitabilityComparisonRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureComparisonAssessmentAsync(
        NevaxisZentraDbContext dbContext,
        Guid assessmentId,
        Guid salesEvidenceId,
        ComparisonScenario scenario,
        Guid materialId,
        CommercialProfitabilityProfile profile,
        Guid tenantId,
        Guid facilityId,
        string customerReference,
        CostMix costMix,
        bool partialCoverage,
        bool sourceConflict,
        DateTimeOffset evaluatedAtUtc,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        int assessmentIndex,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var evidenceTimestamp = evaluatedAtUtc.AddDays(-5);
        await EnsureSalesEvidenceAsync(
            dbContext,
            salesEvidenceId,
            tenantId,
            facilityId,
            materialId,
            customerReference,
            scenario,
            assessmentIndex,
            evidenceTimestamp,
            actorSubjectId,
            seedTimestampUtc,
            provenance,
            cancellationToken);

        var costComponents = BuildCostComponentDefinitions(scenario.ScenarioIndex, assessmentIndex, costMix, partialCoverage, sourceConflict);
        foreach (var component in costComponents)
        {
            await EnsureCostEvidenceAsync(
                dbContext,
                component.EvidenceId,
                tenantId,
                facilityId,
                materialId,
                customerReference,
                scenario,
                assessmentIndex,
                component,
                evidenceTimestamp,
                actorSubjectId,
                seedTimestampUtc,
                provenance,
                cancellationToken);
        }

        var metrics = ComputeAssessmentMetrics(costMix, partialCoverage, sourceConflict, profile);
        var existing = await dbContext.CommercialProfitabilityAssessments
            .Include(a => a.CostComponents)
            .SingleOrDefaultAsync(a => a.Id == assessmentId, cancellationToken);
        if (existing is not null)
        {
            var changed = ApplyAssessmentFields(
                existing,
                scenario,
                materialId,
                customerReference,
                profile,
                salesEvidenceId,
                metrics,
                evaluatedAtUtc,
                provenance,
                assessmentIndex);

            var componentsChanged = SyncAssessmentCostComponents(
                existing,
                costComponents,
                metrics,
                provenance,
                seedTimestampUtc,
                tenantId,
                facilityId,
                scenario.IncludeCostComponents);

            if (changed || componentsChanged)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var evidenceSetReference = $"COMMPROFIT2-SET-{scenario.MaterialCode}-{assessmentIndex:D3}";
        var assessment = new CommercialProfitabilityAssessment
        {
            Id = assessmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
            AssessmentReference = $"COMMPROFIT2-ASM-{scenario.ScenarioIndex:D2}-{assessmentIndex:D3}",
            ProfileId = profile.Id,
            ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SalesEvidenceObservationId = salesEvidenceId,
            CustomerReference = customerReference,
            MaterialId = materialId,
            QuotationReference = $"COMMPROFIT2-QTE-{scenario.MaterialCode}",
            EvidenceSetReference = evidenceSetReference,
            ComparableNetSalesUnitPrice = SalesUnitPrice,
            TotalIncludedUnitCost = metrics.TotalIncludedUnitCost,
            UnitContribution = metrics.UnitContribution,
            ContributionMarginRatio = metrics.ContributionMarginRatio,
            MarkupRatio = metrics.MarkupRatio,
            CandidateCostComponentCount = metrics.CandidateCostComponentCount,
            IncludedCostComponentCount = metrics.IncludedCostComponentCount,
            ExcludedCostComponentCount = metrics.ExcludedCostComponentCount,
            RequiredCostComponentCount = metrics.RequiredCostComponentCount,
            AvailableRequiredCostComponentCount = metrics.AvailableRequiredCostComponentCount,
            MissingRequiredCostComponentCount = metrics.MissingRequiredCostComponentCount,
            MissingRequiredComponentKinds = metrics.MissingRequiredComponentKinds,
            CostCoverageStatus = metrics.CostCoverageStatus,
            CurrencyCode = "TRY",
            UnitOfMeasure = "kg",
            GrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            TaxBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            DiscountBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            FreightBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            EvidenceStatus = metrics.EvidenceStatus,
            Outcome = metrics.Outcome,
            LeadingDriverCode = metrics.LeadingDriverCode,
            RuleSetVersion = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion,
            IdempotencyKey = $"commprofit2-comparison-{assessmentId:N}",
            IdempotencyPayloadFingerprint = $"commprofit2-comparison-{assessmentId:N}",
            SourceProvenance = provenance,
            EvaluatedAtUtc = evaluatedAtUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        };

        if (scenario.IncludeCostComponents)
        {
            AddAssessmentCostComponents(assessment, costComponents, metrics, provenance, seedTimestampUtc, tenantId, facilityId);
        }

        dbContext.CommercialProfitabilityAssessments.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSalesEvidenceAsync(
        NevaxisZentraDbContext dbContext,
        Guid evidenceId,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        string customerReference,
        ComparisonScenario scenario,
        int assessmentIndex,
        DateTimeOffset evidenceTimestampUtc,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.CommercialProfitabilityEvidenceObservations
            .SingleOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        dbContext.CommercialProfitabilityEvidenceObservations.Add(new CommercialProfitabilityEvidenceObservation
        {
            Id = evidenceId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ObservationKind = CommercialProfitabilityObservationKinds.Sales,
            EvidenceSetReference = $"COMMPROFIT2-SET-{scenario.MaterialCode}-{assessmentIndex:D3}",
            CustomerReference = customerReference,
            MaterialId = materialId,
            QuotationReference = $"COMMPROFIT2-QTE-{scenario.MaterialCode}",
            ComparableNetSalesUnitPrice = SalesUnitPrice,
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
            SourceReference = $"COMMPROFIT2-SALES-{evidenceId:N}",
            SourceProvenance = provenance,
            SourceFingerprint = $"commprofit2-comparison-sales-{evidenceId:N}",
            IngestIdempotencyKey = $"commprofit2-comparison-sales-{evidenceId:N}",
            IngestIdempotencyPayloadFingerprint = $"commprofit2-comparison-sales-{evidenceId:N}",
            ReceivedAtUtc = evidenceTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCostEvidenceAsync(
        NevaxisZentraDbContext dbContext,
        Guid evidenceId,
        Guid tenantId,
        Guid facilityId,
        Guid materialId,
        string customerReference,
        ComparisonScenario scenario,
        int assessmentIndex,
        CostComponentDefinition component,
        DateTimeOffset evidenceTimestampUtc,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        string provenance,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.CommercialProfitabilityEvidenceObservations
            .SingleOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        dbContext.CommercialProfitabilityEvidenceObservations.Add(new CommercialProfitabilityEvidenceObservation
        {
            Id = evidenceId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ObservationKind = CommercialProfitabilityObservationKinds.CostComponent,
            EvidenceSetReference = $"COMMPROFIT2-SET-{scenario.MaterialCode}-{assessmentIndex:D3}",
            CustomerReference = customerReference,
            MaterialId = materialId,
            ComponentKind = component.ComponentKind,
            UnitCost = component.UnitCost,
            CurrencyCode = "TRY",
            UnitOfMeasure = "kg",
            GrossNetBasis = CommercialSalesPriceGrossNetBasis.Net,
            TaxBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            DiscountBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            FreightBasis = CommercialIntelligencePriceBasisInclusionStates.Excluded,
            CostBasis = CostBasis,
            SourceKind = SourceKind,
            EvidenceStatus = component.EvidenceStatus,
            EvidenceTimestampUtc = evidenceTimestampUtc,
            IsCancelledOrReturned = false,
            SourceReference = $"COMMPROFIT2-COST-{evidenceId:N}",
            SourceProvenance = provenance,
            SourceFingerprint = $"commprofit2-comparison-cost-{evidenceId:N}",
            IngestIdempotencyKey = $"commprofit2-comparison-cost-{evidenceId:N}",
            IngestIdempotencyPayloadFingerprint = $"commprofit2-comparison-cost-{evidenceId:N}",
            ReceivedAtUtc = evidenceTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<CostComponentDefinition> BuildCostComponentDefinitions(
        int scenarioIndex,
        int assessmentIndex,
        CostMix costMix,
        bool partialCoverage,
        bool sourceConflict)
    {
        var components = new List<CostComponentDefinition>();
        AddComponent(components, scenarioIndex, assessmentIndex, 0, CommercialProfitabilityCostComponentKinds.DirectMaterial, costMix.DirectMaterial, sourceConflict);
        AddComponent(components, scenarioIndex, assessmentIndex, 1, CommercialProfitabilityCostComponentKinds.DirectLabor, costMix.DirectLabor, sourceConflict);
        AddComponent(components, scenarioIndex, assessmentIndex, 2, CommercialProfitabilityCostComponentKinds.Machine, costMix.Machine, sourceConflict);
        if (!partialCoverage && costMix.Energy.HasValue)
        {
            AddComponent(components, scenarioIndex, assessmentIndex, 3, CommercialProfitabilityCostComponentKinds.Energy, costMix.Energy.Value, sourceConflict);
        }

        return components;
    }

    private static void AddComponent(
        List<CostComponentDefinition> components,
        int scenarioIndex,
        int assessmentIndex,
        int componentIndex,
        string componentKind,
        decimal unitCost,
        bool sourceConflict)
    {
        components.Add(new CostComponentDefinition(
            ComparisonCostEvidenceId(scenarioIndex, assessmentIndex, componentIndex),
            componentKind,
            unitCost,
            sourceConflict
                ? CommercialProfitabilityEvidenceStatuses.SourceConflict
                : CommercialProfitabilityEvidenceStatuses.Simulated));
    }

    private static AssessmentMetrics ComputeAssessmentMetrics(
        CostMix costMix,
        bool partialCoverage,
        bool sourceConflict,
        CommercialProfitabilityProfile profile)
    {
        if (sourceConflict)
        {
            return new AssessmentMetrics(
                costMix.TotalIncludedCost,
                SalesUnitPrice - costMix.TotalIncludedCost,
                costMix.ContributionMarginRatio(SalesUnitPrice),
                costMix.TotalIncludedCost <= 0m ? null : (SalesUnitPrice - costMix.TotalIncludedCost) / costMix.TotalIncludedCost,
                costMix.IncludedComponentCount,
                costMix.IncludedComponentCount,
                0,
                4,
                costMix.IncludedComponentCount,
                0,
                string.Empty,
                CommercialProfitabilityCostCoverageStatuses.SourceConflict,
                CommercialProfitabilityEvidenceStatuses.SourceConflict,
                CommercialProfitabilityAssessmentOutcomes.SourceConflict,
                CommercialProfitabilityDriverReasonCodes.SourceConflict);
        }

        var missingKinds = partialCoverage || !costMix.Energy.HasValue
            ? CommercialProfitabilityCostComponentKinds.Energy
            : string.Empty;
        var missingCount = string.IsNullOrEmpty(missingKinds) ? 0 : 1;
        var includedCount = costMix.IncludedComponentCount;
        var totalIncluded = costMix.TotalIncludedCost;
        var unitContribution = SalesUnitPrice - totalIncluded;
        var marginRatio = costMix.ContributionMarginRatio(SalesUnitPrice);
        var markupRatio = totalIncluded <= 0m ? (decimal?)null : unitContribution / totalIncluded;

        string coverageStatus;
        string outcome;
        string leadingDriver;
        string evidenceStatus;
        if (missingCount > 0)
        {
            coverageStatus = CommercialProfitabilityCostCoverageStatuses.PartialCostCoverage;
            outcome = CommercialProfitabilityAssessmentOutcomes.InsufficientData;
            leadingDriver = CommercialProfitabilityDriverReasonCodes.IncompleteCostCoverage;
            evidenceStatus = CommercialProfitabilityEvidenceStatuses.InsufficientData;
        }
        else if (unitContribution < 0m)
        {
            coverageStatus = CommercialProfitabilityCostCoverageStatuses.CompleteRequiredCostCoverage;
            outcome = CommercialProfitabilityAssessmentOutcomes.NegativeContribution;
            leadingDriver = CommercialProfitabilityDriverReasonCodes.NegativeUnitContribution;
            evidenceStatus = CommercialProfitabilityEvidenceStatuses.Simulated;
        }
        else if (marginRatio < profile.MinimumContributionMarginRatio)
        {
            coverageStatus = CommercialProfitabilityCostCoverageStatuses.CompleteRequiredCostCoverage;
            outcome = CommercialProfitabilityAssessmentOutcomes.BelowThreshold;
            leadingDriver = CommercialProfitabilityDriverReasonCodes.ContributionMarginBelowThreshold;
            evidenceStatus = CommercialProfitabilityEvidenceStatuses.Simulated;
        }
        else
        {
            coverageStatus = CommercialProfitabilityCostCoverageStatuses.CompleteRequiredCostCoverage;
            outcome = CommercialProfitabilityAssessmentOutcomes.WithinThreshold;
            leadingDriver = CommercialProfitabilityDriverReasonCodes.WithinProfile;
            evidenceStatus = CommercialProfitabilityEvidenceStatuses.Simulated;
        }

        return new AssessmentMetrics(
            totalIncluded,
            unitContribution,
            marginRatio,
            markupRatio,
            includedCount + missingCount,
            includedCount,
            missingCount,
            4,
            includedCount,
            missingCount,
            missingKinds,
            coverageStatus,
            evidenceStatus,
            outcome,
            leadingDriver);
    }

    private static bool ApplyAssessmentFields(
        CommercialProfitabilityAssessment assessment,
        ComparisonScenario scenario,
        Guid materialId,
        string customerReference,
        CommercialProfitabilityProfile profile,
        Guid salesEvidenceId,
        AssessmentMetrics metrics,
        DateTimeOffset evaluatedAtUtc,
        string provenance,
        int assessmentIndex)
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

        Set(() => assessment.CustomerReference, v => assessment.CustomerReference = v, customerReference);
        Set(() => assessment.MaterialId, v => assessment.MaterialId = v, materialId);
        Set(() => assessment.SalesEvidenceObservationId, v => assessment.SalesEvidenceObservationId = v, salesEvidenceId);
        Set(() => assessment.TotalIncludedUnitCost, v => assessment.TotalIncludedUnitCost = v, metrics.TotalIncludedUnitCost);
        Set(() => assessment.UnitContribution, v => assessment.UnitContribution = v, metrics.UnitContribution);
        Set(() => assessment.ContributionMarginRatio, v => assessment.ContributionMarginRatio = v, metrics.ContributionMarginRatio);
        Set(() => assessment.MarkupRatio, v => assessment.MarkupRatio = v, metrics.MarkupRatio);
        Set(() => assessment.CandidateCostComponentCount, v => assessment.CandidateCostComponentCount = v, metrics.CandidateCostComponentCount);
        Set(() => assessment.IncludedCostComponentCount, v => assessment.IncludedCostComponentCount = v, metrics.IncludedCostComponentCount);
        Set(() => assessment.ExcludedCostComponentCount, v => assessment.ExcludedCostComponentCount = v, metrics.ExcludedCostComponentCount);
        Set(() => assessment.RequiredCostComponentCount, v => assessment.RequiredCostComponentCount = v, metrics.RequiredCostComponentCount);
        Set(() => assessment.AvailableRequiredCostComponentCount, v => assessment.AvailableRequiredCostComponentCount = v, metrics.AvailableRequiredCostComponentCount);
        Set(() => assessment.MissingRequiredCostComponentCount, v => assessment.MissingRequiredCostComponentCount = v, metrics.MissingRequiredCostComponentCount);
        Set(() => assessment.MissingRequiredComponentKinds, v => assessment.MissingRequiredComponentKinds = v, metrics.MissingRequiredComponentKinds);
        Set(() => assessment.CostCoverageStatus, v => assessment.CostCoverageStatus = v, metrics.CostCoverageStatus);
        Set(() => assessment.EvidenceStatus, v => assessment.EvidenceStatus = v, metrics.EvidenceStatus);
        Set(() => assessment.Outcome, v => assessment.Outcome = v, metrics.Outcome);
        Set(() => assessment.LeadingDriverCode, v => assessment.LeadingDriverCode = v, metrics.LeadingDriverCode);
        Set(() => assessment.EvaluatedAtUtc, v => assessment.EvaluatedAtUtc = v, evaluatedAtUtc);
        Set(() => assessment.SourceProvenance, v => assessment.SourceProvenance = v, provenance);
        Set(() => assessment.AssessmentReference, v => assessment.AssessmentReference = v,
            $"COMMPROFIT2-ASM-{scenario.ScenarioIndex:D2}-{assessmentIndex:D3}");

        return changed;
    }

    private static bool SyncAssessmentCostComponents(
        CommercialProfitabilityAssessment assessment,
        IReadOnlyList<CostComponentDefinition> costComponents,
        AssessmentMetrics metrics,
        string provenance,
        DateTimeOffset seedTimestampUtc,
        Guid tenantId,
        Guid facilityId,
        bool includeCostComponents)
    {
        if (!includeCostComponents)
        {
            if (assessment.CostComponents.Count == 0)
            {
                return false;
            }

            assessment.CostComponents.Clear();
            return true;
        }

        if (assessment.CostComponents.Count == costComponents.Count)
        {
            return false;
        }

        assessment.CostComponents.Clear();
        AddAssessmentCostComponents(assessment, costComponents, metrics, provenance, seedTimestampUtc, tenantId, facilityId);
        return true;
    }

    private static void AddAssessmentCostComponents(
        CommercialProfitabilityAssessment assessment,
        IReadOnlyList<CostComponentDefinition> costComponents,
        AssessmentMetrics metrics,
        string provenance,
        DateTimeOffset seedTimestampUtc,
        Guid tenantId,
        Guid facilityId)
    {
        var sortOrder = 0;
        foreach (var component in costComponents)
        {
            sortOrder++;
            assessment.CostComponents.Add(new CommercialProfitabilityAssessmentCostComponent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                AssessmentId = assessment.Id,
                SourceEvidenceObservationId = component.EvidenceId,
                ComponentKind = component.ComponentKind,
                UnitCost = component.UnitCost,
                CurrencyCode = "TRY",
                UnitOfMeasure = "kg",
                CostBasis = CostBasis,
                EvidenceTimestampUtc = seedTimestampUtc,
                SourceKind = SourceKind,
                SourceReference = $"COMMPROFIT2-COST-{component.EvidenceId:N}",
                EvidenceStatus = component.EvidenceStatus,
                InclusionStatus = CommercialProfitabilityComponentInclusionStatuses.Included,
                SourceProvenance = provenance,
                SortOrder = sortOrder,
                CreatedAtUtc = seedTimestampUtc,
            });
        }
    }

    private static bool RequiresTemporalWindows(string comparisonType) =>
        string.Equals(comparisonType, CommercialProfitabilityComparisonTypes.TemporalContributionTrend, StringComparison.Ordinal)
        || string.Equals(comparisonType, CommercialProfitabilityComparisonTypes.CostComponentMixChange, StringComparison.Ordinal);

    private static Guid ComparisonAssessmentId(int scenarioIndex, int assessmentIndex)
    {
        var value = 0x3d00 + scenarioIndex * 20 + assessmentIndex;
        return Guid.Parse($"bb000001-{value:x4}-4000-8000-{value:x012}");
    }

    private static Guid ComparisonSalesEvidenceId(int scenarioIndex, int assessmentIndex)
    {
        var value = 0x3e00 + scenarioIndex * 20 + assessmentIndex;
        return Guid.Parse($"bb000001-{value:x4}-4000-8000-{value:x012}");
    }

    private static Guid ComparisonCostEvidenceId(int scenarioIndex, int assessmentIndex, int componentIndex)
    {
        var value = 0x3f00 + scenarioIndex * 80 + assessmentIndex * 10 + componentIndex;
        return Guid.Parse($"bb000001-{value:x4}-4000-8000-{value:x012}");
    }

    private sealed record CostMix(decimal DirectMaterial, decimal DirectLabor, decimal Machine, decimal? Energy)
    {
        public int IncludedComponentCount =>
            Energy.HasValue ? 4 : 3;

        public decimal TotalIncludedCost =>
            DirectMaterial + DirectLabor + Machine + (Energy ?? 0m);

        public decimal ContributionMarginRatio(decimal salesPrice) =>
            (salesPrice - TotalIncludedCost) / salesPrice;
    }

    private sealed record CostComponentDefinition(
        Guid EvidenceId,
        string ComponentKind,
        decimal UnitCost,
        string EvidenceStatus);

    private sealed record AssessmentMetrics(
        decimal TotalIncludedUnitCost,
        decimal UnitContribution,
        decimal ContributionMarginRatio,
        decimal? MarkupRatio,
        int CandidateCostComponentCount,
        int IncludedCostComponentCount,
        int ExcludedCostComponentCount,
        int RequiredCostComponentCount,
        int AvailableRequiredCostComponentCount,
        int MissingRequiredCostComponentCount,
        string MissingRequiredComponentKinds,
        string CostCoverageStatus,
        string EvidenceStatus,
        string Outcome,
        string LeadingDriverCode);

    private sealed record ComparisonScenario(
        int ScenarioIndex,
        Guid MaterialId,
        string MaterialCode,
        string DefaultCustomerReference,
        IReadOnlyList<AssessmentBatch> AssessmentBatches,
        bool IncludeCostComponents = false,
        Guid? SecondaryMaterialId = null,
        string? SecondaryMaterialCode = null);

    private sealed record AssessmentBatch(
        int WindowKind,
        int Count,
        CostMix CostMix,
        string? CustomerReference = null,
        Guid? MaterialId = null,
        bool PartialCoverage = false,
        bool SourceConflict = false);
}
