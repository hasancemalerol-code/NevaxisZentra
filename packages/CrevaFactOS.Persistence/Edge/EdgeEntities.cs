namespace CrevaFactOS.Persistence.Edge;

public static class EdgeGatewayVendorKeys
{
    public const string Simulator = "simulator";
    public const string MqttBridge = "mqtt-bridge";
    public const string OpcuaBridge = "opcua-bridge";
}

public static class EdgeGatewayStatuses
{
    public const string Unknown = "unknown";
    public const string Online = "online";
    public const string Offline = "offline";
    public const string Degraded = "degraded";
    public const string Disabled = "disabled";
}

public static class MachineStatuses
{
    public const string Unknown = "unknown";
    public const string Running = "running";
    public const string Idle = "idle";
    public const string Stopped = "stopped";
    public const string Fault = "fault";
    public const string Maintenance = "maintenance";
}

public static class MachineEventKinds
{
    public const string Heartbeat = "heartbeat";
    public const string StatusChange = "status-change";
    public const string Telemetry = "telemetry";
    public const string Alarm = "alarm";
    public const string Fault = "fault";
    public const string CycleStart = "cycle-start";
    public const string CycleEnd = "cycle-end";
    // EDGE-2 canonical typed signals
    public const string CycleCompleted = "cycle-completed";
    public const string CounterObserved = "counter-observed";
}

/// <summary>
/// Honest data quality for telemetry samples and cycle events.
/// Do not silently treat invalid/uncertain data as good.
/// </summary>
public static class TelemetryQualities
{
    public const string Good = "good";
    public const string Uncertain = "uncertain";
    public const string Bad = "bad";
    public const string Unknown = "unknown";
}

/// <summary>
/// Simulator-only example metric keys. NOT real machine tag addresses.
/// Do not build hardcoded vendor tag mapping against these.
/// </summary>
public static class TelemetryMetricKeys
{
    public const string CycleTimeMs = "cycle_time_ms";
    public const string MachineCounter = "machine_counter";
    public const string TemperatureZone1 = "temperature_zone_1";
    public const string HydraulicPressure = "hydraulic_pressure";
}

/// <summary>
/// Transport identifier preserved for provenance tracking.
/// </summary>
public static class TelemetryTransports
{
    public const string Simulator = "simulator";
    public const string GatewayHttp = "gateway-http";
}

public static class MachineAssignmentStatuses
{
    public const string Active = "active";
    public const string Inactive = "inactive";
}

/// <summary>
/// Registered Edge gateway device. Each gateway connects one or more machines to CrevaFactOS.
/// </summary>
public sealed class EdgeGateway
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string GatewayCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VendorKey { get; set; } = EdgeGatewayVendorKeys.Simulator;
    public string Status { get; set; } = EdgeGatewayStatuses.Unknown;
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public DateTimeOffset? LastHeartbeatAtUtc { get; set; }
    public string? LastHealthMessage { get; set; }
    public string? CredentialReference { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public ICollection<MachineGatewayAssignment> Assignments { get; set; } = new List<MachineGatewayAssignment>();
}

/// <summary>
/// Registered machine. Source of truth for which physical machines are managed by this facility.
/// Does not replace production order or maintenance asset SoR.
/// </summary>
public sealed class MachineRegistry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string MachineCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AssetClass { get; set; }
    public string? AssetReference { get; set; }
    public string Status { get; set; } = MachineStatuses.Unknown;
    public bool IsReadyForProduction { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public DateTimeOffset? LastEventAtUtc { get; set; }
    public string? LastKnownStatus { get; set; }
    public DateTimeOffset? LastKnownStatusAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public ICollection<MachineGatewayAssignment> Assignments { get; set; } = new List<MachineGatewayAssignment>();
}

/// <summary>
/// Links a machine to a gateway with a vendor-specific adapter key (channel identifier).
/// </summary>
public sealed class MachineGatewayAssignment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public Guid GatewayId { get; set; }
    public string AdapterKey { get; set; } = string.Empty;
    public string Status { get; set; } = MachineAssignmentStatuses.Active;
    public DateTimeOffset AssignedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public MachineRegistry? Machine { get; set; }
    public EdgeGateway? Gateway { get; set; }
}

