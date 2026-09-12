using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Quality;

public interface IQualityTraceabilityRepository
{
    Task<ProductionOrderTraceabilityReadModel?> GetProductionOrderTraceabilityAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);

    Task<ProductQualityHistoryReadModel?> GetProductHistoryAsync(
        QualityOperationContext context,
        string productReference,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default);

    Task<QualityInspectionControlSummaryReadModel?> GetInspectionSummaryAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default);
}

public interface IQualityReleaseIntelligenceReader
{
    Task<QualityReleaseIntelligenceReadModel> GetForInspectionAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default);

    Task<QualityExportReadinessProjectionReadModel> GetExportReadinessAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);
}

public sealed class QualityTraceabilityRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IQualityReportingRepository reportingRepository) : IQualityTraceabilityRepository
{
    public Task<ProductionOrderTraceabilityReadModel?> GetProductionOrderTraceabilityAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var order = await dbContext.ProductionOrders.AsNoTracking()
                .SingleOrDefaultAsync(
                    o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.Id == productionOrderId,
                    ct);

            if (order is null)
            {
                return null;
            }

            var inspections = await dbContext.QualityInspections.AsNoTracking()
                .Include(i => i.Results)
                .Where(i => i.TenantId == context.TenantId
                    && i.FacilityId == context.FacilityId
                    && i.ProductionOrderId == productionOrderId)
                .OrderByDescending(i => i.CreatedAtUtc)
                .ToListAsync(ct);

            var ncrs = await dbContext.QualityNonconformances.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId
                    && n.FacilityId == context.FacilityId
                    && n.ProductionOrderId == productionOrderId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .ToListAsync(ct);

            var ncrIds = ncrs.Select(n => n.Id).ToList();
            var holds = await dbContext.QualityHolds.AsNoTracking()
                .Where(h => h.TenantId == context.TenantId
                    && h.FacilityId == context.FacilityId
                    && (h.ProductionOrderId == productionOrderId || ncrIds.Contains(h.NcrId)))
                .OrderByDescending(h => h.PlacedAtUtc)
                .ToListAsync(ct);

            var actions = await dbContext.QualityCorrectiveActions.AsNoTracking()
                .Where(a => a.TenantId == context.TenantId
                    && a.FacilityId == context.FacilityId
                    && ncrIds.Contains(a.NcrId))
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync(ct);

