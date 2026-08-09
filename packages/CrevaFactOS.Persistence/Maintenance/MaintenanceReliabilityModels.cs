namespace CrevaFactOS.Persistence.Maintenance;

public sealed record MaintenanceReportingPeriod(
    DateTimeOffset DateFromUtc,
    DateTimeOffset DateToUtc);

public sealed record MaintenanceMttrReadModel(
    string Label,
    int EligibleRepairCount,
    int ExcludedRepairCount,
    double TotalRepairMinutes,
    double? MeanRepairMinutes,
    double? MedianRepairMinutes,
    string DataCompletenessStatus);

public sealed record MaintenanceMtbfReadModel(
    bool MtbfAvailable,
    double? MtbfHours,
    string Reason);

public sealed record MaintenanceAgingBucketsReadModel(
    int Day0To1,
    int Day2To3,
    int Day4To7,
    int Day8To14,
    int Day15Plus);

public sealed record MaintenanceBacklogAgingReadModel(
    MaintenanceAgingBucketsReadModel OpenRequests,
    MaintenanceAgingBucketsReadModel OpenWorkOrders,
    MaintenanceAgingBucketsReadModel CriticalOpenWorkOrders,
    MaintenanceAgingBucketsReadModel WaitingWorkOrders);

public sealed record MaintenancePreventiveComplianceReadModel(
    int DueCount,
    int CompletedOnTimeCount,
    int CompletedLateCount,
    int OverdueOpenCount,
    int SkippedCount,
    double? PreventiveCompletionRate,
    double? OnTimeCompletionRate,
    string RuleDescription);

public sealed record MaintenanceRepeatFailureReadModel(
    string AssetReference,
    string AssetType,
    string? AssetName,
    string FailureCategory,
    string FailureSignature,
    int FailureCount,
    int RepeatCount,
    int FailuresLast30Days,
    int FailuresLast90Days,
    DateTimeOffset? LastOccurrenceUtc,
    int? DaysSinceLastFailure,
    int OpenRelatedWorkOrderCount,
    string ConditionStatus);

public sealed record MaintenanceFailureBreakdownRowReadModel(
    string GroupKey,
    string GroupLabel,
    int Count,
    double Percentage,
    int RepeatCount,
    double? AverageRepairMinutes);

public sealed record MaintenanceCostAnalyticsReadModel(
    decimal? TotalMaintenanceCost,
    decimal? AverageCostPerCompletedWorkOrder,
    IReadOnlyList<MaintenanceAssetCostRowReadModel> TopCostAssets,
    int RecordsWithCost,
    int RecordsWithoutCost,
    string DataCompletenessStatus);

public sealed record MaintenanceAssetCostRowReadModel(
    string AssetReference,
    string AssetType,
    decimal TotalCost);

public sealed record MaintenanceReliabilityOverviewReadModel(
    int TotalAssetsWithMaintenanceHistory,
    int OpenRequests,
    int OpenWorkOrders,
    int CriticalOpenWorkOrders,
    int OverduePreventiveCount,
    int CompletedWorkOrdersPeriod,
    int FaultCountPeriod,
    int RepeatFaultCountPeriod,
    double? AverageRepairDurationMinutes,
    double? MedianRepairDurationMinutes,
    double? PreventiveCompletionRate,
    int AssetsAtRiskCount,
    MaintenanceReportingPeriod Period,
    MaintenanceMttrReadModel Mttr,
    MaintenanceMtbfReadModel Mtbf,
    MaintenanceCostAnalyticsReadModel? CostAnalytics);

public sealed record MaintenanceReliabilityReportReadModel(
    MaintenanceReportingPeriod Period,
    MaintenanceMttrReadModel Mttr,
    MaintenanceMtbfReadModel Mtbf,
    IReadOnlyList<MaintenanceRepeatFailureReadModel> RepeatFailures,
    MaintenancePreventiveComplianceReadModel PreventiveCompliance,
    MaintenanceBacklogAgingReadModel BacklogAging,
    MaintenanceCostAnalyticsReadModel? CostAnalytics);

