namespace NevaxisFactOS.Persistence.Tenancy;

public interface ITenantScopedEntity
{
    Guid TenantId { get; set; }
}
