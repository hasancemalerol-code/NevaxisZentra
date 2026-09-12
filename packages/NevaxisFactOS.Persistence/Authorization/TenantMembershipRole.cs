using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Authorization;

public sealed class TenantMembershipRole : ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid TenantMembershipId { get; set; }
    public Guid TenantRoleId { get; set; }
    public AuthorizationStatus Status { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
}
