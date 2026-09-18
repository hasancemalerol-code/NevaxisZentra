namespace NevaxisZentra.Api.Facilities;

public sealed record FacilityAccessContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    string Source,
    bool IsDevelopmentIdentity);
