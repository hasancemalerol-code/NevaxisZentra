namespace NevaxisZentra.MachineSimulator.EdgeBuffer;

public sealed record EdgeBufferedEvent(
    Guid Id,
    Guid TenantId,
    Guid FacilityId,
    Guid GatewayId,
    Guid MachineId,
    string? MachineCode,
    Guid EventId,
    string EventType,
    string IdempotencyKey,
    DateTimeOffset SourceTimestampUtc,
    long? SourceSequence,
    int PayloadVersion,
    string? Payload,
    DateTimeOffset CreatedAtUtc,
    string DeliveryStatus,
    int AttemptCount,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? NextAttemptAtUtc,
    string? LastErrorCode,
    DateTimeOffset? DeliveredAtUtc,
    string? AcknowledgementId,
    DateTimeOffset? DeadLetteredAtUtc,
    DateTimeOffset? InFlightClaimedAtUtc,
    string? InFlightLeaseId);

public sealed record EdgeBufferHealth(
    bool ApiConnected,
    string DeliveryStatus,
    int PendingCount,
    int RetryCount,
    int DeadLetterCount,
    int DeliveredCount,
    TimeSpan? OldestPendingAge,
    DateTimeOffset? LastSuccessfulDeliveryAtUtc,
    DateTimeOffset? LastDeliveryAttemptAtUtc,
    double BufferUtilization,
    string ReplayStatus,
    int MaxBufferedEvents,
    long EventsBufferedTotal,
    long EventsDeliveredTotal,
    long RetriesTotal,
    long DeliveryFailuresTotal);

public sealed record EdgeBufferEnqueueResult(
    bool Accepted,
    string? ErrorCode,
    Guid? BufferedEventId);

public sealed record EdgeDeliveryAttemptResult(
    bool Success,
    bool IsDuplicateAck,
    bool IsRetryable,
    string? ErrorCode,
    string? AcknowledgementId,
    int? HttpStatus);
