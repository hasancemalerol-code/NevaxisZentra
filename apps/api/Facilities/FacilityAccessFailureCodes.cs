namespace NevaxisFactOS.Api.Facilities;

public static class FacilityAccessFailureCodes
{
    public const string Unauthenticated = "unauthenticated";
    public const string TenantContextMissing = "tenant-context-missing";
    public const string MembershipMissing = "membership-missing";
    public const string FacilityMissing = "facility-missing";
    public const string FacilityAccessDenied = "facility-access-denied";
    public const string PermissionDenied = "permission-denied";
    public const string SourceUnavailable = "source-unavailable";
    public const string UnexpectedError = "unexpected-error";
}
