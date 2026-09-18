using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Export;

public sealed class ExportBrokerWorkspace : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public Guid? ShipmentId { get; set; }
    public string BrokerCompanyName { get; set; } = string.Empty;
    public string? BrokerContactName { get; set; }
    public string? BrokerEmail { get; set; }
    public string? BrokerPhone { get; set; }
    public ExportBrokerWorkspaceStatus Status { get; set; }
    public DateTimeOffset? PreparedAtUtc { get; set; }
    public string? PreparedBySubjectId { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public string? SentBySubjectId { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? BrokerReference { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}
