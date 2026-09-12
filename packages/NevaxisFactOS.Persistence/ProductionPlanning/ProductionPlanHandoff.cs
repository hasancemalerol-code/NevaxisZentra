using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.ProductionPlanning;

public sealed class ProductionPlanHandoff : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid FacilityId { get; set; }

    public Guid ProductionPlanId { get; set; }

    public uint ProductionPlanVersion { get; set; }

    public string ReferenceCode { get; set; } = string.Empty;

    public ProductionPlanHandoffStatus Status { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public DateTimeOffset PreparedAtUtc { get; set; }

    public string PreparedBySubjectId { get; set; } = string.Empty;

    public DateTimeOffset? ConsumedAtUtc { get; set; }

    public string? ProductionReference { get; set; }

    public string? FailureCode { get; set; }

    public Guid CorrelationId { get; set; }

    public uint Version { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
