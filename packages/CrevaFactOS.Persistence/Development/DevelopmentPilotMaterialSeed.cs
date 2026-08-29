using CrevaFactOS.Persistence.Material;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotMaterialSeed
{
    internal static async Task EnsureMaterialPilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsurePilotPpMaterialAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotAbsMaterialAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsurePilotBlockedMaterialAsync(
            dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsurePilotPpMaterialAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Materials.AsNoTracking()
            .AnyAsync(m => m.Id == DevelopmentPilotIds.MaterialPilotPpId, cancellationToken);

        if (exists)
        {
            var tracked = await dbContext.Materials
                .FirstOrDefaultAsync(m => m.Id == DevelopmentPilotIds.MaterialPilotPpId, cancellationToken);
            if (tracked is not null && string.IsNullOrWhiteSpace(tracked.ExternalMaterialCode))
            {
                tracked.ExternalMaterialCode = DevelopmentPilotIds.MaterialPilotPpErpProductCode;
                tracked.UpdatedAtUtc = seedTimestampUtc;
            }

            return;
        }

        var material = new MaterialRegistryEntry
        {
            Id = DevelopmentPilotIds.MaterialPilotPpId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = "PILOT-PP-001",
            CodeNormalized = "PILOT-PP-001",
            Name = "Pilot PP Homopolimer (Geliştirme)",
            Description = "Deterministik geliştirme/pilot verisi — OEM/doğrulanmış tedarikçi verisi değildir.",
            MaterialCategory = MaterialCategory.VirginPolymer,
            PolymerFamily = PolymerFamily.Pp,
            Manufacturer = "Pilot Polimer A.Ş.",
            TradeNameOrGrade = "PP-H110",
            ColorName = "Doğal",
            PhysicalForm = PhysicalForm.Granule,
            LifecycleStatus = MaterialLifecycleStatus.Active,
            UnitOfMeasure = "kg",
            IsActive = true,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceSystem = "development-pilot",
            SourceReference = "material-pilot-pp-001",
            ExternalMaterialCode = DevelopmentPilotIds.MaterialPilotPpErpProductCode,
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
            TechnicalSpecification = new MaterialTechnicalSpecification
            {
                Id = DevelopmentPilotIds.MaterialPilotPpSpecId,
                TenantId = tenantId,
                MaterialId = DevelopmentPilotIds.MaterialPilotPpId,
                DensityGPerCm3 = 0.905m,
                MeltFlowRate = 12m,
                MeltFlowUnit = "g/10min",
                MeltFlowTestTemperatureC = 230m,
                MeltFlowTestLoadKg = 2.16m,
                MoistureLimitPercent = 0.05m,
                DryingRequired = false,
                MeltTemperatureMinC = 200m,
                MeltTemperatureMaxC = 260m,
                MoldTemperatureMinC = 20m,
                MoldTemperatureMaxC = 60m,
                StorageTemperatureMinC = 10m,
                StorageTemperatureMaxC = 35m,
                MaximumRegrindPercent = 20m,
                TechnicalNotes = "İllüstratif geliştirme profili — gerçek tedarikçi TDS değil.",
                SpecificationSource = MaterialSpecificationSource.DevelopmentPilot,
                SpecificationSourceReference = "pilot-pp-profile-v1",
                SpecificationRecordedAtUtc = seedTimestampUtc,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBySubjectId = actorSubjectId,
                UpdatedBySubjectId = actorSubjectId,
                Version = 1,
            },
        };

        dbContext.Materials.Add(material);
    }

    private static async Task EnsurePilotAbsMaterialAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Materials.AsNoTracking()
            .AnyAsync(m => m.Id == DevelopmentPilotIds.MaterialPilotAbsId, cancellationToken);

        if (exists)
        {
            return;
        }

        var material = new MaterialRegistryEntry
        {
            Id = DevelopmentPilotIds.MaterialPilotAbsId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = "PILOT-ABS-001",
            CodeNormalized = "PILOT-ABS-001",
            Name = "Pilot ABS (Kısmi Teknik Veri)",
            Description = "Deterministik geliştirme/pilot verisi — kısmi teknik profil.",
            MaterialCategory = MaterialCategory.VirginPolymer,
            PolymerFamily = PolymerFamily.Abs,
            Manufacturer = "Pilot Plastik Ltd.",
            TradeNameOrGrade = "ABS-GP22",
            ColorName = "Siyah",
            PhysicalForm = PhysicalForm.Granule,
            LifecycleStatus = MaterialLifecycleStatus.Active,
            UnitOfMeasure = "kg",
            IsActive = true,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceSystem = "development-pilot",
            SourceReference = "material-pilot-abs-001",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
            TechnicalSpecification = new MaterialTechnicalSpecification
            {
                Id = DevelopmentPilotIds.MaterialPilotAbsSpecId,
                TenantId = tenantId,
                MaterialId = DevelopmentPilotIds.MaterialPilotAbsId,
                DensityGPerCm3 = 1.04m,
                MeltFlowRate = 18m,
                MeltFlowUnit = "g/10min",
                SpecificationSource = MaterialSpecificationSource.DevelopmentPilot,
                SpecificationSourceReference = "pilot-abs-partial-v1",
                SpecificationRecordedAtUtc = seedTimestampUtc,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBySubjectId = actorSubjectId,
                UpdatedBySubjectId = actorSubjectId,
                Version = 1,
            },
        };

        dbContext.Materials.Add(material);
    }

    private static async Task EnsurePilotBlockedMaterialAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Materials.AsNoTracking()
            .AnyAsync(m => m.Id == DevelopmentPilotIds.MaterialPilotBlockedId, cancellationToken);

        if (exists)
        {
            return;
        }

        var material = new MaterialRegistryEntry
        {
            Id = DevelopmentPilotIds.MaterialPilotBlockedId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = "PILOT-BLK-001",
            CodeNormalized = "PILOT-BLK-001",
            Name = "Pilot Bloke Masterbatch",
            Description = "Deterministik geliştirme/pilot verisi — bloke durum örneği.",
            MaterialCategory = MaterialCategory.Masterbatch,
            PolymerFamily = PolymerFamily.Pp,
            Manufacturer = "Pilot Renk Kimya",
            TradeNameOrGrade = "MB-BLACK-3%",
            ColorName = "Siyah",
            PhysicalForm = PhysicalForm.Granule,
            LifecycleStatus = MaterialLifecycleStatus.Blocked,
            UnitOfMeasure = "kg",
            IsActive = false,
            SourceType = MaterialSourceType.DevelopmentPilot,
            SourceSystem = "development-pilot",
            SourceReference = "material-pilot-blocked-001",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            UpdatedBySubjectId = actorSubjectId,
            Version = 1,
        };

        dbContext.Materials.Add(material);
    }
}
