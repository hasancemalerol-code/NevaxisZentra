namespace NevaxisFactOS.Persistence.Production;

/// <summary>Pure deterministic reporting calculations over authoritative Production fields.</summary>
public static class ProductionReportingEngine
{
    public const string ExpectedCycleUnavailable = "expected-cycle-unavailable";

    public static decimal? ComputeCompletionRatePercent(decimal planned, decimal good)
    {
        if (planned <= 0m)
        {
            return null;
        }

        return Math.Round(good / planned * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal? ComputeYieldPercent(decimal good, decimal scrap)
    {
        var total = good + scrap;
        if (total <= 0m)
        {
            return null;
        }

        return Math.Round(good / total * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal ComputeRemainingQuantity(decimal? planned, decimal good, decimal scrap)
    {
        if (planned is null or <= 0m)
        {
            return 0m;
        }

        return Math.Max(0m, planned.Value - good - scrap);
    }

    public static long? ComputeScheduleVarianceMinutes(
        DateTimeOffset plannedEnd,
        DateTimeOffset? actualEnd,
        DateTimeOffset nowUtc)
    {
        var comparisonPoint = actualEnd ?? nowUtc;
        return (long)Math.Round((comparisonPoint - plannedEnd).TotalMinutes, MidpointRounding.AwayFromZero);
    }

    public static long? ComputeAverageCycleDurationMilliseconds(int cycleCount, long totalDurationMs)
    {
        if (cycleCount <= 0 || totalDurationMs <= 0)
        {
            return null;
        }

        return totalDurationMs / cycleCount;
    }

    public static IReadOnlyList<ProductionRiskSignalReadModel> ComputeRiskSignals(
        IReadOnlyList<ProductionOrder> orders,
        IReadOnlyList<ProductionExecutionExceptionRecord> openExceptions,
        IReadOnlyList<ProductionDowntime> activeDowntimes,
        IReadOnlyList<ProductionShiftHandover> recentHandovers,
        ProductionLogisticsRiskSignal logisticsRisk,
        DateTimeOffset nowUtc)
    {
        var signals = new List<ProductionRiskSignalReadModel>();

        foreach (var order in orders.Where(o =>
                     o.Status is ProductionOrderStatus.InProgress or ProductionOrderStatus.Ready or ProductionOrderStatus.Paused))
        {
            if (order.PlannedEndAtUtc < nowUtc
                && order.Status is not ProductionOrderStatus.Completed and not ProductionOrderStatus.Cancelled)
            {
                signals.Add(new ProductionRiskSignalReadModel(
                    "production-behind-plan",
                    "Production behind plan",
                    "warning",
                    order.Id.ToString(),
                    order.ReferenceCode));
            }

            if (order.ScrapQuantity > 0m
                && order.PlannedQuantity is > 0m
                && order.ScrapQuantity / (order.GoodQuantity + order.ScrapQuantity) >= 0.15m)
            {
                signals.Add(new ProductionRiskSignalReadModel(
                    "high-scrap",
                    "High scrap rate",
                    "warning",
                    order.Id.ToString(),
                    order.ReferenceCode));
            }

            if (order.Status == ProductionOrderStatus.InProgress
                && order.ActiveOperatorCount <= 0
                && string.IsNullOrWhiteSpace(order.PrimaryOperatorSubjectId))
            {
                signals.Add(new ProductionRiskSignalReadModel(
                    "missing-operator",
                    "Missing operator assignment",
                    "attention",
                    order.Id.ToString(),
                    order.ReferenceCode));
            }

            if (order.Status == ProductionOrderStatus.InProgress
                && string.IsNullOrWhiteSpace(order.MachineReference))
            {
                signals.Add(new ProductionRiskSignalReadModel(
                    "missing-resource",
                    "Missing machine/resource reference",
                    "attention",
                    order.Id.ToString(),
                    order.ReferenceCode));
            }
        }

        if (activeDowntimes.Count > 0)
        {
            signals.Add(new ProductionRiskSignalReadModel(
                "active-downtime",
                "Active downtime recorded",
                "warning"));
        }

        if (openExceptions.Count > 0)
        {
            signals.Add(new ProductionRiskSignalReadModel(
                "unresolved-exception",
                "Unresolved execution exceptions",
                openExceptions.Any(e => string.Equals(e.Severity, ProductionExceptionSeverities.Critical, StringComparison.Ordinal))
                    ? "critical"
                    : "warning"));
        }

        if (recentHandovers.Any(h => h.ToShiftAssignmentId is null))
        {
            signals.Add(new ProductionRiskSignalReadModel(
                "handover-incomplete",
                "Shift handover incomplete",
                "attention"));
        }

        if (logisticsRisk.Available
            && (logisticsRisk.Blocking
                || string.Equals(logisticsRisk.RiskLevel, "high", StringComparison.OrdinalIgnoreCase)
                || string.Equals(logisticsRisk.RiskLevel, "critical", StringComparison.OrdinalIgnoreCase)))
        {
            signals.Add(new ProductionRiskSignalReadModel(
                "shipment-deadline-risk",
                "Shipment deadline risk (Planning Logistics)",
                logisticsRisk.Blocking ? "critical" : "warning"));
        }

        return signals
            .DistinctBy(s => $"{s.Code}:{s.RelatedProductionOrderId}")
            .ToList();
    }

    public static ProductionPlanActualComparisonReadModel BuildPlanActual(
        ProductionOrder order,
        DateTimeOffset nowUtc)
    {
        var remaining = ComputeRemainingQuantity(order.PlannedQuantity, order.GoodQuantity, order.ScrapQuantity);
        var completion = ComputeCompletionRatePercent(order.PlannedQuantity ?? 0m, order.GoodQuantity);
        var yield = ComputeYieldPercent(order.GoodQuantity, order.ScrapQuantity);
        var variance = ComputeScheduleVarianceMinutes(order.PlannedEndAtUtc, order.ActualEndAtUtc, nowUtc);
        var overrun = variance is > 0
            && order.Status is not ProductionOrderStatus.Completed and not ProductionOrderStatus.Cancelled;

        return new ProductionPlanActualComparisonReadModel(
            order.PlannedStartAtUtc,
            order.PlannedEndAtUtc,
            order.ActualStartAtUtc,
            order.ActualEndAtUtc,
            order.PlannedQuantity,
            order.GoodQuantity,
            order.ScrapQuantity,
            remaining,
            completion,
            yield,
            variance,
            overrun);
    }
}
