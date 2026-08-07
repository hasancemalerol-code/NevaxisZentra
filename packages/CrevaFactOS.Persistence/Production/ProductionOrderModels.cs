namespace CrevaFactOS.Persistence.Production;

public sealed record ProductionOrderOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record ConsumeHandoffCommand(
    Guid HandoffId,
    uint ExpectedHandoffVersion);

public sealed record StartProductionOrderCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion);

public sealed record PauseProductionOrderCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    string Reason);

public sealed record ResumeProductionOrderCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion);

public sealed record CompleteProductionOrderCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    bool AllowShortCompletion = false,
    string? CompletionReason = null);

public sealed record RecordGoodProductionCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    decimal Quantity,
    string? Comment);

public sealed record RecordScrapCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    decimal Quantity,
    string ReasonCode,
    string? Comment);

public sealed record ReverseProductionEntryCommand(
    Guid ProductionOrderId,
    Guid EntryId,
    uint ExpectedVersion,
    string Reason);

// ── 3L: Operational Commands ──────────────────────────────────────────────────

public sealed record StartDowntimeCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    string ReasonCode,
    string? Comment);

public sealed record EndDowntimeCommand(
    Guid ProductionOrderId,
    Guid DowntimeId,
    uint ExpectedVersion);

public sealed record StartSetupSessionCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    string SetupType,
    string? Comment);

public sealed record CompleteSetupSessionCommand(
    Guid ProductionOrderId,
    Guid SetupSessionId,
    uint ExpectedVersion);

public sealed record RecordManualCycleCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    long CycleDurationMilliseconds,
    decimal? QuantityProduced,
    string? Comment);

// ── 4L: Exception Commands ────────────────────────────────────────────────────

public sealed record RaiseExceptionCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    string ExceptionType,
    string Severity,
    string Message);

public sealed record AcknowledgeExceptionCommand(
    Guid ProductionOrderId,
    Guid ExceptionId,
    uint ExpectedVersion);

public sealed record ResolveExceptionCommand(
    Guid ProductionOrderId,
    Guid ExceptionId,
    uint ExpectedVersion,
    string ResolutionNote);

// ── 4L: Recovery Commands ─────────────────────────────────────────────────────

public sealed record RecoverDowntimeCommand(
    Guid ProductionOrderId,
    Guid DowntimeId,
    uint ExpectedVersion,
    string Resolution,
    bool ResumeProduction);

public sealed record RecoverSetupSessionCommand(
    Guid ProductionOrderId,
    Guid SetupSessionId,
    uint ExpectedVersion,
    string Resolution);

// ── 3L: Read Models ───────────────────────────────────────────────────────────

