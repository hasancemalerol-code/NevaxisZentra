using System.Globalization;
using Microsoft.EntityFrameworkCore;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Quality;

public interface IQualityReportingRepository
{
    Task<QualityReportingOverviewReadModel> GetOverviewAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityInspectionControlSummaryReadModel>> GetInspectionReportsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default);

    Task<QualityNcrAnalyticsReadModel> GetNcrAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default);

    Task<QualityHoldAnalyticsReadModel> GetHoldAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default);

    Task<QualityCorrectiveActionAnalyticsReadModel> GetCorrectiveActionAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityTrendPeriodReadModel>> GetTrendsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        string period,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityFailureBreakdownItemReadModel>> GetFailureBreakdownAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityDefinitionHistoryReadModel>> GetDefinitionHistoryAsync(
        QualityOperationContext context,
        Guid definitionId,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default);

    Task<QualityInspectionControlSummaryReadModel?> GetInspectionSummaryAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default);
}

public sealed class QualityReportingRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IQualityReportingRepository
{
    public Task<QualityReportingOverviewReadModel> GetOverviewAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var inspections = await FilterInspections(context, filters)
                .Select(i => new
                {
                    i.Status,
                    i.OverallResult,
                    i.ReleaseStatus,
                    i.ProductionOrderId,
                    i.CompletedAtUtc,
                    i.CreatedAtUtc,
                })
                .ToListAsync(ct);

            var ncrs = await FilterNcrs(context, filters)
                .Select(n => new { n.Status, n.Severity, n.HoldStatus, n.ProductionOrderId })
                .ToListAsync(ct);

            var holds = await FilterHolds(context, filters)
                .Select(h => new { h.Status, h.ProductionOrderId, h.PlacedAtUtc, h.ReleasedAtUtc })
                .ToListAsync(ct);

            var actions = await FilterCorrectiveActions(context, filters)
                .Select(a => new { a.Status, a.DueAtUtc })
                .ToListAsync(ct);

            var now = DateTimeOffset.UtcNow;
            var completed = inspections.Where(i => i.Status == QualityInspectionStatus.Completed).ToList();
            var passed = inspections.Count(i => i.OverallResult == QualityInspectionOverallResult.Pass);
            var failed = inspections.Count(i => i.OverallResult == QualityInspectionOverallResult.Fail);
            var released = inspections.Count(i => i.ReleaseStatus == QualityInspectionReleaseStatus.Released);
            var rejected = inspections.Count(i => i.ReleaseStatus == QualityInspectionReleaseStatus.Rejected);

            var openNcr = ncrs.Count(n =>
                n.Status != QualityNcrStatus.Closed && n.Status != QualityNcrStatus.Cancelled);
            var criticalNcr = ncrs.Count(n =>
                n.Severity == QualityNcrSeverity.Critical
                && n.Status != QualityNcrStatus.Closed
                && n.Status != QualityNcrStatus.Cancelled);
            var activeHolds = holds.Count(h => h.Status == QualityHoldRecordStatus.Active);
            var blockingOrders = holds
                .Where(h => h.Status == QualityHoldRecordStatus.Active && h.ProductionOrderId.HasValue)
                .Select(h => h.ProductionOrderId!.Value)
                .Distinct()
                .Count();

            var openActions = actions.Count(a =>
                a.Status is QualityCorrectiveActionStatus.Open or QualityCorrectiveActionStatus.InProgress);
            var overdueActions = actions.Count(a =>
                (a.Status is QualityCorrectiveActionStatus.Open or QualityCorrectiveActionStatus.InProgress)
                && a.DueAtUtc.HasValue
                && a.DueAtUtc.Value < now);

            var fpy = await ComputeFirstPassYieldAsync(context, filters, ct);
            var passRate = completed.Count > 0
                ? Math.Round((decimal)completed.Count(i => i.OverallResult == QualityInspectionOverallResult.Pass) / completed.Count * 100, 2)
                : (decimal?)null;
            var releaseRate = completed.Count > 0
                ? Math.Round((decimal)released / completed.Count * 100, 2)
                : (decimal?)null;

