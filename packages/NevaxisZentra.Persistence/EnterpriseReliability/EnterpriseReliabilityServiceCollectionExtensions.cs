using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.EnterpriseReliability;

public static class EnterpriseReliabilityServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraEnterpriseReliability(this IServiceCollection services)
    {
        services.AddScoped<IEnterpriseReliabilityRepository, EnterpriseReliabilityRepository>();
        services.AddScoped<IEnterpriseReliabilityService, EnterpriseReliabilityService>();
        return services;
    }
}
