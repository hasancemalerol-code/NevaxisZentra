using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Identity;

public interface ITenantMembershipResolver
{
    Task<TenantContext?> ResolveActiveMembershipAsync(
        Guid identitySubjectId,
        string tenantIdentifier,
        CancellationToken cancellationToken = default);
}
