using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Quality;

public interface IQualityReleaseGuard
{
    Task<QualityBlockingCheckResult> CheckInspectionReleaseAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);
}

public sealed record QualityBlockingCheckResult(bool IsBlocked, string? Reason);

public sealed class QualityReleaseGuard(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IQualityReleaseGuard
{
    public Task<QualityBlockingCheckResult> CheckInspectionReleaseAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var activeHold = await dbContext.QualityHolds.AsNoTracking()
                .AnyAsync(
                    h => h.TenantId == context.TenantId
                        && h.FacilityId == context.FacilityId
                        && h.ProductionOrderId == productionOrderId
                        && h.Status == QualityHoldRecordStatus.Active,
                    ct);

            if (activeHold)
            {
                return new QualityBlockingCheckResult(true, "Active quality hold blocks release.");
            }

            var blockingOpen = await dbContext.QualityNonconformances.AsNoTracking()
                .AnyAsync(
                    n => n.TenantId == context.TenantId
                        && n.FacilityId == context.FacilityId
                        && n.ProductionOrderId == productionOrderId
                        && n.HoldStatus == QualityNcrHoldStatus.Active
                        && n.Status != QualityNcrStatus.Closed
                        && n.Status != QualityNcrStatus.Cancelled,
                    ct);

            if (blockingOpen)
            {
                return new QualityBlockingCheckResult(true, "Open nonconformance with active hold blocks release.");
            }

            return new QualityBlockingCheckResult(false, null);
        }, cancellationToken);
}

public sealed class UnavailableQualityReleaseGuard : IQualityReleaseGuard
{
    public Task<QualityBlockingCheckResult> CheckInspectionReleaseAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityBlockingCheckResult(false, null));
}

public interface IProductionQualitySignalReader
{
    Task<ProductionQualitySignalReadModel> ReadForProductionOrderAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);
}

public sealed class ProductionQualitySignalReader(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IProductionQualitySignalReader
{
    public Task<ProductionQualitySignalReadModel> ReadForProductionOrderAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var orderExists = await dbContext.ProductionOrders.AsNoTracking()
                .AnyAsync(
                    o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.Id == productionOrderId,
                    ct);

            if (!orderExists)
            {
                return new ProductionQualitySignalReadModel(
                    Available: false,
                    QualityHoldActive: false,
                    Blocking: false,
                    OpenNcrCount: 0,
                    CriticalNcrCount: 0,
                    ReleaseStatus: null,
                    Reason: "Production order not found.");
            }

            var openNcrs = await dbContext.QualityNonconformances.AsNoTracking()
                .Where(
                    n => n.TenantId == context.TenantId
                        && n.FacilityId == context.FacilityId
                        && n.ProductionOrderId == productionOrderId
                        && n.Status != QualityNcrStatus.Closed
                        && n.Status != QualityNcrStatus.Cancelled)
                .Select(n => new { n.Severity, n.HoldStatus })
                .ToListAsync(ct);

            var activeHold = await dbContext.QualityHolds.AsNoTracking()
                .AnyAsync(
                    h => h.TenantId == context.TenantId
                        && h.FacilityId == context.FacilityId
                        && h.ProductionOrderId == productionOrderId
                        && h.Status == QualityHoldRecordStatus.Active,
                    ct);

            var holdActive = activeHold || openNcrs.Any(n => n.HoldStatus == QualityNcrHoldStatus.Active);
            var criticalCount = openNcrs.Count(n => n.Severity == QualityNcrSeverity.Critical);
            var blocking = holdActive || criticalCount > 0;

            var latestInspection = await dbContext.QualityInspections.AsNoTracking()
                .Where(
                    i => i.TenantId == context.TenantId
                        && i.FacilityId == context.FacilityId
                        && i.ProductionOrderId == productionOrderId)
                .OrderByDescending(i => i.UpdatedAtUtc)
                .Select(i => QualityValueConversions.ToStorage(i.ReleaseStatus))
                .FirstOrDefaultAsync(ct);

            var reason = holdActive
                ? "Active quality hold."
                : criticalCount > 0
                    ? "Open critical nonconformance."
                    : null;

            return new ProductionQualitySignalReadModel(
                Available: true,
                QualityHoldActive: holdActive,
                Blocking: blocking,
                OpenNcrCount: openNcrs.Count,
                CriticalNcrCount: criticalCount,
                ReleaseStatus: latestInspection,
                Reason: reason);
        }, cancellationToken);
}

public sealed class UnavailableProductionQualitySignalReader : IProductionQualitySignalReader
{
    public Task<ProductionQualitySignalReadModel> ReadForProductionOrderAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProductionQualitySignalReadModel(
            Available: false,
            QualityHoldActive: false,
            Blocking: false,
            OpenNcrCount: 0,
            CriticalNcrCount: 0,
            ReleaseStatus: null,
            Reason: "Quality signal unavailable."));
}
