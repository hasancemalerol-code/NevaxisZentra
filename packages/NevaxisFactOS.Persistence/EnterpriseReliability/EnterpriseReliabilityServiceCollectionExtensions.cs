using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.EnterpriseReliability;

public static class EnterpriseReliabilityServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSEnterpriseReliability(this IServiceCollection services)
    {
        services.AddScoped<IEnterpriseReliabilityRepository, EnterpriseReliabilityRepository>();
        services.AddScoped<IEnterpriseReliabilityService, EnterpriseReliabilityService>();
        return services;
    }
}
