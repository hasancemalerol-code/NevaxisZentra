using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Outbox;

public sealed class OutboxMessage : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string MessageType { get; set; }
    public int SchemaVersion { get; set; }
    public string? AggregateType { get; set; }
    public string? AggregateId { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string PayloadJson { get; set; }
    public required string HeadersJson { get; set; }
    public OutboxMessageStatus Status { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public DateTimeOffset AvailableAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public string? FailureCode { get; set; }
    public Guid? LockId { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }
}
