using NevaxisZentra.Persistence.Material;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotMaterialRequirementSeed
{
    internal static async Task EnsureMaterialRequirementPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsurePilotPpCompleteProfileAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotAbsPartialProfileAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotPpExceedsRegrindProfileAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotPpCompleteConsumptionObservationAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsurePilotPpCompleteProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialRequirementProfiles.AsNoTracking()
                .AnyAsync(p => p.Id == DevelopmentPilotIds.MaterialPilotRequirementPpCompleteId, cancellationToken))
        {
            return;
        }

        dbContext.MaterialRequirementProfiles.Add(new MaterialRequirementProfile
        {
            Id = DevelopmentPilotIds.MaterialPilotRequirementPpCompleteId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = DevelopmentPilotIds.MaterialPilotPpId,
            ProductionOrderReference = "PILOT-PO-PP-001",
            ProductReference = "PILOT-PROD-PP-CAP",
            MoldReference = "PILOT-MOLD-PP-4CAV",
            RequirementType = MaterialRequirementType.ProductionOrder,
            GoodPartTargetQuantity = 10000m,
            PartWeightGrams = 45m,
            RunnerWeightGramsPerShot = 120m,
            EffectiveCavityCount = 4,
            SetupAllowanceGrams = 500m,
            ProcessLossAllowancePercent = 3m,
            PlannedVirginPercent = 80m,
            PlannedRegrindPercent = 20m,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-requirement-pilot-pp-complete",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }

    private static async Task EnsurePilotAbsPartialProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialRequirementProfiles.AsNoTracking()
                .AnyAsync(p => p.Id == DevelopmentPilotIds.MaterialPilotRequirementAbsPartialId, cancellationToken))
        {
            return;
        }

        dbContext.MaterialRequirementProfiles.Add(new MaterialRequirementProfile
        {
            Id = DevelopmentPilotIds.MaterialPilotRequirementAbsPartialId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = DevelopmentPilotIds.MaterialPilotAbsId,
            ProductionOrderReference = "PILOT-PO-ABS-001",
            ProductReference = "PILOT-PROD-ABS-HOUSING",
            MoldReference = "PILOT-MOLD-ABS-2CAV",
            RequirementType = MaterialRequirementType.ProductionOrder,
            GoodPartTargetQuantity = 5000m,
            PartWeightGrams = null,
            RunnerWeightGramsPerShot = 95m,
            EffectiveCavityCount = null,
            SetupAllowanceGrams = 400m,
            ProcessLossAllowancePercent = 2.5m,
            PlannedVirginPercent = 100m,
            PlannedRegrindPercent = 0m,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-requirement-pilot-abs-partial",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }

    private static async Task EnsurePilotPpExceedsRegrindProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialRequirementProfiles.AsNoTracking()
                .AnyAsync(p => p.Id == DevelopmentPilotIds.MaterialPilotRequirementPpExceedsRegrindId, cancellationToken))
        {
            return;
        }

        dbContext.MaterialRequirementProfiles.Add(new MaterialRequirementProfile
        {
            Id = DevelopmentPilotIds.MaterialPilotRequirementPpExceedsRegrindId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = DevelopmentPilotIds.MaterialPilotPpId,
            ProductionOrderReference = "PILOT-PO-PP-002",
            ProductReference = "PILOT-PROD-PP-REGRIND-TEST",
            MoldReference = "PILOT-MOLD-PP-4CAV",
            RequirementType = MaterialRequirementType.ProductionOrder,
            GoodPartTargetQuantity = 8000m,
            PartWeightGrams = 42m,
            RunnerWeightGramsPerShot = 110m,
            EffectiveCavityCount = 4,
            SetupAllowanceGrams = 450m,
            ProcessLossAllowancePercent = 3m,
            PlannedVirginPercent = 75m,
            PlannedRegrindPercent = 25m,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-requirement-pilot-pp-exceeds-regrind",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }

    private static async Task EnsurePilotPpCompleteConsumptionObservationAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.MaterialConsumptionObservations.AsNoTracking()
                .AnyAsync(o => o.Id == DevelopmentPilotIds.MaterialPilotConsumptionObservationId, cancellationToken))
        {
            return;
        }

        dbContext.MaterialConsumptionObservations.Add(new MaterialConsumptionObservation
        {
            Id = DevelopmentPilotIds.MaterialPilotConsumptionObservationId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MaterialId = DevelopmentPilotIds.MaterialPilotPpId,
            MaterialLotId = DevelopmentPilotIds.MaterialPilotLotPpAvailableId,
            RequirementProfileId = DevelopmentPilotIds.MaterialPilotRequirementPpCompleteId,
            ProductionOrderReference = "PILOT-PO-PP-001",
            ProductReference = "PILOT-PROD-PP-CAP",
            MoldReference = "PILOT-MOLD-PP-4CAV",
            ObservationType = MaterialConsumptionObservationType.ConsumptionObserved,
            Quantity = 500m,
            UnitOfMeasure = "kg",
            VirginQuantity = 400m,
            RegrindQuantity = 100m,
            GoodPartQuantity = 6000m,
            ScrapQuantity = 120m,
            ObservedAtUtc = seedTimestampUtc.AddHours(-6),
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceReference = "material-consumption-pilot-pp-complete",
            DataRecordedAtUtc = seedTimestampUtc,
            Notes = "Deterministik geliştirme/pilot verisi — operasyonel tüketim gözlemi; ERP stok hareketi değildir.",
            IdempotencyKey = "pilot-pp-consumption-001",
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            Version = 1,
        });
    }
}
