using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Identity;

public interface ITenantMembershipResolver
{
    Task<TenantContext?> ResolveActiveMembershipAsync(
        Guid identitySubjectId,
        string tenantIdentifier,
        CancellationToken cancellationToken = default);
}
