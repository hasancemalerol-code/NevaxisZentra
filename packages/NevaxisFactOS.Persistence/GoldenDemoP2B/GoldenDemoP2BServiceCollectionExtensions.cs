using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.GoldenDemoP2B;

public static class GoldenDemoP2BServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSGoldenDemoP2B(this IServiceCollection services)
    {
        services.AddScoped<IGoldenDemoP2BService, GoldenDemoP2BService>();
        return services;
    }
}
