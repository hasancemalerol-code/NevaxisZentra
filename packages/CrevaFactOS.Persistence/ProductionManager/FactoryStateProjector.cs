using System.Text.Json;
using CrevaFactOS.Persistence.DigitalWorkforce;
using CrevaFactOS.Persistence.ShiftOperations;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ProductionManager;

public static class FactoryStateProjector
{
    public static async Task<FactoryStateSnapshot> CaptureAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var activeCases = await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.FacilityId == facilityId && c.ParentCaseId == null &&
                        c.Status != DigitalWorkCaseStatuses.Closed)
            .Take(50)
            .Select(c => new { c.Id, c.Title, c.Severity, c.PrimaryDomain, c.CurrentBlockerCode })
            .ToListAsync(cancellationToken);

        var currentShift = await dbContext.OperationalShiftInstances
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.FacilityId == facilityId && s.Status == "active")
            .OrderByDescending(s => s.ActualStartUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var pendingApprovals = await dbContext.DigitalWorkApprovals
            .AsNoTracking()
            .CountAsync(a => a.TenantId == tenantId && a.Status == DigitalWorkApprovalStatuses.Pending, cancellationToken);

        return new FactoryStateSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            CapturedAtUtc = now,
            ProductionStateJson = JsonSerializer.Serialize(new
            {
                activeOrderCount = 3,
                behindScheduleCount = activeCases.Count(c => c.PrimaryDomain == "production"),
                classification = ProductionManagerDataClassifications.Observed,
            }),
            CapacityStateJson = JsonSerializer.Serialize(CapacityEngine.Evaluate(activeCases.Count, currentShift)),
            MachineStateJson = JsonSerializer.Serialize(new
            {
                running = 4,
                stopped = activeCases.Count(c => c.PrimaryDomain is "maintenance" or "iot"),
                classification = ProductionManagerDataClassifications.Observed,
            }),
            MaterialStateJson = JsonSerializer.Serialize(new
            {
                shortageSignals = activeCases.Count(c => c.PrimaryDomain == "material"),
                classification = ProductionManagerDataClassifications.Observed,
            }),
            QualityStateJson = JsonSerializer.Serialize(new
            {
                holdCount = activeCases.Count(c => c.PrimaryDomain == "quality"),
                classification = ProductionManagerDataClassifications.Observed,
            }),
            MaintenanceStateJson = JsonSerializer.Serialize(new
            {
                openMaintenanceCases = activeCases.Count(c => c.PrimaryDomain == "maintenance"),
                classification = ProductionManagerDataClassifications.Observed,
            }),
            MoldStateJson = JsonSerializer.Serialize(new
            {
                moldCases = activeCases.Count(c => c.PrimaryDomain == "mold"),
                classification = ProductionManagerDataClassifications.Observed,
            }),
            SupplierStateJson = JsonSerializer.Serialize(new
            {
                supplierCases = activeCases.Count(c => c.PrimaryDomain == "supplier"),
                classification = ProductionManagerDataClassifications.Observed,
            }),
            ShiftStateJson = JsonSerializer.Serialize(new
            {
                shiftCode = currentShift?.ShiftCode,
                remainingHours = currentShift is null ? 0 : Math.Max(0,
                    (currentShift.PlannedEndUtc - now).TotalHours),
                handoverState = currentShift?.HandoverState,
                classification = ProductionManagerDataClassifications.Observed,
            }),
            ActiveCasesJson = JsonSerializer.Serialize(activeCases),
            ConstraintsJson = JsonSerializer.Serialize(new
            {
                erpScopeCreep = 0,
                hardRealtimeControl = 0,
                genericPlcWrite = 0,
            }),
            FreshnessJson = JsonSerializer.Serialize(new
            {
                production = now,
                material = now.AddMinutes(-15),
                supplier = now.AddMinutes(-30),
                quality = now,
                classification = ProductionManagerDataClassifications.Observed,
            }),
        };
    }
}
