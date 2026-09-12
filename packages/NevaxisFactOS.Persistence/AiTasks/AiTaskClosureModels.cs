namespace NevaxisFactOS.Persistence.AiTasks;

public sealed record AiTaskEvidenceReadModel(
    Guid Id,
    Guid TaskId,
    string EvidenceType,
    string Title,
    string? Description,
    string? ReferenceMetadataJson,
    string? SourceModule,
    string? SourceEntityId,
    Guid SubmittedByMembershipId,
    string SubmittedByLabel,
    DateTimeOffset SubmittedAtUtc);

public sealed record AiTaskClosureReadModel(
    string ClosureState,
    string? ResolutionSummary,
    string? ResolutionCode,
    string? ClosureReason,
    bool ClosureApprovalRequired,
    DateTimeOffset? ClosureRequestedAtUtc,
    Guid? ClosureRequestedByMembershipId,
    DateTimeOffset? ClosureApprovedAtUtc,
    Guid? ClosureApprovedByMembershipId,
    DateTimeOffset? ClosureRejectedAtUtc,
    Guid? ClosureRejectedByMembershipId,
    string? ClosureRejectionReason,
    DateTimeOffset? ReopenedAtUtc,
    Guid? ReopenedByMembershipId,
    string? ReopenReason,
    DateTimeOffset? FinalClosedAtUtc);

public sealed record AiTaskClosurePolicyReadModel(
    string PolicyKey,
    string Name,
    int MinimumEvidenceCount,
    IReadOnlyList<string> RequiredEvidenceTypes,
    bool ApprovalRequired,
    bool FourEyesRequired,
    bool ResolutionSummaryRequired);

public sealed record AiTaskClosureValidationIssue(
    string Code,
    string Message);

public sealed record AiTaskClosureValidationResult(
    bool IsValid,
    IReadOnlyList<AiTaskClosureValidationIssue> Issues)
{
    public static AiTaskClosureValidationResult Success() =>
        new(true, Array.Empty<AiTaskClosureValidationIssue>());

    public static AiTaskClosureValidationResult Fail(params AiTaskClosureValidationIssue[] issues) =>
        new(false, issues);
}

public sealed record SubmitAiTaskEvidenceCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string EvidenceType,
    string Title,
    string? Description,
    string? ReferenceMetadataJson,
    string? SourceModule,
    string? SourceEntityId);

public sealed record UpdateAiTaskResolutionCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string ResolutionSummary,
    string ResolutionCode,
    string? ClosureReason);

public sealed record RequestAiTaskClosureCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string? Note);

public sealed record ApproveAiTaskClosureCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string? Note);

public sealed record RejectAiTaskClosureCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string RejectionReason);

public sealed record ReopenAiTaskCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string ReopenReason);
