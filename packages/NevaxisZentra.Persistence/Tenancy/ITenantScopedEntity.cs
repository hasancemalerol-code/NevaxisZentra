namespace NevaxisZentra.Persistence.Tenancy;

public interface ITenantScopedEntity
{
    Guid TenantId { get; set; }
}