            return new QualityReportingOverviewReadModel(
                inspections.Count,
                inspections.Count(i => i.Status is QualityInspectionStatus.Draft or QualityInspectionStatus.InProgress),
                completed.Count,
                passed,
                failed,
                released,
                rejected,
                openNcr,
                criticalNcr,
                activeHolds,
                blockingOrders,
                openActions,
                overdueActions,
                fpy,
                passRate,
                releaseRate);
        }, cancellationToken);

    public Task<IReadOnlyList<QualityInspectionControlSummaryReadModel>> GetInspectionReportsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var rows = await FilterInspections(context, filters)
                .OrderByDescending(i => i.UpdatedAtUtc)
                .Skip(filters.Offset)
                .Take(Math.Clamp(filters.Limit, 1, 500))
                .ToListAsync(ct);

            var summaries = new List<QualityInspectionControlSummaryReadModel>();
            foreach (var inspection in rows)
            {
                summaries.Add(await BuildInspectionSummaryAsync(context, inspection, ct));
            }

            return (IReadOnlyList<QualityInspectionControlSummaryReadModel>)summaries;
        }, cancellationToken);

    public Task<QualityNcrAnalyticsReadModel> GetNcrAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var ncrs = await FilterNcrs(context, filters)
                .Select(n => new
                {
                    n.Status,
                    n.Severity,
                    n.CreatedAtUtc,
                    n.ClosedAtUtc,
                    n.UpdatedAtUtc,
                })
                .ToListAsync(ct);

            var now = DateTimeOffset.UtcNow;
            var resolved = ncrs.Where(n => n.Status == QualityNcrStatus.Resolved).ToList();
            var closed = ncrs.Where(n => n.Status == QualityNcrStatus.Closed).ToList();
            var open = ncrs.Where(n =>
                n.Status != QualityNcrStatus.Closed && n.Status != QualityNcrStatus.Cancelled).ToList();

            double? avgAge = open.Count > 0
                ? open.Average(n => (now - n.CreatedAtUtc).TotalHours)
                : null;

            var finished = ncrs.Where(n =>
                n.Status is QualityNcrStatus.Resolved or QualityNcrStatus.Closed && n.ClosedAtUtc.HasValue).ToList();
            double? avgResolution = finished.Count > 0
                ? finished.Average(n => (n.ClosedAtUtc!.Value - n.CreatedAtUtc).TotalHours)
                : null;

            var overdueActions = await FilterCorrectiveActions(context, filters)
                .CountAsync(
                    a => (a.Status == QualityCorrectiveActionStatus.Open
                            || a.Status == QualityCorrectiveActionStatus.InProgress)
                        && a.DueAtUtc.HasValue
                        && a.DueAtUtc.Value < now,
                    ct);

            return new QualityNcrAnalyticsReadModel(
                open.Count,
                resolved.Count,
                closed.Count,
                open.Count(n => n.Severity == QualityNcrSeverity.Critical),
                open.Count(n => n.Severity == QualityNcrSeverity.Major),
                open.Count(n => n.Severity == QualityNcrSeverity.Minor),
                avgAge,
                avgResolution,
                overdueActions);
        }, cancellationToken);

    public Task<QualityHoldAnalyticsReadModel> GetHoldAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var holds = await FilterHolds(context, filters)
                .Select(h => new { h.Status, h.ProductionOrderId, h.PlacedAtUtc, h.ReleasedAtUtc })
                .ToListAsync(ct);

            var active = holds.Where(h => h.Status == QualityHoldRecordStatus.Active).ToList();
            var released = holds.Where(h => h.Status == QualityHoldRecordStatus.Released).ToList();
            var affectedOrders = holds
                .Where(h => h.ProductionOrderId.HasValue)
                .Select(h => h.ProductionOrderId!.Value)
                .Distinct()
                .Count();

            double? avgDuration = released
                .Where(h => h.ReleasedAtUtc.HasValue)
                .Select(h => (h.ReleasedAtUtc!.Value - h.PlacedAtUtc).TotalHours)
                .DefaultIfEmpty()
                .Average();
            if (released.Count == 0)
            {
                avgDuration = null;
            }

            return new QualityHoldAnalyticsReadModel(
                active.Count,
                released.Count,
                affectedOrders,
                avgDuration);
        }, cancellationToken);

    public Task<QualityCorrectiveActionAnalyticsReadModel> GetCorrectiveActionAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var actions = await FilterCorrectiveActions(context, filters)
                .Select(a => new { a.Status, a.DueAtUtc, a.CreatedAtUtc, a.CompletedAtUtc })
                .ToListAsync(ct);

            var now = DateTimeOffset.UtcNow;
            var completed = actions.Where(a => a.Status == QualityCorrectiveActionStatus.Completed && a.CompletedAtUtc.HasValue).ToList();
            double? leadTime = completed.Count > 0
                ? completed.Average(a => (a.CompletedAtUtc!.Value - a.CreatedAtUtc).TotalHours)
                : null;

            return new QualityCorrectiveActionAnalyticsReadModel(
                actions.Count(a => a.Status == QualityCorrectiveActionStatus.Open),
                actions.Count(a => a.Status == QualityCorrectiveActionStatus.InProgress),
                completed.Count,
                actions.Count(a =>
                    (a.Status == QualityCorrectiveActionStatus.Open || a.Status == QualityCorrectiveActionStatus.InProgress)
                    && a.DueAtUtc.HasValue
                    && a.DueAtUtc.Value < now),
                leadTime);
        }, cancellationToken);

    public Task<IReadOnlyList<QualityTrendPeriodReadModel>> GetTrendsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        string period,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var normalized = period.ToLowerInvariant() switch
            {
                "week" => "week",
                "month" => "month",
                _ => "day",
            };

            var inspections = await FilterInspections(context, filters)
                .Select(i => new { i.CreatedAtUtc, i.OverallResult })
                .ToListAsync(ct);
            var ncrs = await FilterNcrs(context, filters)
                .Select(n => new { n.CreatedAtUtc, n.Severity })
                .ToListAsync(ct);
            var holds = await FilterHolds(context, filters)
                .Select(h => new { h.PlacedAtUtc })
                .ToListAsync(ct);

            var keys = inspections.Select(i => PeriodKey(i.CreatedAtUtc, normalized))
                .Concat(ncrs.Select(n => PeriodKey(n.CreatedAtUtc, normalized)))
                .Concat(holds.Select(h => PeriodKey(h.PlacedAtUtc, normalized)))
                .Distinct()
                .OrderBy(k => k.start)
                .Take(90)
                .ToList();

            return (IReadOnlyList<QualityTrendPeriodReadModel>)keys.Select(k =>
            {
                var insp = inspections.Where(i => PeriodKey(i.CreatedAtUtc, normalized) == k).ToList();
                var ncr = ncrs.Where(n => PeriodKey(n.CreatedAtUtc, normalized) == k).ToList();
                var hold = holds.Where(h => PeriodKey(h.PlacedAtUtc, normalized) == k).ToList();
                return new QualityTrendPeriodReadModel(
                    k.key,
                    k.start,
                    insp.Count,
                    insp.Count(i => i.OverallResult == QualityInspectionOverallResult.Pass),
                    insp.Count(i => i.OverallResult == QualityInspectionOverallResult.Fail),
                    ncr.Count,
                    ncr.Count(n => n.Severity == QualityNcrSeverity.Critical),
                    hold.Count);
            }).ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<QualityFailureBreakdownItemReadModel>> GetFailureBreakdownAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var failedInspections = await FilterInspections(context, filters)
                .Where(i => i.OverallResult == QualityInspectionOverallResult.Fail)
                .Select(i => new { i.QualityControlPlanId, i.ProductionOrderId })
                .ToListAsync(ct);

            var planIds = failedInspections.Select(i => i.QualityControlPlanId).Distinct().ToList();
            var plans = await dbContext.QualityControlPlans.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId && planIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Code, p.ProductReference, p.OperationReference })
                .ToListAsync(ct);

            var failedResults = await dbContext.QualityInspectionResults.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.Result == QualityInspectionItemResult.Fail)
                .Join(
                    FilterInspections(context, filters),
                    r => r.QualityInspectionId,
                    i => i.Id,
                    (r, _) => r)
                .Select(r => new { r.InspectionDefinitionId, r.DefinitionCodeSnapshot, r.DefinitionNameSnapshot })
                .ToListAsync(ct);

            var ncrs = await FilterNcrs(context, filters)
                .Where(n => n.Status != QualityNcrStatus.Cancelled)
                .Select(n => new { n.Category, n.Severity })
                .ToListAsync(ct);

            var items = new List<QualityFailureBreakdownItemReadModel>();

            foreach (var group in failedResults.GroupBy(r => r.DefinitionCodeSnapshot ?? r.InspectionDefinitionId.ToString()))
            {
                var label = group.First().DefinitionNameSnapshot ?? group.Key;
                items.Add(new QualityFailureBreakdownItemReadModel("inspection-definition", group.Key, label, group.Count()));
            }

            foreach (var group in plans.GroupBy(p => p.Code))
            {
                var count = failedInspections.Count(i => plans.Any(p => p.Id == i.QualityControlPlanId && p.Code == group.Key));
                if (count > 0)
                {
                    items.Add(new QualityFailureBreakdownItemReadModel("control-plan", group.Key, group.Key, count));
                }
            }

            foreach (var group in ncrs.Where(n => n.Category != null).GroupBy(n => n.Category!))
            {
                items.Add(new QualityFailureBreakdownItemReadModel("ncr-category", group.Key, group.Key, group.Count()));
            }

            foreach (var group in ncrs.GroupBy(n => QualityValueConversions.ToStorage(n.Severity)))
            {
                items.Add(new QualityFailureBreakdownItemReadModel("ncr-severity", group.Key, group.Key, group.Count()));
            }

            return (IReadOnlyList<QualityFailureBreakdownItemReadModel>)items.OrderByDescending(i => i.Count).Take(50).ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<QualityDefinitionHistoryReadModel>> GetDefinitionHistoryAsync(
        QualityOperationContext context,
        Guid definitionId,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var results = await dbContext.QualityInspectionResults.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.InspectionDefinitionId == definitionId
                    && r.MeasuredNumericValue.HasValue)
                .Join(
                    FilterInspections(context, filters),
                    r => r.QualityInspectionId,
                    i => i.Id,
                    (r, _) => r)
                .Select(r => new
                {
                    r.MeasuredNumericValue,
                    r.Result,
                    r.DefinitionCodeSnapshot,
                    r.DefinitionNameSnapshot,
                })
                .ToListAsync(ct);

            if (results.Count == 0)
            {
                return Array.Empty<QualityDefinitionHistoryReadModel>();
            }

            var numeric = results.Where(r => r.MeasuredNumericValue.HasValue).Select(r => r.MeasuredNumericValue!.Value).ToList();
            return (IReadOnlyList<QualityDefinitionHistoryReadModel>)
            [
                new QualityDefinitionHistoryReadModel(
                    definitionId,
                    results[0].DefinitionCodeSnapshot ?? definitionId.ToString(),
                    results[0].DefinitionNameSnapshot ?? "Definition",
                    results.Count,
                    results.Count(r => r.Result == QualityInspectionItemResult.Fail),
                    numeric.Min(),
                    numeric.Max(),
                    Math.Round(numeric.Average(), 4)),
            ];
        }, cancellationToken);

    public Task<QualityInspectionControlSummaryReadModel?> GetInspectionSummaryAsync(
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

            return inspection is null
                ? null
                : await BuildInspectionSummaryAsync(context, inspection, ct);
        }, cancellationToken);

    private async Task<decimal?> ComputeFirstPassYieldAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken ct)
    {
        var completed = await FilterInspections(context, filters)
            .Where(i => i.Status == QualityInspectionStatus.Completed)
            .Select(i => new { i.ProductionOrderId, i.CompletedAtUtc, i.OverallResult })
            .ToListAsync(ct);

        var firstByOrder = completed
            .GroupBy(i => i.ProductionOrderId)
            .Select(g => g.OrderBy(i => i.CompletedAtUtc).First())
            .ToList();

        if (firstByOrder.Count == 0)
        {
            return null;
        }

        var passed = firstByOrder.Count(i => i.OverallResult == QualityInspectionOverallResult.Pass);
        return Math.Round((decimal)passed / firstByOrder.Count * 100, 2);
    }

    private async Task<QualityInspectionControlSummaryReadModel> BuildInspectionSummaryAsync(
        QualityOperationContext context,
        QualityInspection inspection,
        CancellationToken ct)
    {
        var plan = await dbContext.QualityControlPlans.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == inspection.QualityControlPlanId, ct);

        var order = await dbContext.ProductionOrders.AsNoTracking()
            .SingleOrDefaultAsync(
                o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId && o.Id == inspection.ProductionOrderId,
                ct);

        var results = await dbContext.QualityInspectionResults.AsNoTracking()
            .Where(r => r.QualityInspectionId == inspection.Id)
            .ToListAsync(ct);

        var openNcrs = await dbContext.QualityNonconformances.AsNoTracking()
            .CountAsync(
                n => n.TenantId == context.TenantId
                    && n.FacilityId == context.FacilityId
                    && n.QualityInspectionId == inspection.Id
                    && n.Status != QualityNcrStatus.Closed
                    && n.Status != QualityNcrStatus.Cancelled,
                ct);

        var hasHold = await dbContext.QualityHolds.AsNoTracking()
            .AnyAsync(
                h => h.TenantId == context.TenantId
                    && h.FacilityId == context.FacilityId
                    && h.ProductionOrderId == inspection.ProductionOrderId
                    && h.Status == QualityHoldRecordStatus.Active,
                ct);

        var measurements = results.Select(r => new QualityInspectionControlSummaryMeasurementReadModel(
            r.DefinitionCodeSnapshot ?? r.InspectionDefinitionId.ToString(),
            r.DefinitionNameSnapshot ?? "Measurement",
            r.UnitSnapshot,
            r.NominalValueSnapshot,
            r.LowerLimitSnapshot,
            r.UpperLimitSnapshot,
            r.RequiredSnapshot ?? true,
            r.CriticalSnapshot ?? false,
            r.MeasuredNumericValue,
            r.MeasuredTextValue,
            r.BooleanResult,
            QualityValueConversions.ToStorage(r.Result))).ToList();

        return new QualityInspectionControlSummaryReadModel(
            inspection.Id,
            inspection.InspectionNumber,
            context.FacilityId.ToString(),
            inspection.ProductionOrderId,
            order?.ReferenceCode,
            order?.ProductReference,
            inspection.QualityControlPlanId,
            inspection.PlanCodeSnapshot ?? plan?.Code ?? "unknown",
            inspection.PlanNameSnapshot ?? plan?.Name ?? "unknown",
            inspection.PlanRevisionSnapshot ?? plan?.Revision ?? "unknown",
            inspection.CompletedAtUtc ?? inspection.StartedAtUtc ?? inspection.CreatedAtUtc,
            inspection.InspectorSubjectId,
            QualityValueConversions.ToStorage(inspection.OverallResult),
            QualityValueConversions.ToStorage(inspection.ReleaseStatus),
            measurements,
            hasHold,
            openNcrs,
            hasHold || openNcrs > 0 ? "Active quality blockers present." : null);
    }

    private IQueryable<QualityInspection> FilterInspections(
        QualityOperationContext context,
        QualityReportingFilters filters)
    {
        var query = dbContext.QualityInspections.AsNoTracking()
            .Where(i => i.TenantId == context.TenantId && i.FacilityId == context.FacilityId);

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(i => i.CreatedAtUtc >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(i => i.CreatedAtUtc <= filters.DateTo.Value);
        }

        if (filters.ProductionOrderId.HasValue)
        {
            query = query.Where(i => i.ProductionOrderId == filters.ProductionOrderId.Value);
        }

        if (filters.ControlPlanId.HasValue)
        {
            query = query.Where(i => i.QualityControlPlanId == filters.ControlPlanId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.OverallResult))
        {
            var parsed = QualityValueConversions.OverallResultFromStorage(filters.OverallResult);
            query = query.Where(i => i.OverallResult == parsed);
        }

        if (!string.IsNullOrWhiteSpace(filters.ReleaseStatus))
        {
            var parsed = QualityValueConversions.ReleaseStatusFromStorage(filters.ReleaseStatus);
            query = query.Where(i => i.ReleaseStatus == parsed);
        }

        return query;
    }

    private IQueryable<QualityNonconformance> FilterNcrs(
        QualityOperationContext context,
        QualityReportingFilters filters)
    {
        var query = dbContext.QualityNonconformances.AsNoTracking()
            .Where(n => n.TenantId == context.TenantId && n.FacilityId == context.FacilityId);

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(n => n.CreatedAtUtc >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(n => n.CreatedAtUtc <= filters.DateTo.Value);
        }

        if (filters.ProductionOrderId.HasValue)
        {
            query = query.Where(n => n.ProductionOrderId == filters.ProductionOrderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.NcrSeverity))
        {
            var parsed = QualityValueConversions.NcrSeverityFromStorage(filters.NcrSeverity);
            query = query.Where(n => n.Severity == parsed);
        }

        if (!string.IsNullOrWhiteSpace(filters.NcrStatus))
        {
            var parsed = QualityValueConversions.NcrStatusFromStorage(filters.NcrStatus);
            query = query.Where(n => n.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(filters.ProductReference))
        {
            query = query.Where(n => n.ProductReference == filters.ProductReference);
        }

        return query;
    }

    private IQueryable<QualityHold> FilterHolds(
        QualityOperationContext context,
        QualityReportingFilters filters)
    {
        var query = dbContext.QualityHolds.AsNoTracking()
            .Where(h => h.TenantId == context.TenantId && h.FacilityId == context.FacilityId);

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(h => h.PlacedAtUtc >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(h => h.PlacedAtUtc <= filters.DateTo.Value);
        }

        if (filters.ProductionOrderId.HasValue)
        {
            query = query.Where(h => h.ProductionOrderId == filters.ProductionOrderId.Value);
        }

        return query;
    }

    private IQueryable<QualityCorrectiveAction> FilterCorrectiveActions(
        QualityOperationContext context,
        QualityReportingFilters filters)
    {
        var query = dbContext.QualityCorrectiveActions.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc <= filters.DateTo.Value);
        }

        if (filters.ProductionOrderId.HasValue)
        {
            query = query.Where(a => dbContext.QualityNonconformances
                .Any(n => n.Id == a.NcrId && n.ProductionOrderId == filters.ProductionOrderId.Value));
        }

        return query;
    }

    private static (string key, DateTimeOffset start) PeriodKey(DateTimeOffset at, string period) =>
        period switch
        {
            "week" => ($"{at.Year}-W{ISOWeek.GetWeekOfYear(at.UtcDateTime):D2}", StartOfWeek(at)),
            "month" => ($"{at.Year}-{at.Month:D2}", new DateTimeOffset(at.Year, at.Month, 1, 0, 0, 0, TimeSpan.Zero)),
            _ => ($"{at:yyyy-MM-dd}", new DateTimeOffset(at.Date, TimeSpan.Zero)),
        };

    private static DateTimeOffset StartOfWeek(DateTimeOffset value)
    {
        var diff = (7 + (value.DayOfWeek - DayOfWeek.Monday)) % 7;
        return new DateTimeOffset(value.Date.AddDays(-diff), TimeSpan.Zero);
    }
}

