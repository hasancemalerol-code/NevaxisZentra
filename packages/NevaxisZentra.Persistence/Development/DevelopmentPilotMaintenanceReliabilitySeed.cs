using NevaxisZentra.Persistence.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotMaintenanceReliabilitySeed
{
    public static async Task EnsureMaintenanceReliabilityPilotDataAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string createdBySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var primaryRequest = await dbContext.MaintenanceRequests
            .SingleOrDefaultAsync(r => r.Id == DevelopmentPilotIds.MaintenancePilotRequestId, cancellationToken);

        if (primaryRequest is not null && primaryRequest.FailureCategory == MaintenanceFailureCategory.Other)
        {
            primaryRequest.FailureCategory = MaintenanceFailureCategory.Hydraulic;
            primaryRequest.UpdatedAtUtc = seedTimestampUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var markerExists = await dbContext.MaintenanceWorkOrders
            .AsNoTracking()
            .AnyAsync(w => w.Id == DevelopmentPilotIds.MaintenancePilotCorrectiveWoAId, cancellationToken);

        var repeatRequestExists = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .AnyAsync(r => r.Id == DevelopmentPilotIds.MaintenancePilotReliabilityRequest2Id, cancellationToken);

        if (markerExists || repeatRequestExists)
        {
            return;
        }

        primaryRequest = await dbContext.MaintenanceRequests
            .SingleOrDefaultAsync(r => r.Id == DevelopmentPilotIds.MaintenancePilotRequestId, cancellationToken);

        if (primaryRequest is not null)
        {
            primaryRequest.FailureCategory = MaintenanceFailureCategory.Hydraulic;
            primaryRequest.UpdatedAtUtc = seedTimestampUtc;
        }

        var repeatRequest = new MaintenanceRequest
        {
            Id = DevelopmentPilotIds.MaintenancePilotReliabilityRequest2Id,
            TenantId = tenantId,
            FacilityId = facilityId,
            RequestNumber = "MR-2026-RL3L-002",
            AssetType = "machine",
            AssetId = "machine-im-120",
            AssetReference = "IM-120",
            AssetName = "Injection Machine IM-120",
            Type = MaintenanceRequestType.Fault,
            Priority = MaintenancePriority.High,
            Status = MaintenanceRequestStatus.Open,
            Title = "Hidrolik sızıntı tekrarı",
            Description = "Pilot tekrarlayan hidrolik arıza — MAINTENANCE-3L güvenilirlik kaydı.",
            FailureCategory = MaintenanceFailureCategory.Hydraulic,
            ReportedAtUtc = seedTimestampUtc.AddDays(-10),
            ReportedBySubjectId = createdBySubjectId,
            Source = "manual",
            CreatedAtUtc = seedTimestampUtc.AddDays(-10),
            UpdatedAtUtc = seedTimestampUtc.AddDays(-10),
            Version = 1,
        };

        var repairStartedA = seedTimestampUtc.AddDays(-5);
        var repairCompletedA = repairStartedA.AddMinutes(120);
        var correctiveWoA = new MaintenanceWorkOrder
        {
            Id = DevelopmentPilotIds.MaintenancePilotCorrectiveWoAId,
            TenantId = tenantId,
            FacilityId = facilityId,
            WorkOrderNumber = "MWO-2026-RL3L-002",
            AssetType = "machine",
            AssetId = "machine-im-120",
            AssetReference = "IM-120",
            AssetName = "Injection Machine IM-120",
            Type = MaintenanceWorkOrderType.Corrective,
            Priority = MaintenancePriority.High,
            Status = MaintenanceWorkOrderStatus.Completed,
            Title = "Hidrolik hat onarımı A",
            Description = "Pilot MTTR kaydı — 120 dakika.",
            StartedAtUtc = repairStartedA,
            CompletedAtUtc = repairCompletedA,
            ResolutionCode = MaintenanceResolutionCode.Repaired,
            CompletionNote = "Pilot MTTR repair A",
            ExternalServiceCost = 1500m,
            CreatedAtUtc = repairStartedA,
            UpdatedAtUtc = repairCompletedA,
            CreatedBySubjectId = createdBySubjectId,
            Version = 1,
        };

        var repairStartedB = seedTimestampUtc.AddDays(-3);
        var repairCompletedB = repairStartedB.AddMinutes(60);
        var correctiveWoB = new MaintenanceWorkOrder
        {
            Id = DevelopmentPilotIds.MaintenancePilotCorrectiveWoBId,
            TenantId = tenantId,
            FacilityId = facilityId,
            WorkOrderNumber = "MWO-2026-RL3L-003",
            AssetType = "machine",
            AssetId = "machine-im-120",
            AssetReference = "IM-120",
            AssetName = "Injection Machine IM-120",
            Type = MaintenanceWorkOrderType.Corrective,
            Priority = MaintenancePriority.Normal,
            Status = MaintenanceWorkOrderStatus.Completed,
            Title = "Hidrolik hat onarımı B",
            Description = "Pilot MTTR kaydı — 60 dakika.",
            StartedAtUtc = repairStartedB,
            CompletedAtUtc = repairCompletedB,
            ResolutionCode = MaintenanceResolutionCode.Repaired,
            CompletionNote = "Pilot MTTR repair B",
            OtherCost = 250m,
            CreatedAtUtc = repairStartedB,
            UpdatedAtUtc = repairCompletedB,
            CreatedBySubjectId = createdBySubjectId,
            Version = 1,
        };

        var overdueDue = new PreventiveMaintenanceDue
        {
            Id = DevelopmentPilotIds.MaintenancePilotOverdueDueId,
            TenantId = tenantId,
            FacilityId = facilityId,
            PreventivePlanId = DevelopmentPilotIds.MaintenancePilotPreventivePlanId,
            AssetType = "machine",
            AssetReference = "IM-120",
            DueAtUtc = seedTimestampUtc.AddDays(-14),
            Status = PreventiveMaintenanceDueStatus.Due,
            GeneratedAtUtc = seedTimestampUtc.AddDays(-30),
            Version = 1,
        };

        var onTimeDue = new PreventiveMaintenanceDue
        {
            Id = DevelopmentPilotIds.MaintenancePilotCompletedDueOnTimeId,
            TenantId = tenantId,
            FacilityId = facilityId,
            PreventivePlanId = DevelopmentPilotIds.MaintenancePilotPreventivePlanId,
            AssetType = "machine",
            AssetReference = "IM-120",
            DueAtUtc = seedTimestampUtc.AddDays(-20),
            Status = PreventiveMaintenanceDueStatus.Completed,
            GeneratedAtUtc = seedTimestampUtc.AddDays(-40),
            CompletedAtUtc = seedTimestampUtc.AddDays(-21),
            Version = 1,
        };

        var lateDue = new PreventiveMaintenanceDue
        {
            Id = DevelopmentPilotIds.MaintenancePilotCompletedDueLateId,
            TenantId = tenantId,
            FacilityId = facilityId,
            PreventivePlanId = DevelopmentPilotIds.MaintenancePilotPreventivePlanId,
            AssetType = "machine",
            AssetReference = "IM-120",
            DueAtUtc = seedTimestampUtc.AddDays(-15),
            Status = PreventiveMaintenanceDueStatus.Completed,
            GeneratedAtUtc = seedTimestampUtc.AddDays(-35),
            CompletedAtUtc = seedTimestampUtc.AddDays(-10),
            Version = 1,
        };

        dbContext.MaintenanceRequests.Add(repeatRequest);
        dbContext.MaintenanceWorkOrders.AddRange(correctiveWoA, correctiveWoB);
        dbContext.PreventiveMaintenanceDues.AddRange(overdueDue, onTimeDue, lateDue);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
