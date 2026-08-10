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
