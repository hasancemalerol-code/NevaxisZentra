using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NevaxisZentra.Api.Development;

public sealed class DevelopmentPilotHostedBootstrapService(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    IOptions<NevaxisZentra.Persistence.Development.DevelopmentPilotOptions> options)
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
                NevaxisZentra.Persistence.Development.IDevelopmentPilotBootstrapService>();

        if (bootstrapService is null)
        {
            Console.Error.WriteLine(
                "Development pilot bootstrap skipped: IDevelopmentPilotBootstrapService is not registered.");
            serviceProvider.GetRequiredService<DevelopmentPilotBootstrapState>().MarkCompleted();
            return;
        }

        try
        {
            await bootstrapService.EnsurePaidAccessAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Development pilot paid-access ensure failed: {exception.Message}");
        }

        try
        {
            await bootstrapService.EnsureSaasIdentityFixturesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Development pilot SaaS identity ensure failed: {exception.Message}");
        }

        try
        {
            await bootstrapService.EnsureAdminAccessFixturesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Development pilot admin access ensure failed: {exception.Message}");
        }

        try
        {
            _ = await bootstrapService.BootstrapAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Main bootstrap may fail on re-seed; acceptance-role ensure runs separately.
        }

        var state = serviceProvider.GetRequiredService<DevelopmentPilotBootstrapState>();
        try
        {
            await bootstrapService.EnsureAcceptanceRoleContractAsync(cancellationToken);
            state.MarkAcceptanceRoleVerified();
        }
        catch (Exception exception)
        {
            var message = exception is NevaxisZentra.Persistence.Authorization.AcceptanceRoleContractException
                ? exception.Message
                : $"Acceptance role contract ensure failed: {exception.Message}";
            state.MarkAcceptanceRoleFailure(message);
            Console.Error.WriteLine(message);
        }

        state.MarkCompleted();
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public static class DevelopmentPilotApiServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraDevelopmentPilotHostedBootstrap(
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
