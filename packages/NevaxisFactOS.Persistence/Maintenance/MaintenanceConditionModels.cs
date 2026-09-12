namespace NevaxisFactOS.Persistence.Maintenance;

public sealed record ConditionMetricEvaluationInput(
    string MetricKey,
    string DisplayName,
    string? ExpectedUnit,
    int EvaluationWindowHours,
    int MinimumSamples,
    string BaselineMethod,
    decimal DeviationThresholdPercent,
    decimal TrendThresholdPercent,
    decimal? VolatilityThresholdPercent,
    IReadOnlyList<ConditionTelemetrySampleInput> Samples,
    DateTimeOffset? LatestSampleAtUtc);

public sealed record ConditionTelemetrySampleInput(
    decimal Value,
    string Quality,
    DateTimeOffset SourceTimestampUtc);

public sealed record ConditionCycleSampleInput(
    decimal DurationMs,
    DateTimeOffset ObservedAtUtc);

public sealed record ConditionAlarmContextInput(
    int RecentAlarmCount24h,
    string? HighestSeverity24h);

public sealed record ConditionDowntimeContextInput(
    int EpisodeCount24h,
    decimal TotalDurationMinutes24h);

public sealed record ConditionMaintenanceHistoryInput(
    int RecentFailureCount90d,
    string? LastFailureCategory,
    DateTimeOffset? LastMaintenanceCompletedAtUtc);

public sealed record ConditionAssetEvaluationInput(
    string AssetReference,
    Guid MachineId,
    string? MachineCode,
    string? MachineName,
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<ConditionMetricEvaluationInput> Metrics,
    IReadOnlyList<ConditionCycleSampleInput> CycleSamples,
    ConditionAlarmContextInput AlarmContext,
    ConditionDowntimeContextInput DowntimeContext,
    ConditionMaintenanceHistoryInput MaintenanceHistory,
    bool SourceIsStale);

public sealed record ConditionMetricEvaluationResult(
    string MetricKey,
    bool SufficientData,
    string DataSufficiency,
    string DataFreshness,
    string? TrendState,
    decimal? BaselineValue,
    decimal? ObservedValue,
    decimal? DeviationPercent,
    IReadOnlyList<ConditionAnomalyCandidate> Anomalies,
    IReadOnlyList<ConditionSignalCandidate> Signals);

public sealed record ConditionAnomalyCandidate(
    string MetricKey,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    decimal ObservedValue,
    decimal BaselineValue,
    decimal DeviationPercent,
    string Severity,
    string Direction,
    string EvidenceSummary,
    string SourceFreshness,
    string? DataQuality,
    string EvaluationIdentity);

public sealed record ConditionSignalCandidate(
    string SignalType,
    string MetricKey,
    string Severity,
    string Summary);

public sealed record ConditionAssetEvaluationResult(
    string AssetReference,
    Guid MachineId,
    string ConditionStatus,
    decimal? OverallConditionScore,
    string DataFreshness,
    string DataSufficiency,
    string TrendState,
    int ActiveAnomalyCount,
    string? HighestAnomalySeverity,
    string EvaluationIdentity,
    IReadOnlyList<ConditionMetricEvaluationResult> MetricResults,
    IReadOnlyList<ConditionAnomalyCandidate> Anomalies,
    IReadOnlyList<ConditionSignalCandidate> Signals);

public sealed record MaintenanceConditionOverviewReadModel(
    int MonitoredAssetCount,
    int NormalCount,
    int WatchCount,
    int DegradedCount,
    int CriticalCount,
    int InsufficientDataCount,
    DateTimeOffset? LastEvaluatedAtUtc);

public sealed record MaintenanceConditionAssetRowReadModel(
    string AssetReference,
    string? AssetName,
    string? MachineCode,
    string? MachineName,
    Guid MachineId,
    string ConditionStatus,
    int ActiveAnomalyCount,
    string? HighestAnomalySeverity,
    string DataFreshness,
    string DataSufficiency,
    DateTimeOffset? LastEvaluatedAtUtc);

public sealed record MaintenanceConditionProfileReadModel(
    string AssetReference,
    string? AssetName,
    Guid MachineId,
    string? MachineCode,
    string? MachineName,
    string ConditionStatus,
    decimal? OverallConditionScore,
    DateTimeOffset? LastEvaluatedAtUtc,
    string DataFreshness,
    string DataSufficiency,
    int ActiveAnomalyCount,
    string? HighestAnomalySeverity,
    string TrendState,
    IReadOnlyList<MaintenancePredictiveConditionSignalReadModel> Signals,
    IReadOnlyList<MaintenanceConditionAnomalyReadModel> ActiveAnomalies,
    MaintenanceConditionContextReadModel Context);

public sealed record MaintenancePredictiveConditionSignalReadModel(
    string SignalType,
    string MetricKey,
    string Severity,
    string Summary);

public sealed record MaintenanceConditionAnomalyReadModel(
    Guid Id,
    string MetricKey,
    string MetricDisplayName,
    DateTimeOffset DetectedAtUtc,
    decimal ObservedValue,
    decimal BaselineValue,
    decimal DeviationPercent,
    string Severity,
    string Direction,
    string EvidenceSummary,
    string SourceFreshness,
    string? DataQuality,
    string Status);

public sealed record MaintenanceConditionContextReadModel(
    ConditionAlarmContextInput AlarmContext,
    ConditionDowntimeContextInput DowntimeContext,
    ConditionMaintenanceHistoryInput MaintenanceHistory,
    IReadOnlyList<MaintenanceConditionMetricSnapshotReadModel> MetricSnapshots);

public sealed record MaintenanceConditionMetricSnapshotReadModel(
    string MetricKey,
    string DisplayName,
    string? ExpectedUnit,
    string DataSufficiency,
    string DataFreshness,
    string? TrendState,
    decimal? BaselineValue,
    decimal? ObservedValue,
    decimal? DeviationPercent);

public interface IConditionEvaluationService
{
    ConditionAssetEvaluationResult EvaluateAsset(ConditionAssetEvaluationInput input);
}

public interface IMaintenanceConditionRepository
{
    Task<MaintenanceConditionOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenanceConditionAssetRowReadModel>> ListAssetRowsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken);

    Task<MaintenanceConditionProfileReadModel?> GetAssetConditionAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenanceConditionAnomalyReadModel>> ListAnomaliesAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenancePredictiveConditionSignalReadModel>> ListConditionSignalsAsync(
        MaintenanceOperationContext context,
        string? assetReference,
        CancellationToken cancellationToken);

    Task<bool> EvaluateFacilityAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken);
}
