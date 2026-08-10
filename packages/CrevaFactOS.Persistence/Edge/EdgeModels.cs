namespace CrevaFactOS.Persistence.Edge;

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
