using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Api.Development;

public sealed class DevelopmentPilotHostedBootstrapService(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    IOptions<NevaxisFactOS.Persistence.Development.DevelopmentPilotOptions> options)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() || !options.Value.Enabled)
        {
            serviceProvider.GetRequiredService<DevelopmentPilotBootstrapState>().MarkCompleted();
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var bootstrapService =
            scope.ServiceProvider.GetService<
                NevaxisFactOS.Persistence.Development.IDevelopmentPilotBootstrapService>();

        if (bootstrapService is null)
        {
            serviceProvider.GetRequiredService<DevelopmentPilotBootstrapState>().MarkCompleted();
            return;
        }

        _ = await bootstrapService.BootstrapAsync(cancellationToken);
        serviceProvider.GetRequiredService<DevelopmentPilotBootstrapState>().MarkCompleted();
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public static class DevelopmentPilotApiServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSDevelopmentPilotHostedBootstrap(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            services.AddHostedService<DevelopmentPilotHostedBootstrapService>();
        }

        return services;
    }
}
