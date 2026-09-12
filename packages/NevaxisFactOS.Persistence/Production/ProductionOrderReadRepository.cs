using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Production;

public sealed class ProductionOrderReadRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionOrderReadRepository
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public async Task<ProductionOrderListResult> ListAsync(
        ProductionOrderOperationContext context,
        string? statusFilter,
        string? searchFilter,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOrderListResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var effectiveLimit = limit.HasValue
                        ? Math.Clamp(limit.Value, 1, MaxLimit)
                        : DefaultLimit;

                    var query = dbContext.ProductionOrders
                        .AsNoTracking()
                        .Where(o =>
                            o.TenantId == context.TenantId
                            && o.FacilityId == context.FacilityId);

                    if (!string.IsNullOrWhiteSpace(statusFilter))
                    {
                        var normalizedStatus = statusFilter.Trim().ToLowerInvariant();
                        if (ProductionOrderValueConversions.TryFromStorage(normalizedStatus, out var parsedStatus))
                        {
                            query = query.Where(o => o.Status == parsedStatus);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(searchFilter))
                    {
                        var pattern = $"%{searchFilter.Trim()}%";
                        query = query.Where(o =>
                            EF.Functions.ILike(o.ReferenceCode, pattern)
                            || EF.Functions.ILike(o.Title, pattern));
                    }

                    var orders = await query
                        .OrderBy(o =>
                            o.Status == ProductionOrderStatus.InProgress ? 0 :
                            o.Status == ProductionOrderStatus.Paused ? 1 :
                            o.Status == ProductionOrderStatus.Ready ? 2 :
                            3)
                        .ThenBy(o => o.PlannedStartAtUtc)
                        .ThenBy(o => o.ReferenceCode)
                        .Take(effectiveLimit)
                        .Select(o => new ProductionOrderListReadModel(
                            o.Id,
                            o.FacilityId,
                            o.SourceProductionPlanId,
                            o.ReferenceCode,
                            o.Title,
                            ProductionOrderValueConversions.ToStorage(o.Status),
                            o.PlannedStartAtUtc,
                            o.PlannedEndAtUtc,
                            o.ActualStartAtUtc,
                            o.MachineReference,
                            o.MoldReference,
                            o.ShiftName,
                            o.PlannedQuantity,
                            o.GoodQuantity,
                            o.ScrapQuantity,
                            o.Version,
                            o.UpdatedAtUtc,
                            o.TotalDowntimeSeconds,
                            o.ManualCycleCount,
                            o.CurrentShiftCode,
                            o.PrimaryOperatorSubjectId,
                            o.ActiveOperatorCount))
                        .ToListAsync(ct);

                    return ProductionOrderListResult.Success(orders);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOrderListResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production order list is unavailable.");
        }
    }

    public async Task<ProductionOrderDetailResult> FindByIdAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOrderDetailResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var order = await dbContext.ProductionOrders
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            o =>
                                o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == productionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionOrderDetailResult.Missing();
                    }

                    return ProductionOrderDetailResult.Success(new ProductionOrderReadModel(
                        order.Id,
                        order.FacilityId,
                        order.ProductionPlanHandoffId,
                        order.SourceProductionPlanId,
                        order.SourceProductionPlanVersion,
                        order.ReferenceCode,
                        order.Title,
                        ProductionOrderValueConversions.ToStorage(order.Status),
                        order.PlannedStartAtUtc,
                        order.PlannedEndAtUtc,
                        order.ActualStartAtUtc,
                        order.ActualEndAtUtc,
                        order.ShiftName,
                        order.MachineReference,
                        order.MoldReference,
                        order.ProductReference,
                        order.PlannedQuantity,
                        order.PlannedQuantityUnit,
                        order.GoodQuantity,
                        order.ScrapQuantity,
                        order.LastProductionEntryAtUtc,
                        order.CreatedBySubjectId,
                        order.LastModifiedBySubjectId,
                        order.Version,
                        order.CreatedAtUtc,
                        order.UpdatedAtUtc,
                        order.HistoryJson,
                        order.TotalDowntimeSeconds,
                        order.TotalSetupSeconds,
                        order.ManualCycleCount,
                        order.TotalManualCycleDurationMilliseconds,
                        order.LastCycleAtUtc,
                        order.CurrentShiftId,
                        order.CurrentShiftCode,
                        order.CurrentShiftStartAtUtc,
                        order.CurrentShiftEndAtUtc,
                        order.PrimaryOperatorSubjectId,
                        order.ActiveOperatorCount,
                        order.LastAssignmentAtUtc));
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOrderDetailResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production order detail is unavailable.");
        }
    }
}

public sealed class UnavailableProductionOrderReadRepository : IProductionOrderReadRepository
{
    private static readonly ProductionOrderListResult UnavailableList =
        ProductionOrderListResult.Fail(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production order service is unavailable because persistence is disabled.");

    private static readonly ProductionOrderDetailResult UnavailableDetail =
        ProductionOrderDetailResult.Fail(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production order service is unavailable because persistence is disabled.");

    public Task<ProductionOrderListResult> ListAsync(
        ProductionOrderOperationContext context,
        string? statusFilter,
        string? searchFilter,
        int? limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableList);

    public Task<ProductionOrderDetailResult> FindByIdAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableDetail);
}
