namespace CrevaFactOS.Persistence.ProductionPlanning;

public sealed record ProductionPlanHandoffContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record PrepareProductionPlanHandoffCommand(
    Guid ProductionPlanId,
    uint ExpectedVersion);

public static class ProductionPlanHandoffFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string NotFound = "not-found";
    public const string NotEligible = "handoff-not-eligible";
    public const string NotScheduled = "plan-not-scheduled";
    public const string BlockingRisk = "blocking-risk-present";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string AlreadyPrepared = "handoff-already-prepared";
    public const string AuditFailure = "audit-failure";
    public const string SourceUnavailable = "source-unavailable";
    public const string UnexpectedError = "unexpected-error";
}

public sealed record ProductionPlanHandoffResult
{
    public required bool Succeeded { get; init; }
    public Guid? HandoffId { get; init; }
    public Guid? ProductionPlanId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? Status { get; init; }
    public uint? HandoffVersion { get; init; }
    public DateTimeOffset? PreparedAtUtc { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionPlanHandoffResult Success(
        Guid handoffId,
        Guid productionPlanId,
        string referenceCode,
        string status,
        uint handoffVersion,
        DateTimeOffset preparedAtUtc) =>
        new()
        {
            Succeeded = true,
            HandoffId = handoffId,
            ProductionPlanId = productionPlanId,
            ReferenceCode = referenceCode,
            Status = status,
            HandoffVersion = handoffVersion,
            PreparedAtUtc = preparedAtUtc,
        };

    public static ProductionPlanHandoffResult Fail(
        string failureCode,
        string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = failureCode,
            FailureReason = reason,
        };
}
