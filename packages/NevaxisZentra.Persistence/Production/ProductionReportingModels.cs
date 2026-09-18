namespace NevaxisZentra.Persistence.Production;

public sealed record ProductionReportingFilter(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    string? Status = null,
    Guid? ProductionOrderId = null,
    Guid? ShiftId = null,
    string? ResourceReference = null,
    string? OperatorSubjectId = null,
    string? RiskState = null);

public sealed record ProductionLogisticsRiskSignal(
    string RiskLevel,
    int SlackMinutes,
    bool Blocking,
    string? Reason,
    DateTimeOffset? LatestProductionCompletionUtc,
    bool Available);

public sealed record ProductionRiskSignalReadModel(
    string Code,
    string Label,
    string Severity,
    string? RelatedProductionOrderId = null,
    string? RelatedReferenceCode = null);

public sealed record ProductionOperationalOverviewReadModel(
    int TotalOrderCount,
    int ActiveOrderCount,
    int CompletedOrderCount,
    int PausedOrderCount,
    int ReadyOrderCount,
    decimal PlannedQuantity,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    decimal RemainingQuantity,
    decimal? CompletionRatePercent,
    decimal? YieldPercent,
    int ActiveDowntimeCount,
    long TotalDowntimeMinutes,
    long TotalSetupMinutes,
    int ManualCycleCount,
    int OpenExceptionCount,
    int UnresolvedExceptionCount,
    int ShiftCoverageCount,
    int OperatorCoverageCount,
    IReadOnlyList<ProductionRiskSignalReadModel> RiskSignals,
    ProductionLogisticsRiskSignal LogisticsRisk);

public sealed record ProductionPlanActualComparisonReadModel(
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    DateTimeOffset? ActualStartAtUtc,
    DateTimeOffset? ActualEndAtUtc,
    decimal? PlannedQuantity,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    decimal? RemainingQuantity,
    decimal? CompletionPercent,
    decimal? YieldPercent,
    long? ScheduleVarianceMinutes,
    bool ScheduleOverrun);

public sealed record ProductionOrderPerformanceReadModel(
    Guid ProductionOrderId,
    string ReferenceCode,
    string Title,
    string Status,
    string? ProductReference,
    string? MachineReference,
    Guid SourceProductionPlanId,
    ProductionPlanActualComparisonReadModel PlanActual,
    bool HasActiveException,
    bool HasActiveDowntime,
    string? OperationalRisk,
    ProductionLogisticsRiskSignal? LogisticsRisk);

public sealed record ProductionDowntimeAnalyticsEventReadModel(
    Guid DowntimeId,
    Guid ProductionOrderId,
    string OrderReferenceCode,
    string Category,
    string ReasonCode,
    string? ResourceReference,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    long? DurationMinutes,
    bool IsActive);

public sealed record ProductionDowntimeAnalyticsReadModel(
    long TotalDowntimeMinutes,
    int ActiveDowntimeCount,
    int EventCount,
    ProductionDowntimeAnalyticsEventReadModel? LongestEvent,
    IReadOnlyList<ProductionDowntimeReasonSummaryReadModel> TopReasons,
    IReadOnlyList<ProductionDowntimeResourceSummaryReadModel> TopResources,
    IReadOnlyList<ProductionDowntimeAnalyticsEventReadModel> Events);

public sealed record ProductionDowntimeReasonSummaryReadModel(
    string ReasonCode,
    int EventCount,
    long TotalMinutes);

public sealed record ProductionDowntimeResourceSummaryReadModel(
    string ResourceReference,
    int EventCount,
    long TotalMinutes);

public sealed record ProductionScrapAnalyticsReadModel(
    decimal ScrapQuantity,
    decimal GoodQuantity,
    decimal TotalProcessedQuantity,
    decimal? ScrapRatePercent,
    IReadOnlyList<ProductionScrapOrderSummaryReadModel> TopOrders,
    IReadOnlyList<ProductionScrapShiftSummaryReadModel> ByShift);

public sealed record ProductionScrapOrderSummaryReadModel(
    Guid ProductionOrderId,
    string ReferenceCode,
    decimal ScrapQuantity,
    decimal GoodQuantity,
    decimal? ScrapRatePercent);

public sealed record ProductionScrapShiftSummaryReadModel(
    string ShiftCode,
    decimal ScrapQuantity,
    decimal GoodQuantity,
    decimal? ScrapRatePercent);

public sealed record ProductionSetupAnalyticsReadModel(
    int SetupEventCount,
    long TotalSetupMinutes,
    long? AverageSetupMinutes,
    IReadOnlyList<ProductionSetupResourceSummaryReadModel> ByResource,
    IReadOnlyList<ProductionSetupOrderSummaryReadModel> ByOrder);

public sealed record ProductionSetupResourceSummaryReadModel(
    string ResourceReference,
    int EventCount,
    long TotalMinutes);

public sealed record ProductionSetupOrderSummaryReadModel(
    Guid ProductionOrderId,
    string ReferenceCode,
    int EventCount,
    long TotalMinutes);

public sealed record ProductionCycleAnalyticsReadModel(
    int CycleCount,
    long? AverageCycleDurationMilliseconds,
    long? RecentCycleDurationMilliseconds,
    string ExpectedCycleStatus,
    IReadOnlyList<ProductionCycleOrderSummaryReadModel> ByOrder);

public sealed record ProductionCycleOrderSummaryReadModel(
    Guid ProductionOrderId,
    string ReferenceCode,
    int CycleCount,
    long? AverageCycleDurationMilliseconds);

public sealed record ProductionShiftReportingReadModel(
    Guid ShiftId,
    string ShiftCode,
    string ShiftName,
    DateTimeOffset? ActiveWindowStartUtc,
    DateTimeOffset? ActiveWindowEndUtc,
    int ProductionOrderCount,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    long DowntimeMinutes,
    long SetupMinutes,
    int OpenExceptionCount,
    int AssignedResourceCount,
    int AssignedOperatorCount,
    string HandoverState);

public sealed record ProductionExceptionAnalyticsReadModel(
    int OpenCount,
    int UnassignedCount,
    int ResolvedCount,
    IReadOnlyList<ProductionExceptionAnalyticsItemReadModel> Items);

public sealed record ProductionExceptionAnalyticsItemReadModel(
    Guid ExceptionId,
    Guid ProductionOrderId,
    string OrderReferenceCode,
    string ExceptionType,
    string Severity,
    string Status,
    long AgeMinutes,
    string? AssignedToSubjectId,
    string? ShiftCode,
    string? ResourceReference);

public sealed record ProductionReportingReadResult<T>
{
    public required bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
}

public static class ProductionReportingResults
{
    public static ProductionReportingReadResult<T> Success<T>(T value) =>
        new() { Succeeded = true, Value = value };

    public static ProductionReportingReadResult<T> Fail<T>(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}
