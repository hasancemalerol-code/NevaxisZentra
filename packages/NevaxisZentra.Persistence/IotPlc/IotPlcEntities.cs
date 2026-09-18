namespace NevaxisZentra.Persistence.IotPlc;

public sealed class IotPlcEndpoint
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string EndpointCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ProtocolType { get; set; } = string.Empty;
    public string ConnectionDirection { get; set; } = string.Empty;
    public string EndpointStatus { get; set; } = IotPlcEndpointStatuses.Draft;
    public string ConfigurationProvenance { get; set; } = string.Empty;
    public string? ExternalSecretReference { get; set; }
    public string ConnectionProfileJson { get; set; } = "{}";
    public string? LastObservedConnectionState { get; set; }
    public DateTimeOffset? LastObservedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<IotPlcSignal> Signals { get; set; } = [];
}

public sealed class IotPlcSignal
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid EndpointId { get; set; }
    public string SignalCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SourceAddress { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? EngineeringUnit { get; set; }
    public string SemanticType { get; set; } = IotPlcSemanticTypes.GenericObservation;
    public string MappedAssetType { get; set; } = IotPlcAssetTypes.Unmapped;
    public Guid? MappedCanonicalAssetId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? SamplingExpectation { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public IotPlcEndpoint? Endpoint { get; set; }
}

public sealed class IotPlcSignalObservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid EndpointId { get; set; }
    public Guid SignalDefinitionId { get; set; }
    public string? MappedAssetType { get; set; }
    public Guid? MappedCanonicalAssetId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public bool? BooleanValue { get; set; }
    public long? IntegerValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public string? TextValue { get; set; }
    public DateTimeOffset? TimestampValue { get; set; }
    public string QualityState { get; set; } = IotPlcQualityStates.Good;
    public string EvidenceState { get; set; } = IotPlcEvidenceStates.Measured;
    public string? SourceSequenceReference { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? PayloadFingerprint { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public IotPlcSignal? Signal { get; set; }
}

public sealed class IotPlcConnectionHealthObservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid EndpointId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public string HealthState { get; set; } = IotPlcConnectionStates.Unknown;
    public string Source { get; set; } = string.Empty;
    public string EvidenceState { get; set; } = IotPlcEvidenceStates.Simulated;
    public string ReasonCode { get; set; } = string.Empty;
    public int? LatencyMs { get; set; }
    public DateTimeOffset? LastSuccessfulObservationAtUtc { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? PayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public IotPlcEndpoint? Endpoint { get; set; }
}

public sealed class IotPlcAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }
}
