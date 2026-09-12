using System.Globalization;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Maintenance;

public sealed class MaintenanceReliabilityRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IMaintenanceOperatingTimeSource operatingTimeSource) : IMaintenanceReliabilityRepository
{
    private const string MttrLabel = "MTTR (eligible completed corrective maintenance)";
    private const string MtbfUnavailableReason = "trusted operating-time source unavailable";
    private const string PreventiveComplianceRule =
        "Due records with due_at_utc in period: on-time when completed_at_utc <= due_at_utc; " +
        "late when completed_at_utc > due_at_utc; overdue open when status is due/work-order-created and due_at_utc < now.";

    public Task<MaintenanceReliabilityOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
        var now = DateTimeOffset.UtcNow;
        var snapshot = await LoadSnapshotAsync(context, dateFromUtc, dateToUtc, now, ct);
        if (snapshot is null)
        {
            return null;
        }

        var mttr = CalculateMttr(snapshot.CompletedCorrectiveRepairs);
        var mtbf = await CalculateMtbfAsync(context, snapshot, dateFromUtc, dateToUtc, cancellationToken);
        var preventive = CalculatePreventiveCompliance(snapshot.Dues, dateFromUtc, dateToUtc, now);
        var repeatFailures = CalculateRepeatFailures(snapshot.FaultEvents, now);
        var cost = CalculateCostAnalytics(snapshot.CompletedWorkOrdersWithCost, snapshot.AllCompletedWorkOrderCount);

        var repeatInPeriod = repeatFailures.Count(r => r.LastOccurrenceUtc >= dateFromUtc && r.LastOccurrenceUtc <= dateToUtc && r.RepeatCount >= 1);
        var assetsAtRisk = snapshot.AssetReferences.Count(a =>
        {
            var status = EvaluateConditionStatus(snapshot, a, now);
            return status is "warning" or "critical";
        });

