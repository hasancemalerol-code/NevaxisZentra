namespace NevaxisZentra.Api.Facilities;

public sealed record FacilityAccessResolutionResult
{
    public required bool Succeeded { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? FacilityId { get; init; }

    public Guid? MembershipId { get; init; }

    public Guid? IdentitySubjectId { get; init; }

    public string PermissionKey { get; init; } = string.Empty;

    public string Source { get; init; } = "facility-access-resolver";

    public string Decision { get; init; } = "denied";

    public string? FailureCode { get; init; }

    public DateTimeOffset EvaluatedAtUtc { get; init; }

    public bool Retryable { get; init; }

    public string InternalReason { get; init; } = string.Empty;

    public bool IsDevelopmentIdentity { get; init; }

    public FacilityAccessContext? Access { get; init; }

    public static FacilityAccessResolutionResult Grant(
        FacilityAccessContext access,
        DateTimeOffset evaluatedAtUtc) =>
        new()
        {
            Succeeded = true,
            TenantId = access.TenantId,
            FacilityId = access.FacilityId,
            MembershipId = access.MembershipId,
            IdentitySubjectId = access.IdentitySubjectId,
            PermissionKey = access.PermissionKey,
            Source = access.Source,
            Decision = "granted",
            EvaluatedAtUtc = evaluatedAtUtc,
            Retryable = false,
            InternalReason = "Active facility membership and permission verified.",
            IsDevelopmentIdentity = access.IsDevelopmentIdentity,
            Access = access,
        };

    public static FacilityAccessResolutionResult Deny(
        string failureCode,
        string internalReason,
        DateTimeOffset evaluatedAtUtc,
        bool retryable = false,
        Guid? tenantId = null,
        Guid? facilityId = null,
        Guid? membershipId = null,
        Guid? identitySubjectId = null,
        string? permissionKey = null,
        bool isDevelopmentIdentity = false) =>
        new()
        {
            Succeeded = false,
            TenantId = tenantId,
            FacilityId = facilityId,
            MembershipId = membershipId,
            IdentitySubjectId = identitySubjectId,
            PermissionKey = permissionKey ?? string.Empty,
            Decision = "denied",
            FailureCode = failureCode,
            EvaluatedAtUtc = evaluatedAtUtc,
            Retryable = retryable,
            InternalReason = internalReason,
            IsDevelopmentIdentity = isDevelopmentIdentity,
        };
}
