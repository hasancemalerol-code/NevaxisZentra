using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.AiUsage;

/// <summary>Generic AI usage metering record (not Export-specific).</summary>
public sealed class AiUsageRecord : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string UserSubjectId { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public Guid RunId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int InputUsage { get; set; }
    public int OutputUsage { get; set; }
    public decimal CreditsConsumed { get; set; }
    public decimal? EstimatedProviderCost { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
