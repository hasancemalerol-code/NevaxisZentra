using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportDocument : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public ExportDocumentStatus DocumentStatus { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? FileReference { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTimeOffset? IssuedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string? VerifiedBySubjectId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}
