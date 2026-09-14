using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.GoldenDemoP3;

public static class GoldenDemoP3ServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSGoldenDemoP3(this IServiceCollection services)
    {
        services.AddScoped<IGoldenDemoP3Service, GoldenDemoP3Service>();
        return services;
    }
}
