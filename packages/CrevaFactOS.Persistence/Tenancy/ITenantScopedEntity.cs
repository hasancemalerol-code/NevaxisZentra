namespace CrevaFactOS.Persistence.Tenancy;

public interface ITenantScopedEntity
{
    Guid TenantId { get; set; }
}
