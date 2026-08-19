using CrevaFactOS.Persistence.AdminAccess;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasBilling;
using CrevaFactOS.Persistence.SaasBilling.Payments;
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

        return MapFailureResult(result.FailureCode, result.FailureMessage);
    }

    public static IResult MapWriteResult<T>(SaasBillingWriteResult<T> result, Func<T, object> mapSuccess)
    {
        if (result.Succeeded && result.Value is not null)
        {
            return result.IdempotentReplay
                ? TypedResults.Ok(mapSuccess(result.Value))
                : TypedResults.Created("/api/saas-billing/payments/checkout/initialize", mapSuccess(result.Value));
        }

        return MapFailureResult(result.FailureCode, result.FailureMessage);
    }

    private static IResult MapFailureResult(string? failureCode, string? failureMessage) =>
        failureCode switch
        {
            SaasBillingFailureCodes.ConcurrencyConflict
                or SaasBillingFailureCodes.IdempotencyConflict
                or SaasBillingFailureCodes.Conflict
                or SaasBillingFailureCodes.InvalidTransition
                or SaasBillingFailureCodes.IdempotencyUnverifiable
                or SaasPaymentFailureCodes.IdempotencyConflict
                or SaasPaymentFailureCodes.IdempotencyUnverifiable
                or SaasPaymentFailureCodes.Conflict
                or SaasPaymentFailureCodes.ConcurrencyConflict =>
                TypedResults.Conflict(new { failureCode, message = failureMessage }),
            SaasBillingFailureCodes.ValidationFailed
                or SaasBillingFailureCodes.AmountExceeded
                or SaasBillingFailureCodes.CurrencyMismatch
                or SaasBillingFailureCodes.ReconciliationFailed
                or SaasPaymentFailureCodes.ValidationFailed
                or SaasPaymentFailureCodes.AmountMismatch
                or SaasPaymentFailureCodes.CurrencyMismatch
                or SaasPaymentFailureCodes.ReconciliationReviewRequired
                or SaasPaymentFailureCodes.ProviderNotConfigured
                or SaasPaymentFailureCodes.ProviderUnavailable
                or SaasPaymentFailureCodes.CheckoutVerificationFailed
                or SaasPaymentFailureCodes.CheckoutExpired
                or SaasPaymentFailureCodes.WebhookRejected =>
                TypedResults.UnprocessableEntity(new { failureCode, message = failureMessage }),
            SaasBillingFailureCodes.NotFound
                or SaasPaymentFailureCodes.NotFound =>
                TypedResults.NotFound(new { failureCode, message = failureMessage }),
            SaasPaymentFailureCodes.WebhookSignatureInvalid
                or SaasPaymentFailureCodes.Unauthorized =>
                TypedResults.Unauthorized(),
            SaasPaymentFailureCodes.Forbidden =>
                TypedResults.Json(
                    new { failureCode, message = failureMessage },
                    statusCode: StatusCodes.Status403Forbidden),
            _ => TypedResults.Json(
                new { failureCode, message = failureMessage },
                statusCode: StatusCodes.Status403Forbidden),
        };
}
