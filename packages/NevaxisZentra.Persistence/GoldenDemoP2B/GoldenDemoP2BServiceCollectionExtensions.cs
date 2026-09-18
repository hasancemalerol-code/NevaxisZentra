using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.GoldenDemoP2B;

public static class GoldenDemoP2BServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraGoldenDemoP2B(this IServiceCollection services)
    {
        services.AddScoped<IGoldenDemoP2BService, GoldenDemoP2BService>();
        return services;
    }
}
