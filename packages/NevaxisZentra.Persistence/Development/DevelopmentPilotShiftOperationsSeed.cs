using NevaxisZentra.Persistence.ShiftOperations;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotShiftOperationsSeed
{
    public static async Task EnsureShiftOperationsPilotDataAsync(
        NevaxisZentraDbContext dbContext,
        TenantContextAccessor tenantContextAccessor,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        await dbContext.SetTenantSessionContextAsync(tenantId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        if (!await dbContext.OperationalShiftInstances.AnyAsync(
                s => s.Id == DevelopmentPilotShiftOperationsIds.ActiveShiftInstanceId, cancellationToken))
        {
            dbContext.OperationalShiftInstances.Add(new OperationalShiftInstance
            {
                Id = DevelopmentPilotShiftOperationsIds.ActiveShiftInstanceId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ShiftCode = "A",
                Name = "Sabah Vardiyası",
                ShiftDate = today,
                PlannedStartUtc = now.AddHours(-4),
                PlannedEndUtc = now.AddHours(4),
                ActualStartUtc = now.AddHours(-4),
                Status = ShiftInstanceStatuses.Active,
                HandoverState = "none",
                SupervisorLabel = "Pilot Vardiya Amiri",
                TeamLabel = "Üretim A",
                ProductionContextJson = """{"lines":["Hat-1","Hat-2"],"plannedOrders":3}""",
                HandoverReadinessScore = 0,
                CreatedAtUtc = now.AddHours(-5),
                UpdatedAtUtc = now,
                ConcurrencyToken = 1,
            });
        }

        if (!await dbContext.OperationalShiftInstances.AnyAsync(
                s => s.Id == DevelopmentPilotShiftOperationsIds.NextShiftInstanceId, cancellationToken))
        {
            dbContext.OperationalShiftInstances.Add(new OperationalShiftInstance
            {
                Id = DevelopmentPilotShiftOperationsIds.NextShiftInstanceId,
                TenantId = tenantId,
                FacilityId = facilityId,
                ShiftCode = "B",
                Name = "Akşam Vardiyası",
                ShiftDate = today,
                PlannedStartUtc = now.AddHours(4),
                PlannedEndUtc = now.AddHours(12),
                Status = ShiftInstanceStatuses.Planned,
                HandoverState = "none",
                TeamLabel = "Üretim B",
                ProductionContextJson = """{"lines":["Hat-1","Hat-3"]}""",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ConcurrencyToken = 1,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
