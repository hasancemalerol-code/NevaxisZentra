using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.FactoryNetworkOptimization;

public static class FactoryNetworkServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSFactoryNetwork(this IServiceCollection services)
    {
        services.AddScoped<IFactoryNetworkRepository, FactoryNetworkRepository>();
        services.AddScoped<IFactoryNetworkService, FactoryNetworkService>();
        return services;
    }
}
