namespace NevaxisFactOS.Persistence.Outbox;

public sealed record OutboxWriteRequest(
    string MessageType,
    int SchemaVersion,
    string IdempotencyKey,
    IReadOnlyDictionary<string, string?> Payload,
    IReadOnlyDictionary<string, string?> Headers,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset AvailableAtUtc,
    string? AggregateType = null,
    string? AggregateId = null,
    Guid? CausationId = null);
