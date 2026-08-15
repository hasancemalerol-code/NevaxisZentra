using CrevaFactOS.Persistence.AdminAccess;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasBilling;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Http;

namespace CrevaFactOS.Api.SaasBilling;

internal sealed record SaasBillingOperationContext(
    Guid TenantId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

internal static class SaasBillingEndpointHelpers
{
    public static async Task<(SaasBillingOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
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
            SaasBillingPermissionKeys.Read,
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
                SaasBillingPermissionKeys.Manage,
                cancellationToken);
            if (!adminReadResult.Granted && !manageResult.Granted)
            {
                return (null, TypedResults.Json(
                    new { failureCode = SaasBillingFailureCodes.Unavailable },
                    statusCode: StatusCodes.Status403Forbidden));
            }

            return (new SaasBillingOperationContext(
                authorization.TenantId,
                authorization.MembershipId,
                authorization.IdentitySubjectId,
                adminReadResult.Granted
                    ? AdminAccessPermissionKeys.Read
                    : SaasBillingPermissionKeys.Manage,
                correlationContext.CorrelationId), null);
        }

        return (new SaasBillingOperationContext(
            authorization.TenantId,
            authorization.MembershipId,
            authorization.IdentitySubjectId,
            SaasBillingPermissionKeys.Read,
            correlationContext.CorrelationId), null);
    }

    public static async Task<(SaasBillingOperationContext? Context, IResult? Failure)> ResolveManageAccessAsync(
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
            SaasBillingPermissionKeys.Manage,
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
                    new { failureCode = SaasBillingFailureCodes.Unavailable },
                    statusCode: StatusCodes.Status403Forbidden));
            }
        }

        return (new SaasBillingOperationContext(
            authorization.TenantId,
            authorization.MembershipId,
            authorization.IdentitySubjectId,
            SaasBillingPermissionKeys.Manage,
            correlationContext.CorrelationId), null);
    }

    public static IResult MapWriteResult(SaasBillingWriteResult result)
    {
        if (result.Succeeded)
        {
            return result.IdempotentReplay
                ? TypedResults.Ok(result)
                : TypedResults.Created("/api/saas-billing/workspace", result);
        }

        return result.FailureCode switch
        {
            SaasBillingFailureCodes.ConcurrencyConflict
                or SaasBillingFailureCodes.IdempotencyConflict
                or SaasBillingFailureCodes.Conflict
                or SaasBillingFailureCodes.InvalidTransition
                or SaasBillingFailureCodes.IdempotencyUnverifiable =>
                TypedResults.Conflict(new { failureCode = result.FailureCode, message = result.FailureMessage }),
            SaasBillingFailureCodes.ValidationFailed
                or SaasBillingFailureCodes.AmountExceeded
                or SaasBillingFailureCodes.CurrencyMismatch
                or SaasBillingFailureCodes.ReconciliationFailed =>
                TypedResults.UnprocessableEntity(new { failureCode = result.FailureCode, message = result.FailureMessage }),
            SaasBillingFailureCodes.NotFound =>
                TypedResults.NotFound(new { failureCode = result.FailureCode, message = result.FailureMessage }),
            _ => TypedResults.Json(
                new { failureCode = result.FailureCode, message = result.FailureMessage },
                statusCode: StatusCodes.Status403Forbidden),
        };
    }
}
