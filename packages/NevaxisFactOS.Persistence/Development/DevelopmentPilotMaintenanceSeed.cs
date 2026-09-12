using NevaxisFactOS.Persistence.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotMaintenanceSeed
{
    public static async Task EnsureMaintenancePilotDataAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string createdBySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .AnyAsync(r => r.Id == DevelopmentPilotIds.MaintenancePilotRequestId, cancellationToken);

        if (existing)
        {
            return;
        }

        var request = new MaintenanceRequest
        {
            Id = DevelopmentPilotIds.MaintenancePilotRequestId,
            TenantId = tenantId,
            FacilityId = facilityId,
            RequestNumber = "MR-2026-000001",
            AssetType = "machine",
            AssetId = "machine-im-120",
            AssetReference = "IM-120",
            AssetName = "Injection Machine IM-120",
            Type = MaintenanceRequestType.Fault,
            Priority = MaintenancePriority.High,
            Status = MaintenanceRequestStatus.Open,
            Title = "Hidrolik basınç düşüşü",
            Description = "Pilot makine hidrolik basınç arızası — MAINTENANCE-1L geliştirme kaydı.",
            FailureCategory = MaintenanceFailureCategory.Hydraulic,
            ReportedAtUtc = seedTimestampUtc,
            ReportedBySubjectId = createdBySubjectId,
            Source = "manual",
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        };

        var workOrder = new MaintenanceWorkOrder
        {
            Id = DevelopmentPilotIds.MaintenancePilotWorkOrderId,
            TenantId = tenantId,
            FacilityId = facilityId,
            WorkOrderNumber = "MWO-2026-000001",
            AssetType = "mold",
            AssetId = "mold-pp-cap-32",
            AssetReference = "MOLD-PP-CAP-32",
            AssetName = "PP Cap 32-cavity Mold",
            Type = MaintenanceWorkOrderType.Preventive,
            Priority = MaintenancePriority.Normal,
            Status = MaintenanceWorkOrderStatus.Planned,
            Title = "Kalıp soğutma kanalı kontrolü",
            Description = "Pilot periyodik kalıp bakım emri.",
            PlannedStartUtc = seedTimestampUtc.AddDays(7),
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = createdBySubjectId,
            Version = 1,
        };

        var plan = new PreventiveMaintenancePlan
        {
            Id = DevelopmentPilotIds.MaintenancePilotPreventivePlanId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = "PM-IM-120-MONTHLY",
            Name = "IM-120 aylık bakım",
            AssetType = "machine",
            AssetId = "machine-im-120",
            AssetReference = "IM-120",
            IntervalType = PreventiveIntervalType.Months,
            IntervalValue = 1,
            NextDueAtUtc = seedTimestampUtc.AddDays(14),
            Status = PreventivePlanStatus.Active,
            Description = "Pilot enjeksiyon makinesi aylık bakım planı.",
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        };

        dbContext.MaintenanceRequests.Add(request);
        dbContext.MaintenanceWorkOrders.Add(workOrder);
        dbContext.PreventiveMaintenancePlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
