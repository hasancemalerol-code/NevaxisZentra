using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.SaasIdentity;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Api.SaasIdentity;

public static class SaasIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSSaasIdentity(this IServiceCollection services)
    {
        services.AddScoped<ISaasIdentityRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
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
