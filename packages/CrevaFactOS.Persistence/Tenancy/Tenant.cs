namespace CrevaFactOS.Persistence.Tenancy;

public sealed class Tenant
{
    public Guid Id { get; set; }

    public string TenantKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public TenantStatus Status { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
