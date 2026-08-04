namespace CrevaFactOS.Persistence.Tenancy;

public interface ITenantResolver
{
    Task<TenantContext?> ResolveActiveTenantAsync(
        string tenantIdentifier,
        CancellationToken cancellationToken = default);
}
