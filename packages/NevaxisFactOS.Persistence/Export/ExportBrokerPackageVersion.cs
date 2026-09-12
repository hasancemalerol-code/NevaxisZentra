using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportBrokerPackageVersion : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public Guid BrokerWorkspaceId { get; set; }
    public int VersionNumber { get; set; }
    public ExportBrokerPackageVersionStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset? SentAtUtc { get; set; }
    public string? SentBySubjectId { get; set; }
}
