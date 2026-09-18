using NevaxisZentra.Api.Authentication;
using NevaxisZentra.Api.SaasIdentity;
using NevaxisZentra.Persistence.AdminAccess;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.AdminAccess;

internal static class AdminAccessEndpointHelpers
{
    internal static async Task<(AdminAccessActorContext? Actor, IResult? Failure)> ResolveReadAccessAsync(
        HttpContext httpContext,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        bool requireAdminAccessRead,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = ResolveActor(
            httpContext,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext);
        if (actor is null)
        {
            return (null, failure);
        }

        if (!requireAdminAccessRead)
        {
            return (actor, null);
        }

        var permission = await permissionEvaluator.EvaluateAsync(
            actor.IdentitySubjectId,
            actor.TenantId,
            actor.MembershipId,
            AdminAccessPermissionKeys.Read,
            cancellationToken);
        return permission.Granted
            ? (actor, null)
            : (null, Forbidden(AdminAccessFailureCodes.Forbidden, "Authorization denied."));
    }

    internal static async Task<(AdminAccessActorContext? Actor, IResult? Failure)> ResolveMutationAccessAsync(
        HttpContext httpContext,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = ResolveActor(
            httpContext,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext);
        if (actor is null)
        {
            return (null, failure);
        }

        if (!actor.MfaSatisfied)
        {
            return (null, Forbidden(AdminAccessFailureCodes.MfaRequired, "Recent multi-factor authentication is required."));
        }

        var permission = await permissionEvaluator.EvaluateAsync(
            actor.IdentitySubjectId,
            actor.TenantId,
            actor.MembershipId,
            AdminAccessPermissionKeys.Manage,
            cancellationToken);
        return permission.Granted
            ? (actor, null)
            : (null, Forbidden(AdminAccessFailureCodes.Forbidden, "Authorization denied."));
    }

    internal static IResult MapWriteFailure(AdminAccessWriteResult result) =>
        result.FailureCode switch
        {
            AdminAccessFailureCodes.ValidationFailed or AdminAccessFailureCodes.InvalidGrant
                or AdminAccessFailureCodes.InvalidScope or AdminAccessFailureCodes.RetiredPermission
                or AdminAccessFailureCodes.RetiredRole => TypedResults.Json(
                    Problem(400, result.FailureCode, result.FailureMessage),
                    statusCode: StatusCodes.Status400BadRequest),
            AdminAccessFailureCodes.NotFound => TypedResults.Json(
                Problem(404, result.FailureCode, result.FailureMessage),
                statusCode: StatusCodes.Status404NotFound),
            AdminAccessFailureCodes.ConcurrencyConflict or AdminAccessFailureCodes.IdempotencyConflict => TypedResults.Json(
                Problem(409, result.FailureCode, result.FailureMessage),
                statusCode: StatusCodes.Status409Conflict),
            AdminAccessFailureCodes.Unauthenticated => TypedResults.Json(
                Problem(401, result.FailureCode, result.FailureMessage),
                statusCode: StatusCodes.Status401Unauthorized),
            _ => Forbidden(result.FailureCode ?? AdminAccessFailureCodes.Forbidden, result.FailureMessage ?? "Authorization denied."),
        };

    internal static IResult MapInvitationFailure(CreateAdminAccessInvitationResult result) =>
        result.FailureCode switch
        {
            AdminAccessFailureCodes.ValidationFailed or AdminAccessFailureCodes.InvalidGrant => TypedResults.Json(
                Problem(400, result.FailureCode, result.FailureMessage),
                statusCode: StatusCodes.Status400BadRequest),
            AdminAccessFailureCodes.NotFound => TypedResults.Json(
                Problem(404, result.FailureCode, result.FailureMessage),
                statusCode: StatusCodes.Status404NotFound),
            AdminAccessFailureCodes.IdempotencyConflict => TypedResults.Json(
                Problem(409, result.FailureCode, result.FailureMessage),
                statusCode: StatusCodes.Status409Conflict),
            _ => Forbidden(result.FailureCode ?? AdminAccessFailureCodes.Forbidden, result.FailureMessage ?? "Authorization denied."),
        };

    internal static IResult Forbidden(string code, string title) =>
        TypedResults.Json(Problem(403, code, title), statusCode: StatusCodes.Status403Forbidden);

    internal static object Problem(int status, string code, string? title) =>
        new
        {
            type = "about:blank",
            status,
            title = title ?? "Request failed.",
            code,
        };

    private static (AdminAccessActorContext? Actor, IResult? Failure) ResolveActor(
        HttpContext httpContext,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext)
    {
        if (!identityAccessor.IsResolved || identityAccessor.Current is not { } identity)
        {
            return (null, TypedResults.Json(
                Problem(401, AdminAccessFailureCodes.Unauthenticated, "Authentication is required."),
                statusCode: StatusCodes.Status401Unauthorized));
        }

        if (!tenantContextAccessor.IsAvailable || tenantContextAccessor.Current is not { } tenantContext)
        {
            return (null, Forbidden(AdminAccessFailureCodes.Forbidden, "Tenant context is required."));
        }

        if (!authorizationContext.IsAvailable || authorizationContext.Current is not { } authContext)
        {
            return (null, Forbidden(AdminAccessFailureCodes.Forbidden, "Active tenant membership context is required."));
        }

        if (authContext.TenantId != tenantContext.TenantId
            || authContext.MembershipId != tenantContext.MembershipId)
        {
            return (null, Forbidden(AdminAccessFailureCodes.CrossTenant, "Authorization context does not match tenant membership."));
        }

        var session = SaasIdentityEndpointHelpers.GetSessionContext(httpContext);
        var actorReference = session?.AccountReference ?? $"subject_{authContext.IdentitySubjectId:N}";
        var membershipReference = TenantMembership.CreateReference(authContext.MembershipId);
        return (new AdminAccessActorContext(
            authContext.TenantId,
            authContext.MembershipId,
            authContext.IdentitySubjectId,
            membershipReference,
            actorReference,
            session?.MfaSatisfied == true && session.PendingMfa == false,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null), null);
    }
}
