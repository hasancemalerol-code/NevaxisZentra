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

    // 3L
    public const string ActiveDowntimeExists = "active-downtime-exists";
    public const string NoActiveDowntime = "no-active-downtime";
    public const string NoActiveSetup = "no-active-setup";
    public const string DowntimeNotFound = "downtime-not-found";
    public const string DowntimeAlreadyEnded = "downtime-already-ended";
    public const string ActiveSetupExists = "active-setup-exists";
    public const string SetupSessionNotFound = "setup-session-not-found";
    public const string SetupSessionAlreadyCompleted = "setup-session-already-completed";
    public const string InvalidDowntimeReason = "invalid-downtime-reason";
    public const string InvalidSetupType = "invalid-setup-type";
    public const string InvalidCycleDuration = "invalid-cycle-duration";
    public const string InvalidCycleQuantity = "invalid-cycle-quantity";

    // 4L
    public const string ExceptionNotFound = "exception-not-found";
    public const string ExceptionAlreadyResolved = "exception-already-resolved";
    public const string InvalidExceptionType = "invalid-exception-type";
    public const string InvalidExceptionSeverity = "invalid-exception-severity";
    public const string ResolutionNoteRequired = "resolution-note-required";
    public const string CriticalExceptionOpen = "critical-exception-open";
    public const string ProductionStateInconsistent = "production-state-inconsistent";
    public const string DowntimeNotActive = "downtime-not-active";
    public const string SetupSessionNotActive = "setup-session-not-active";
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
    DateTimeOffset UpdatedAtUtc,
    long TotalDowntimeSeconds,
    int ManualCycleCount);

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
    DateTimeOffset UpdatedAtUtc,
    long TotalDowntimeSeconds,
    long TotalSetupSeconds,
    int ManualCycleCount,
    long TotalManualCycleDurationMilliseconds,
    DateTimeOffset? LastCycleAtUtc);

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

// ── 3L ─────────────────────────────────────────────────────────────────────────

public sealed record DowntimeSummaryDto(
    Guid Id,
    string Category,
    string ReasonCode,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    long? DurationSeconds,
    string? Comment,
    string StartedBySubjectId,
    string? EndedBySubjectId,
    bool IsActive);

public sealed record SetupSessionSummaryDto(
    Guid Id,
    string SetupType,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long? DurationSeconds,
    string? Comment,
    string StartedBySubjectId,
    string? CompletedBySubjectId,
    bool IsActive);

public sealed record CycleEntrySummaryDto(
    Guid Id,
    int CycleNumber,
    DateTimeOffset CycleStartedAtUtc,
    DateTimeOffset CycleEndedAtUtc,
    long CycleDurationMilliseconds,
    decimal? QuantityProduced,
    string RecordedBySubjectId,
    DateTimeOffset RecordedAtUtc,
    string Source,
    string? Comment);

public sealed record ProductionRuntimeSummaryDto(
    DateTimeOffset? ActualStartAtUtc,
    DateTimeOffset? ActualEndAtUtc,
    long? CurrentElapsedSeconds,
    long TotalDowntimeSeconds,
    long TotalSetupSeconds,
    long? NetRunningSeconds,
    int ManualCycleCount,
    long? AverageManualCycleMilliseconds,
    DateTimeOffset? LastCycleAtUtc,
    DowntimeSummaryDto? ActiveDowntime,
    SetupSessionSummaryDto? ActiveSetup);

public sealed record ProductionOperationsWriteSuccessDto(
    Guid ResourceId,
    uint OrderVersion);

public sealed record StartDowntimeApiRequest(
    uint ExpectedVersion,
    string ReasonCode,
    string? Comment);

public sealed record EndDowntimeApiRequest(uint ExpectedVersion);

public sealed record StartSetupApiRequest(
    uint ExpectedVersion,
    string SetupType,
    string? Comment);

public sealed record CompleteSetupApiRequest(uint ExpectedVersion);

public sealed record RecordManualCycleApiRequest(
    uint ExpectedVersion,
    long CycleDurationMilliseconds,
    decimal? QuantityProduced,
    string? Comment);

public sealed record ConsumeHandoffApiRequest(uint ExpectedHandoffVersion);

// ── 4L ─────────────────────────────────────────────────────────────────────────

public sealed record ExceptionSummaryDto(
    Guid Id,
    string ExceptionType,
    string Severity,
    string Status,
    string Message,
    DateTimeOffset RaisedAtUtc,
    string RaisedBySubjectId,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolvedBySubjectId,
    string? ResolutionNote,
    uint Version);

public sealed record ProductionExecutionHealthSummaryDto(
    string Status,
    int OpenExceptionCount,
    int CriticalExceptionCount,
    bool HasActiveDowntime,
    bool DowntimeIsStale,
    bool HasActiveSetup,
    bool SetupIsStale,
    decimal? QuantityProgress,
    bool ScheduleOverrun,
    DateTimeOffset? LastOperationalActivityAtUtc,
    bool RequiresAttention);

public sealed record RaiseExceptionApiRequest(
    uint ExpectedVersion,
    string ExceptionType,
    string Severity,
    string Message);

public sealed record AcknowledgeExceptionApiRequest(uint ExpectedVersion);

public sealed record ResolveExceptionApiRequest(
    uint ExpectedVersion,
    string ResolutionNote);

public sealed record ExceptionWriteSuccessDto(
    Guid ExceptionId,
    uint OrderVersion,
    uint ExceptionVersion);

public sealed record RecoverDowntimeApiRequest(
    uint ExpectedVersion,
    string Resolution,
    bool ResumeProduction = false);

public sealed record RecoverSetupApiRequest(
    uint ExpectedVersion,
    string Resolution);

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
