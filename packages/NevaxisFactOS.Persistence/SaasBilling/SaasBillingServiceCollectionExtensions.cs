using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.SaasBilling;

public static class SaasBillingServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSSaasBilling(this IServiceCollection services)
    {
        services.AddScoped<ISaasBillingRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
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
