namespace NevaxisZentra.Persistence.IotPlc;

public sealed record IotPlcOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record IotPlcWorkspaceReadModel(
    int ActiveEndpointCount,
    int ActiveSignalCount,
    int RecentObservationCount,
    string ProvenanceDisclaimer,
    bool HasDevelopmentPilotData);

public sealed record IotPlcEndpointReadModel(
    Guid Id,
    string EndpointCode,
    string DisplayName,
    string ProtocolType,
    string ConnectionDirection,
    string EndpointStatus,
    string ConfigurationProvenance,
    string? LastObservedConnectionState,
    DateTimeOffset? LastObservedAtUtc,
    uint Version,
    int SignalCount,
    bool IsPilotData);

public sealed record IotPlcEndpointDetailReadModel(
    IotPlcEndpointReadModel Endpoint,
    string ConnectionProfileJson,
    IReadOnlyList<IotPlcSignalReadModel> Signals);

public sealed record IotPlcEndpointListReadModel(
    IReadOnlyList<IotPlcEndpointReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record IotPlcEndpointListFilters(
    string? ProtocolType,
    string? EndpointStatus,
    int Page,
    int PageSize);

public sealed record IotPlcSignalReadModel(
    Guid Id,
    Guid EndpointId,
    string EndpointCode,
    string SignalCode,
    string DisplayName,
    string SourceAddress,
    string DataType,
    string? EngineeringUnit,
    string SemanticType,
    string MappedAssetType,
    Guid? MappedCanonicalAssetId,
    bool IsEnabled,
    string? SamplingExpectation,
    string SourceProvenance,
    uint Version,
    bool IsPilotData);

public sealed record IotPlcSignalListReadModel(
    IReadOnlyList<IotPlcSignalReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record IotPlcSignalListFilters(
    string? EndpointReference,
    string? MappedAssetType,
    Guid? MappedCanonicalAssetId,
    bool? IsEnabled,
    int Page,
    int PageSize);

public sealed record IotPlcSignalObservationReadModel(
    Guid Id,
    Guid SignalDefinitionId,
    string SignalCode,
    string? MappedAssetType,
    Guid? MappedCanonicalAssetId,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset ReceivedAtUtc,
    object? Value,
    string QualityState,
    string EvidenceState,
    string? SourceSequenceReference,
    string SourceProvenance);

public sealed record IotPlcSignalObservationListReadModel(
    IReadOnlyList<IotPlcSignalObservationReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record IotPlcSignalObservationListFilters(
    DateTimeOffset? ObservedFromUtc,
    DateTimeOffset? ObservedToUtc,
    int Page,
    int PageSize);

public sealed record IotPlcConnectionHealthReadModel(
    Guid Id,
    Guid EndpointId,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset ReceivedAtUtc,
    string HealthState,
    string Source,
    string EvidenceState,
    string ReasonCode,
    int? LatencyMs,
    DateTimeOffset? LastSuccessfulObservationAtUtc);

public sealed record IotPlcConnectionHealthListReadModel(
    IReadOnlyList<IotPlcConnectionHealthReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CreateIotPlcEndpointCommand(
    string EndpointCode,
    string DisplayName,
    string ProtocolType,
    string ConnectionDirection,
    string EndpointStatus,
    string ConfigurationProvenance,
    string? ExternalSecretReference,
    string ConnectionProfileJson);

public sealed record UpdateIotPlcEndpointCommand(
    string EndpointCode,
    string DisplayName,
    string ProtocolType,
    string ConnectionDirection,
    string EndpointStatus,
    string ConfigurationProvenance,
    string? ExternalSecretReference,
    string ConnectionProfileJson,
    uint ExpectedVersion);

public sealed record CreateIotPlcSignalCommand(
    string EndpointReference,
    string SignalCode,
    string DisplayName,
    string SourceAddress,
    string DataType,
    string? EngineeringUnit,
    string SemanticType,
    string MappedAssetType,
    Guid? MappedCanonicalAssetId,
    bool IsEnabled,
    string? SamplingExpectation,
    string SourceProvenance);

public sealed record UpdateIotPlcSignalCommand(
    string DisplayName,
    string SourceAddress,
    string DataType,
    string? EngineeringUnit,
    string SemanticType,
    string MappedAssetType,
    Guid? MappedCanonicalAssetId,
    bool IsEnabled,
    string? SamplingExpectation,
    string SourceProvenance,
    uint ExpectedVersion);

public sealed record IngestIotPlcObservationCommand(
    string SignalReference,
    DateTimeOffset ObservedAtUtc,
    bool? BooleanValue,
    long? IntegerValue,
    decimal? DecimalValue,
    string? TextValue,
    DateTimeOffset? TimestampValue,
    string QualityState,
    string EvidenceState,
    string? SourceSequenceReference,
    string SourceProvenance,
    string? IdempotencyKey);

public sealed record IngestIotPlcConnectionHealthCommand(
    string EndpointReference,
    DateTimeOffset ObservedAtUtc,
    string HealthState,
    string Source,
    string EvidenceState,
    string ReasonCode,
    int? LatencyMs,
    DateTimeOffset? LastSuccessfulObservationAtUtc,
    string? IdempotencyKey);

public sealed class IotPlcWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static IotPlcWriteResult Success(Guid entityId, uint? version = null, bool idempotentReplay = false) =>
        new() { Succeeded = true, EntityId = entityId, Version = version, IdempotentReplay = idempotentReplay };

    public static IotPlcWriteResult Failure(string failureCode, string? failureReason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = failureReason };
}
