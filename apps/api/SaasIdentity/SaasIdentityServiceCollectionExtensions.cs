using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.SaasIdentity;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.SaasIdentity;

public static class SaasIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraSaasIdentity(this IServiceCollection services)
    {
        services.AddScoped<ISaasIdentityRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
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
