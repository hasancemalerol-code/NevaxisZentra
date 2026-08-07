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
    public const string InvalidQuantity = "invalid-quantity";
    public const string InvalidScrapReason = "invalid-scrap-reason";
    public const string ProductionEntryNotFound = "production-entry-not-found";
    public const string ProductionEntryAlreadyReversed = "production-entry-already-reversed";
    public const string ProductionTargetNotReached = "production-target-not-reached";
    public const string CompletionReasonRequired = "completion-reason-required";
    public const string CommentRequired = "comment-required";
}

public sealed record QuantitySummaryDto(
    decimal? PlannedQuantity,
    string? PlannedQuantityUnit,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    decimal GrossProducedQuantity,
    decimal? RemainingQuantity,
    decimal? CompletionPercentage,
    DateTimeOffset? LastEntryAtUtc);

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
    decimal? PlannedQuantity,
    decimal GoodQuantity,
    decimal ScrapQuantity,
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
    QuantitySummaryDto QuantitySummary,
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

public sealed record ExecutionEntryWriteSuccessDto(
    Guid EntryId,
    uint OrderVersion);

public sealed record ProductionExecutionEntryDto(
    Guid Id,
    string EntryType,
    decimal Quantity,
    string Unit,
    string? ReasonCode,
    string? Comment,
    DateTimeOffset RecordedAtUtc,
    string RecordedBySubjectId,
    bool IsReversed,
    Guid? ReversesEntryId);

public sealed record ConsumeHandoffApiRequest(uint ExpectedHandoffVersion);

public sealed record StartProductionOrderApiRequest(uint ExpectedVersion);

public sealed record PauseProductionOrderApiRequest(uint ExpectedVersion, string Reason);

public sealed record ResumeProductionOrderApiRequest(uint ExpectedVersion);

public sealed record CompleteProductionOrderApiRequest(
    uint ExpectedVersion,
    bool AllowShortCompletion = false,
    string? CompletionReason = null);

public sealed record RecordGoodProductionApiRequest(
    uint ExpectedVersion,
    decimal Quantity,
    string? Comment);

public sealed record RecordScrapApiRequest(
    uint ExpectedVersion,
    decimal Quantity,
    string ReasonCode,
    string? Comment);

public sealed record ReverseEntryApiRequest(
    uint ExpectedVersion,
    string Reason);
