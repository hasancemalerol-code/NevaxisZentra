namespace NevaxisFactOS.Persistence.Integration.FactoryEventSpine;

public sealed class IntegrationInboundFactoryEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectorId { get; set; }
    public string SourceEventId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string RawPayloadJson { get; set; } = "{}";
    public string ProcessingStatus { get; set; } = FactoryInboundEventStatuses.Received;
    public string? ValidationState { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public string? LastError { get; set; }
    public string? ErrorClassification { get; set; }
    public DateTimeOffset? NextRetryAtUtc { get; set; }
    public Guid? NormalizedEventId { get; set; }
    public Guid? QuarantineId { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class IntegrationCanonicalFactoryEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectorId { get; set; }
    public Guid InboundEventId { get; set; }
    public string SchemaVersion { get; set; } = FactoryEventSpineSchemaVersions.V1;
    public string EventType { get; set; } = string.Empty;
    public string EventVersion { get; set; } = "1";
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceEventId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public string? MachineId { get; set; }
    public string? OrderId { get; set; }
    public string? MaterialId { get; set; }
    public string? MoldId { get; set; }
    public string? ProductionLineId { get; set; }
    public string? ShiftId { get; set; }
    public string MeasurementsJson { get; set; } = "{}";
    public string BusinessAttributesJson { get; set; } = "{}";
    public string NormalizedPayloadJson { get; set; } = "{}";
    public string CorrelationKeysJson { get; set; } = "[]";
    public string ProcessingState { get; set; } = FactoryCanonicalEventStates.DispatchPending;
    public string ValidationState { get; set; } = "valid";
    public bool IsLateEvent { get; set; }
    public long? SourceSequence { get; set; }
    public string Severity { get; set; } = "medium";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public string? LastError { get; set; }
    public string? ErrorClassification { get; set; }
    public DateTimeOffset? NextRetryAtUtc { get; set; }
    public Guid? RoutedCaseId { get; set; }
    public string? RoutedCorrelationKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class IntegrationEventConsumerReceipt
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid CanonicalEventId { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
    public string ProcessingKey { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAtUtc { get; set; }
}

public sealed class IntegrationEventDispatchOutbox
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid CanonicalEventId { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
    public string Status { get; set; } = FactoryCanonicalEventStates.DispatchPending;
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? DispatchedAtUtc { get; set; }
}
