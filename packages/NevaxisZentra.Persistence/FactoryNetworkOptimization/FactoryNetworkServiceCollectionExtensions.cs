using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.FactoryNetworkOptimization;

public static class FactoryNetworkServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraFactoryNetwork(this IServiceCollection services)
    {
        services.AddScoped<IFactoryNetworkRepository, FactoryNetworkRepository>();
        services.AddScoped<IFactoryNetworkService, FactoryNetworkService>();
        return services;
    }
}