/// <summary>
/// Canonical machine event envelope. Append-only. Never mutated after insert.
/// Carries EventId (PK), SourceTimestampUtc, ReceivedAtUtc, SourceSequence, IdempotencyKey.
/// Duplicate events are stored with IsDuplicate = true for audit purposes.
/// </summary>
public sealed class MachineEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public Guid GatewayId { get; set; }
    public string EventKind { get; set; } = MachineEventKinds.Heartbeat;
    public DateTimeOffset SourceTimestampUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public long SourceSequence { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public bool IsDuplicate { get; set; }
    public string? DuplicateOfIdempotencyKey { get; set; }
}

/// <summary>
/// Last-known runtime state projection per machine. Updated on each non-duplicate event.
/// Persists across API restarts. One row per machine.
/// </summary>
public sealed class MachineRuntime
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public string Status { get; set; } = MachineStatuses.Unknown;
    public bool IsReadyForProduction { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public Guid? LastEventId { get; set; }
    public long? LastSourceSequence { get; set; }
    public bool LastSequenceOutOfOrder { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

// ============================================================
// EDGE-2: Telemetry, Cycle, Counter, State Transition, Signal
// ============================================================

/// <summary>
/// Append-only typed telemetry sample. Exactly one value field (Numeric/Text/Boolean) should be
/// populated per sample. No arbitrary unbounded JSON as the primary model.
/// </summary>
public sealed class MachineTelemetrySample
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public Guid GatewayId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public double? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? Unit { get; set; }
    public string Quality { get; set; } = TelemetryQualities.Unknown;
    public string? Transport { get; set; }
    public DateTimeOffset SourceTimestampUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public long? SourceSequence { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Latest telemetry projection — one row per (machine, metric).
/// Out-of-order samples must not regress this projection.
/// </summary>
public sealed class MachineLatestTelemetry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public double? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? Unit { get; set; }
    public string Quality { get; set; } = TelemetryQualities.Unknown;
    public DateTimeOffset SourceTimestampUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public long? SourceSequence { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// Append-only observed cycle event (machine.cycle.completed).
/// Does NOT represent official Production quantity. Name clearly as observed.
/// </summary>
public sealed class MachineObservedCycle
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public Guid GatewayId { get; set; }
    public string CycleIdentity { get; set; } = string.Empty;
    public long CycleDurationMs { get; set; }
    public long? ObservedCounter { get; set; }
    public string Quality { get; set; } = TelemetryQualities.Unknown;
    public DateTimeOffset SourceTimestampUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public long? SourceSequence { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public bool IsOutOfOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Observed cycle projection — one row per machine.
/// ObservedCycleCount is NOT official production quantity.
/// </summary>
public sealed class MachineCycleProjection
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public long ObservedCycleCount { get; set; }
    public long? LatestObservedCounter { get; set; }
    public DateTimeOffset? LastCycleAtUtc { get; set; }
    public long? LastCycleDurationMs { get; set; }
    public long? AverageRecentCycleDurationMs { get; set; }
    public long? LastSourceSequence { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// Append-only machine counter observation (machine.counter.observed).
/// Counter may reset or wrap — CounterResetSuspected is flagged when a lower value
/// follows a trusted newer higher value. No Production quantity inferred.
/// </summary>
public sealed class MachineCounterObservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public Guid GatewayId { get; set; }
    public string CounterKey { get; set; } = string.Empty;
    public long CounterValue { get; set; }
    public string Quality { get; set; } = TelemetryQualities.Unknown;
    public bool CounterResetSuspected { get; set; }
    public DateTimeOffset SourceTimestampUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public long? SourceSequence { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Append-only state transition record.
/// Out-of-order transitions may be stored but must NOT regress current machine state.
/// </summary>
public sealed class MachineStateTransition
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public string FromState { get; set; } = MachineStatuses.Unknown;
    public string ToState { get; set; } = MachineStatuses.Unknown;
    public string? Reason { get; set; }
    public DateTimeOffset SourceTimestampUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public long? SourceSequence { get; set; }
    public bool IsOutOfOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Read-only downstream signal projection for other bounded contexts.
/// EDGE-2 observation data only. No Production mutation allowed.
/// One row per machine.
/// </summary>
public sealed class MachineSignalProjection
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineId { get; set; }
    public string Connectivity { get; set; } = "offline";
    public string ObservedState { get; set; } = MachineStatuses.Unknown;
    public DateTimeOffset? LastCycleAtUtc { get; set; }
    public long? LastCycleDurationMs { get; set; }
    public long ObservedCycleCount { get; set; }
    public long? LatestCounter { get; set; }
    public string Quality { get; set; } = TelemetryQualities.Unknown;
    public DateTimeOffset? LastTelemetryAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
