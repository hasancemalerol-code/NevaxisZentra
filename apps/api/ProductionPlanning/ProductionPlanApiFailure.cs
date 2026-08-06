namespace CrevaFactOS.Api.ProductionPlanning;

public static class ProductionPlanApiFailureCodes
{
    public const string InvalidRequest = "invalid-request";
    public const string Unauthenticated = "unauthenticated";
    public const string MembershipMissing = "membership-missing";
    public const string FacilityAccessDenied = "facility-access-denied";
    public const string PermissionDenied = "permission-denied";
    public const string NotFound = "not-found";
    public const string SourceUnavailable = "source-unavailable";
    public const string InvalidRow = "invalid-row";
    public const string DataIntegrity = "data-integrity";
    public const string UnexpectedError = "unexpected-error";
}

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
