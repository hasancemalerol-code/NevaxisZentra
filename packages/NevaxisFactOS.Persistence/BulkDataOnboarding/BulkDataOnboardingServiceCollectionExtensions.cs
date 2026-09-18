using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.BulkDataOnboarding;

public static class BulkDataOnboardingServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSBulkDataOnboarding(this IServiceCollection services)
    {
        services.AddSingleton<IBulkDataSourceProvider, SyntheticStructuredSourceProvider>();
        services.AddSingleton<IBulkDataSourceProviderResolver, BulkDataSourceProviderResolver>();
        services.AddScoped<IBulkDataCanonicalizationService, BulkDataCanonicalizationService>();
        services.AddScoped<IBulkDataOnboardingService, BulkDataOnboardingService>();
        return services;
    }
}
