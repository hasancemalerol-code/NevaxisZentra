using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public static class BulkDataOnboardingServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraBulkDataOnboarding(this IServiceCollection services)
    {
        services.AddSingleton<IBulkImportFileStore, BulkImportFileStore>();
        services.AddScoped<IBulkImportSourceContextAccessor, BulkImportSourceContextAccessor>();
        services.AddScoped<IBulkDataSourceProvider, SyntheticStructuredSourceProvider>();
        services.AddScoped<IBulkDataSourceProvider, CsvFileSourceProvider>();
        services.AddScoped<IBulkDataSourceProvider, XlsxFileSourceProvider>();
        services.AddScoped<IBulkDataSourceProviderResolver, BulkDataSourceProviderResolver>();
        services.AddScoped<IBulkDataCanonicalizationService, BulkDataCanonicalizationService>();
        services.AddScoped<IBulkDataOnboardingService, BulkDataOnboardingService>();
        return services;
    }
}