public sealed record DowntimeReadModel(
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

public sealed record SetupSessionReadModel(
    Guid Id,
    string SetupType,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long? DurationSeconds,
    string? Comment,
    string StartedBySubjectId,
    string? CompletedBySubjectId,
    bool IsActive);

public sealed record CycleEntryReadModel(
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

/// <summary>
/// Derived runtime summary computed from ProductionOrder denormalized counters
/// and optional active session queries. Elapsed, net running, and average cycle
/// are computed at read time — not persisted.
/// </summary>
public sealed record ProductionRuntimeSummary(
    DateTimeOffset? ActualStartAtUtc,
    DateTimeOffset? ActualEndAtUtc,
    long? CurrentElapsedSeconds,
    long TotalDowntimeSeconds,
    long TotalSetupSeconds,
    long? NetRunningSeconds,
    int ManualCycleCount,
    long? AverageManualCycleMilliseconds,
    DateTimeOffset? LastCycleAtUtc,
    DowntimeReadModel? ActiveDowntime,
    SetupSessionReadModel? ActiveSetup);

// ── 4L: Exception Read Model ──────────────────────────────────────────────────

public sealed record ExceptionReadModel(
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
    Guid CorrelationId,
    uint Version);

// ── 4L: Health Summary Read Model ────────────────────────────────────────────

/// <summary>
/// Derived read-time health summary for a production order.
/// All fields are computed from current state — none are persisted.
/// </summary>
public sealed record ProductionExecutionHealthSummary(
    /// <summary>"normal" | "attention" | "critical"</summary>
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
    /// <summary>True when any signal requires operator attention (stale, critical exception, overrun).</summary>
    bool RequiresAttention);

// ── 3L: Write Result wrappers ─────────────────────────────────────────────────

public sealed record ProductionOperationsWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ResourceId { get; init; }
    public uint? OrderVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionOperationsWriteResult Success(Guid resourceId, uint orderVersion) =>
        new() { Succeeded = true, ResourceId = resourceId, OrderVersion = orderVersion };

    public static ProductionOperationsWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ProductionOperationsListResult<T>
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<T>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
}

public static class ProductionOperationsListResult
{
    public static ProductionOperationsListResult<T> Success<T>(IReadOnlyList<T> items) =>
        new() { Succeeded = true, Items = items };

    public static ProductionOperationsListResult<T> Fail<T>(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ProductionRuntimeSummaryResult
{
    public required bool Succeeded { get; init; }
    public ProductionRuntimeSummary? Summary { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionRuntimeSummaryResult Success(ProductionRuntimeSummary summary) =>
        new() { Succeeded = true, Summary = summary };

    public static ProductionRuntimeSummaryResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public static class ProductionOrderFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string HandoffNotFound = "handoff-not-found";
    public const string HandoffNotReady = "handoff-not-ready";
    public const string HandoffAlreadyConsumed = "handoff-already-consumed";
    public const string HandoffInvalid = "handoff-invalid";
    public const string NotFound = "not-found";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string SourceUnavailable = "source-unavailable";
    public const string DataIntegrity = "data-integrity";
    public const string InvalidQuantity = "invalid-quantity";
    public const string InvalidScrapReason = "invalid-scrap-reason";
    public const string ProductionEntryNotFound = "production-entry-not-found";
    public const string ProductionEntryAlreadyReversed = "production-entry-already-reversed";
    public const string ProductionTargetNotReached = "production-target-not-reached";
    public const string CompletionReasonRequired = "completion-reason-required";
    public const string CommentRequired = "comment-required";

    // 3L additions
    public const string ActiveDowntimeExists = "active-downtime-exists";
    public const string NoActiveDowntime = "no-active-downtime";
    public const string DowntimeNotFound = "downtime-not-found";
    public const string DowntimeWrongOrder = "downtime-wrong-order";
    public const string DowntimeAlreadyEnded = "downtime-already-ended";
    public const string ActiveSetupExists = "active-setup-exists";
    public const string NoActiveSetup = "no-active-setup";
    public const string SetupSessionNotFound = "setup-session-not-found";
    public const string SetupSessionWrongOrder = "setup-session-wrong-order";
    public const string SetupSessionAlreadyCompleted = "setup-session-already-completed";
    public const string InvalidDowntimeReason = "invalid-downtime-reason";
    public const string InvalidSetupType = "invalid-setup-type";
    public const string InvalidCycleDuration = "invalid-cycle-duration";
    public const string InvalidCycleQuantity = "invalid-cycle-quantity";

    // 4L additions
    public const string ExceptionNotFound = "exception-not-found";
    public const string ExceptionAlreadyResolved = "exception-already-resolved";
    public const string InvalidExceptionType = "invalid-exception-type";
    public const string InvalidExceptionSeverity = "invalid-exception-severity";
    public const string ResolutionNoteRequired = "resolution-note-required";
    public const string CriticalExceptionOpen = "critical-exception-open";
    public const string ProductionStateInconsistent = "production-state-inconsistent";
    public const string DowntimeNotActive = "downtime-not-active";
    public const string SetupSessionNotActive = "setup-session-not-active";
    public const string MessageTooLong = "message-too-long";
}

public sealed record ProductionQuantitySummary(
    decimal? PlannedQuantity,
    string? PlannedQuantityUnit,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    decimal GrossProducedQuantity,
    decimal? RemainingQuantity,
    decimal? CompletionPercentage,
    DateTimeOffset? LastEntryAtUtc);

public sealed record ProductionExecutionEntryReadModel(
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

public sealed record ProductionExecutionEntryWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? EntryId { get; init; }
    public uint? OrderVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionExecutionEntryWriteResult Success(Guid entryId, uint orderVersion) =>
        new() { Succeeded = true, EntryId = entryId, OrderVersion = orderVersion };

    public static ProductionExecutionEntryWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ProductionExecutionEntryListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<ProductionExecutionEntryReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionExecutionEntryListResult Success(IReadOnlyList<ProductionExecutionEntryReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ProductionExecutionEntryListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ProductionOrderReadModel(
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
    decimal? PlannedQuantity,
    string? PlannedQuantityUnit,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    DateTimeOffset? LastProductionEntryAtUtc,
    string CreatedBySubjectId,
    string LastModifiedBySubjectId,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string HistoryJson,
    long TotalDowntimeSeconds,
    long TotalSetupSeconds,
    int ManualCycleCount,
    long TotalManualCycleDurationMilliseconds,
    DateTimeOffset? LastCycleAtUtc);

public sealed record ProductionOrderListReadModel(
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

public sealed record ProductionOrderWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ProductionOrderId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? Status { get; init; }
    public uint? Version { get; init; }
    public DateTimeOffset? CreatedAtUtc { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionOrderWriteResult Success(
        Guid orderId,
        string referenceCode,
        string status,
        uint version,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new()
        {
            Succeeded = true,
            ProductionOrderId = orderId,
            ReferenceCode = referenceCode,
            Status = status,
            Version = version,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
        };

    public static ProductionOrderWriteResult Fail(string failureCode, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = failureCode,
            FailureReason = reason,
        };
}

public sealed record ProductionOrderListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<ProductionOrderListReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionOrderListResult Success(IReadOnlyList<ProductionOrderListReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ProductionOrderListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ProductionOrderDetailResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ProductionOrderReadModel? Order { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionOrderDetailResult Success(ProductionOrderReadModel order) =>
        new() { Succeeded = true, Order = order };

    public static ProductionOrderDetailResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ProductionOrderFailureCodes.NotFound };

    public static ProductionOrderDetailResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

// ── 4L: Exception Write Result ────────────────────────────────────────────────

public sealed record ProductionExceptionWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ExceptionId { get; init; }
    public uint? OrderVersion { get; init; }
    public uint? ExceptionVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionExceptionWriteResult Success(Guid exceptionId, uint orderVersion, uint exceptionVersion) =>
        new() { Succeeded = true, ExceptionId = exceptionId, OrderVersion = orderVersion, ExceptionVersion = exceptionVersion };

    public static ProductionExceptionWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

// ── 4L: Health Summary Result ─────────────────────────────────────────────────

public sealed record ProductionHealthSummaryResult
{
    public required bool Succeeded { get; init; }
    public ProductionExecutionHealthSummary? Summary { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionHealthSummaryResult Success(ProductionExecutionHealthSummary summary) =>
        new() { Succeeded = true, Summary = summary };

    public static ProductionHealthSummaryResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}
