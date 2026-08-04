namespace CrevaFactOS.Persistence.Audit;

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? IdentitySubjectId { get; set; }
    public Guid? TenantMembershipId { get; set; }
    public required string EventType { get; set; }
    public required string Action { get; set; }
    public required string ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public AuditOutcome Outcome { get; set; }
    public string? PermissionKey { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public string? RequestId { get; set; }
    public AuditSource Source { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public required string MetadataJson { get; set; }
    public string? FailureCode { get; set; }
}
