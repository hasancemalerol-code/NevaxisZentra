using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.GoldenDemoCustomsFile;

public static class GoldenDemoCustomsFileServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraGoldenDemoCustomsFile(this IServiceCollection services)
    {
        services.AddScoped<IGoldenDemoCustomsFileRepository, GoldenDemoCustomsFileRepository>();
        services.AddScoped<IGoldenDemoCustomsFileService, GoldenDemoCustomsFileService>();
        return services;
    }
}
