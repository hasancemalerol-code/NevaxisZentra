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
    Guid? AssessmentId,
    string AssetReference,
    string? AssetName,
    string? MachineCode,
    string? MachineName,
    string RiskBand,
    string ReviewPriority,
    string DataFreshness,
    string DataSufficiency,
    DateTimeOffset? AssessedAtUtc,
    string FollowUpStatus,
    Guid? LinkedAiTaskId);

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
    Guid MachineId,
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
    string FollowUpStatus,
    Guid? LinkedAiTaskId,
    IReadOnlyList<MaintenancePredictiveRiskDriverReadModel> Drivers,
    IReadOnlyList<MaintenancePredictiveRiskEvidenceReadModel> Evidence,
    MaintenancePredictiveRecommendationReadModel? Recommendation);

public sealed record MaintenancePredictiveRiskAssessmentSummaryReadModel(
    Guid Id,
    string RiskBand,
    string ReviewPriority,
    DateTimeOffset AssessedAtUtc,
    string RuleSetVersion);

public sealed record MaintenancePredictiveRiskWorkspaceReadModel(
    string RuleSetVersion,
    string AutomationDisclaimer,
    int ActiveRiskCount,
    int TaskLinkedCount,
    int HumanReviewedCount,
    bool CanRead,
    bool CanManage,
    bool CanCreateTask);

public sealed record LinkMaintenancePredictiveRiskTaskCommand(
    Guid AssessmentId,
    Guid AiTaskId);

public sealed record TransitionMaintenancePredictiveRiskFollowUpCommand(
    Guid AssessmentId,
    string TargetFollowUpStatus,
    string? Note);

public sealed class MaintenancePredictiveRiskWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public bool IdempotentReplay { get; init; }
    public MaintenancePredictiveRiskAssessmentReadModel? Assessment { get; init; }

    public static MaintenancePredictiveRiskWriteResult Success(
        Guid entityId,
        bool idempotentReplay = false,
        MaintenancePredictiveRiskAssessmentReadModel? assessment = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            IdempotentReplay = idempotentReplay,
            Assessment = assessment,
        };

    public static MaintenancePredictiveRiskWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

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

    Task<MaintenancePredictiveRiskWorkspaceReadModel> GetWorkspaceAsync(
        MaintenanceOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken);

    Task<MaintenancePredictiveRiskAssessmentReadModel?> GetAssessmentByIdAsync(
        MaintenanceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken);

    Task<MaintenancePredictiveRiskWriteResult> LinkTaskAsync(
        MaintenanceOperationContext context,
        LinkMaintenancePredictiveRiskTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenancePredictiveRiskWriteResult> TransitionFollowUpAsync(
        MaintenanceOperationContext context,
        TransitionMaintenancePredictiveRiskFollowUpCommand command,
        CancellationToken cancellationToken = default);
}
