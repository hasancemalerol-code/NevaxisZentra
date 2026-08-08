using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportCase : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid SourceOrderId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string SourceOrderReference { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DestinationCountryCode { get; set; } = string.Empty;
    public string? OriginCountryCode { get; set; }
    public string? IncotermCode { get; set; }
    public ExportCaseStatus Status { get; set; }
    public ExportReadinessStatus ReadinessStatus { get; set; }
    public int ReadinessPercent { get; set; }
    public int BlockingIssueCount { get; set; }
    public int MissingRequirementCount { get; set; }
    public ExportPackingStatus PackingStatus { get; set; } = ExportPackingStatus.Draft;
    public DateTimeOffset? PackingCompletedAtUtc { get; set; }
    public string? PackingCompletedBySubjectId { get; set; }
    public DateTimeOffset? PackingVerifiedAtUtc { get; set; }
    public string? PackingVerifiedBySubjectId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}
