using CrevaFactOS.Persistence.Mold;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotMoldSeed
{
    public static async Task EnsureMoldFollowUpPilotFixturesAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string createdBySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Molds
            .AsNoTracking()
            .AnyAsync(
                m => m.Id == DevelopmentPilotIds.MoldPilotMaintenanceFollowUpId,
                cancellationToken);

        if (existing)
        {
            return;
        }

        dbContext.Molds.AddRange(
            new MoldRegistryEntry
            {
                Id = DevelopmentPilotIds.MoldPilotMaintenanceFollowUpId,
                TenantId = tenantId,
                FacilityId = facilityId,
                Code = "MOLD-PILOT-001",
                CodeNormalized = "mold-pilot-001",
                Name = "Pilot bakım kalıbı",
                Description = "Bakım yaşam döngüsünde pilot kalıp kaydı; teknik inceleme görev taslağı adayı.",
                LifecycleState = MoldLifecycleState.Maintenance,
                LocationType = MoldLocationType.MaintenanceArea,
                LocationLabel = "Bakım alanı A",
                CavityCount = 4,
                NominalShotLife = 500_000,
                CurrentShotCount = 420_000,
                CounterRecordedAtUtc = seedTimestampUtc,
                CounterSource = MoldCounterSourceType.Manual,
                PrimaryProductReference = "POM-WHEEL-55",
                MaterialFamilyReference = "pp",
                Manufacturer = "Pilot Kalıp A.Ş.",
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBySubjectId = createdBySubjectId,
                UpdatedBySubjectId = createdBySubjectId,
                Version = 1,
                FollowUpStatus = MoldFollowUpStatuses.RequiresReview,
            },
            new MoldRegistryEntry
            {
                Id = DevelopmentPilotIds.MoldPilotInsufficientEvidenceFollowUpId,
                TenantId = tenantId,
                FacilityId = facilityId,
                Code = " ",
                CodeNormalized = " ",
                Name = "Pilot eksik kanıt kalıbı",
                Description = "Bakım durumunda ancak geçerli kalıp kodu kanıtı eksik pilot kayıt; veri inceleme görev taslağı adayı.",
                LifecycleState = MoldLifecycleState.Maintenance,
                LocationType = MoldLocationType.MaintenanceArea,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBySubjectId = createdBySubjectId,
                UpdatedBySubjectId = createdBySubjectId,
                Version = 1,
                FollowUpStatus = MoldFollowUpStatuses.RequiresReview,
            });

        dbContext.MoldLifecycleEvents.Add(new MoldLifecycleEvent
        {
            Id = DevelopmentPilotIds.MoldPilotMaintenanceLifecycleEventId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MoldId = DevelopmentPilotIds.MoldPilotMaintenanceFollowUpId,
            PreviousState = MoldLifecycleState.Active,
            NewState = MoldLifecycleState.Maintenance,
            Reason = "Planlı bakım ve teknik inceleme",
            ActorSubjectId = createdBySubjectId,
            OccurredAtUtc = seedTimestampUtc,
            IdempotencyKey = "pilot-mold-maintenance-transition",
            SourceReference = "pilot-seed",
            Version = 1,
        });

        dbContext.MoldCounterSnapshots.Add(new MoldCounterSnapshot
        {
            Id = DevelopmentPilotIds.MoldPilotMaintenanceCounterSnapshotId,
            TenantId = tenantId,
            FacilityId = facilityId,
            MoldId = DevelopmentPilotIds.MoldPilotMaintenanceFollowUpId,
            ShotCount = 420_000,
            RecordedAtUtc = seedTimestampUtc,
            SourceType = MoldCounterSourceType.Manual,
            SourceReference = "pilot-seed",
            RecordedBySubjectId = createdBySubjectId,
            IdempotencyKey = "pilot-mold-counter-snapshot",
            Version = 1,
        });
    }
}
