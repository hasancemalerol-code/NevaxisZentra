using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public static class BulkDataOnboardingServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraBulkDataOnboarding(this IServiceCollection services)
    {
        services.AddSingleton<IBulkDataSourceProvider, SyntheticStructuredSourceProvider>();
        services.AddSingleton<IBulkDataSourceProviderResolver, BulkDataSourceProviderResolver>();
        services.AddScoped<IBulkDataCanonicalizationService, BulkDataCanonicalizationService>();
        services.AddScoped<IBulkDataOnboardingService, BulkDataOnboardingService>();
        return services;
    }
}