            return new ProductionOrderTraceabilityReadModel(
                productionOrderId,
                order.ReferenceCode,
                order.ProductReference,
                inspections.Select(MapInspection).ToList(),
                ncrs.Select(MapNcr).ToList(),
                holds.Select(MapHold).ToList(),
                actions.Select(MapAction).ToList());
        }, cancellationToken);

    public async Task<ProductQualityHistoryReadModel?> GetProductHistoryAsync(
        QualityOperationContext context,
        string productReference,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default)
    {
        var orderIds = await tenantDatabaseSession.ExecuteReadAsync(async ct =>
            await dbContext.ProductionOrders.AsNoTracking()
                .Where(o => o.TenantId == context.TenantId
                    && o.FacilityId == context.FacilityId
                    && o.ProductReference == productReference)
                .Select(o => o.Id)
                .ToListAsync(ct),
            cancellationToken);

        var hasProductNcr = await tenantDatabaseSession.ExecuteReadAsync(async ct =>
            await dbContext.QualityNonconformances.AsNoTracking()
                .AnyAsync(
                    n => n.TenantId == context.TenantId
                        && n.FacilityId == context.FacilityId
                        && n.ProductReference == productReference,
                    ct),
            cancellationToken);

        if (orderIds.Count == 0 && !hasProductNcr)
        {
            return null;
        }

        var productFilters = filters with { ProductReference = productReference, Limit = 20 };
        var inspections = await reportingRepository.GetInspectionReportsAsync(
            context,
            productFilters,
            cancellationToken);

        return await tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var ncrs = await dbContext.QualityNonconformances.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId
                    && n.FacilityId == context.FacilityId
                    && (n.ProductReference == productReference
                        || (n.ProductionOrderId.HasValue && orderIds.Contains(n.ProductionOrderId.Value))))
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(50)
                .ToListAsync(ct);

            var holds = await dbContext.QualityHolds.AsNoTracking()
                .Where(h => h.TenantId == context.TenantId
                    && h.FacilityId == context.FacilityId
                    && h.ProductionOrderId.HasValue
                    && orderIds.Contains(h.ProductionOrderId.Value))
                .OrderByDescending(h => h.PlacedAtUtc)
                .Take(50)
                .ToListAsync(ct);

            return new ProductQualityHistoryReadModel(
                productReference,
                inspections,
                ncrs.Select(MapNcr).ToList(),
                holds.Select(MapHold).ToList());
        }, cancellationToken);
    }

    public Task<QualityInspectionControlSummaryReadModel?> GetInspectionSummaryAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default) =>
        reportingRepository.GetInspectionSummaryAsync(context, inspectionId, cancellationToken);

    private static QualityInspectionTraceabilityItemReadModel MapInspection(QualityInspection inspection) =>
        new(
            inspection.Id,
            inspection.InspectionNumber,
            QualityValueConversions.ToStorage(inspection.Status),
            QualityValueConversions.ToStorage(inspection.OverallResult),
            QualityValueConversions.ToStorage(inspection.ReleaseStatus),
            inspection.CompletedAtUtc,
            inspection.InspectorSubjectId,
            inspection.Results.Select(r => new QualityResultTraceabilityItemReadModel(
                r.Id,
                r.DefinitionCodeSnapshot ?? r.InspectionDefinitionId.ToString(),
                r.DefinitionNameSnapshot ?? "Measurement",
                QualityValueConversions.ToStorage(r.Result),
                r.RecordedAtUtc,
                r.RecordedBySubjectId)).ToList());

    private static QualityNcrTraceabilityItemReadModel MapNcr(QualityNonconformance ncr) =>
        new(
            ncr.Id,
            ncr.NcrNumber,
            QualityValueConversions.ToStorage(ncr.Severity),
            QualityValueConversions.ToStorage(ncr.Status),
            ncr.DispositionType.HasValue ? QualityValueConversions.ToStorage(ncr.DispositionType.Value) : null,
            QualityValueConversions.ToStorage(ncr.HoldStatus),
            ncr.DetectedAtUtc,
            ncr.OwnerSubjectId);

    private static QualityHoldTraceabilityItemReadModel MapHold(QualityHold hold) =>
        new(
            hold.Id,
            hold.NcrId,
            hold.Scope,
            QualityValueConversions.ToStorage(hold.Status),
            hold.Reason,
            hold.PlacedAtUtc,
            hold.PlacedBySubjectId,
            hold.ReleasedAtUtc,
            hold.ReleasedBySubjectId);

    private static QualityCorrectiveActionTraceabilityItemReadModel MapAction(QualityCorrectiveAction action) =>
        new(
            action.Id,
            action.NcrId,
            action.Title,
            QualityValueConversions.ToStorage(action.Status),
            action.DueAtUtc,
            action.CompletedAtUtc,
            action.OwnerSubjectId);
}

