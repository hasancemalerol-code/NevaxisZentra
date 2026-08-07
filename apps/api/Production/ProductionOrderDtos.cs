namespace CrevaFactOS.Api.Production;

public static class ProductionExecutionApiFailureCodes
{
    public const string InvalidRequest = "invalid-request";
    public const string Unauthenticated = "unauthenticated";
    public const string MembershipMissing = "membership-missing";
    public const string FacilityAccessDenied = "facility-access-denied";
    public const string PermissionDenied = "permission-denied";
    public const string NotFound = "not-found";
    public const string HandoffNotFound = "handoff-not-found";
    public const string HandoffNotReady = "handoff-not-ready";
    public const string HandoffAlreadyConsumed = "handoff-already-consumed";
    public const string HandoffInvalid = "handoff-invalid";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string SourceUnavailable = "source-unavailable";
    public const string UnexpectedError = "unexpected-error";
}

public sealed record ProductionOrderSummaryDto(
    Guid Id,
    Guid FacilityId,
    Guid SourceProductionPlanId,
    string ReferenceCode,
    string Title,
    string Status,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    DateTimeOffset? ActualStartAtUtc,
    string? MachineReference,
    string? MoldReference,
    string? ShiftName,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProductionOrderDetailDto(
    Guid Id,
    Guid FacilityId,
    Guid ProductionPlanHandoffId,
    Guid SourceProductionPlanId,
    uint SourceProductionPlanVersion,
    string ReferenceCode,
    string Title,
    string Status,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    DateTimeOffset? ActualStartAtUtc,
    DateTimeOffset? ActualEndAtUtc,
    string? ShiftName,
    string? MachineReference,
    string? MoldReference,
    string? ProductReference,
    string CreatedBySubjectId,
    string LastModifiedBySubjectId,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProductionOrderWriteSuccessDto(
    Guid Id,
    string ReferenceCode,
    string Status,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ConsumeHandoffApiRequest(uint ExpectedHandoffVersion);

public sealed record StartProductionOrderApiRequest(uint ExpectedVersion);

public sealed record PauseProductionOrderApiRequest(uint ExpectedVersion, string Reason);

public sealed record ResumeProductionOrderApiRequest(uint ExpectedVersion);

public sealed record CompleteProductionOrderApiRequest(uint ExpectedVersion);
