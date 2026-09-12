namespace NevaxisFactOS.Persistence.Material;

public sealed record MaterialAvailabilityRiskFollowUpAuditEventReadModel(
    string EventType,
    string PayloadJson,
    string Actor,
    DateTimeOffset OccurredAtUtc);

public sealed record MaterialAvailabilityRiskFollowUpDetailReadModel(
    MaterialAvailabilityRiskDetailReadModel Assessment,
    string FollowUpStatus,
    Guid? LinkedAiTaskId,
    bool CanCreateTaskDraft,
    string TaskDraftPurpose,
    bool HasInsufficientEvidence,
    string EvidenceSummary,
    IReadOnlyList<MaterialAvailabilityRiskFollowUpAuditEventReadModel> AuditEvents);

public sealed record LinkMaterialAvailabilityRiskFollowUpTaskCommand(Guid AssessmentId, Guid AiTaskId);

public sealed record TransitionMaterialAvailabilityRiskFollowUpCommand(
    Guid AssessmentId,
    uint ExpectedVersion,
    string TargetFollowUpStatus,
    string? Note);

public sealed class MaterialAvailabilityRiskFollowUpWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public bool IdempotentReplay { get; init; }
    public MaterialAvailabilityRiskFollowUpDetailReadModel? Detail { get; init; }

    public static MaterialAvailabilityRiskFollowUpWriteResult Success(
        Guid entityId,
        bool idempotentReplay = false,
        MaterialAvailabilityRiskFollowUpDetailReadModel? detail = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            IdempotentReplay = idempotentReplay,
            Detail = detail,
        };

    public static MaterialAvailabilityRiskFollowUpWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}
