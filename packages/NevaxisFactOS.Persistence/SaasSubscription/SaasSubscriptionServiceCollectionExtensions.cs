using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.SaasSubscription;

public static class SaasSubscriptionServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSSaasSubscription(this IServiceCollection services)
    {
        services.AddScoped<ISaasSubscriptionRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableSaasSubscriptionRepository()
                : new SaasSubscriptionRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<ITenantEntitlementEvaluator>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableTenantEntitlementEvaluator()
                : new TenantEntitlementEvaluator(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}

internal sealed class UnavailableTenantEntitlementEvaluator : ITenantEntitlementEvaluator
{
    private static TenantEntitlementEvaluationReadModel Unavailable() =>
        TenantEntitlementEvaluationReadModel.Deny(
            SaasSubscriptionEntitlementReasonCodes.EvaluationUnavailable,
            SaasSubscriptionEntitlementExplanationKeys.EvaluationUnavailable);

    public Task<TenantEntitlementEvaluationReadModel> EvaluatePaidApplicationAccessAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<TenantEntitlementEvaluationReadModel> EvaluateModuleAccessAsync(
        string routePath,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<TenantEntitlementEvaluationReadModel> EvaluateFeatureAccessAsync(
        string featureCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<TenantEntitlementEvaluationReadModel> EvaluateLimitAsync(
        string limitEntitlementCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<TenantEntitlementEvaluationReadModel> EvaluateEntitlementAsync(
        string entitlementCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<TenantEntitlementEvaluationReadModel> EvaluateUserLimitForAdditionalSeatsAsync(
        int additionalSeats,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
