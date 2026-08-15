using CrevaFactOS.Persistence.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace CrevaFactOS.Persistence.SaasBilling;

public static class SaasBillingServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSSaasBilling(this IServiceCollection services)
    {
        services.AddScoped<ISaasBillingRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
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
