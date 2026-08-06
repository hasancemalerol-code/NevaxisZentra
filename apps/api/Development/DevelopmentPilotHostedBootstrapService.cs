using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Api.Development;

public sealed class DevelopmentPilotHostedBootstrapService(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    IOptions<CrevaFactOS.Persistence.Development.DevelopmentPilotOptions> options)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() || !options.Value.Enabled)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var bootstrapService =
            scope.ServiceProvider.GetRequiredService<
                CrevaFactOS.Persistence.Development.IDevelopmentPilotBootstrapService>();

        _ = await bootstrapService.BootstrapAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public static class DevelopmentPilotApiServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSDevelopmentPilotHostedBootstrap(
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
