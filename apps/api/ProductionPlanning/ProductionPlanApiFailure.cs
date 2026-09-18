using NevaxisZentra.Persistence.ProductionPlanning;

namespace NevaxisZentra.Api.ProductionPlanning;

public static class ProductionPlanApiFailureCodes
{
    public const string InvalidRequest = "invalid-request";
    public const string ValidationFailed = "validation-failed";
    public const string Unauthenticated = "unauthenticated";
    public const string MembershipMissing = "membership-missing";
    public const string FacilityAccessDenied = "facility-access-denied";
    public const string PermissionDenied = "permission-denied";
    public const string NotFound = "not-found";
    public const string PlanNotEditable = "plan-not-editable";
    public const string ReferenceCodeConflict = "reference-code-conflict";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string AuditFailure = "audit-failure";
    public const string SourceUnavailable = "source-unavailable";
    public const string InvalidRow = "invalid-row";
    public const string DataIntegrity = "data-integrity";
    public const string UnexpectedError = "unexpected-error";
    public const string ScheduleBlockedByRisk = "schedule-blocked-by-risk";
    public const string TransferAlreadyRequested = "transfer-already-requested";
    public const string TransferAlreadySucceeded = "transfer-already-succeeded";
    public const string TransferNotRetryable = "transfer-not-retryable";
    public const string AdapterNotFound = "adapter-not-found";
}

public sealed record ProductionPlanWriteSuccessResponse(
    Guid Id,
    string ReferenceCode,
    string Status,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProductionPlanReadOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    string AccessDecision,
    string EndpointAction,
    DateTimeOffset EvaluatedAtUtc,
    Guid? ProductionPlanId,
    Guid? CorrelationId);
