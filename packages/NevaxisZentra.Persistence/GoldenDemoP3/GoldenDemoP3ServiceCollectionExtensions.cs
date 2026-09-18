using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.GoldenDemoP3;

public static class GoldenDemoP3ServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraGoldenDemoP3(this IServiceCollection services)
    {
        services.AddScoped<IGoldenDemoP3Service, GoldenDemoP3Service>();
        return services;
    }
}