public sealed class QualityReleaseIntelligenceReader(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IQualityReleaseIntelligenceReader
{
    public Task<QualityReleaseIntelligenceReadModel> GetForInspectionAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var inspection = await dbContext.QualityInspections.AsNoTracking()
                .SingleOrDefaultAsync(
                    i => i.TenantId == context.TenantId
                        && i.FacilityId == context.FacilityId
                        && i.Id == inspectionId,
                    ct);

            if (inspection is null)
            {
                return new QualityReleaseIntelligenceReadModel(
                    false, null, null, null, true, 0, 0, 0, 0, false,
                    ["inspection-not-found"], "Inspection not found.");
            }

            return await BuildIntelligenceAsync(context, inspection, ct);
        }, cancellationToken);

    public Task<QualityExportReadinessProjectionReadModel> GetExportReadinessAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var latest = await dbContext.QualityInspections.AsNoTracking()
                .Where(i => i.TenantId == context.TenantId
                    && i.FacilityId == context.FacilityId
                    && i.ProductionOrderId == productionOrderId)
                .OrderByDescending(i => i.UpdatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (latest is null)
            {
                return new QualityExportReadinessProjectionReadModel(null, false, false, "No quality inspection.");
            }

            var intel = await BuildIntelligenceAsync(context, latest, ct);
            return new QualityExportReadinessProjectionReadModel(
                intel.ReleaseStatus,
                intel.Blocking,
                intel.ReadyForRelease,
                intel.ReasonSummary);
        }, cancellationToken);

    private async Task<QualityReleaseIntelligenceReadModel> BuildIntelligenceAsync(
        QualityOperationContext context,
        QualityInspection inspection,
        CancellationToken ct)
    {
        var activeHolds = await dbContext.QualityHolds.AsNoTracking()
            .CountAsync(
                h => h.TenantId == context.TenantId
                    && h.FacilityId == context.FacilityId
                    && h.ProductionOrderId == inspection.ProductionOrderId
                    && h.Status == QualityHoldRecordStatus.Active,
                ct);

        var openNcrs = await dbContext.QualityNonconformances.AsNoTracking()
            .Where(n => n.TenantId == context.TenantId
                && n.FacilityId == context.FacilityId
                && n.ProductionOrderId == inspection.ProductionOrderId
                && n.Status != QualityNcrStatus.Closed
                && n.Status != QualityNcrStatus.Cancelled)
            .Select(n => new { n.Severity, n.Id })
            .ToListAsync(ct);

        var ncrIds = openNcrs.Select(n => n.Id).ToList();
        var incompleteActions = await dbContext.QualityCorrectiveActions.AsNoTracking()
            .CountAsync(
                a => a.TenantId == context.TenantId
                    && a.FacilityId == context.FacilityId
                    && ncrIds.Contains(a.NcrId)
                    && (a.Status == QualityCorrectiveActionStatus.Open
                        || a.Status == QualityCorrectiveActionStatus.InProgress),
                ct);

        var criticalCount = openNcrs.Count(n => n.Severity == QualityNcrSeverity.Critical);
        var reasons = new List<string>();
        if (inspection.Status != QualityInspectionStatus.Completed)
        {
            reasons.Add("inspection-not-completed");
        }

        if (inspection.OverallResult == QualityInspectionOverallResult.Fail)
        {
            reasons.Add("inspection-failed");
        }

        if (activeHolds > 0)
        {
            reasons.Add("active-quality-hold");
        }

        if (criticalCount > 0)
        {
            reasons.Add("critical-ncr-open");
        }
        else if (openNcrs.Count > 0)
        {
            reasons.Add("open-ncr");
        }

        if (incompleteActions > 0)
        {
            reasons.Add("incomplete-corrective-action");
        }

        var blocking = reasons.Count > 0
            && !reasons.All(r => r is "inspection-not-completed");
        var ready = inspection.Status == QualityInspectionStatus.Completed
            && inspection.OverallResult != QualityInspectionOverallResult.Fail
            && activeHolds == 0
            && criticalCount == 0
            && openNcrs.Count == 0
            && incompleteActions == 0
            && inspection.ReleaseStatus != QualityInspectionReleaseStatus.Released;

        var summary = ready
            ? "Ready for human quality release review."
            : reasons.Count > 0
                ? string.Join("; ", reasons)
                : null;

        return new QualityReleaseIntelligenceReadModel(
            true,
            QualityValueConversions.ToStorage(inspection.Status),
            QualityValueConversions.ToStorage(inspection.OverallResult),
            QualityValueConversions.ToStorage(inspection.ReleaseStatus),
            blocking || activeHolds > 0 || criticalCount > 0,
            activeHolds,
            openNcrs.Count,
            criticalCount,
            incompleteActions,
            ready,
            reasons,
            summary);
    }
}

public sealed class UnavailableQualityTraceabilityRepository : IQualityTraceabilityRepository
{
    public Task<ProductionOrderTraceabilityReadModel?> GetProductionOrderTraceabilityAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProductionOrderTraceabilityReadModel?>(null);

    public Task<ProductQualityHistoryReadModel?> GetProductHistoryAsync(
        QualityOperationContext context,
        string productReference,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProductQualityHistoryReadModel?>(null);

    public Task<QualityInspectionControlSummaryReadModel?> GetInspectionSummaryAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<QualityInspectionControlSummaryReadModel?>(null);
}

public sealed class UnavailableQualityReleaseIntelligenceReader : IQualityReleaseIntelligenceReader
{
    public Task<QualityReleaseIntelligenceReadModel> GetForInspectionAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityReleaseIntelligenceReadModel(
            false, null, null, null, false, 0, 0, 0, 0, false, [], "Unavailable."));

    public Task<QualityExportReadinessProjectionReadModel> GetExportReadinessAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityExportReadinessProjectionReadModel(null, false, false, "Unavailable."));
}
