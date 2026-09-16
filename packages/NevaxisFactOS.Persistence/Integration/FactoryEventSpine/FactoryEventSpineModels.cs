namespace NevaxisFactOS.Persistence.Integration.FactoryEventSpine;

public sealed record FactoryCanonicalEventContract(
    string EventType,
    string EventVersion,
    string SourceEventId,
    DateTimeOffset OccurredAtUtc,
    string? MachineId,
    string? OrderId,
    string? MaterialId,
    string? MoldId,
    string? ProductionLineId,
    string? ShiftId,
    string Severity,
    string Title,
    string Description,
    IReadOnlyDictionary<string, FactoryNormalizedMeasurement> Measurements,
    IReadOnlyDictionary<string, string> BusinessAttributes);

public sealed record FactoryNormalizedMeasurement(
    decimal? NumericValue,
    string? TextValue,
    string SourceUnit,
    string? NormalizedUnit,
    decimal? NormalizedValue,
    bool QuarantineRequired,
    string? QuarantineReason);

public sealed record FactoryEventIngestionRequest(
    Guid ConnectorId,
    string SourceEventId,
    string RawPayloadJson,
    string? IdempotencyKeyOverride = null);

public sealed record FactoryEventIngestionResult(
    bool Accepted,
    bool Duplicate,
    Guid? InboundEventId,
    Guid? CanonicalEventId,
    string? ErrorCode,
    string? SafeMessage);

public sealed record FactoryEventSpineProcessingResult(
    int ProcessedCount,
    int FailedCount,
    int QuarantinedCount,
    int SkippedCount);

public sealed record FactoryConnectorOperationsSnapshot(
    Guid ConnectorId,
    string Name,
    string ConnectorKind,
    string HealthState,
    string? HealthSummary,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset? LastEventReceivedAtUtc,
    int ProcessedToday,
    int QuarantinedToday,
    int FailedToday,
    string? CheckpointSummary,
    string? LastError);

public sealed record FactoryLiveEventMonitorItem(
    Guid CanonicalEventId,
    Guid? InboundEventId,
    string SourceSystem,
    string EventType,
    string? MachineId,
    string? OrderId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ReceivedAtUtc,
    string ProcessingState,
    string? RoutedCorrelationKey,
    Guid? RoutedCaseId);
