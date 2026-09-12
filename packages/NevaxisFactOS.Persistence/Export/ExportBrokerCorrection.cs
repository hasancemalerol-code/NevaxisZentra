using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportBrokerCorrection : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public Guid BrokerWorkspaceId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExportBrokerCorrectionSeverity Severity { get; set; }
    public ExportBrokerCorrectionStatus Status { get; set; }
    public Guid? RelatedDocumentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolvedBySubjectId { get; set; }
    public string? ResolutionNote { get; set; }
}
