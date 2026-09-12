namespace NevaxisFactOS.Persistence.Production;

public sealed record ProductionFollowUpAuditEventReadModel(
    string EventType,
    string PayloadJson,
    string Actor,
    DateTimeOffset OccurredAtUtc);

public sealed record ProductionFollowUpWorkspaceReadModel(
    string AutomationDisclaimer,
    int RequiresReviewCount,
    int AcknowledgedCount,
    int TaskLinkedCount,
    int BlockedOrDelayedCount,
    int DataReviewCount,
    bool CanRead,
    bool CanManage,
    bool CanCreateTask);

public sealed record ProductionFollowUpRowReadModel(
    Guid Id,
    Guid ProductionOrderId,
    string ProductionOrderReference,
    string ExceptionType,
    string Severity,
    string ExceptionStatus,
    string Message,
    DateTimeOffset RaisedAtUtc,
    string FollowUpStatus,
    Guid? LinkedAiTaskId,
    uint Version,
    bool CanCreateTaskDraft,
    string TaskDraftPurpose,
    bool HasInsufficientEvidence,
    bool IsBlockedOrDelayed);

public sealed record ProductionFollowUpDetailReadModel(
    Guid Id,
    Guid ProductionOrderId,
    string ProductionOrderReference,
    string ProductionOrderTitle,
    string ProductionOrderStatus,
    string? MachineReference,
    string? MoldReference,
    string? ProductReference,
    string ExceptionType,
    string Severity,
    string ExceptionStatus,
    string Message,
    DateTimeOffset RaisedAtUtc,
    string RaisedBySubjectId,
    string FollowUpStatus,
    Guid? LinkedAiTaskId,
    uint Version,
    bool CanCreateTaskDraft,
    string TaskDraftPurpose,
    bool HasInsufficientEvidence,
    string EvidenceSummary,
    IReadOnlyList<ProductionFollowUpAuditEventReadModel> AuditEvents);

public sealed record LinkProductionFollowUpTaskCommand(Guid ExceptionId, Guid AiTaskId);

public sealed record TransitionProductionFollowUpCommand(
    Guid ExceptionId,
    uint ExpectedVersion,
    string TargetFollowUpStatus,
    string? Note);

public sealed class ProductionFollowUpWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public bool IdempotentReplay { get; init; }
    public ProductionFollowUpDetailReadModel? Detail { get; init; }

    public static ProductionFollowUpWriteResult Success(
        Guid entityId,
        bool idempotentReplay = false,
        ProductionFollowUpDetailReadModel? detail = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            IdempotentReplay = idempotentReplay,
            Detail = detail,
        };

    public static ProductionFollowUpWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}
