using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Production;

public sealed class ProductionReportingRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IProductionPlanningLogisticsSignalReader logisticsSignalReader)
    : IProductionReportingRepository
{
    private const int MaxRows = 500;

    public Task<ProductionReportingReadResult<ProductionOperationalOverviewReadModel>> GetOverviewAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, filter, async (orders, ct) =>
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var orderIds = orders.Select(o => o.Id).ToList();
            var openExceptions = await LoadOpenExceptionsAsync(context, orderIds, ct);
            var activeDowntimes = await LoadActiveDowntimesAsync(context, orderIds, ct);
            var recentHandovers = await LoadRecentHandoversAsync(context, orderIds, ct);

            var planned = orders.Sum(o => o.PlannedQuantity ?? 0m);
            var good = orders.Sum(o => o.GoodQuantity);
            var scrap = orders.Sum(o => o.ScrapQuantity);
            var remaining = ProductionReportingEngine.ComputeRemainingQuantity(planned, good, scrap);

            var logistics = await logisticsSignalReader.ReadAggregateForFacilityAsync(
                context.TenantId,
                context.FacilityId,
                orders.Select(o => o.SourceProductionPlanId).Distinct().ToList(),
                ct);

            var overview = new ProductionOperationalOverviewReadModel(
                orders.Count,
                orders.Count(o => o.Status == ProductionOrderStatus.InProgress),
                orders.Count(o => o.Status == ProductionOrderStatus.Completed),
                orders.Count(o => o.Status == ProductionOrderStatus.Paused),
                orders.Count(o => o.Status == ProductionOrderStatus.Ready),
                planned,
                good,
                scrap,
                remaining,
                ProductionReportingEngine.ComputeCompletionRatePercent(planned, good),
                ProductionReportingEngine.ComputeYieldPercent(good, scrap),
                activeDowntimes.Count,
                orders.Sum(o => o.TotalDowntimeSeconds) / 60,
                orders.Sum(o => o.TotalSetupSeconds) / 60,
                orders.Sum(o => o.ManualCycleCount),
                openExceptions.Count,
                openExceptions.Count,
                orders.Count(o => o.CurrentShiftId.HasValue),
                orders.Sum(o => o.ActiveOperatorCount),
                ProductionReportingEngine.ComputeRiskSignals(
                    orders,
                    openExceptions,
                    activeDowntimes,
                    recentHandovers,
                    logistics,
                    nowUtc),
                logistics);

            return ProductionReportingResults.Success(overview);
        }, cancellationToken);

    public Task<ProductionReportingReadResult<IReadOnlyList<ProductionOrderPerformanceReadModel>>> ListOrderPerformanceAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, filter, async (orders, ct) =>
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var logisticsByPlan = await logisticsSignalReader.ReadForPlansAsync(
                context.TenantId,
                context.FacilityId,
                orders.Select(o => o.SourceProductionPlanId).Distinct().ToList(),
                ct);

            var orderIds = orders.Select(o => o.Id).ToList();
            var openByOrder = (await LoadOpenExceptionsAsync(context, orderIds, ct))
                .GroupBy(e => e.ProductionOrderId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var activeDowntimeOrders = (await LoadActiveDowntimesAsync(context, orderIds, ct))
                .Select(d => d.ProductionOrderId)
                .ToHashSet();

            var items = orders.Select(order =>
            {
                logisticsByPlan.TryGetValue(order.SourceProductionPlanId, out var logistics);
                openByOrder.TryGetValue(order.Id, out var exceptions);
                var planActual = ProductionReportingEngine.BuildPlanActual(order, nowUtc);
                var operationalRisk = planActual.ScheduleOverrun
                    ? "schedule-overrun"
                    : exceptions?.Any(e => string.Equals(e.Severity, ProductionExceptionSeverities.Critical, StringComparison.Ordinal)) == true
                        ? "critical-exception"
                        : activeDowntimeOrders.Contains(order.Id)
                            ? "active-downtime"
                            : null;

                return new ProductionOrderPerformanceReadModel(
                    order.Id,
                    order.ReferenceCode,
                    order.Title,
                    ProductionOrderValueConversions.ToStorage(order.Status),
                    order.ProductReference,
                    order.MachineReference,
                    order.SourceProductionPlanId,
                    planActual,
                    exceptions?.Count > 0,
                    activeDowntimeOrders.Contains(order.Id),
                    operationalRisk,
                    logistics?.Available == true ? logistics : null);
            }).ToList();

            return ProductionReportingResults.Success<IReadOnlyList<ProductionOrderPerformanceReadModel>>(items);
        }, cancellationToken);

    public Task<ProductionReportingReadResult<ProductionDowntimeAnalyticsReadModel>> GetDowntimeAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, filter, async (orders, ct) =>
        {
            var orderMap = orders.ToDictionary(o => o.Id);
            var orderIds = orderMap.Keys.ToList();
            if (orderIds.Count == 0)
            {
                return ProductionReportingResults.Success(EmptyDowntime());
            }

            var query = dbContext.ProductionDowntimes
                .AsNoTracking()
                .Where(d =>
                    d.TenantId == context.TenantId
                    && d.FacilityId == context.FacilityId
                    && orderIds.Contains(d.ProductionOrderId));

            query = ApplyDowntimeDateFilter(query, filter);

            var rows = await query
                .OrderByDescending(d => d.StartedAtUtc)
                .Take(MaxRows)
                .ToListAsync(ct);

            var events = rows.Select(d =>
            {
                orderMap.TryGetValue(d.ProductionOrderId, out var order);
                var minutes = d.DurationSeconds.HasValue
                    ? d.DurationSeconds.Value / 60
                    : (long?)Math.Max(0, Math.Round((DateTimeOffset.UtcNow - d.StartedAtUtc).TotalMinutes));
                return new ProductionDowntimeAnalyticsEventReadModel(
                    d.Id,
                    d.ProductionOrderId,
                    order?.ReferenceCode ?? d.ProductionOrderId.ToString(),
                    d.Category,
                    d.ReasonCode,
                    order?.MachineReference,
                    d.StartedAtUtc,
                    d.EndedAtUtc,
                    minutes,
                    d.EndedAtUtc is null);
            }).ToList();

            return ProductionReportingResults.Success(new ProductionDowntimeAnalyticsReadModel(
                events.Sum(e => e.DurationMinutes ?? 0),
                events.Count(e => e.IsActive),
                events.Count,
                events.OrderByDescending(e => e.DurationMinutes ?? 0).FirstOrDefault(),
                events.GroupBy(e => e.ReasonCode)
                    .Select(g => new ProductionDowntimeReasonSummaryReadModel(g.Key, g.Count(), g.Sum(x => x.DurationMinutes ?? 0)))
                    .OrderByDescending(x => x.TotalMinutes).Take(5).ToList(),
                events.Where(e => !string.IsNullOrWhiteSpace(e.ResourceReference))
                    .GroupBy(e => e.ResourceReference!)
                    .Select(g => new ProductionDowntimeResourceSummaryReadModel(g.Key, g.Count(), g.Sum(x => x.DurationMinutes ?? 0)))
                    .OrderByDescending(x => x.TotalMinutes).Take(5).ToList(),
                events));
        }, cancellationToken);

    public Task<ProductionReportingReadResult<ProductionScrapAnalyticsReadModel>> GetScrapAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, filter, (orders, _) =>
        {
            var good = orders.Sum(o => o.GoodQuantity);
            var scrap = orders.Sum(o => o.ScrapQuantity);
            var total = good + scrap;

            var model = new ProductionScrapAnalyticsReadModel(
                scrap,
                good,
                total,
                total > 0 ? Math.Round(scrap / total * 100m, 2, MidpointRounding.AwayFromZero) : null,
                orders.Where(o => o.ScrapQuantity > 0)
                    .Select(o => new ProductionScrapOrderSummaryReadModel(
                        o.Id,
                        o.ReferenceCode,
                        o.ScrapQuantity,
                        o.GoodQuantity,
                        ProductionReportingEngine.ComputeYieldPercent(o.GoodQuantity, o.ScrapQuantity) is decimal yield
                            ? Math.Round(100m - yield, 2, MidpointRounding.AwayFromZero)
                            : null))
                    .OrderByDescending(o => o.ScrapQuantity).Take(10).ToList(),
                orders.Where(o => !string.IsNullOrWhiteSpace(o.CurrentShiftCode))
                    .GroupBy(o => o.CurrentShiftCode!)
                    .Select(g =>
                    {
                        var gGood = g.Sum(x => x.GoodQuantity);
                        var gScrap = g.Sum(x => x.ScrapQuantity);
                        return new ProductionScrapShiftSummaryReadModel(
                            g.Key,
                            gScrap,
                            gGood,
                            ProductionReportingEngine.ComputeYieldPercent(gGood, gScrap) is decimal yield
                                ? Math.Round(100m - yield, 2, MidpointRounding.AwayFromZero)
                                : null);
                    }).OrderByDescending(x => x.ScrapQuantity).ToList());

            return Task.FromResult(ProductionReportingResults.Success(model));
        }, cancellationToken);

    public Task<ProductionReportingReadResult<ProductionSetupAnalyticsReadModel>> GetSetupAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, filter, async (orders, ct) =>
        {
            var orderMap = orders.ToDictionary(o => o.Id);
            var orderIds = orderMap.Keys.ToList();
            if (orderIds.Count == 0)
            {
                return ProductionReportingResults.Success(new ProductionSetupAnalyticsReadModel(0, 0, null, [], []));
            }

            var query = dbContext.ProductionSetupSessions.AsNoTracking()
                .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId
                    && orderIds.Contains(s.ProductionOrderId) && s.CompletedAtUtc != null);
            if (filter.FromUtc.HasValue) query = query.Where(s => s.StartedAtUtc >= filter.FromUtc.Value);
            if (filter.ToUtc.HasValue) query = query.Where(s => s.StartedAtUtc <= filter.ToUtc.Value);

            var sessions = await query.Take(MaxRows).ToListAsync(ct);
            var totalMinutes = sessions.Sum(s => (s.DurationSeconds ?? 0) / 60);

            return ProductionReportingResults.Success(new ProductionSetupAnalyticsReadModel(
                sessions.Count,
                totalMinutes,
                sessions.Count == 0 ? null : totalMinutes / sessions.Count,
                sessions.Select(s => new
                    {
                        Resource = s.TargetMachineReference
                            ?? orderMap.GetValueOrDefault(s.ProductionOrderId)?.MachineReference,
                        Minutes = (s.DurationSeconds ?? 0) / 60,
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Resource))
                    .GroupBy(x => x.Resource!)
                    .Select(g => new ProductionSetupResourceSummaryReadModel(
                        g.Key, g.Count(), g.Sum(x => x.Minutes)))
                    .OrderByDescending(x => x.TotalMinutes).Take(10).ToList(),
                sessions.GroupBy(s => s.ProductionOrderId)
                    .Select(g => new ProductionSetupOrderSummaryReadModel(
                        g.Key,
                        orderMap.GetValueOrDefault(g.Key)?.ReferenceCode ?? g.Key.ToString(),
                        g.Count(),
                        g.Sum(x => (x.DurationSeconds ?? 0) / 60)))
                    .OrderByDescending(x => x.TotalMinutes).Take(10).ToList()));
        }, cancellationToken);

    public Task<ProductionReportingReadResult<ProductionCycleAnalyticsReadModel>> GetCycleAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, filter, async (orders, ct) =>
        {
            var orderMap = orders.ToDictionary(o => o.Id);
            var orderIds = orderMap.Keys.ToList();
            if (orderIds.Count == 0)
            {
                return ProductionReportingResults.Success(new ProductionCycleAnalyticsReadModel(
                    0, null, null, ProductionReportingEngine.ExpectedCycleUnavailable, []));
            }

            var query = dbContext.ProductionCycleEntries.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId
                    && orderIds.Contains(c.ProductionOrderId));
            if (filter.FromUtc.HasValue) query = query.Where(c => c.RecordedAtUtc >= filter.FromUtc.Value);
            if (filter.ToUtc.HasValue) query = query.Where(c => c.RecordedAtUtc <= filter.ToUtc.Value);

            var cycles = await query.OrderByDescending(c => c.RecordedAtUtc).Take(MaxRows).ToListAsync(ct);

            return ProductionReportingResults.Success(new ProductionCycleAnalyticsReadModel(
                cycles.Count,
                cycles.Count == 0 ? null : (long?)cycles.Average(c => (double)c.CycleDurationMilliseconds),
                cycles.FirstOrDefault()?.CycleDurationMilliseconds,
                ProductionReportingEngine.ExpectedCycleUnavailable,
                cycles.GroupBy(c => c.ProductionOrderId)
                    .Select(g => new ProductionCycleOrderSummaryReadModel(
                        g.Key,
                        orderMap.GetValueOrDefault(g.Key)?.ReferenceCode ?? g.Key.ToString(),
                        g.Count(),
                        ProductionReportingEngine.ComputeAverageCycleDurationMilliseconds(
                            g.Count(), g.Sum(x => x.CycleDurationMilliseconds))))
                    .OrderByDescending(x => x.CycleCount).Take(10).ToList()));
        }, cancellationToken);

    public Task<ProductionReportingReadResult<IReadOnlyList<ProductionShiftReportingReadModel>>> ListShiftAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, filter, async (orders, ct) =>
        {
            var shifts = await dbContext.ProductionShifts.AsNoTracking()
                .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId && s.IsActive)
                .OrderBy(s => s.SortOrder).ToListAsync(ct);
            if (filter.ShiftId.HasValue) shifts = shifts.Where(s => s.Id == filter.ShiftId.Value).ToList();

            var orderIds = orders.Select(o => o.Id).ToHashSet();
            var openExceptions = await LoadOpenExceptionsAsync(context, orderIds.ToList(), ct);
            var exceptionsByOrder = openExceptions.GroupBy(e => e.ProductionOrderId).ToDictionary(g => g.Key, g => g.Count());

            var models = shifts.Select(shift =>
            {
                var shiftOrders = orders.Where(o => o.CurrentShiftId == shift.Id).ToList();
                return new ProductionShiftReportingReadModel(
                    shift.Id, shift.Code, shift.Name,
                    shiftOrders.FirstOrDefault()?.CurrentShiftStartAtUtc,
                    shiftOrders.FirstOrDefault()?.CurrentShiftEndAtUtc,
                    shiftOrders.Count,
                    shiftOrders.Sum(o => o.GoodQuantity),
                    shiftOrders.Sum(o => o.ScrapQuantity),
                    shiftOrders.Sum(o => o.TotalDowntimeSeconds) / 60,
                    shiftOrders.Sum(o => o.TotalSetupSeconds) / 60,
                    shiftOrders.Sum(o => exceptionsByOrder.GetValueOrDefault(o.Id)),
                    shiftOrders.Count(o => !string.IsNullOrWhiteSpace(o.MachineReference)),
                    shiftOrders.Sum(o => o.ActiveOperatorCount),
                    "none");
            }).ToList();

            return ProductionReportingResults.Success<IReadOnlyList<ProductionShiftReportingReadModel>>(models);
        }, cancellationToken);

    public Task<ProductionReportingReadResult<ProductionExceptionAnalyticsReadModel>> GetExceptionAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, filter, async (orders, ct) =>
        {
            var orderMap = orders.ToDictionary(o => o.Id);
            var orderIds = orderMap.Keys.ToList();
            if (orderIds.Count == 0)
            {
                return ProductionReportingResults.Success(new ProductionExceptionAnalyticsReadModel(0, 0, 0, []));
            }

            var query = dbContext.ProductionExecutionExceptions.AsNoTracking()
                .Where(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId
                    && orderIds.Contains(e.ProductionOrderId));
            if (filter.FromUtc.HasValue) query = query.Where(e => e.RaisedAtUtc >= filter.FromUtc.Value);
            if (filter.ToUtc.HasValue) query = query.Where(e => e.RaisedAtUtc <= filter.ToUtc.Value);

            var rows = await query.OrderByDescending(e => e.RaisedAtUtc).Take(MaxRows).ToListAsync(ct);
            var nowUtc = DateTimeOffset.UtcNow;
            var items = rows.Select(e =>
            {
                orderMap.TryGetValue(e.ProductionOrderId, out var order);
                return new ProductionExceptionAnalyticsItemReadModel(
                    e.Id, e.ProductionOrderId,
                    order?.ReferenceCode ?? e.ProductionOrderId.ToString(),
                    e.ExceptionType, e.Severity, e.Status,
                    (long)Math.Max(0, Math.Round((nowUtc - e.RaisedAtUtc).TotalMinutes)),
                    e.AssignedToSubjectId, order?.CurrentShiftCode, order?.MachineReference);
            }).ToList();

            return ProductionReportingResults.Success(new ProductionExceptionAnalyticsReadModel(
                items.Count(i => i.Status == ProductionExceptionStatuses.Open
                    || i.Status == ProductionExceptionStatuses.Acknowledged),
                items.Count(i => string.IsNullOrWhiteSpace(i.AssignedToSubjectId)
                    && (i.Status == ProductionExceptionStatuses.Open
                        || i.Status == ProductionExceptionStatuses.Acknowledged)),
                items.Count(i => i.Status == ProductionExceptionStatuses.Resolved),
                items));
        }, cancellationToken);

    private async Task<ProductionReportingReadResult<T>> ExecuteAsync<T>(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        Func<IReadOnlyList<ProductionOrder>, CancellationToken, Task<ProductionReportingReadResult<T>>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionReportingResults.Fail<T>(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var orders = await LoadFilteredOrdersAsync(context, filter, ct);
                    return await action(orders, ct);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionReportingResults.Fail<T>(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production reporting is unavailable.");
        }
    }

    private async Task<IReadOnlyList<ProductionOrder>> LoadFilteredOrdersAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ProductionOrders.AsNoTracking()
            .Where(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId);

        if (filter.ProductionOrderId.HasValue) query = query.Where(o => o.Id == filter.ProductionOrderId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)
            && ProductionOrderValueConversions.TryFromStorage(filter.Status.Trim().ToLowerInvariant(), out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }
        if (filter.ShiftId.HasValue) query = query.Where(o => o.CurrentShiftId == filter.ShiftId.Value);
        if (!string.IsNullOrWhiteSpace(filter.ResourceReference))
            query = query.Where(o => o.MachineReference == filter.ResourceReference.Trim());
        if (!string.IsNullOrWhiteSpace(filter.OperatorSubjectId))
            query = query.Where(o => o.PrimaryOperatorSubjectId == filter.OperatorSubjectId.Trim());
        if (filter.FromUtc.HasValue) query = query.Where(o => o.UpdatedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) query = query.Where(o => o.UpdatedAtUtc <= filter.ToUtc.Value);

        return await query.OrderBy(o => o.ReferenceCode).Take(MaxRows).ToListAsync(cancellationToken);
    }

    private static IQueryable<ProductionDowntime> ApplyDowntimeDateFilter(
        IQueryable<ProductionDowntime> query,
        ProductionReportingFilter filter)
    {
        if (filter.FromUtc.HasValue) query = query.Where(d => d.StartedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) query = query.Where(d => d.StartedAtUtc <= filter.ToUtc.Value);
        return query;
    }

    private async Task<List<ProductionExecutionExceptionRecord>> LoadOpenExceptionsAsync(
        ProductionOrderOperationContext context,
        List<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0) return [];
        return await dbContext.ProductionExecutionExceptions.AsNoTracking()
            .Where(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId
                && orderIds.Contains(e.ProductionOrderId)
                && (e.Status == ProductionExceptionStatuses.Open
                    || e.Status == ProductionExceptionStatuses.Acknowledged))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ProductionDowntime>> LoadActiveDowntimesAsync(
        ProductionOrderOperationContext context,
        List<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0) return [];
        return await dbContext.ProductionDowntimes.AsNoTracking()
            .Where(d => d.TenantId == context.TenantId && d.FacilityId == context.FacilityId
                && orderIds.Contains(d.ProductionOrderId) && d.EndedAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ProductionShiftHandover>> LoadRecentHandoversAsync(
        ProductionOrderOperationContext context,
        List<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0) return [];
        return await dbContext.ProductionShiftHandovers.AsNoTracking()
            .Where(h => h.TenantId == context.TenantId && h.FacilityId == context.FacilityId
                && orderIds.Contains(h.ProductionOrderId))
            .OrderByDescending(h => h.CreatedAtUtc).Take(50).ToListAsync(cancellationToken);
    }

    private static ProductionDowntimeAnalyticsReadModel EmptyDowntime() =>
        new(0, 0, 0, null, [], [], []);
}
