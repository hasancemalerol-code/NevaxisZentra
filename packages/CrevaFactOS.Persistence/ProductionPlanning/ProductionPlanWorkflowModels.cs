namespace CrevaFactOS.Persistence.ProductionPlanning;

public sealed record ProductionPlanWorkflowContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record SubmitProductionPlanForReviewCommand(
    Guid ProductionPlanId,
    uint ExpectedVersion,
    string? Comment);

public sealed record RequestProductionPlanRevisionCommand(
    Guid ProductionPlanId,
    uint ExpectedVersion,
    string Reason);

public sealed record ApproveProductionPlanCommand(
    Guid ProductionPlanId,
    uint ExpectedVersion,
    string? Note);

public sealed record RejectProductionPlanCommand(
    Guid ProductionPlanId,
    uint ExpectedVersion,
    string Reason);

public sealed record ScheduleProductionPlanCommand(
    Guid ProductionPlanId,
    uint ExpectedVersion,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    string? ShiftName,
    string? SchedulingComment);

public static class ProductionPlanWorkflowFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string ValidationFailed = "validation-failed";
    public const string NotFound = "not-found";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string SourceUnavailable = "source-unavailable";
    public const string AuditFailure = "audit-failure";
    public const string DataIntegrity = "data-integrity";
    public const string UnexpectedError = "unexpected-error";
    public const string ScheduleBlockedByRisk = "schedule-blocked-by-risk";
    public const string TransferAlreadyRequested = "transfer-already-requested";
    public const string TransferAlreadySucceeded = "transfer-already-succeeded";
    public const string TransferNotRetryable = "transfer-not-retryable";
    public const string SelfApprovalNotAllowed = "self-approval-not-allowed";
    public const string HandoffAlreadyPrepared = "handoff-already-prepared";
    public const string HandoffNotEligible = "handoff-not-eligible";
}

public sealed record ProductionPlanWorkflowFieldError(
    string Field,
    string Code,
    string Message);

public sealed record ProductionPlanWorkflowResult
{
    public required bool Succeeded { get; init; }
    public Guid? ProductionPlanId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? PreviousStatus { get; init; }
    public string? Status { get; init; }
    public uint? Version { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public string? ApprovalStatus { get; init; }
    public DateTimeOffset? PlannedStartAtUtc { get; init; }
    public DateTimeOffset? PlannedEndAtUtc { get; init; }
    public DateTimeOffset? ScheduledAtUtc { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public IReadOnlyList<ProductionPlanWorkflowFieldError>? FieldErrors { get; init; }

    public static ProductionPlanWorkflowResult Success(
        Guid productionPlanId,
        string referenceCode,
        string previousStatus,
        string status,
        uint version,
        DateTimeOffset updatedAtUtc,
        string approvalStatus) =>
        new()
        {
            Succeeded = true,
            ProductionPlanId = productionPlanId,
            ReferenceCode = referenceCode,
            PreviousStatus = previousStatus,
            Status = status,
            Version = version,
            UpdatedAtUtc = updatedAtUtc,
            ApprovalStatus = approvalStatus,
        };

    public static ProductionPlanWorkflowResult SuccessScheduled(
        Guid productionPlanId,
        string referenceCode,
        string previousStatus,
        string status,
        uint version,
        DateTimeOffset updatedAtUtc,
        string approvalStatus,
        DateTimeOffset plannedStartAtUtc,
        DateTimeOffset plannedEndAtUtc,
        DateTimeOffset scheduledAtUtc) =>
        new()
        {
            Succeeded = true,
            ProductionPlanId = productionPlanId,
            ReferenceCode = referenceCode,
            PreviousStatus = previousStatus,
            Status = status,
            Version = version,
            UpdatedAtUtc = updatedAtUtc,
            ApprovalStatus = approvalStatus,
            PlannedStartAtUtc = plannedStartAtUtc,
            PlannedEndAtUtc = plannedEndAtUtc,
            ScheduledAtUtc = scheduledAtUtc,
        };

    public static ProductionPlanWorkflowResult Fail(
        string failureCode,
        string reason,
        IReadOnlyList<ProductionPlanWorkflowFieldError>? fieldErrors = null) =>
        new()
        {
            Succeeded = false,
            FailureCode = failureCode,
            FailureReason = reason,
            FieldErrors = fieldErrors,
        };
}
