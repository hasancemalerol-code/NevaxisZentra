using NevaxisZentra.Persistence.Mold;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotMoldCompatibilitySeed
{
    internal static async Task EnsureMoldCompatibilityPilotProfilesAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsurePressProfileAsync(dbContext, tenantId, facilityId, seedTimestampUtc, cancellationToken);
        await EnsureCncInsufficientProfileAsync(dbContext, tenantId, facilityId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsurePressProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.MachineCompatibilityCapabilityProfiles
            .IgnoreQueryFilters()
            .AnyAsync(
                p => p.TenantId == tenantId
                    && p.FacilityId == facilityId
                    && p.MachineAssetId == DevelopmentPilotIds.EdgePilotPressMachineId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.MachineCompatibilityCapabilityProfiles.Add(new MachineCompatibilityCapabilityProfile
        {
            Id = DevelopmentPilotIds.MoldCompatibilityPressProfileId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MachineAssetId = DevelopmentPilotIds.EdgePilotPressMachineId,
            MachineCode = "PRESS-001",
            MachineDisplayName = "Enjeksiyon Pres 350T",
            ClampForceTon = 350m,
            MaximumMoldWidthMm = 620m,
            MaximumMoldHeightMm = 560m,
            MinimumMoldDepthMm = 180m,
            MaximumMoldDepthMm = 520m,
            TieBarWidthMm = 580m,
            TieBarHeightMm = 520m,
            MaximumShotCapacityGrams = 1200m,
            InjectionUnit = "A",
            SupportedMaterialGroups = "pp,abs",
            SourceType = MoldSourceTypes.Manual,
            SourceReference = "pilot-seed",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString(),
            UpdatedBySubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString(),
            Version = 1,
        });
    }

    private static async Task EnsureCncInsufficientProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.MachineCompatibilityCapabilityProfiles
            .IgnoreQueryFilters()
            .AnyAsync(
                p => p.TenantId == tenantId
                    && p.FacilityId == facilityId
                    && p.MachineAssetId == DevelopmentPilotIds.EdgePilotCncMachineId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.MachineCompatibilityCapabilityProfiles.Add(new MachineCompatibilityCapabilityProfile
        {
            Id = DevelopmentPilotIds.MoldCompatibilityCncProfileId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MachineAssetId = DevelopmentPilotIds.EdgePilotCncMachineId,
            MachineCode = "CNC-001",
            MachineDisplayName = "CNC Tezgâh 001",
            SourceType = MoldSourceTypes.Manual,
            SourceReference = "pilot-seed-insufficient",
            DataRecordedAtUtc = seedTimestampUtc,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString(),
            UpdatedBySubjectId = DevelopmentPilotIds.IdentitySubjectId.ToString(),
            Version = 1,
        });
    }
}
