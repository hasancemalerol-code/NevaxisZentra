namespace CrevaFactOS.Persistence.Maintenance;

public sealed record PredictiveRiskAnomalyInput(
    string MetricKey,
    string Severity,
    string EvidenceSummary,
    DateTimeOffset DetectedAtUtc,
    string EvaluationIdentity);

public sealed record PredictiveRiskEvaluationInput(
    string AssetReference,
    Guid MachineId,
    string? AssetName,
    string? MachineCode,
    string? MachineName,
    string ConditionStatus,
    string DataFreshness,
    string DataSufficiency,
    string TrendState,
    int ActiveAnomalyCount,
    string? HighestAnomalySeverity,
    string? ConditionEvaluationIdentity,
    DateTimeOffset? ConditionEvaluatedAtUtc,
    IReadOnlyList<PredictiveRiskAnomalyInput> OpenAnomalies,
    ConditionAlarmContextInput AlarmContext,
    ConditionDowntimeContextInput DowntimeContext,
    ConditionMaintenanceHistoryInput MaintenanceHistory,
    DateTimeOffset AssessedAtUtc);

public sealed record PredictiveRiskDriverCandidate(
    string DriverCode,
    string Summary,
    int SortOrder);

public sealed record PredictiveRiskEvidenceCandidate(
    string EvidenceType,
    string EvidenceReference,
    string Summary,
    int SortOrder);

public sealed record PredictiveRiskEvaluationResult(
    string RiskBand,
    string ReviewPriority,
    string Explanation,
    string MaintenanceHistorySummary,
    IReadOnlyList<PredictiveRiskDriverCandidate> Drivers,
    IReadOnlyList<PredictiveRiskEvidenceCandidate> Evidence,
    string RecommendationCategory,
    string RecommendationExplanation);

public sealed record MaintenancePredictiveRiskOverviewReadModel(
    int MonitoredAssetCount,
    int InsufficientDataCount,
    int LowRiskCount,
    int WatchCount,
    int HighRiskCount,
    int CriticalRiskCount,
    DateTimeOffset? LastAssessedAtUtc);

public sealed record MaintenancePredictiveRiskAssetRowReadModel(
    string AssetReference,
    string? AssetName,
    string? MachineCode,
    string? MachineName,
    string RiskBand,
    string ReviewPriority,
    string DataFreshness,
    string DataSufficiency,
    DateTimeOffset? AssessedAtUtc);

public sealed record MaintenancePredictiveRiskDriverReadModel(
    string DriverCode,
    string Summary);

public sealed record MaintenancePredictiveRiskEvidenceReadModel(
    string EvidenceType,
    string EvidenceReference,
    string Summary);

public sealed record MaintenancePredictiveRecommendationReadModel(
    string Category,
    string ReviewPriority,
    string Explanation,
    string RuleSetVersion,
    DateTimeOffset GeneratedAtUtc,
    string Status);

public sealed record MaintenancePredictiveRiskAssessmentReadModel(
    Guid Id,
    string AssetReference,
    string? AssetName,
    string? MachineCode,
    string? MachineName,
    string RiskBand,
    string ReviewPriority,
    string RuleSetVersion,
    string? ConditionEvaluationIdentity,
    string DataFreshness,
    string DataSufficiency,
    int ActiveAnomalyCount,
    string MaintenanceHistorySummary,
    string Explanation,
    DateTimeOffset AssessedAtUtc,
    IReadOnlyList<MaintenancePredictiveRiskDriverReadModel> Drivers,
    IReadOnlyList<MaintenancePredictiveRiskEvidenceReadModel> Evidence,
    MaintenancePredictiveRecommendationReadModel? Recommendation);

public sealed record MaintenancePredictiveRiskAssessmentSummaryReadModel(
    Guid Id,
    string RiskBand,
    string ReviewPriority,
    DateTimeOffset AssessedAtUtc,
    string RuleSetVersion);

public interface IPredictiveRiskEvaluationService
{
    PredictiveRiskEvaluationResult EvaluateAsset(PredictiveRiskEvaluationInput input);
}

public interface IMaintenancePredictiveRiskRepository
{
    Task<MaintenancePredictiveRiskOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenancePredictiveRiskAssetRowReadModel>> ListAssetRowsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken);

    Task<MaintenancePredictiveRiskAssessmentReadModel?> GetCurrentAssessmentAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenancePredictiveRiskAssessmentSummaryReadModel>> ListAssessmentHistoryAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int limit,
        CancellationToken cancellationToken);

    Task<bool> EvaluateFacilityRiskAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken);
}
