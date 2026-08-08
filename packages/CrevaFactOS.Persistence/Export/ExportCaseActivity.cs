using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Export;

/// <summary>Immutable append-only activity log for an export case.</summary>
public sealed class ExportCaseActivity : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public ExportActivitySourceType SourceType { get; set; }
    public string? ActorSubjectId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public Guid CorrelationId { get; set; }
}
