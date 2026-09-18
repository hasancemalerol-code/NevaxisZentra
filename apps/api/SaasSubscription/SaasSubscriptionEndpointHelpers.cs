using NevaxisZentra.Persistence.AdminAccess;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.SaasSubscription;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Http;

namespace NevaxisZentra.Api.SaasSubscription;

internal static class SaasSubscriptionEndpointHelpers
{
    public static async Task<(SaasSubscriptionOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (!identityAccessor.IsResolved || !tenantContextAccessor.IsAvailable)
        {
            return (null, TypedResults.Unauthorized());
        }

        var authorization = authorizationContext.Current;
        if (authorization is null)
        {
            return (null, TypedResults.Unauthorized());
        }

        var readResult = await permissionEvaluator.EvaluateAsync(
            authorization.IdentitySubjectId,
            authorization.TenantId,
            authorization.MembershipId,
            SaasSubscriptionPermissionKeys.Read,
            cancellationToken);
        if (!readResult.Granted)
        {
            var adminReadResult = await permissionEvaluator.EvaluateAsync(
                authorization.IdentitySubjectId,
                authorization.TenantId,
                authorization.MembershipId,
                AdminAccessPermissionKeys.Read,
                cancellationToken);
            var manageResult = await permissionEvaluator.EvaluateAsync(
                authorization.IdentitySubjectId,
                authorization.TenantId,
                authorization.MembershipId,
                SaasSubscriptionPermissionKeys.Manage,
                cancellationToken);
            if (!adminReadResult.Granted && !manageResult.Granted)
            {
                return (null, TypedResults.Json(
                    new { failureCode = SaasSubscriptionFailureCodes.Unavailable },
                    statusCode: StatusCodes.Status403Forbidden));
            }

            return (new SaasSubscriptionOperationContext(
                authorization.TenantId,
                authorization.MembershipId,
                authorization.IdentitySubjectId,
                adminReadResult.Granted
                    ? AdminAccessPermissionKeys.Read
                    : SaasSubscriptionPermissionKeys.Manage,
                correlationContext.CorrelationId), null);
        }

        return (new SaasSubscriptionOperationContext(
            authorization.TenantId,
            authorization.MembershipId,
            authorization.IdentitySubjectId,
            SaasSubscriptionPermissionKeys.Read,
            correlationContext.CorrelationId), null);
    }

    public static async Task<(SaasSubscriptionOperationContext? Context, IResult? Failure)> ResolveManageAccessAsync(
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (!identityAccessor.IsResolved || !tenantContextAccessor.IsAvailable)
        {
            return (null, TypedResults.Unauthorized());
        }

        var authorization = authorizationContext.Current;
        if (authorization is null)
        {
            return (null, TypedResults.Unauthorized());
        }

        var manageResult = await permissionEvaluator.EvaluateAsync(
            authorization.IdentitySubjectId,
            authorization.TenantId,
            authorization.MembershipId,
            SaasSubscriptionPermissionKeys.Manage,
            cancellationToken);
        if (!manageResult.Granted)
        {
            var adminManageResult = await permissionEvaluator.EvaluateAsync(
                authorization.IdentitySubjectId,
                authorization.TenantId,
                authorization.MembershipId,
                AdminAccessPermissionKeys.Manage,
                cancellationToken);
            if (!adminManageResult.Granted)
            {
                return (null, TypedResults.Json(
                    new { failureCode = SaasSubscriptionFailureCodes.Unavailable },
                    statusCode: StatusCodes.Status403Forbidden));
            }
        }

        return (new SaasSubscriptionOperationContext(
            authorization.TenantId,
            authorization.MembershipId,
            authorization.IdentitySubjectId,
            SaasSubscriptionPermissionKeys.Manage,
            correlationContext.CorrelationId), null);
    }
}
