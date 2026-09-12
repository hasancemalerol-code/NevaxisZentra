namespace NevaxisFactOS.Persistence.Edge;

public sealed record EdgeOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public static class EdgeFailureCodes
{
    public const string InvalidScope = "edge.invalid-scope";
    public const string NotFound = "edge.not-found";
    public const string MachineNotFound = "edge.machine-not-found";
    public const string GatewayNotFound = "edge.gateway-not-found";
    public const string DuplicateEvent = "edge.duplicate-event";
    public const string OutOfOrder = "edge.out-of-order";
    public const string NoWriteControl = "edge.no-write-control";
    public const string ValidationFailed = "edge.validation-failed";
    // EDGE-2 typed signal failure codes
    public const string TelemetryInvalid = "edge.telemetry.invalid";
    public const string TelemetryDuplicate = "edge.telemetry.duplicate";
    public const string TelemetryOutOfOrder = "edge.telemetry.out-of-order";
    public const string CycleInvalid = "edge.cycle.invalid";
    public const string CycleDuplicate = "edge.cycle.duplicate";
    public const string CounterInvalid = "edge.counter.invalid";
    public const string CounterResetSuspected = "edge.counter.reset-suspected";
    // EDGE-3 operational observation failure codes
    public const string DowntimeInvalid = "edge.downtime.invalid";
    public const string DowntimeDuplicate = "edge.downtime.duplicate";
    public const string DowntimeUnmatchedEnd = "edge.downtime.unmatched-end";
    public const string AlarmInvalid = "edge.alarm.invalid";
    public const string AlarmDuplicate = "edge.alarm.duplicate";
    public const string AlarmUnmatchedClear = "edge.alarm.unmatched-clear";
    public const string SetupInvalid = "edge.setup.invalid";
    public const string SetupDuplicate = "edge.setup.duplicate";
    public const string SetupUnmatchedComplete = "edge.setup.unmatched-complete";
    public const string ScrapInvalid = "edge.scrap.invalid";
    public const string ScrapDuplicate = "edge.scrap.duplicate";
}

// ---------- Read models ----------

public sealed record EdgeGatewayReadModel(
    Guid Id,
    Guid FacilityId,
    string GatewayCode,
    string Name,
    string? Description,
    string VendorKey,
    string Status,
    DateTimeOffset? LastSeenAtUtc,
    DateTimeOffset? LastHeartbeatAtUtc,
    string? LastHealthMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int MachineCount);

public sealed record MachineRegistryReadModel(
    Guid Id,
    Guid FacilityId,
    string MachineCode,
    string Name,
    string? Description,
    string? AssetClass,
    string? AssetReference,
    string Status,
    bool IsReadyForProduction,
    DateTimeOffset? LastSeenAtUtc,
    DateTimeOffset? LastEventAtUtc,
    string? LastKnownStatus,
    DateTimeOffset? LastKnownStatusAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? ActiveGatewayId,
    string? ActiveGatewayCode);

public sealed record MachineEventReadModel(
    Guid Id,
    Guid MachineId,
    Guid GatewayId,
    string EventKind,
    DateTimeOffset SourceTimestampUtc,
    DateTimeOffset ReceivedAtUtc,
    long SourceSequence,
    string IdempotencyKey,
    string? Payload,
    bool IsDuplicate,
    string? DuplicateOfIdempotencyKey);

public sealed record MachineRuntimeReadModel(
    Guid Id,
    Guid MachineId,
    string Status,
    bool IsReadyForProduction,
    DateTimeOffset? LastSeenAtUtc,
    Guid? LastEventId,
    long? LastSourceSequence,
    bool LastSequenceOutOfOrder,
    DateTimeOffset UpdatedAtUtc);

// ---------- Ingestion DTOs ----------

public sealed record EdgeIngestRequest(
    Guid FacilityId,
    Guid MachineId,
    Guid GatewayId,
    string EventKind,
    string IdempotencyKey,
    DateTimeOffset SourceTimestampUtc,
    long SourceSequence,
    string? Payload);

