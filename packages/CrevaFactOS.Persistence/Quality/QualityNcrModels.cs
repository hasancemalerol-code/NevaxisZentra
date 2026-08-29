namespace CrevaFactOS.Persistence.Quality;

public static class QualityNcrFailureCodes
{
    public const string DuplicateNonconformance = "duplicate-nonconformance";
    public const string BlockingQualityHold = "blocking-quality-hold";
    public const string CloseNotAllowed = "close-not-allowed";
    public const string ResolveNotAllowed = "resolve-not-allowed";
    public const string DispositionRequired = "disposition-required";
    public const string CorrectiveActionIncomplete = "corrective-action-incomplete";
    public const string ActiveHold = "active-hold";
}

public sealed record CreateQualityNonconformanceCommand(
    Guid? ProductionOrderId,
    Guid? QualityInspectionId,
    Guid? QualityInspectionResultId,
    string? ProductReference,
    string? OperationReference,
    string Title,
    string Description,
    string? Category,
    string Severity,
    string? OwnerSubjectId);

public sealed record UpdateQualityNonconformanceCommand(
    Guid NcrId,
    uint ExpectedVersion,
    string Title,
    string Description,
    string? Category,
    string Severity,
    string Status,
    string? OwnerSubjectId);

public sealed record QualityNonconformanceLifecycleCommand(
    Guid NcrId,
    uint ExpectedVersion,
    string? Note);

public sealed record PlaceQualityHoldCommand(
    Guid NcrId,
    uint ExpectedVersion,
    string Scope,
    string Reason);

public sealed record ReleaseQualityHoldCommand(
    Guid NcrId,
    uint ExpectedVersion,
    string ReleaseReason);

public sealed record DecideQualityDispositionCommand(
    Guid NcrId,
    uint ExpectedVersion,
    string DispositionType,
    string? Note);

public sealed record CreateQualityCorrectiveActionCommand(
    Guid NcrId,
    string Title,
    string Description,
    string? OwnerSubjectId,
    DateTimeOffset? DueAtUtc);

public sealed record QualityCorrectiveActionLifecycleCommand(
    Guid CorrectiveActionId,
    uint ExpectedVersion,
    string? Note);

public sealed record QualityNonconformanceSummaryReadModel(
    Guid Id,
    string NcrNumber,
    string Title,
    string Severity,
    string Status,
    string HoldStatus,
    string DispositionStatus,
    string? DispositionType,
    Guid? ProductionOrderId,
    string? ProductionOrderReference,
    string? ProductReference,
    string? OwnerSubjectId,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset CreatedAtUtc,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record QualityNonconformanceDetailReadModel(
    Guid Id,
    string NcrNumber,
    string Title,
    string Description,
    string? Category,
    string Severity,
    string Status,
    string HoldStatus,
    string DispositionStatus,
    string? DispositionType,
    string? DispositionNote,
    string? DispositionDecidedBySubjectId,
    DateTimeOffset? DispositionDecidedAtUtc,
    Guid? ProductionOrderId,
    ProductionOrderQualityContext? ProductionContext,
    Guid? QualityInspectionId,
    string? QualityInspectionNumber,
    Guid? QualityInspectionResultId,
    string? ProductReference,
    string? OperationReference,
    string DetectedBySubjectId,
    string? OwnerSubjectId,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    string? ClosedBySubjectId,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    QualityHoldReadModel? ActiveHold,
    IReadOnlyList<QualityCorrectiveActionReadModel> CorrectiveActions);

public sealed record QualityHoldReadModel(
    Guid Id,
    string Scope,
    string Status,
    string Reason,
    DateTimeOffset PlacedAtUtc,
    string PlacedBySubjectId,
    DateTimeOffset? ReleasedAtUtc,
    string? ReleasedBySubjectId,
    string? ReleaseReason,
    uint Version);

public sealed record QualityCorrectiveActionReadModel(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string? OwnerSubjectId,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? CompletionNote,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProductionQualitySignalReadModel(
    bool Available,
    bool QualityHoldActive,
    bool Blocking,
    int OpenNcrCount,
    int CriticalNcrCount,
    string? ReleaseStatus,
    string? Reason);

public sealed record QualityNcrWorkspaceExtensionReadModel(
    int OpenNcrCount,
    int ActiveHoldCount,
    int ActionPendingCount);

public sealed record QualityNcrFollowUpAuditEventReadModel(
    string EventType,
    string PayloadJson,
    string Actor,
    DateTimeOffset OccurredAtUtc);

public sealed record QualityNcrFollowUpWorkspaceReadModel(
    string AutomationDisclaimer,
    int RequiresReviewCount,
    int AcknowledgedCount,
    int TaskLinkedCount,
    int HighPriorityCount,
    int DataReviewCount,
    bool CanRead,
    bool CanManage,
    bool CanCreateTask);

public sealed record QualityNcrFollowUpRowReadModel(
    Guid Id,
    string NcrNumber,
    string Title,
    string Severity,
    string Status,
    string FollowUpStatus,
    string HoldStatus,
    string DispositionStatus,
    string? ProductReference,
    Guid? ProductionOrderId,
    string? ProductionOrderReference,
    DateTimeOffset DetectedAtUtc,
    Guid? LinkedAiTaskId,
    bool CanCreateTaskDraft,
    string TaskDraftPurpose,
    bool HasInsufficientEvidence);

public sealed record QualityNcrFollowUpDetailReadModel(
    QualityNonconformanceDetailReadModel Detail,
    string FollowUpStatus,
    Guid? LinkedAiTaskId,
    bool CanCreateTaskDraft,
    string TaskDraftPurpose,
    bool HasInsufficientEvidence,
    string EvidenceSummary,
    IReadOnlyList<QualityNcrFollowUpAuditEventReadModel> AuditEvents);

public sealed record LinkQualityNcrFollowUpTaskCommand(Guid NcrId, Guid AiTaskId);

public sealed record TransitionQualityNcrFollowUpCommand(
    Guid NcrId,
    uint ExpectedVersion,
    string TargetFollowUpStatus,
    string? Note);

public sealed class QualityNcrFollowUpWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public bool IdempotentReplay { get; init; }
    public QualityNcrFollowUpDetailReadModel? Detail { get; init; }

    public static QualityNcrFollowUpWriteResult Success(
        Guid entityId,
        bool idempotentReplay = false,
        QualityNcrFollowUpDetailReadModel? detail = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            IdempotentReplay = idempotentReplay,
            Detail = detail,
        };

    public static QualityNcrFollowUpWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record LinkedNonconformanceReadModel(
    Guid Id,
    string NcrNumber,
    string Status);
