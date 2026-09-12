namespace NevaxisFactOS.Persistence.Tenancy;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    public bool IsAvailable => Current is not null;

    public TenantContext? Current { get; private set; }

    public Guid RequireTenantId() => Current?.TenantId
        ?? throw new InvalidOperationException("Tenant context is required for this operation.");

    public void Set(TenantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Current is not null)
        {
            throw new InvalidOperationException("Tenant context has already been established for this scope.");
        }

        Current = context;
    }
}