public sealed record EdgeIngestResult
{
    public bool Accepted { get; init; }
    public bool IsDuplicate { get; init; }
    public bool IsOutOfOrder { get; init; }
    public Guid? EventId { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static EdgeIngestResult Duplicate(Guid existingEventId) =>
        new() { Accepted = false, IsDuplicate = true, EventId = existingEventId };

    public static EdgeIngestResult Failure(string code, string? reason = null) =>
        new() { Accepted = false, FailureCode = code, FailureReason = reason };

    public static EdgeIngestResult Success(Guid eventId, bool outOfOrder) =>
        new() { Accepted = true, IsOutOfOrder = outOfOrder, EventId = eventId };
}

// ============================================================
// EDGE-2: Telemetry read models
// ============================================================

public sealed record TelemetrySampleReadModel(
    Guid Id,
    Guid MachineId,
    string MetricKey,
    double? NumericValue,
    string? TextValue,
    bool? BooleanValue,
    string? Unit,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    DateTimeOffset ReceivedAtUtc,
    long? SourceSequence,
    string IdempotencyKey,
    bool IsDuplicate);

public sealed record LatestTelemetryReadModel(
    Guid MachineId,
    string MetricKey,
    double? NumericValue,
    string? TextValue,
    bool? BooleanValue,
    string? Unit,
    string Quality,
    DateTimeOffset SourceTimestampUtc,
    DateTimeOffset ReceivedAtUtc,
    long? SourceSequence,
    DateTimeOffset UpdatedAtUtc);

// ============================================================
// EDGE-2: Cycle read models
// ============================================================

public sealed record ObservedCycleReadModel(
    Guid Id,
    Guid MachineId,
    string CycleIdentity,
    long CycleDurationMs,
    long? ObservedCounter,
    string Quality,
    DateTimeOffset SourceTimestampUtc,
    DateTimeOffset ReceivedAtUtc,
    long? SourceSequence,
    string IdempotencyKey,
    bool IsDuplicate,
    bool IsOutOfOrder);

public sealed record CycleProjectionReadModel(
    Guid MachineId,
    long ObservedCycleCount,
    long? LatestObservedCounter,
    DateTimeOffset? LastCycleAtUtc,
    long? LastCycleDurationMs,
    long? AverageRecentCycleDurationMs,
    DateTimeOffset UpdatedAtUtc);

// ============================================================
// EDGE-2: Counter read models
// ============================================================

public sealed record CounterObservationReadModel(
    Guid Id,
    Guid MachineId,
    string CounterKey,
    long CounterValue,
    string Quality,
    bool CounterResetSuspected,
    DateTimeOffset SourceTimestampUtc,
    DateTimeOffset ReceivedAtUtc,
    long? SourceSequence,
    string IdempotencyKey);

// ============================================================
// EDGE-2: State transition read models
// ============================================================

public sealed record StateTransitionReadModel(
    Guid Id,
    Guid MachineId,
    string FromState,
    string ToState,
    string? Reason,
    DateTimeOffset SourceTimestampUtc,
    DateTimeOffset ReceivedAtUtc,
    long? SourceSequence,
    bool IsOutOfOrder);

// ============================================================
// EDGE-2: Signal projection read model
// ============================================================

public sealed record MachineSignalProjectionReadModel(
    Guid MachineId,
    string Connectivity,
    string ObservedState,
    DateTimeOffset? LastCycleAtUtc,
    long? LastCycleDurationMs,
    long ObservedCycleCount,
    long? LatestCounter,
    string Quality,
    DateTimeOffset? LastTelemetryAtUtc,
    bool IsInObservedDowntime,
    DateTimeOffset? CurrentDowntimeStartedAtUtc,
    long? ObservedDowntimeDurationMs,
    int ActiveAlarmCount,
    string? HighestAlarmSeverity,
    bool IsInObservedSetup,
    DateTimeOffset? ObservedSetupStartedAtUtc,
    long ObservedScrapCount,
    double ObservedScrapQuantity,
    DateTimeOffset? LastObservedScrapAtUtc,
    DateTimeOffset UpdatedAtUtc);

// ============================================================
// EDGE-3: Downtime / Alarm / Setup / Scrap read models
// ============================================================

public sealed record DowntimeEpisodeReadModel(
    Guid Id,
    Guid MachineId,
    string? ExternalEpisodeId,
    DateTimeOffset StartedAtSourceUtc,
    DateTimeOffset? EndedAtSourceUtc,
    long? DurationMs,
    string? ReasonCode,
    string? ReasonCategory,
    string? ReasonText,
    bool IsMicroStop,
    string Quality,
    bool IsClosed,
    bool IsOutOfOrder,
    long? StartSourceSequence,
    long? EndSourceSequence);

public sealed record ActiveAlarmReadModel(
    Guid Id,
    Guid MachineId,
    string AlarmIdentity,
    string ExternalAlarmCode,
    string? AlarmText,
    string Severity,
    DateTimeOffset RaisedAtSourceUtc,
    long? SourceSequence,
    string Quality);

public sealed record AlarmObservationReadModel(
    Guid Id,
    Guid MachineId,
    string AlarmIdentity,
    string ExternalAlarmCode,
    string? AlarmText,
    string Severity,
    string EventKind,
    DateTimeOffset SourceTimestampUtc,
    DateTimeOffset ReceivedAtUtc,
    long? SourceSequence,
    string Quality,
    bool IsDuplicate,
    bool IsOutOfOrder,
    bool IsUnmatchedClear);

public sealed record AlarmProjectionReadModel(
    Guid MachineId,
    int ActiveAlarmCount,
    string? HighestSeverity,
    string? LatestAlarmCode,
    string? LatestAlarmText,
    DateTimeOffset? LatestRaisedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SetupEpisodeReadModel(
    Guid Id,
    Guid MachineId,
    string? ExternalEpisodeId,
    string? SetupType,
    string? FromReference,
    string? ToReference,
    DateTimeOffset StartedAtSourceUtc,
    DateTimeOffset? CompletedAtSourceUtc,
    long? DurationMs,
    string Quality,
    bool IsClosed,
    bool IsOutOfOrder,
    long? StartSourceSequence,
    long? CompleteSourceSequence);

public sealed record ScrapObservationReadModel(
    Guid Id,
    Guid MachineId,
    string ExternalObservationId,
    double Quantity,
    string Unit,
    string? ReasonCode,
    string? ReasonCategory,
    string? ReasonText,
    string Quality,
    DateTimeOffset SourceTimestampUtc,
    DateTimeOffset ReceivedAtUtc,
    long? SourceSequence,
    bool IsDuplicate,
    bool IsOutOfOrder);

public sealed record MachineOperationalSummaryReadModel(
    Guid MachineId,
    bool IsInObservedDowntime,
    DateTimeOffset? CurrentDowntimeStartedAtUtc,
    long? ObservedDowntimeDurationMs,
    long ObservedDowntimeCount,
    long ObservedDowntimeTotalDurationMs,
    long ObservedMicroStopCount,
    int ActiveAlarmCount,
    string? HighestAlarmSeverity,
    int AlarmHistoryCount,
    int CriticalAlarmCount,
    bool IsInObservedSetup,
    DateTimeOffset? ObservedSetupStartedAtUtc,
    long ObservedSetupCount,
    long ObservedSetupTotalDurationMs,
    long ObservedScrapCount,
    double ObservedScrapQuantity,
    DateTimeOffset? LastObservedScrapAtUtc,
    DateTimeOffset? UpdatedAtUtc);

// ============================================================
// EDGE-2: Ingestion request/result types
// ============================================================

public sealed record TelemetryIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string MetricKey,
    double? NumericValue,
    string? TextValue,
    bool? BooleanValue,
    string? Unit,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record CycleIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string CycleIdentity,
    long CycleDurationMs,
    long? ObservedCounter,
    string Quality,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record CounterIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string CounterKey,
    long CounterValue,
    string Quality,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record TelemetryIngestResult
{
    public bool Accepted { get; init; }
    public bool IsDuplicate { get; init; }
    public bool IsOutOfOrder { get; init; }
    public Guid? SampleId { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static TelemetryIngestResult Duplicate(Guid existingId) =>
        new() { Accepted = false, IsDuplicate = true, SampleId = existingId };

    public static TelemetryIngestResult Failure(string code, string? reason = null) =>
        new() { Accepted = false, FailureCode = code, FailureReason = reason };

    public static TelemetryIngestResult Success(Guid sampleId, bool outOfOrder) =>
        new() { Accepted = true, IsOutOfOrder = outOfOrder, SampleId = sampleId };
}

public sealed record CycleIngestResult
{
    public bool Accepted { get; init; }
    public bool IsDuplicate { get; init; }
    public bool IsOutOfOrder { get; init; }
    public Guid? CycleId { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static CycleIngestResult Duplicate(Guid existingId) =>
        new() { Accepted = false, IsDuplicate = true, CycleId = existingId };

    public static CycleIngestResult Failure(string code, string? reason = null) =>
        new() { Accepted = false, FailureCode = code, FailureReason = reason };

    public static CycleIngestResult Success(Guid cycleId, bool outOfOrder) =>
        new() { Accepted = true, IsOutOfOrder = outOfOrder, CycleId = cycleId };
}

public sealed record CounterIngestResult
{
    public bool Accepted { get; init; }
    public bool CounterResetSuspected { get; init; }
    public Guid? ObservationId { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static CounterIngestResult Failure(string code, string? reason = null) =>
        new() { Accepted = false, FailureCode = code, FailureReason = reason };

    public static CounterIngestResult Success(Guid observationId, bool resetSuspected) =>
        new() { Accepted = true, CounterResetSuspected = resetSuspected, ObservationId = observationId };
}

// ============================================================
// EDGE-3: Ingestion request/result types
// ============================================================

public sealed record DowntimeStartIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string? ExternalEpisodeId,
    string? ReasonCode,
    string? ReasonCategory,
    string? ReasonText,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record DowntimeEndIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string? ExternalEpisodeId,
    string? ReasonCode,
    string? ReasonCategory,
    string? ReasonText,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record AlarmRaiseIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string AlarmIdentity,
    string ExternalAlarmCode,
    string? AlarmText,
    string Severity,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record AlarmClearIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string AlarmIdentity,
    string? ExternalAlarmCode,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record SetupStartIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string? ExternalEpisodeId,
    string? SetupType,
    string? FromReference,
    string? ToReference,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record SetupCompleteIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string? ExternalEpisodeId,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record ScrapIngestRequest(
    Guid MachineId,
    Guid GatewayId,
    string ExternalObservationId,
    double Quantity,
    string Unit,
    string? ReasonCode,
    string? ReasonCategory,
    string? ReasonText,
    string Quality,
    string? Transport,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    string IdempotencyKey);

public sealed record OperationalIngestResult
{
    public bool Accepted { get; init; }
    public bool IsDuplicate { get; init; }
    public bool IsOutOfOrder { get; init; }
    public bool IsUnmatched { get; init; }
    public Guid? ObservationId { get; init; }
    public Guid? EpisodeId { get; init; }
    public bool? IsMicroStop { get; init; }
    public long? DurationMs { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static OperationalIngestResult Duplicate(Guid existingId) =>
        new() { Accepted = false, IsDuplicate = true, ObservationId = existingId };

    public static OperationalIngestResult Failure(string code, string? reason = null) =>
        new() { Accepted = false, FailureCode = code, FailureReason = reason };

    public static OperationalIngestResult Unmatched(string code, Guid? observationId = null, string? reason = null) =>
        new() { Accepted = false, IsUnmatched = true, ObservationId = observationId, FailureCode = code, FailureReason = reason };

    public static OperationalIngestResult Success(
        Guid observationId,
        bool outOfOrder,
        Guid? episodeId = null,
        bool? isMicroStop = null,
        long? durationMs = null) =>
        new()
        {
            Accepted = true,
            IsOutOfOrder = outOfOrder,
            ObservationId = observationId,
            EpisodeId = episodeId,
            IsMicroStop = isMicroStop,
            DurationMs = durationMs,
        };
}
