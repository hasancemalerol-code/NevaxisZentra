namespace NevaxisZentra.Persistence.Tenancy;

public sealed class TenantIsolationProbe : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string ProbeValue { get; set; } = string.Empty;
}
