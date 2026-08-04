using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Identity;

public interface ITenantMembershipResolver
{
    Task<TenantContext?> ResolveActiveMembershipAsync(
        Guid identitySubjectId,
        string tenantIdentifier,
        CancellationToken cancellationToken = default);
}