        return new MaintenanceReliabilityOverviewReadModel(
            TotalAssetsWithMaintenanceHistory: snapshot.AssetReferences.Count,
            OpenRequests: snapshot.OpenRequests.Count,
            OpenWorkOrders: snapshot.OpenWorkOrders.Count,
            CriticalOpenWorkOrders: snapshot.OpenWorkOrders.Count(w => w.Priority == MaintenancePriority.Critical),
            OverduePreventiveCount: snapshot.Dues.Count(d => IsOverdueOpenDue(d, now)),
            CompletedWorkOrdersPeriod: snapshot.CompletedWorkOrdersInPeriod.Count,
            FaultCountPeriod: snapshot.FaultEvents.Count(f => f.OccurredAtUtc >= dateFromUtc && f.OccurredAtUtc <= dateToUtc),
            RepeatFaultCountPeriod: repeatInPeriod,
            AverageRepairDurationMinutes: mttr.MeanRepairMinutes,
            MedianRepairDurationMinutes: mttr.MedianRepairMinutes,
            PreventiveCompletionRate: preventive.PreventiveCompletionRate,
            AssetsAtRiskCount: assetsAtRisk,
            Period: new MaintenanceReportingPeriod(dateFromUtc, dateToUtc),
            Mttr: mttr,
            Mtbf: mtbf,
            CostAnalytics: cost);
        }, cancellationToken);

    public Task<MaintenanceReliabilityReportReadModel?> GetReliabilityReportAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
        var now = DateTimeOffset.UtcNow;
        var snapshot = await LoadSnapshotAsync(context, dateFromUtc, dateToUtc, now, ct);
        if (snapshot is null)
        {
            return null;
        }

        var mttr = CalculateMttr(snapshot.CompletedCorrectiveRepairs);
        var mtbf = await CalculateMtbfAsync(context, snapshot, dateFromUtc, dateToUtc, cancellationToken);
        var preventive = CalculatePreventiveCompliance(snapshot.Dues, dateFromUtc, dateToUtc, now);
        var repeatFailures = CalculateRepeatFailures(snapshot.FaultEvents, now);
        var backlog = CalculateBacklogAging(snapshot.OpenRequests, snapshot.OpenWorkOrders, now);
        var cost = CalculateCostAnalytics(snapshot.CompletedWorkOrdersWithCost, snapshot.AllCompletedWorkOrderCount);

        return new MaintenanceReliabilityReportReadModel(
            Period: new MaintenanceReportingPeriod(dateFromUtc, dateToUtc),
            Mttr: mttr,
            Mtbf: mtbf,
            RepeatFailures: repeatFailures,
            PreventiveCompliance: preventive,
            BacklogAging: backlog,
            CostAnalytics: cost);
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenanceFailureBreakdownRowReadModel>> GetFailureBreakdownAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        string groupBy,
        string? assetType,
        string? assetReference,
        string? failureCategory,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
        var now = DateTimeOffset.UtcNow;
        var snapshot = await LoadSnapshotAsync(context, dateFromUtc, dateToUtc, now, ct);
        if (snapshot is null)
        {
            return (IReadOnlyList<MaintenanceFailureBreakdownRowReadModel>)[];
        }

        var events = snapshot.FaultEvents
            .Where(f => f.OccurredAtUtc >= dateFromUtc && f.OccurredAtUtc <= dateToUtc)
            .Where(f => assetType is null || string.Equals(f.AssetType, assetType, StringComparison.OrdinalIgnoreCase))
            .Where(f => assetReference is null || string.Equals(f.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase))
            .Where(f => failureCategory is null || string.Equals(f.FailureCategory, failureCategory, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var repeatMap = CalculateRepeatFailures(snapshot.FaultEvents, now)
            .ToDictionary(r => r.FailureSignature, r => r.RepeatCount, StringComparer.OrdinalIgnoreCase);

        var repairByAsset = snapshot.CompletedCorrectiveRepairs
            .GroupBy(r => r.AssetReference, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Average(x => x.DurationMinutes), StringComparer.OrdinalIgnoreCase);

        IEnumerable<(string Key, string Label)> groups = groupBy switch
        {
            "asset-type" => events.GroupBy(e => e.AssetType).Select(g => (g.Key, g.Key)),
            "asset-reference" => events.GroupBy(e => e.AssetReference).Select(g => (g.Key, g.Key)),
            "priority" => events.GroupBy(e => e.Priority).Select(g => (g.Key, g.Key)),
            "resolution-code" => events.Where(e => e.ResolutionCode is not null).GroupBy(e => e.ResolutionCode!).Select(g => (g.Key, g.Key)),
            _ => events.GroupBy(e => e.FailureCategory).Select(g => (g.Key, g.Key)),
        };

        var grouped = groups
            .Select(g =>
            {
                var groupEvents = events.Where(e => GetGroupKey(e, groupBy) == g.Key).ToList();
                var count = groupEvents.Count;
                var signatures = groupEvents.Select(e => BuildFailureSignature(e.AssetReference, e.FailureCategory)).Distinct(StringComparer.OrdinalIgnoreCase);
                var repeatCount = signatures.Sum(s => repeatMap.TryGetValue(s, out var rc) ? Math.Max(0, rc) : 0);
                var assetRefs = groupEvents.Select(e => e.AssetReference).Distinct(StringComparer.OrdinalIgnoreCase);
                var repairValues = assetRefs.Where(repairByAsset.ContainsKey).Select(a => repairByAsset[a]).ToList();
                double? avgRepair = repairValues.Count > 0 ? repairValues.Average() : null;
                return new MaintenanceFailureBreakdownRowReadModel(g.Key, g.Label, count, 0, repeatCount, avgRepair);
            })
            .Where(r => r.Count > 0)
            .OrderByDescending(r => r.Count)
            .ToList();

        var total = grouped.Sum(g => g.Count);
        if (total == 0)
        {
            return grouped;
        }

        return grouped
            .Select(r => r with { Percentage = Math.Round(r.Count * 100.0 / total, 2) })
            .ToList();
        }, cancellationToken);

    public Task<MaintenanceAssetReliabilityReadModel?> GetAssetReliabilityAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
        var now = DateTimeOffset.UtcNow;
        var dateFrom = now.AddYears(-1);
        var snapshot = await LoadSnapshotAsync(context, dateFrom, now, now, ct);
        if (snapshot is null)
        {
            return null;
        }

        var normalized = assetReference.Trim();
        var assetEvents = snapshot.FaultEvents
            .Where(f => string.Equals(f.AssetReference, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (assetEvents.Count == 0 &&
            !snapshot.OpenWorkOrders.Any(w => string.Equals(w.AssetReference, normalized, StringComparison.OrdinalIgnoreCase)) &&
            !snapshot.Dues.Any(d => string.Equals(d.AssetReference, normalized, StringComparison.OrdinalIgnoreCase)) &&
            !snapshot.CompletedCorrectiveRepairs.Any(r => string.Equals(r.AssetReference, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var firstEvent = assetEvents.FirstOrDefault();
        var repairs = snapshot.CompletedCorrectiveRepairs
            .Where(r => string.Equals(r.AssetReference, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var mttr = CalculateMttr(repairs);
        var repeat = CalculateRepeatFailures(snapshot.FaultEvents, now)
            .Where(r => string.Equals(r.AssetReference, normalized, StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.RepeatCount);
        var openWos = snapshot.OpenWorkOrders
            .Where(w => string.Equals(w.AssetReference, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var assetDues = snapshot.Dues
            .Where(d => string.Equals(d.AssetReference, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var preventive = CalculatePreventiveCompliance(assetDues, dateFrom, now, now);
        var (status, reasons) = EvaluateConditionStatusWithReasons(snapshot, normalized, now);
        var mtbf = await CalculateMtbfForAssetAsync(context, normalized, dateFrom, now, cancellationToken);
        var assetCosts = snapshot.CompletedWorkOrdersWithCost
            .Where(w => string.Equals(w.AssetReference, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var cost = CalculateCostAnalytics(assetCosts, assetCosts.Count);

        return new MaintenanceAssetReliabilityReadModel(
            AssetReference: normalized,
            AssetType: firstEvent?.AssetType ?? openWos.FirstOrDefault()?.AssetType ?? assetCosts.FirstOrDefault()?.AssetType ?? "unknown",
            AssetName: firstEvent?.AssetName ?? openWos.FirstOrDefault()?.AssetName,
            TotalFaults: assetEvents.Count,
            FaultsLast30Days: assetEvents.Count(f => f.OccurredAtUtc >= now.AddDays(-30)),
            FaultsLast90Days: assetEvents.Count(f => f.OccurredAtUtc >= now.AddDays(-90)),
            RepeatFaultCount: repeat,
            CompletedCorrectiveWorkOrders: repairs.Count,
            MeanRepairMinutes: mttr.MeanRepairMinutes,
            OpenWorkOrders: openWos.Count,
            CriticalOpenWorkOrders: openWos.Count(w => w.Priority == MaintenancePriority.Critical),
            OverduePreventiveCount: assetDues.Count(d => IsOverdueOpenDue(d, now)),
            PreventiveComplianceRate: preventive.PreventiveCompletionRate,
            LastFailureAtUtc: assetEvents.MaxBy(f => f.OccurredAtUtc)?.OccurredAtUtc,
            LastMaintenanceCompletedAtUtc: repairs.MaxBy(r => r.CompletedAtUtc)?.CompletedAtUtc,
            ConditionStatus: status,
            ConditionReasons: reasons,
            Mtbf: mtbf,
            CostAnalytics: cost);
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenanceConditionSignalReadModel>> ListConditionSignalsAsync(
        MaintenanceOperationContext context,
        string? assetReference,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
        var now = DateTimeOffset.UtcNow;
        var snapshot = await LoadSnapshotAsync(context, now.AddYears(-1), now, now, ct);
        if (snapshot is null)
        {
            return (IReadOnlyList<MaintenanceConditionSignalReadModel>)[];
        }

        var signals = BuildDerivedConditionSignals(context, snapshot, now);
        if (!string.IsNullOrWhiteSpace(assetReference))
        {
            signals = signals
                .Where(s => string.Equals(s.AssetReference, assetReference.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return signals;
        }, cancellationToken);

    public Task<MaintenanceAssetConditionReadModel?> GetAssetConditionAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
        var now = DateTimeOffset.UtcNow;
        var snapshot = await LoadSnapshotAsync(context, now.AddYears(-1), now, now, ct);
        if (snapshot is null)
        {
            return null;
        }

        var normalized = assetReference.Trim();
        var (status, reasons) = EvaluateConditionStatusWithReasons(snapshot, normalized, now);
        var signals = BuildDerivedConditionSignals(context, snapshot, now)
            .Where(s => string.Equals(s.AssetReference, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (status == "unknown" && signals.Count == 0)
        {
            return null;
        }

        return new MaintenanceAssetConditionReadModel(
            normalized,
            status,
            reasons,
            signals,
            now);
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenanceAssetRiskProjectionReadModel>> ListRiskProjectionsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
        var now = DateTimeOffset.UtcNow;
        var snapshot = await LoadSnapshotAsync(context, now.AddYears(-1), now, now, ct);
        if (snapshot is null)
        {
            return (IReadOnlyList<MaintenanceAssetRiskProjectionReadModel>)[];
        }

        return snapshot.AssetReferences
            .Select(assetReference =>
            {
                var (status, reasons) = EvaluateConditionStatusWithReasons(snapshot, assetReference, now);
                var repeat = CalculateRepeatFailures(snapshot.FaultEvents, now)
                    .Where(r => string.Equals(r.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase))
                    .Sum(r => r.RepeatCount);
                var overdue = snapshot.Dues.Count(d =>
                    string.Equals(d.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase) &&
                    IsOverdueOpenDue(d, now));
                var criticalOpen = snapshot.OpenWorkOrders.Count(w =>
                    string.Equals(w.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase) &&
                    w.Priority == MaintenancePriority.Critical);
                var blocking = criticalOpen > 0 || snapshot.OpenRequests.Any(r =>
                    string.Equals(r.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase) &&
                    r.Priority == MaintenancePriority.Critical);

                return new MaintenanceAssetRiskProjectionReadModel(
                    assetReference,
                    status,
                    blocking,
                    reasons.Count > 0 ? reasons[0] : "No active maintenance risk",
                    repeat,
                    overdue,
                    criticalOpen,
                    now);
            })
            .OrderByDescending(p => p.ConditionStatus switch
            {
                "critical" => 4,
                "warning" => 3,
                "attention" => 2,
                "normal" => 1,
                _ => 0,
            })
            .ThenByDescending(p => p.RepeatFailureCount)
            .ToList();
        }, cancellationToken);

    private async Task<FacilitySnapshot?> LoadSnapshotAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var facilityId = context.FacilityId;
        var tenantId = context.TenantId;

        var requests = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.FacilityId == facilityId)
            .ToListAsync(cancellationToken);

        var workOrders = await dbContext.MaintenanceWorkOrders
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.FacilityId == facilityId)
            .ToListAsync(cancellationToken);

        var dues = await dbContext.PreventiveMaintenanceDues
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.FacilityId == facilityId)
            .ToListAsync(cancellationToken);

        var partCosts = await dbContext.MaintenancePartUsages
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.FacilityId == facilityId)
            .ToListAsync(cancellationToken);

        var partCostMap = partCosts
            .GroupBy(p => p.WorkOrderId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var actual = g.Where(u => u.ActualUnitCost.HasValue)
                        .Sum(u => u.ActualUnitCost!.Value * u.Quantity);
                    if (actual > 0m)
                    {
                        return actual;
                    }

                    return g.Where(u => u.EstimatedUnitCost.HasValue)
                        .Sum(u => u.EstimatedUnitCost!.Value * u.Quantity);
                });

        var assetReferences = requests.Select(r => r.AssetReference)
            .Concat(workOrders.Select(w => w.AssetReference))
            .Concat(dues.Select(d => d.AssetReference))
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var faultEvents = BuildFaultEvents(requests, workOrders);
        var openRequests = requests.Where(r =>
            r.Status is MaintenanceRequestStatus.Open or MaintenanceRequestStatus.Triaged).ToList();
        var openWorkOrders = workOrders.Where(w =>
            w.Status is not MaintenanceWorkOrderStatus.Completed and not MaintenanceWorkOrderStatus.Cancelled).ToList();
        var completedInPeriod = workOrders.Where(w =>
            w.Status == MaintenanceWorkOrderStatus.Completed &&
            w.CompletedAtUtc >= dateFromUtc &&
            w.CompletedAtUtc <= dateToUtc).ToList();

        var completedCorrectiveRepairs = workOrders
            .Where(IsEligibleMttrRepair)
            .Select(w => new RepairDurationRow(
                w.AssetReference,
                w.AssetType,
                w.AssetName,
                w.StartedAtUtc!.Value,
                w.CompletedAtUtc!.Value,
                (w.CompletedAtUtc!.Value - w.StartedAtUtc!.Value).TotalMinutes))
            .ToList();

        var completedWithCost = workOrders
            .Where(w => w.Status == MaintenanceWorkOrderStatus.Completed)
            .Select(w =>
            {
                partCostMap.TryGetValue(w.Id, out var parts);
                var total = parts + (w.ExternalServiceCost ?? 0m) + (w.OtherCost ?? 0m);
                return new WorkOrderCostRow(w.AssetReference, w.AssetType, total, total > 0m);
            })
            .ToList();

        return new FacilitySnapshot(
            assetReferences,
            faultEvents,
            openRequests,
            openWorkOrders,
            dues,
            completedInPeriod,
            completedCorrectiveRepairs,
            completedWithCost,
            workOrders.Count(w => w.Status == MaintenanceWorkOrderStatus.Completed));
    }

    private static List<FaultEventRow> BuildFaultEvents(
        IReadOnlyList<MaintenanceRequest> requests,
        IReadOnlyList<MaintenanceWorkOrder> workOrders)
    {
        var events = new List<FaultEventRow>();
        foreach (var request in requests.Where(r => r.Type is MaintenanceRequestType.Fault or MaintenanceRequestType.Corrective))
        {
            events.Add(new FaultEventRow(
                request.AssetReference,
                request.AssetType,
                request.AssetName,
                MaintenanceValueConversions.ToStorage(request.FailureCategory),
                MaintenanceValueConversions.ToStorage(request.Priority),
                null,
                request.ReportedAtUtc));
        }

        foreach (var workOrder in workOrders.Where(w =>
                     w.Type is MaintenanceWorkOrderType.Fault or MaintenanceWorkOrderType.Corrective &&
                     w.Status == MaintenanceWorkOrderStatus.Completed))
        {
            var category = workOrder.MaintenanceRequestId is not null
                ? requests.FirstOrDefault(r => r.Id == workOrder.MaintenanceRequestId)?.FailureCategory
                : MaintenanceFailureCategory.Other;
            events.Add(new FaultEventRow(
                workOrder.AssetReference,
                workOrder.AssetType,
                workOrder.AssetName,
                MaintenanceValueConversions.ToStorage(category ?? MaintenanceFailureCategory.Other),
                MaintenanceValueConversions.ToStorage(workOrder.Priority),
                workOrder.ResolutionCode is null ? null : MaintenanceValueConversions.ToStorage(workOrder.ResolutionCode.Value),
                workOrder.CompletedAtUtc ?? workOrder.UpdatedAtUtc));
        }

        return events;
    }

    private static bool IsEligibleMttrRepair(MaintenanceWorkOrder workOrder) =>
        workOrder.Type is MaintenanceWorkOrderType.Fault or MaintenanceWorkOrderType.Corrective &&
        workOrder.Status == MaintenanceWorkOrderStatus.Completed &&
        workOrder.StartedAtUtc is not null &&
        workOrder.CompletedAtUtc is not null &&
        workOrder.CompletedAtUtc > workOrder.StartedAtUtc;

    private static MaintenanceMttrReadModel CalculateMttr(IReadOnlyList<RepairDurationRow> repairs)
    {
        var eligible = repairs.Where(r => r.DurationMinutes > 0).ToList();
        var excluded = repairs.Count - eligible.Count;
        if (eligible.Count == 0)
        {
            return new MaintenanceMttrReadModel(
                MttrLabel,
                0,
                excluded,
                0,
                null,
                null,
                "insufficient-data");
        }

        var total = eligible.Sum(r => r.DurationMinutes);
        var mean = total / eligible.Count;
        var sorted = eligible.Select(r => r.DurationMinutes).OrderBy(v => v).ToList();
        double? median = sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;

        return new MaintenanceMttrReadModel(
            MttrLabel,
            eligible.Count,
            excluded,
            total,
            Math.Round(mean, 2),
            median is null ? null : Math.Round(median.Value, 2),
            "complete");
    }

    private async Task<MaintenanceMtbfReadModel> CalculateMtbfAsync(
        MaintenanceOperationContext context,
        FacilitySnapshot snapshot,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken)
    {
        if (!operatingTimeSource.IsTrustedRuntimeAvailable)
        {
            return new MaintenanceMtbfReadModel(false, null, MtbfUnavailableReason);
        }

        var faultCount = snapshot.FaultEvents.Count(f => f.OccurredAtUtc >= dateFromUtc && f.OccurredAtUtc <= dateToUtc);
        if (faultCount == 0)
        {
            return new MaintenanceMtbfReadModel(false, null, "no failures in period for MTBF denominator");
        }

        double totalHours = 0;
        foreach (var asset in snapshot.AssetReferences)
        {
            var hours = await operatingTimeSource.TryGetOperatingHoursAsync(
                context, asset, dateFromUtc, dateToUtc, cancellationToken);
            if (hours is > 0)
            {
                totalHours += hours.Value;
            }
        }

        if (totalHours <= 0)
        {
            return new MaintenanceMtbfReadModel(false, null, MtbfUnavailableReason);
        }

        return new MaintenanceMtbfReadModel(true, Math.Round(totalHours / faultCount, 2), "derived from trusted operating-time source");
    }

    private async Task<MaintenanceMtbfReadModel> CalculateMtbfForAssetAsync(
        MaintenanceOperationContext context,
        string assetReference,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken)
    {
        if (!operatingTimeSource.IsTrustedRuntimeAvailable)
        {
            return new MaintenanceMtbfReadModel(false, null, MtbfUnavailableReason);
        }

        var hours = await operatingTimeSource.TryGetOperatingHoursAsync(
            context, assetReference, dateFromUtc, dateToUtc, cancellationToken);
        if (hours is null or <= 0)
        {
            return new MaintenanceMtbfReadModel(false, null, MtbfUnavailableReason);
        }

        return new MaintenanceMtbfReadModel(true, null, "asset-level MTBF requires failure count context");
    }

    private static MaintenancePreventiveComplianceReadModel CalculatePreventiveCompliance(
        IReadOnlyList<PreventiveMaintenanceDue> dues,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        DateTimeOffset now)
    {
        var inPeriod = dues.Where(d => d.DueAtUtc >= dateFromUtc && d.DueAtUtc <= dateToUtc).ToList();
        var dueCount = inPeriod.Count;
        var onTime = inPeriod.Count(d =>
            d.Status == PreventiveMaintenanceDueStatus.Completed &&
            d.CompletedAtUtc is not null &&
            d.CompletedAtUtc <= d.DueAtUtc);
        var late = inPeriod.Count(d =>
            d.Status == PreventiveMaintenanceDueStatus.Completed &&
            d.CompletedAtUtc is not null &&
            d.CompletedAtUtc > d.DueAtUtc);
        var overdueOpen = inPeriod.Count(d => IsOverdueOpenDue(d, now));
        var skipped = inPeriod.Count(d => d.Status == PreventiveMaintenanceDueStatus.Skipped);
        var completed = onTime + late;
        double? completionRate = dueCount == 0 ? null : Math.Round(completed * 100.0 / dueCount, 2);
        double? onTimeRate = completed == 0 ? null : Math.Round(onTime * 100.0 / completed, 2);

        return new MaintenancePreventiveComplianceReadModel(
            dueCount,
            onTime,
            late,
            overdueOpen,
            skipped,
            completionRate,
            onTimeRate,
            PreventiveComplianceRule);
    }

    private static bool IsOverdueOpenDue(PreventiveMaintenanceDue due, DateTimeOffset now) =>
        due.DueAtUtc < now &&
        due.Status is PreventiveMaintenanceDueStatus.Due or PreventiveMaintenanceDueStatus.WorkOrderCreated;

    private static MaintenanceBacklogAgingReadModel CalculateBacklogAging(
        IReadOnlyList<MaintenanceRequest> openRequests,
        IReadOnlyList<MaintenanceWorkOrder> openWorkOrders,
        DateTimeOffset now)
    {
        return new MaintenanceBacklogAgingReadModel(
            Bucketize(openRequests.Select(r => r.ReportedAtUtc), now),
            Bucketize(openWorkOrders.Select(w => w.CreatedAtUtc), now),
            Bucketize(openWorkOrders.Where(w => w.Priority == MaintenancePriority.Critical).Select(w => w.CreatedAtUtc), now),
            Bucketize(openWorkOrders.Where(w => w.Status == MaintenanceWorkOrderStatus.Waiting).Select(w => w.UpdatedAtUtc), now));
    }

    private static MaintenanceAgingBucketsReadModel Bucketize(IEnumerable<DateTimeOffset> timestamps, DateTimeOffset now)
    {
        var buckets = new MaintenanceAgingBucketsReadModel(0, 0, 0, 0, 0);
        foreach (var timestamp in timestamps)
        {
            var ageDays = (now - timestamp).TotalDays;
            buckets = ageDays switch
            {
                <= 1 => buckets with { Day0To1 = buckets.Day0To1 + 1 },
                <= 3 => buckets with { Day2To3 = buckets.Day2To3 + 1 },
                <= 7 => buckets with { Day4To7 = buckets.Day4To7 + 1 },
                <= 14 => buckets with { Day8To14 = buckets.Day8To14 + 1 },
                _ => buckets with { Day15Plus = buckets.Day15Plus + 1 },
            };
        }

        return buckets;
    }

    private static List<MaintenanceRepeatFailureReadModel> CalculateRepeatFailures(
        IReadOnlyList<FaultEventRow> events,
        DateTimeOffset now)
    {
        return events
            .GroupBy(e => BuildFailureSignature(e.AssetReference, e.FailureCategory), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group.OrderByDescending(e => e.OccurredAtUtc).ToList();
                var last = ordered[0];
                var repeatCount = Math.Max(0, ordered.Count - 1);
                var openRelated = 0;
                return new MaintenanceRepeatFailureReadModel(
                    last.AssetReference,
                    last.AssetType,
                    last.AssetName,
                    last.FailureCategory,
                    group.Key,
                    ordered.Count,
                    repeatCount,
                    ordered.Count(e => e.OccurredAtUtc >= now.AddDays(-30)),
                    ordered.Count(e => e.OccurredAtUtc >= now.AddDays(-90)),
                    last.OccurredAtUtc,
                    (int)Math.Floor((now - last.OccurredAtUtc).TotalDays),
                    openRelated,
                    repeatCount >= 2 ? "warning" : repeatCount >= 1 ? "attention" : "normal");
            })
            .Where(r => r.FailureCount >= 2)
            .OrderByDescending(r => r.RepeatCount)
            .ThenByDescending(r => r.LastOccurrenceUtc)
            .ToList();
    }

    private static MaintenanceCostAnalyticsReadModel? CalculateCostAnalytics(
        IReadOnlyList<WorkOrderCostRow> rows,
        int allCompletedCount)
    {
        if (allCompletedCount == 0)
        {
            return new MaintenanceCostAnalyticsReadModel(null, null, [], 0, 0, "no-completed-work-orders");
        }

        var withCost = rows.Where(r => r.HasCost).ToList();
        var withoutCost = allCompletedCount - withCost.Count;
        if (withCost.Count == 0)
        {
            return new MaintenanceCostAnalyticsReadModel(null, null, [], 0, withoutCost, "partial-or-missing-cost");
        }

        var total = withCost.Sum(r => r.TotalCost);
        var avg = total / withCost.Count;
        var topAssets = withCost
            .GroupBy(r => r.AssetReference, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MaintenanceAssetCostRowReadModel(g.Key, g.First().AssetType, g.Sum(x => x.TotalCost)))
            .OrderByDescending(a => a.TotalCost)
            .Take(5)
            .ToList();

        return new MaintenanceCostAnalyticsReadModel(
            total,
            Math.Round(avg, 2),
            topAssets,
            withCost.Count,
            withoutCost,
            withoutCost == 0 ? "complete" : "partial");
    }

    private static string EvaluateConditionStatus(FacilitySnapshot snapshot, string assetReference, DateTimeOffset now) =>
        EvaluateConditionStatusWithReasons(snapshot, assetReference, now).Status;

    private static (string Status, IReadOnlyList<string> Reasons) EvaluateConditionStatusWithReasons(
        FacilitySnapshot snapshot,
        string assetReference,
        DateTimeOffset now)
    {
        var reasons = new List<string>();
        var openCriticalWo = snapshot.OpenWorkOrders.Any(w =>
            string.Equals(w.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase) &&
            w.Priority == MaintenancePriority.Critical);
        var openCriticalRequest = snapshot.OpenRequests.Any(r =>
            string.Equals(r.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase) &&
            r.Priority == MaintenancePriority.Critical);

        if (openCriticalWo || openCriticalRequest)
        {
            reasons.Add("Critical open maintenance item");
            return ("critical", reasons);
        }

        var repeatCount = CalculateRepeatFailures(snapshot.FaultEvents, now)
            .Where(r => string.Equals(r.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.RepeatCount);
        var heavilyOverdue = snapshot.Dues.Any(d =>
            string.Equals(d.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase) &&
            IsOverdueOpenDue(d, now) &&
            (now - d.DueAtUtc).TotalDays >= 7);

        if (repeatCount >= 2 || heavilyOverdue)
        {
            if (repeatCount >= 2)
            {
                reasons.Add("Repeat failure threshold reached");
            }

            if (heavilyOverdue)
            {
                reasons.Add("Preventive maintenance heavily overdue");
            }

            return ("warning", reasons);
        }

        var recentFault = snapshot.FaultEvents.Any(f =>
            string.Equals(f.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase) &&
            f.OccurredAtUtc >= now.AddDays(-7));
        var overduePreventive = snapshot.Dues.Any(d =>
            string.Equals(d.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase) &&
            IsOverdueOpenDue(d, now));

        if (recentFault || overduePreventive)
        {
            if (recentFault)
            {
                reasons.Add("Recent fault recorded");
            }

            if (overduePreventive)
            {
                reasons.Add("Overdue preventive maintenance");
            }

            return ("attention", reasons);
        }

        var hasHistory = snapshot.FaultEvents.Any(f => string.Equals(f.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase)) ||
                         snapshot.OpenWorkOrders.Any(w => string.Equals(w.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase)) ||
                         snapshot.Dues.Any(d => string.Equals(d.AssetReference, assetReference, StringComparison.OrdinalIgnoreCase));

        if (hasHistory)
        {
            reasons.Add("No active deterministic maintenance risk");
            return ("normal", reasons);
        }

        reasons.Add("Insufficient maintenance history");
        return ("unknown", reasons);
    }

    private static List<MaintenanceConditionSignalReadModel> BuildDerivedConditionSignals(
        MaintenanceOperationContext context,
        FacilitySnapshot snapshot,
        DateTimeOffset now)
    {
        var signals = new List<MaintenanceConditionSignalReadModel>();
        foreach (var repeat in CalculateRepeatFailures(snapshot.FaultEvents, now).Where(r => r.RepeatCount >= 1))
        {
            signals.Add(new MaintenanceConditionSignalReadModel(
                $"repeat-failure:{repeat.FailureSignature}",
                context.TenantId,
                context.FacilityId,
                repeat.AssetType,
                repeat.AssetReference,
                "repeat-failure",
                repeat.RepeatCount >= 2 ? "warning" : "attention",
                repeat.LastOccurrenceUtc ?? now,
                "maintenance-derived",
                repeat.FailureSignature,
                repeat.RepeatCount.ToString(CultureInfo.InvariantCulture),
                "count",
                $"Repeat failure signature {repeat.FailureSignature}",
                true,
                null));
        }

        foreach (var due in snapshot.Dues.Where(d => IsOverdueOpenDue(d, now)))
        {
            signals.Add(new MaintenanceConditionSignalReadModel(
                $"overdue-preventive:{due.Id}",
                context.TenantId,
                context.FacilityId,
                due.AssetType,
                due.AssetReference,
                "overdue-preventive",
                (now - due.DueAtUtc).TotalDays >= 7 ? "warning" : "attention",
                due.DueAtUtc,
                "maintenance-derived",
                due.Id.ToString(),
                Math.Floor((now - due.DueAtUtc).TotalDays).ToString(CultureInfo.InvariantCulture),
                "days",
                "Preventive maintenance due is overdue",
                true,
                null));
        }

        foreach (var workOrder in snapshot.OpenWorkOrders.Where(w => w.Priority == MaintenancePriority.Critical))
        {
            signals.Add(new MaintenanceConditionSignalReadModel(
                $"critical-open-maintenance:{workOrder.Id}",
                context.TenantId,
                context.FacilityId,
                workOrder.AssetType,
                workOrder.AssetReference,
                "critical-open-maintenance",
                "critical",
                workOrder.UpdatedAtUtc,
                "maintenance-derived",
                workOrder.Id.ToString(),
                null,
                null,
                "Critical open maintenance work order",
                true,
                null));
        }

        foreach (var repair in snapshot.CompletedCorrectiveRepairs.Where(r => r.DurationMinutes >= 240))
        {
            signals.Add(new MaintenanceConditionSignalReadModel(
                $"high-repair-duration:{repair.AssetReference}:{repair.CompletedAtUtc:O}",
                context.TenantId,
                context.FacilityId,
                repair.AssetType,
                repair.AssetReference,
                "high-repair-duration",
                "attention",
                repair.CompletedAtUtc,
                "maintenance-derived",
                repair.AssetReference,
                Math.Round(repair.DurationMinutes, 0).ToString(CultureInfo.InvariantCulture),
                "minutes",
                "Completed corrective repair duration exceeded 240 minutes",
                true,
                null));
        }

        return signals;
    }

    private static string BuildFailureSignature(string assetReference, string failureCategory) =>
        $"{assetReference}|{failureCategory}";

    private static string GetGroupKey(FaultEventRow faultEvent, string groupBy) => groupBy switch
    {
        "asset-type" => faultEvent.AssetType,
        "asset-reference" => faultEvent.AssetReference,
        "priority" => faultEvent.Priority,
        "resolution-code" => faultEvent.ResolutionCode ?? "unknown",
        _ => faultEvent.FailureCategory,
    };

    private sealed record FacilitySnapshot(
        IReadOnlyList<string> AssetReferences,
        IReadOnlyList<FaultEventRow> FaultEvents,
        IReadOnlyList<MaintenanceRequest> OpenRequests,
        IReadOnlyList<MaintenanceWorkOrder> OpenWorkOrders,
        IReadOnlyList<PreventiveMaintenanceDue> Dues,
        IReadOnlyList<MaintenanceWorkOrder> CompletedWorkOrdersInPeriod,
        IReadOnlyList<RepairDurationRow> CompletedCorrectiveRepairs,
        IReadOnlyList<WorkOrderCostRow> CompletedWorkOrdersWithCost,
        int AllCompletedWorkOrderCount);

    private sealed record FaultEventRow(
        string AssetReference,
        string AssetType,
        string? AssetName,
        string FailureCategory,
        string Priority,
        string? ResolutionCode,
        DateTimeOffset OccurredAtUtc);

    private sealed record RepairDurationRow(
        string AssetReference,
        string AssetType,
        string? AssetName,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset CompletedAtUtc,
        double DurationMinutes);

    private sealed record WorkOrderCostRow(
        string AssetReference,
        string AssetType,
        decimal TotalCost,
        bool HasCost);
}
