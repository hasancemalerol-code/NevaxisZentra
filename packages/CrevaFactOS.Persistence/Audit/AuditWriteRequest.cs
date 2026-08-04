namespace CrevaFactOS.Persistence.Audit;

public sealed record AuditWriteRequest(
    string EventType,
    string Action,
    string ResourceType,
    AuditOutcome Outcome,
    string? ResourceId = null,
    string? PermissionKey = null,
    Guid? CausationId = null,
    string? FailureCode = null,
    IReadOnlyDictionary<string, object?>? Metadata = null,
    DateTimeOffset? OccurredAtUtc = null,
    AuditSource Source = AuditSource.Api);
