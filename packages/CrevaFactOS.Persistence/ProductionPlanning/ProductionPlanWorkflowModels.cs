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
