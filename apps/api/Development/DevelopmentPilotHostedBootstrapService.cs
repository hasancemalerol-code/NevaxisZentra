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
            await RetryTransientAsync(
                bootstrapService.EnsurePaidAccessAsync,
                cancellationToken);
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
            await RetryTransientAsync(
                bootstrapService.EnsureAcceptanceRoleContractAsync,
                cancellationToken);
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

    private static async Task RetryTransientAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        const int attempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await action(cancellationToken);
                return;
            }
            catch (Exception exception) when (
                attempt < attempts
                && exception is not NevaxisZentra.Persistence.Authorization.AcceptanceRoleContractException)
            {
                Console.Error.WriteLine(
                    $"Development pilot ensure attempt {attempt} failed: {exception.Message}");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
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
