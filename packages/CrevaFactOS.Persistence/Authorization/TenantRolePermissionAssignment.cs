using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Authorization;

public sealed class TenantRolePermissionAssignment : ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid TenantRoleId { get; set; }
    public Guid PermissionDefinitionId { get; set; }
}
