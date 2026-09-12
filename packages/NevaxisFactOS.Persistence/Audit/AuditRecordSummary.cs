namespace NevaxisFactOS.Persistence.Audit;

public sealed record AuditRecordSummary(
    Guid Id,
    Guid TenantId,
    Guid? IdentitySubjectId,
    Guid? TenantMembershipId,
    string EventType,
    string Action,
    string ResourceType,
    string? ResourceId,
    AuditOutcome Outcome,
    string? PermissionKey,
    Guid CorrelationId,
    Guid? CausationId,
    string? RequestId,
    AuditSource Source,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset RecordedAtUtc,
    string? FailureCode);
