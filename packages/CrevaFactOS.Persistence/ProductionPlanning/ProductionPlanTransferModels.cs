namespace CrevaFactOS.Persistence.ProductionPlanning;

public sealed record ProductionPlanTransferContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record CreateTransferRequestCommand(
    Guid ProductionPlanId,
    uint ExpectedPlanVersion,
    string TargetSystem);

public sealed record ExecuteTransferCommand(
    Guid ProductionPlanId,
    Guid TransferRequestId,
    uint ExpectedPlanVersion);

public static class ProductionPlanTransferFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string ValidationFailed = "validation-failed";
    public const string NotFound = "not-found";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string SourceUnavailable = "source-unavailable";
    public const string AuditFailure = "audit-failure";
    public const string TransferAlreadyRequested = "transfer-already-requested";
    public const string TransferAlreadySucceeded = "transfer-already-succeeded";
    public const string TransferNotRetryable = "transfer-not-retryable";
    public const string AdapterNotFound = "adapter-not-found";
    public const string UnexpectedError = "unexpected-error";
}

public sealed record ProductionPlanTransferRequestResult
{
    public required bool Succeeded { get; init; }
    public Guid? TransferRequestId { get; init; }
    public Guid? ProductionPlanId { get; init; }
    public string? TargetSystem { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? RequestedAtUtc { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public bool IsIdempotentDuplicate { get; init; }

    public static ProductionPlanTransferRequestResult Success(
        Guid transferRequestId,
        Guid productionPlanId,
        string targetSystem,
        string status,
        DateTimeOffset requestedAtUtc,
        bool isIdempotentDuplicate = false) =>
        new()
        {
            Succeeded = true,
            TransferRequestId = transferRequestId,
            ProductionPlanId = productionPlanId,
            TargetSystem = targetSystem,
            Status = status,
            RequestedAtUtc = requestedAtUtc,
            IsIdempotentDuplicate = isIdempotentDuplicate,
        };

    public static ProductionPlanTransferRequestResult Fail(
        string failureCode,
        string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = failureCode,
            FailureReason = reason,
        };
}

public sealed record ProductionPlanTransferExecuteResult
{
    public required bool Succeeded { get; init; }
    public Guid? TransferRequestId { get; init; }
    public string? FinalStatus { get; init; }
    public string? ExternalReference { get; init; }
    public bool? Retryable { get; init; }
    public string? SafeFailureMessage { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public bool ReconciliationRequired { get; init; }

    public static ProductionPlanTransferExecuteResult Success(
        Guid transferRequestId,
        string? externalReference) =>
        new()
        {
            Succeeded = true,
            TransferRequestId = transferRequestId,
            FinalStatus = "succeeded",
            ExternalReference = externalReference,
        };

    public static ProductionPlanTransferExecuteResult Failed(
        Guid transferRequestId,
        bool retryable,
        string? safeMessage) =>
        new()
        {
            Succeeded = true,
            TransferRequestId = transferRequestId,
            FinalStatus = "failed",
            Retryable = retryable,
            SafeFailureMessage = safeMessage,
        };

    public static ProductionPlanTransferExecuteResult UncertainResult(
        Guid transferRequestId) =>
        new()
        {
            Succeeded = true,
            TransferRequestId = transferRequestId,
            FinalStatus = "uncertain",
            ReconciliationRequired = true,
        };

    public static ProductionPlanTransferExecuteResult Fail(
        string failureCode,
        string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = failureCode,
            FailureReason = reason,
        };
}
