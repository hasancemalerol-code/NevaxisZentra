using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.ProductionPlanning;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Export;

public static class ExportCrossModuleIntelligence
{
    public static async Task<ExportProductionIntelligenceRef> LoadProductionAsync(
        CrevaFactOSDbContext dbContext,
        ExportOperationContext context,
        Guid sourceOrderId,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.ProductionOrders
            .AsNoTracking()
            .Where(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId)
            .OrderByDescending(o => o.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return UnavailableProduction("No linked production order found for this facility.");
        }

        var hasDowntime = await dbContext.ProductionDowntimes
            .AsNoTracking()
            .AnyAsync(
                d => d.TenantId == context.TenantId && d.FacilityId == context.FacilityId
                    && d.ProductionOrderId == order.Id && d.EndedAtUtc == null,
                cancellationToken);

        var hasException = await dbContext.ProductionExecutionExceptions
            .AsNoTracking()
            .AnyAsync(
                e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId
                    && e.ProductionOrderId == order.Id && e.ResolvedAtUtc == null,
                cancellationToken);

        return new ExportProductionIntelligenceRef(
            true,
            order.Id,
            order.ReferenceCode,
            ProductionOrderValueConversions.ToStorage(order.Status),
            order.GoodQuantity,
            order.PlannedQuantity,
            order.PlannedEndAtUtc,
            hasDowntime,
            hasException,
            null);
    }

    public static async Task<ExportPlanningIntelligenceRef> LoadPlanningAsync(
        CrevaFactOSDbContext dbContext,
        ExportOperationContext context,
        Guid sourceOrderId,
        IReadOnlyList<ExportShipment> shipments,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.ProductionPlans
            .AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return UnavailablePlanning("No linked production plan found for this facility.");
        }

        DateTimeOffset? earliestShipmentDeadline = shipments
            .Select(s => s.PlannedPickupAtUtc ?? s.PlannedDepartureAtUtc)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault();

        var logistics = new ExportPlanningLogisticsConstraintRef(
            null,
            earliestShipmentDeadline,
            earliestShipmentDeadline,
            null,
            plan.PlannedEndAtUtc,
            false,
            earliestShipmentDeadline.HasValue
                && plan.PlannedEndAtUtc > earliestShipmentDeadline.Value);

        return new ExportPlanningIntelligenceRef(
            true,
            plan.Id,
            plan.ReferenceCode,
            plan.Status.ToString().ToLowerInvariant(),
            plan.PlannedEndAtUtc,
            logistics.ScheduleRisk,
            logistics,
            null);
    }

    public static ExportQualityIntelligenceRef LoadQuality() =>
        new(false, null, false, "Quality module context is not yet available.");

    public static ExportErpIntelligenceRef LoadErp(string? orderReference, string? customerName) =>
        string.IsNullOrWhiteSpace(orderReference)
            ? UnavailableErp("ERP read context is not connected for this export case.")
            : new ExportErpIntelligenceRef(true, customerName, orderReference, null);

    public static ExportProductionIntelligenceRef UnavailableProduction(string? reason = null) =>
        new(false, null, null, null, null, null, null, false, false, reason ?? "context-unavailable");

    public static ExportPlanningIntelligenceRef UnavailablePlanning(string? reason = null) =>
        new(false, null, null, null, null, false, null, reason ?? "context-unavailable");

    public static ExportErpIntelligenceRef UnavailableErp(string? reason = null) =>
        new(false, null, null, reason ?? "context-unavailable");

    public static IReadOnlyList<ExportCrossModuleRiskReadModel> DetectRisks(
        ExportCase exportCase,
        IReadOnlyList<ExportShipment> shipments,
        ExportAiContextComplianceRef? compliance,
        ExportProductionIntelligenceRef production,
        ExportPlanningIntelligenceRef planning,
        ExportQualityIntelligenceRef quality,
        ExportErpIntelligenceRef erp)
    {
        var risks = new List<ExportCrossModuleRiskReadModel>();
        var now = DateTimeOffset.UtcNow;

        if (planning.Available && planning.LogisticsConstraints?.ScheduleRisk == true)
        {
            risks.Add(new ExportCrossModuleRiskReadModel(
                ExportCrossModuleRiskType.PlanningDeadlineRisk,
                ExportCrossModuleRiskSeverity.Critical,
                ExportCrossModuleSourceModule.Planning,
                planning.PlanNumber ?? planning.ProductionPlanId?.ToString() ?? "plan",
                "Planned production completion is after the required shipment preparation deadline.",
                now,
                true));
        }

        if (production.Available && production.HasActiveDowntime)
        {
            risks.Add(new ExportCrossModuleRiskReadModel(
                ExportCrossModuleRiskType.ProductionDelay,
                ExportCrossModuleRiskSeverity.Warning,
                ExportCrossModuleSourceModule.Production,
                production.OrderNumber ?? production.ProductionOrderId?.ToString() ?? "production",
                "Active production downtime may delay export readiness.",
                now,
                false));
        }

        if (production.Available && production.HasExecutionException)
        {
            risks.Add(new ExportCrossModuleRiskReadModel(
                ExportCrossModuleRiskType.ProductionDelay,
                ExportCrossModuleRiskSeverity.Critical,
                ExportCrossModuleSourceModule.Production,
                production.OrderNumber ?? production.ProductionOrderId?.ToString() ?? "production",
                "Unresolved production execution exception detected.",
                now,
                true));
        }

        if (!quality.Available)
        {
            risks.Add(new ExportCrossModuleRiskReadModel(
                ExportCrossModuleRiskType.ContextUnavailable,
                ExportCrossModuleRiskSeverity.Info,
                ExportCrossModuleSourceModule.Quality,
                "quality",
                quality.UnavailableReason ?? "Quality context unavailable.",
                now,
                false));
        }

        if (!erp.Available)
        {
            risks.Add(new ExportCrossModuleRiskReadModel(
                ExportCrossModuleRiskType.ErpDataMissing,
                ExportCrossModuleRiskSeverity.Info,
                ExportCrossModuleSourceModule.Erp,
                "erp",
                erp.UnavailableReason ?? "ERP context unavailable.",
                now,
                false));
        }

        var cutoff = shipments
            .Select(s => s.PlannedPickupAtUtc ?? s.PlannedDepartureAtUtc)
            .Where(d => d.HasValue && d.Value < now.AddDays(2))
            .FirstOrDefault();

        if (cutoff.HasValue && exportCase.ReadinessStatus != ExportReadinessStatus.Ready)
        {
            risks.Add(new ExportCrossModuleRiskReadModel(
                ExportCrossModuleRiskType.ShipmentCutoffRisk,
                ExportCrossModuleRiskSeverity.Warning,
                ExportCrossModuleSourceModule.Shipment,
                "shipment",
                "Export case is not ready and shipment cutoff is approaching.",
                now,
                true));
        }

        return risks;
    }
}
