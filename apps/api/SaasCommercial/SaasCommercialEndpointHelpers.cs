using NevaxisFactOS.Api.Authentication;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.SaasCommercial;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.SaasCommercial;

internal static class SaasCommercialEndpointHelpers
{
    internal static async Task<(SaasCommercialOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (!identityAccessor.IsResolved || identityAccessor.Current is not { } identity)
        {
            return (null, TypedResults.Unauthorized());
        }

        if (!tenantContextAccessor.IsAvailable || tenantContextAccessor.Current is not { } tenantContext)
        {
            return (null, TypedResults.Json(
                new { type = "about:blank", status = 403, title = "Tenant context is required." },
                statusCode: StatusCodes.Status403Forbidden));
        }

        if (!authorizationContext.IsAvailable || authorizationContext.Current is not { } authContext)
        {
            return (null, TypedResults.Json(
                new { type = "about:blank", status = 403, title = "Active tenant membership context is required." },
                statusCode: StatusCodes.Status403Forbidden));
        }

        if (authContext.TenantId != tenantContext.TenantId
            || authContext.MembershipId != tenantContext.MembershipId)
        {
            return (null, TypedResults.Json(
                new { type = "about:blank", status = 403, title = "Authorization context does not match tenant membership." },
                statusCode: StatusCodes.Status403Forbidden));
        }

        var permissionResult = await permissionEvaluator.EvaluateAsync(
            authContext.IdentitySubjectId,
            authContext.TenantId,
            authContext.MembershipId,
            SaasCommercialPermissionKeys.Read,
            cancellationToken);

        if (!permissionResult.Granted)
        {
            return (null, TypedResults.Json(
                new { type = "about:blank", status = 403, title = "Authorization denied." },
                statusCode: StatusCodes.Status403Forbidden));
        }

        return (new SaasCommercialOperationContext(
            authContext.TenantId,
            authContext.MembershipId,
            authContext.IdentitySubjectId,
            SaasCommercialPermissionKeys.Read,
            correlationContext.CorrelationId), null);
    }
}
