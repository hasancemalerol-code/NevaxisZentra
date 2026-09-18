using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Authorization;

public sealed class TenantRolePermissionAssignment : ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid TenantRoleId { get; set; }
    public Guid PermissionDefinitionId { get; set; }
    public string? RecordScope { get; set; }
    public DateTimeOffset? EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveUntilUtc { get; set; }
}
