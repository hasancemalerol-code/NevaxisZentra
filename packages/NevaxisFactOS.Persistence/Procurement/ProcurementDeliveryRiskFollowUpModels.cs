namespace NevaxisFactOS.Persistence.Procurement;

public sealed record ProcurementDeliveryRiskFollowUpAuditEventReadModel(
    string EventType,
    string PayloadJson,
    string Actor,
    DateTimeOffset OccurredAtUtc);

public sealed record ProcurementDeliveryRiskFollowUpDetailReadModel(
    ProcurementDeliveryRiskDetailReadModel Assessment,
    string FollowUpStatus,
    Guid? LinkedAiTaskId,
    bool CanCreateTaskDraft,
    string TaskDraftPurpose,
    bool HasInsufficientEvidence,
    string EvidenceSummary,
    IReadOnlyList<ProcurementDeliveryRiskFollowUpAuditEventReadModel> AuditEvents);

public sealed record LinkProcurementDeliveryRiskFollowUpTaskCommand(Guid AssessmentId, Guid AiTaskId);

public sealed record TransitionProcurementDeliveryRiskFollowUpCommand(
    Guid AssessmentId,
    uint ExpectedVersion,
    string TargetFollowUpStatus,
    string? Note);

public sealed class ProcurementDeliveryRiskFollowUpWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public bool IdempotentReplay { get; init; }
    public ProcurementDeliveryRiskFollowUpDetailReadModel? Detail { get; init; }

    public static ProcurementDeliveryRiskFollowUpWriteResult Success(
        Guid entityId,
        bool idempotentReplay = false,
        ProcurementDeliveryRiskFollowUpDetailReadModel? detail = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            IdempotentReplay = idempotentReplay,
            Detail = detail,
        };

    public static ProcurementDeliveryRiskFollowUpWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}
