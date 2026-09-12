using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Authorization;

internal sealed class CurrentAuthorizationContext(
    ICurrentIdentityAccessor identityAccessor,
    ITenantContextAccessor tenantAccessor) : IAuthorizationContext
{
    public bool IsAvailable => identityAccessor.IsResolved && tenantAccessor.IsAvailable;

    public AuthorizationContext? Current => IsAvailable
        ? new AuthorizationContext(
            identityAccessor.Current!.IdentitySubjectId,
            tenantAccessor.Current!.TenantId,
            tenantAccessor.Current.MembershipId)
        : null;
}
