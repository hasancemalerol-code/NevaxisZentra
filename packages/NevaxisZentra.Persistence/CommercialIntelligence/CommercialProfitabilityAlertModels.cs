namespace NevaxisZentra.Persistence.CommercialIntelligence;

public sealed record CommercialProfitabilityAlertListFilters(
    int Page = 1,
    int PageSize = 25,
    string? Category = null,
    string? Severity = null,
    string? LifecycleStatus = null,
    string? EvidenceStatus = null,
    string? ComparisonType = null);

public sealed record CommercialProfitabilityAlertSummaryReadModel(
    Guid Id,
    string AlertReference,
    string AlertCategory,
    string Severity,
    string LifecycleStatus,
    Guid SourceComparisonId,
    string SourceComparisonType,
    string Title,
    string EvidenceSummary,
    string EvidenceStatus,
    string ComparisonOutcome,
    string? LeadingDriverCode,
    Guid? LinkedAiTaskId,
    DateTimeOffset MaterializedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record CommercialProfitabilityAlertAuditEventReadModel(
    Guid Id,
    string EventType,
    string PayloadJson,
    string ActorSubjectId,
    DateTimeOffset OccurredAtUtc);

public sealed record CommercialProfitabilityAlertDetailReadModel(
    Guid Id,
    string AlertReference,
    string AlertCategory,
    string Severity,
    string LifecycleStatus,
    Guid SourceComparisonId,
    string SourceComparisonType,
    string Title,
    string EvidenceSummary,
    string EvidenceStatus,
    string ComparisonOutcome,
    string? LeadingDriverCode,
    string SourceSnapshotJson,
    Guid? LinkedAiTaskId,
    string RuleSetVersion,
    string SourceProvenance,
    DateTimeOffset MaterializedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version,
    CommercialProfitabilityComparisonDetailReadModel SourceComparison,
    IReadOnlyList<CommercialProfitabilityAlertAuditEventReadModel> AuditEvents);

public sealed record CommercialProfitabilityAlertWorkspaceReadModel(
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    int ActiveCount,
    int HighOrCriticalCount,
    int AcknowledgedCount,
    int TaskCreatedCount,
    int EvidenceReviewCount,
    bool CanRead,
    bool CanManage,
    bool CanCreateTask);

public sealed record MaterializeCommercialProfitabilityAlertsCommand(
    string? IdempotencyKey = null);

public sealed record TransitionCommercialProfitabilityAlertCommand(
    Guid AlertId,
    uint ExpectedVersion,
    string TargetLifecycleStatus,
    string? Note = null);

public sealed record LinkCommercialProfitabilityAlertTaskCommand(
    Guid AlertId,
    uint ExpectedVersion,
    Guid AiTaskId);

public sealed record CommercialProfitabilityAlertMaterializationResult(
    int CreatedCount,
    int RefreshedCount,
    int SkippedCount,
    IReadOnlyList<Guid> AlertIds);

public sealed class CommercialProfitabilityAlertWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid EntityId { get; init; }
    public uint Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public CommercialProfitabilityAlertDetailReadModel? Alert { get; init; }
    public CommercialProfitabilityAlertMaterializationResult? Materialization { get; init; }

    public static CommercialProfitabilityAlertWriteResult Success(
        Guid entityId,
        uint version,
        bool idempotentReplay = false,
        CommercialProfitabilityAlertDetailReadModel? alert = null,
        CommercialProfitabilityAlertMaterializationResult? materialization = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
            Alert = alert,
            Materialization = materialization,
        };

    public static CommercialProfitabilityAlertWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}

public interface ICommercialProfitabilityAlertRepository
{
    Task<CommercialProfitabilityAlertWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialProfitabilityAlertSummaryReadModel>> ListAlertsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityAlertListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityAlertDetailReadModel?> GetAlertByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid alertId,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityAlertWriteResult> MaterializeAlertsAsync(
        CommercialIntelligenceOperationContext context,
        MaterializeCommercialProfitabilityAlertsCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityAlertWriteResult> TransitionAlertAsync(
        CommercialIntelligenceOperationContext context,
        TransitionCommercialProfitabilityAlertCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityAlertWriteResult> LinkTaskAsync(
        CommercialIntelligenceOperationContext context,
        LinkCommercialProfitabilityAlertTaskCommand command,
        CancellationToken cancellationToken = default);
}
