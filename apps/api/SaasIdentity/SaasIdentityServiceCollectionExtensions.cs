using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.SaasIdentity;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.SaasIdentity;

public static class SaasIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSSaasIdentity(this IServiceCollection services)
    {
        services.AddScoped<ISaasIdentityRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableSaasIdentityRepository()
                : new SaasIdentityRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<SaasIdentityMfaSecretProtector>(),
                    serviceProvider.GetRequiredService<IIdentityEmailDeliveryProvider>(),
                    serviceProvider.GetRequiredService<TenantContextAccessor>(),
                    serviceProvider.GetService<IDevelopmentTokenDeliverySink>());
        });

        return services;
    }
}
