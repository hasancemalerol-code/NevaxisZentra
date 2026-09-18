namespace NevaxisZentra.Persistence.Outbox;

public sealed record OutboxMessageSummary(
    Guid Id,
    string MessageType,
    int SchemaVersion,
    string IdempotencyKey,
    OutboxMessageStatus Status,
    Guid CorrelationId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset AvailableAtUtc,
    int AttemptCount);
