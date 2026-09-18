using NevaxisZentra.Persistence.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.SaasBilling;

public static class SaasBillingServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraSaasBilling(this IServiceCollection services)
    {
        services.AddScoped<ISaasBillingRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableSaasBillingRepository()
                : new SaasBillingRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddSingleton<IBillingPaymentProvider, DevelopmentBillingPaymentProvider>();
        return services;
    }
}
