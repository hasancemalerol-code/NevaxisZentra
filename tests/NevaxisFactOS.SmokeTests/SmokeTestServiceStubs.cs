using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using NevaxisFactOS.Api.Development;
using NevaxisFactOS.Persistence.SaasSubscription;

namespace NevaxisFactOS.SmokeTests;

internal static class SmokeTestServiceStubs
{
    internal static void ConfigureIsolatedDevelopmentHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "Development",
                ["Persistence:Enabled"] = "false",
                ["DevelopmentPilot:Enabled"] = "false",
                ["DevelopmentPilot:ProductionPlanSeedEnabled"] = "false",
            });
        });
        builder.ConfigureServices(services =>
        {
            RemoveDevelopmentPilotHostedBootstrap(services);
            services.ReplaceEntitlementEvaluatorWithAllowStub();
        });
    }

    internal static void RemoveDevelopmentPilotHostedBootstrap(IServiceCollection services)
    {
        var descriptors = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(DevelopmentPilotHostedBootstrapService))
            .ToArray();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    internal static void ReplaceEntitlementEvaluatorWithAllowStub(this IServiceCollection services)
    {
        services.RemoveAll<ITenantEntitlementEvaluator>();
        services.AddScoped<ITenantEntitlementEvaluator, AllowingEntitlementEvaluator>();
    }

    private sealed class AllowingEntitlementEvaluator : ITenantEntitlementEvaluator
    {
        private static TenantEntitlementEvaluationReadModel Allowed() =>
            TenantEntitlementEvaluationReadModel.Allow("active", null);

        public Task<TenantEntitlementEvaluationReadModel> EvaluatePaidApplicationAccessAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Allowed());

        public Task<TenantEntitlementEvaluationReadModel> EvaluateModuleAccessAsync(
            string routePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Allowed());

        public Task<TenantEntitlementEvaluationReadModel> EvaluateFeatureAccessAsync(
            string featureCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Allowed());

        public Task<TenantEntitlementEvaluationReadModel> EvaluateLimitAsync(
            string limitEntitlementCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Allowed());

        public Task<TenantEntitlementEvaluationReadModel> EvaluateEntitlementAsync(
            string entitlementCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Allowed());

        public Task<TenantEntitlementEvaluationReadModel> EvaluateUserLimitForAdditionalSeatsAsync(
            int additionalSeats,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Allowed());
    }
}
