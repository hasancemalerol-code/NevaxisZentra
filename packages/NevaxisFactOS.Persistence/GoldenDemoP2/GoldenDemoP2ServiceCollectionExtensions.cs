using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.GoldenDemoP2;

public static class GoldenDemoP2ServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSGoldenDemoP2(this IServiceCollection services)
    {
        services.AddScoped<IGoldenDemoP2Repository, GoldenDemoP2Repository>();
        services.AddScoped<IGoldenDemoP2Service, GoldenDemoP2Service>();
        return services;
    }
}
