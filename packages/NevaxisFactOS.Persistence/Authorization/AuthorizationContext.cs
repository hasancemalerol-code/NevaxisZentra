namespace NevaxisFactOS.Persistence.Authorization;

public sealed record AuthorizationContext(
    Guid IdentitySubjectId,
    Guid TenantId,
    Guid MembershipId);
