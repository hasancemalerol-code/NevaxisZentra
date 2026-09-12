using NevaxisFactOS.Api.Authentication;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.Facilities;

public sealed class FacilityAccessResolver(
    ICurrentIdentityAccessor identityAccessor,
    ITenantContextAccessor tenantContextAccessor,
    IAuthorizationContext authorizationContext,
    IPermissionEvaluator permissionEvaluator,
    IFacilityAccessReadStore facilityAccessReadStore,
    IHttpContextAccessor httpContextAccessor) : IFacilityAccessResolver
{
    public async Task<FacilityAccessResolutionResult> ResolveAsync(
        FacilityAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        var evaluatedAtUtc = DateTimeOffset.UtcNow;
        var isDevelopmentIdentity = IsDevelopmentIdentity();

        if (!identityAccessor.IsResolved || identityAccessor.Current is not { } identity)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.Unauthenticated,
                "Authenticated identity is required.",
                evaluatedAtUtc,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        if (!tenantContextAccessor.IsAvailable || tenantContextAccessor.Current is not { } tenantContext)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.TenantContextMissing,
                "Tenant context is required.",
                evaluatedAtUtc,
                identitySubjectId: identity.IdentitySubjectId,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        if (!authorizationContext.IsAvailable || authorizationContext.Current is not { } authContext)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.MembershipMissing,
                "Active tenant membership context is required.",
                evaluatedAtUtc,
                tenantId: tenantContext.TenantId,
                identitySubjectId: identity.IdentitySubjectId,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        if (request.FacilityId == Guid.Empty)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.",
                evaluatedAtUtc,
                tenantId: authContext.TenantId,
                membershipId: authContext.MembershipId,
                identitySubjectId: authContext.IdentitySubjectId,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        if (authContext.TenantId != tenantContext.TenantId
            || authContext.MembershipId != tenantContext.MembershipId)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.MembershipMissing,
                "Authorization context does not match the resolved tenant membership.",
                evaluatedAtUtc,
                tenantId: tenantContext.TenantId,
                facilityId: request.FacilityId,
                identitySubjectId: authContext.IdentitySubjectId,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        FacilityAccessProbeResult probe;
        try
        {
            probe = await facilityAccessReadStore.ProbeAsync(
                authContext.TenantId,
                request.FacilityId,
                authContext.MembershipId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.SourceUnavailable,
                "Facility access data could not be evaluated.",
                evaluatedAtUtc,
                retryable: true,
                tenantId: authContext.TenantId,
                facilityId: request.FacilityId,
                membershipId: authContext.MembershipId,
                identitySubjectId: authContext.IdentitySubjectId,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        if (!probe.FacilityExistsAndActive || !probe.MembershipExistsAndActive)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.FacilityAccessDenied,
                "Active facility membership is required for the requested facility.",
                evaluatedAtUtc,
                tenantId: authContext.TenantId,
                facilityId: request.FacilityId,
                membershipId: authContext.MembershipId,
                identitySubjectId: authContext.IdentitySubjectId,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        PermissionCheckResult permissionResult;
        try
        {
            permissionResult = await permissionEvaluator.EvaluateAsync(
                authContext.IdentitySubjectId,
                authContext.TenantId,
                authContext.MembershipId,
                request.PermissionKey,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.SourceUnavailable,
                "Permission evaluation could not be completed.",
                evaluatedAtUtc,
                retryable: true,
                tenantId: authContext.TenantId,
                facilityId: request.FacilityId,
                membershipId: authContext.MembershipId,
                identitySubjectId: authContext.IdentitySubjectId,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        if (!permissionResult.Granted)
        {
            return FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.PermissionDenied,
                permissionResult.InternalReason,
                evaluatedAtUtc,
                tenantId: authContext.TenantId,
                facilityId: request.FacilityId,
                membershipId: authContext.MembershipId,
                identitySubjectId: authContext.IdentitySubjectId,
                permissionKey: request.PermissionKey,
                isDevelopmentIdentity: isDevelopmentIdentity);
        }

        var access = new FacilityAccessContext(
            authContext.TenantId,
            request.FacilityId,
            authContext.MembershipId,
            authContext.IdentitySubjectId,
            request.PermissionKey,
            Source: "facility-access-resolver",
            isDevelopmentIdentity);

        return FacilityAccessResolutionResult.Grant(access, evaluatedAtUtc);
    }

    private bool IsDevelopmentIdentity() =>
        httpContextAccessor.HttpContext?.User.Identity?.AuthenticationType
            == DevelopmentAuthenticationHandler.SchemeName;
}
