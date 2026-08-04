namespace CrevaFactOS.Persistence.Tenancy;

public interface ITenantContextAccessor
{
    bool IsAvailable { get; }

    TenantContext? Current { get; }

    Guid RequireTenantId();
}
