using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.GoldenDemoP1;

public static class GoldenDemoP1ServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSGoldenDemoP1(this IServiceCollection services)
    {
        services.AddScoped<IGoldenDemoP1Repository, GoldenDemoP1Repository>();
        services.AddScoped<IGoldenDemoPresentationAiTaskSyncService, GoldenDemoPresentationAiTaskSyncService>();
        services.AddScoped<IGoldenDemoP1Service, GoldenDemoP1Service>();
        return services;
    }
}