public sealed class UnavailableQualityReportingRepository : IQualityReportingRepository
{
    private static QualityReportingOverviewReadModel EmptyOverview() =>
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null, null);

    public Task<QualityReportingOverviewReadModel> GetOverviewAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EmptyOverview());

    public Task<IReadOnlyList<QualityInspectionControlSummaryReadModel>> GetInspectionReportsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityInspectionControlSummaryReadModel>>([]);

    public Task<QualityNcrAnalyticsReadModel> GetNcrAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityNcrAnalyticsReadModel(0, 0, 0, 0, 0, 0, null, null, 0));

    public Task<QualityHoldAnalyticsReadModel> GetHoldAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityHoldAnalyticsReadModel(0, 0, 0, null));

    public Task<QualityCorrectiveActionAnalyticsReadModel> GetCorrectiveActionAnalyticsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityCorrectiveActionAnalyticsReadModel(0, 0, 0, 0, null));

    public Task<IReadOnlyList<QualityTrendPeriodReadModel>> GetTrendsAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        string period,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityTrendPeriodReadModel>>([]);

    public Task<IReadOnlyList<QualityFailureBreakdownItemReadModel>> GetFailureBreakdownAsync(
        QualityOperationContext context,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityFailureBreakdownItemReadModel>>([]);

    public Task<IReadOnlyList<QualityDefinitionHistoryReadModel>> GetDefinitionHistoryAsync(
        QualityOperationContext context,
        Guid definitionId,
        QualityReportingFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityDefinitionHistoryReadModel>>([]);

    public Task<QualityInspectionControlSummaryReadModel?> GetInspectionSummaryAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<QualityInspectionControlSummaryReadModel?>(null);
}
