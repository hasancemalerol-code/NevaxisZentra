using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasCommercial;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.SaasCommercial;

public static class SaasCommercialEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSaasCommercialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/saas-commercial").RequireAuthorization();

        group.MapGet("/catalog", ListCatalogAsync);
        group.MapGet("/catalog/{planCode}", GetPlanAsync);
        group.MapGet("/catalog/{planCode}/versions/{version:int}", GetPlanVersionAsync);
        group.MapGet("/catalog/{planCode}/entitlements", GetPlanEntitlementsAsync);
        group.MapGet("/catalog/{planCode}/payment-methods", GetPaymentMethodsAsync);

        return endpoints;
    }

    private static async Task<IResult> ListCatalogAsync(
        ISaasCommercialRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasCommercialEndpointHelpers.ResolveReadAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var catalog = await repository.ListAvailablePlansAsync(context, cancellationToken);
        return TypedResults.Ok(new { plans = catalog.Plans });
    }

    private static async Task<IResult> GetPlanAsync(
        string planCode,
        ISaasCommercialRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasCommercialEndpointHelpers.ResolveReadAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var plan = await repository.GetPlanByCodeAsync(context, planCode, cancellationToken);
        return plan is null ? TypedResults.NotFound() : TypedResults.Ok(plan);
    }

    private static async Task<IResult> GetPlanVersionAsync(
        string planCode,
        int version,
        ISaasCommercialRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasCommercialEndpointHelpers.ResolveReadAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var planVersion = await repository.GetPlanVersionAsync(context, planCode, version, cancellationToken);
        return planVersion is null ? TypedResults.NotFound() : TypedResults.Ok(planVersion);
    }

    private static async Task<IResult> GetPlanEntitlementsAsync(
        string planCode,
        [FromQuery] int? version,
        ISaasCommercialRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasCommercialEndpointHelpers.ResolveReadAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var entitlements = await repository.ResolvePlanEntitlementsAsync(context, planCode, version, cancellationToken);
        return TypedResults.Ok(new { planCode, version, entitlements });
    }

    private static async Task<IResult> GetPaymentMethodsAsync(
        string planCode,
        [FromQuery] int? version,
        ISaasCommercialRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await SaasCommercialEndpointHelpers.ResolveReadAccessAsync(
            permissionEvaluator,
            identityAccessor,
            tenantContextAccessor,
            authorizationContext,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var paymentMethods = await repository.ResolvePaymentMethodCapabilitiesAsync(
            context, planCode, version, cancellationToken);
        return TypedResults.Ok(new { planCode, version, paymentMethods });
    }
}

public static class SaasCommercialServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSSaasCommercial(this IServiceCollection services)
    {
        services.AddScoped<ISaasCommercialRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableSaasCommercialRepository()
                : new SaasCommercialRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
