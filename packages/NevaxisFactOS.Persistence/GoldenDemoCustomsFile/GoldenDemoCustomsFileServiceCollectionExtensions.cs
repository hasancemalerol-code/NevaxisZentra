using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.GoldenDemoCustomsFile;

public static class GoldenDemoCustomsFileServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSGoldenDemoCustomsFile(this IServiceCollection services)
    {
        services.AddScoped<IGoldenDemoCustomsFileRepository, GoldenDemoCustomsFileRepository>();
        services.AddScoped<IGoldenDemoCustomsFileService, GoldenDemoCustomsFileService>();
        return services;
    }
}