public sealed record MaintenanceAssetReliabilityReadModel(
    string AssetReference,
    string AssetType,
    string? AssetName,
    int TotalFaults,
    int FaultsLast30Days,
    int FaultsLast90Days,
    int RepeatFaultCount,
    int CompletedCorrectiveWorkOrders,
    double? MeanRepairMinutes,
    int OpenWorkOrders,
    int CriticalOpenWorkOrders,
    int OverduePreventiveCount,
    double? PreventiveComplianceRate,
    DateTimeOffset? LastFailureAtUtc,
    DateTimeOffset? LastMaintenanceCompletedAtUtc,
    string ConditionStatus,
    IReadOnlyList<string> ConditionReasons,
    MaintenanceMtbfReadModel Mtbf,
    MaintenanceCostAnalyticsReadModel? CostAnalytics);

public sealed record MaintenanceConditionSignalReadModel(
    string SignalId,
    Guid TenantId,
    Guid FacilityId,
    string AssetType,
    string AssetReference,
    string SignalType,
    string Severity,
    DateTimeOffset ObservedAtUtc,
    string SourceType,
    string? SourceReference,
    string? Value,
    string? Unit,
    string Summary,
    bool IsVerified,
    DateTimeOffset? ExpiresAtUtc);

public sealed record MaintenanceAssetConditionReadModel(
    string AssetReference,
    string ConditionStatus,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<MaintenanceConditionSignalReadModel> Signals,
    DateTimeOffset UpdatedAtUtc);

public sealed record MaintenanceAssetRiskProjectionReadModel(
    string AssetReference,
    string ConditionStatus,
    bool Blocking,
    string PrimaryReason,
    int RepeatFailureCount,
    int OverduePreventiveCount,
    int CriticalOpenWorkOrderCount,
    DateTimeOffset UpdatedAtUtc);

public interface IMaintenanceReliabilityRepository
{
    Task<MaintenanceReliabilityOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken);

    Task<MaintenanceReliabilityReportReadModel?> GetReliabilityReportAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenanceFailureBreakdownRowReadModel>> GetFailureBreakdownAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        string groupBy,
        string? assetType,
        string? assetReference,
        string? failureCategory,
        CancellationToken cancellationToken);

    Task<MaintenanceAssetReliabilityReadModel?> GetAssetReliabilityAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenanceConditionSignalReadModel>> ListConditionSignalsAsync(
        MaintenanceOperationContext context,
        string? assetReference,
        CancellationToken cancellationToken);

    Task<MaintenanceAssetConditionReadModel?> GetAssetConditionAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenanceAssetRiskProjectionReadModel>> ListRiskProjectionsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken);
}

public interface IMaintenanceOperatingTimeSource
{
    bool IsTrustedRuntimeAvailable { get; }
    Task<double?> TryGetOperatingHoursAsync(
        MaintenanceOperationContext context,
        string assetReference,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken);
}

public sealed class UnavailableMaintenanceOperatingTimeSource : IMaintenanceOperatingTimeSource
{
    public bool IsTrustedRuntimeAvailable => false;

    public Task<double?> TryGetOperatingHoursAsync(
        MaintenanceOperationContext context,
        string assetReference,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken) =>
        Task.FromResult<double?>(null);
}

public sealed class UnavailableMaintenanceReliabilityRepository : IMaintenanceReliabilityRepository
{
    public Task<MaintenanceReliabilityOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken) =>
        Task.FromResult<MaintenanceReliabilityOverviewReadModel?>(null);

    public Task<MaintenanceReliabilityReportReadModel?> GetReliabilityReportAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        CancellationToken cancellationToken) =>
        Task.FromResult<MaintenanceReliabilityReportReadModel?>(null);

    public Task<IReadOnlyList<MaintenanceFailureBreakdownRowReadModel>> GetFailureBreakdownAsync(
        MaintenanceOperationContext context,
        DateTimeOffset dateFromUtc,
        DateTimeOffset dateToUtc,
        string groupBy,
        string? assetType,
        string? assetReference,
        string? failureCategory,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceFailureBreakdownRowReadModel>>([]);

    public Task<MaintenanceAssetReliabilityReadModel?> GetAssetReliabilityAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        Task.FromResult<MaintenanceAssetReliabilityReadModel?>(null);

    public Task<IReadOnlyList<MaintenanceConditionSignalReadModel>> ListConditionSignalsAsync(
        MaintenanceOperationContext context,
        string? assetReference,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceConditionSignalReadModel>>([]);

    public Task<MaintenanceAssetConditionReadModel?> GetAssetConditionAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        Task.FromResult<MaintenanceAssetConditionReadModel?>(null);

    public Task<IReadOnlyList<MaintenanceAssetRiskProjectionReadModel>> ListRiskProjectionsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceAssetRiskProjectionReadModel>>([]);
}
