using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportPackingReadRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportPackingReadRepository
{
    public async Task<ExportPackingResult> GetPackingAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var exportCase = await dbContext.ExportCases
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            c => c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == exportCaseId,
                            ct);

                    if (exportCase is null)
                    {
                        return ExportPackingResult.Missing();
                    }

                    var units = await dbContext.ExportPackingUnits
                        .AsNoTracking()
                        .Where(u => u.TenantId == context.TenantId
                            && u.FacilityId == context.FacilityId
                            && u.ExportCaseId == exportCaseId)
                        .OrderBy(u => u.CreatedAtUtc)
                        .ToListAsync(ct);

                    var allItems = await dbContext.ExportPackingItems
                        .AsNoTracking()
                        .Where(i => i.TenantId == context.TenantId
                            && i.FacilityId == context.FacilityId
                            && i.ExportCaseId == exportCaseId)
                        .OrderBy(i => i.CreatedAtUtc)
                        .ToListAsync(ct);

                    var unitModels = units.Select(u =>
                    {
                        var unitItems = allItems
                            .Where(i => i.PackingUnitId == u.Id)
                            .Select(MapItem)
                            .ToList();

                        return new ExportPackingUnitReadModel(
                            u.Id,
                            u.ExportCaseId,
                            u.UnitCode,
                            u.UnitType,
                            u.ParentPackingUnitId,
                            u.GrossWeight,
                            u.NetWeight,
                            u.WeightUnit,
                            u.Length,
                            u.Width,
                            u.Height,
                            u.DimensionUnit,
                            u.Volume,
                            u.MarksAndNumbers,
                            ExportValueConversions.ToStorage(u.Status),
                            u.Version,
                            u.CreatedAtUtc,
                            u.UpdatedAtUtc,
                            unitItems);
                    }).ToList();

                    var model = new ExportPackingReadModel(
                        exportCase.Id,
                        ExportValueConversions.ToStorage(exportCase.PackingStatus),
                        exportCase.PackingCompletedAtUtc,
                        exportCase.PackingCompletedBySubjectId,
                        exportCase.PackingVerifiedAtUtc,
                        exportCase.PackingVerifiedBySubjectId,
                        unitModels);

                    return ExportPackingResult.Success(model);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportPackingResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export packing service is unavailable.");
        }
    }

    public async Task<ExportPackingReconciliationResult> GetReconciliationAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        decimal? orderedQuantityTotal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var caseExists = await dbContext.ExportCases
                        .AsNoTracking()
                        .AnyAsync(
                            c => c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == exportCaseId,
                            ct);

                    if (!caseExists)
                    {
                        return ExportPackingReconciliationResult.Missing();
                    }

                    var items = await dbContext.ExportPackingItems
                        .AsNoTracking()
                        .Where(i => i.TenantId == context.TenantId
                            && i.FacilityId == context.FacilityId
                            && i.ExportCaseId == exportCaseId)
                        .ToListAsync(ct);

                    var result = ExportPackingReconciliationEngine.Reconcile(items, orderedQuantityTotal);

                    var model = new ExportPackingReconciliationReadModel(
                        exportCaseId,
                        result.Status switch
                        {
                            ExportPackingReconciliationStatus.NotStarted => "not-started",
                            ExportPackingReconciliationStatus.Incomplete => "incomplete",
                            ExportPackingReconciliationStatus.Matched => "matched",
                            ExportPackingReconciliationStatus.Overpacked => "overpacked",
                            ExportPackingReconciliationStatus.Mismatch => "mismatch",
                            _ => "unable-to-validate",
                        },
                        result.Explanation,
                        result.ItemCount,
                        result.TotalQuantity);

                    return ExportPackingReconciliationResult.Success(model);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportPackingReconciliationResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export packing reconciliation service is unavailable.");
        }
    }

    private static ExportPackingItemReadModel MapItem(ExportPackingItem item) =>
        new(
            item.Id,
            item.PackingUnitId,
            item.SourceOrderLineId,
            item.ProductCode,
            item.ProductName,
            item.Quantity,
            item.UnitOfMeasure,
            item.LotBatchReference,
            item.CreatedAtUtc);
}

public sealed class UnavailableExportPackingReadRepository : IExportPackingReadRepository
{
    private static readonly ExportPackingResult Unavailable =
        ExportPackingResult.Fail(ExportFailureCodes.SourceUnavailable, "Export packing persistence is disabled.");

    private static readonly ExportPackingReconciliationResult UnavailableReconciliation =
        ExportPackingReconciliationResult.Fail(ExportFailureCodes.SourceUnavailable, "Export packing persistence is disabled.");

    public Task<ExportPackingResult> GetPackingAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable);

    public Task<ExportPackingReconciliationResult> GetReconciliationAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        decimal? orderedQuantityTotal,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableReconciliation);
}
