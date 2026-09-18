using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.SaasCommercial;
using NevaxisZentra.Persistence.SaasSubscription;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.SaasSubscription;

public static class SaasSubscriptionEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSaasSubscriptionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/saas-subscription").RequireAuthorization();

        group.MapGet("/current", GetCurrentSubscriptionAsync);
        group.MapGet("/access-status", GetAccessStatusAsync);
        group.MapPost("/development/assign", AssignDevelopmentSubscriptionAsync);
        group.MapPost("/transitions", TransitionSubscriptionAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCurrentSubscriptionAsync(
        ISaasSubscriptionRepository repository,
        ISaasCommercialRepository commercialRepository,
        ITenantEntitlementEvaluator entitlementEvaluator,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasSubscriptionEndpointHelpers.ResolveReadAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var summary = await repository.GetCurrentSubscriptionSummaryAsync(cancellationToken);
        if (summary is null)
        {
            return TypedResults.NotFound();
        }

        var paidAccess = await entitlementEvaluator.EvaluatePaidApplicationAccessAsync(cancellationToken);
        var userLimit = await entitlementEvaluator.EvaluateLimitAsync("user-limit", cancellationToken);
        var facilityLimit = await entitlementEvaluator.EvaluateLimitAsync("facility-limit", cancellationToken);
        var machineLimit = await entitlementEvaluator.EvaluateLimitAsync("connected-machine-limit", cancellationToken);
        var shiftModule = await entitlementEvaluator.EvaluateModuleAccessAsync("/vardiyalar", cancellationToken);

        IReadOnlyList<SaasCommercialEntitlementReadModel> entitlements = [];
        if (!string.IsNullOrWhiteSpace(summary.PlanCode) && summary.PlanVersionNumber.HasValue)
        {
            entitlements = await commercialRepository.ResolvePlanEntitlementsAsync(
                new SaasCommercialOperationContext(
                    context.TenantId,
                    context.MembershipId,
                    context.IdentitySubjectId,
                    context.PermissionKey,
                    context.CorrelationId),
                summary.PlanCode,
                summary.PlanVersionNumber,
                cancellationToken);
        }

        return TypedResults.Ok(new
        {
            subscription = summary,
            paidApplicationAccess = paidAccess,
            entitlements,
            limits = new
            {
                users = userLimit,
                facilities = facilityLimit,
                connectedMachines = machineLimit,
            },
            moduleAccess = new
            {
                shiftWorkforce = shiftModule,
            },
            evaluatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private static async Task<IResult> GetAccessStatusAsync(
        [FromQuery] string? routePath,
        ITenantEntitlementEvaluator entitlementEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        CancellationToken cancellationToken)
    {
        if (!identityAccessor.IsResolved || !tenantContextAccessor.IsAvailable)
        {
            return TypedResults.Unauthorized();
        }

        if (SaasSubscriptionModuleEntitlements.IsRecoveryExemptRoute(routePath))
        {
            return TypedResults.Ok(new
            {
                allowed = true,
                denialReason = (string?)null,
                graceWarning = false,
                evaluatedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        var paidAccess = await entitlementEvaluator.EvaluatePaidApplicationAccessAsync(cancellationToken);
        if (!paidAccess.Allowed)
        {
            return TypedResults.Ok(new
            {
                allowed = false,
                denialReason = "paid-application",
                graceWarning = paidAccess.GraceState?.InGracePeriod == true,
                evaluatedAtUtc = paidAccess.EvaluatedAtUtc,
            });
        }

        if (!string.IsNullOrWhiteSpace(routePath))
        {
            var moduleAccess = await entitlementEvaluator.EvaluateModuleAccessAsync(
                routePath,
                cancellationToken);
            if (!moduleAccess.Allowed)
            {
                return TypedResults.Ok(new
                {
                    allowed = false,
                    denialReason = "module",
                    graceWarning = paidAccess.GraceState?.InGracePeriod == true,
                    evaluatedAtUtc = moduleAccess.EvaluatedAtUtc,
                });
            }
        }

        return TypedResults.Ok(new
        {
            allowed = true,
            denialReason = (string?)null,
            graceWarning = paidAccess.GraceState?.InGracePeriod == true,
            evaluatedAtUtc = paidAccess.EvaluatedAtUtc,
        });
    }

    private static async Task<IResult> AssignDevelopmentSubscriptionAsync(
        [FromBody] AssignDevelopmentSubscriptionRequest request,
        ISaasSubscriptionRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasSubscriptionEndpointHelpers.ResolveManageAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var command = new AssignDevelopmentSaasSubscriptionCommand(
            request.CommercialPlanVersionId,
            request.BillingCadence,
            request.SubscriptionStatus,
            request.StartsAtUtc,
            request.CurrentPeriodStartUtc,
            request.CurrentPeriodEndUtc,
            request.TrialEndsAtUtc,
            request.SourceProvenance,
            request.TransitionReason,
            request.IdempotencyKey);

        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.AssignOrChangeDevelopmentSubscriptionAsync(
            command,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return MapWriteResult(result);
    }

    private static async Task<IResult> TransitionSubscriptionAsync(
        [FromBody] TransitionSubscriptionRequest request,
        ISaasSubscriptionRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasSubscriptionEndpointHelpers.ResolveManageAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var command = new TransitionSaasSubscriptionCommand(
            request.SubscriptionId,
            request.TargetStatus,
            request.TransitionReason,
            request.ExpectedVersion,
            request.CancelAtPeriodEnd,
            request.GraceUntilUtc,
            request.IdempotencyKey);

        var actorReference = $"membership:{context.MembershipId:N}";
        var result = await repository.TransitionSubscriptionAsync(
            command,
            actorReference,
            context.CorrelationId,
            cancellationToken);
        return MapWriteResult(result);
    }

    private static IResult MapWriteResult(SaasSubscriptionWriteResult result)
    {
        if (result.Succeeded)
        {
            return result.IdempotentReplay
                ? TypedResults.Ok(result)
                : TypedResults.Created("/api/saas-subscription/current", result);
        }

        return result.FailureCode switch
        {
            SaasSubscriptionFailureCodes.ConcurrencyConflict
                or SaasSubscriptionFailureCodes.IdempotencyConflict
                or SaasSubscriptionFailureCodes.Conflict
                or SaasSubscriptionFailureCodes.InvalidTransition =>
                TypedResults.Conflict(new { failureCode = result.FailureCode, message = result.FailureMessage }),
            SaasSubscriptionFailureCodes.ValidationFailed =>
                TypedResults.UnprocessableEntity(new { failureCode = result.FailureCode, message = result.FailureMessage }),
            SaasSubscriptionFailureCodes.NotFound =>
                TypedResults.NotFound(new { failureCode = result.FailureCode, message = result.FailureMessage }),
            _ => TypedResults.Json(
                new { failureCode = result.FailureCode, message = result.FailureMessage },
                statusCode: StatusCodes.Status403Forbidden),
        };
    }
}

public static class SaasSubscriptionApiServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraSaasSubscriptionApi(this IServiceCollection services)
    {
        services.AddNevaxisZentraSaasSubscription();
        return services;
    }
}

public sealed record AssignDevelopmentSubscriptionRequest(
    Guid CommercialPlanVersionId,
    string BillingCadence,
    string SubscriptionStatus,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset CurrentPeriodStartUtc,
    DateTimeOffset CurrentPeriodEndUtc,
    DateTimeOffset? TrialEndsAtUtc,
    string SourceProvenance,
    string TransitionReason,
    string? IdempotencyKey);

public sealed record TransitionSubscriptionRequest(
    Guid SubscriptionId,
    string TargetStatus,
    string TransitionReason,
    long ExpectedVersion,
    bool? CancelAtPeriodEnd,
    DateTimeOffset? GraceUntilUtc,
    string? IdempotencyKey);
