using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NevaxisFactOS.Configuration;

namespace NevaxisFactOS.EdgeAgent;

public static class Program
{
    public static Task Main(string[] args) => EdgeAgentApplication.BuildHost(args).RunAsync();
}

public static class EdgeAgentApplication
{
    public static IHost BuildHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration["Application:Name"] ??= "NevaxisFactOS Edge Agent";

        ValidateConfiguration(builder.Configuration);
        builder.Services.AddInfrastructureOptions(builder.Configuration);
        builder.Services.AddHostedService<StartupReporter>();

        return builder.Build();
    }

    private static void ValidateConfiguration(ConfigurationManager configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration["Application:Name"]))
        {
            throw new InvalidOperationException("Application:Name must be configured.");
        }
    }
}

internal sealed partial class StartupReporter(
    IConfiguration configuration,
    ILogger<StartupReporter> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogApplicationStarted(logger, configuration["Application:Name"]);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Application {ApplicationName} started")]
    private static partial void LogApplicationStarted(
        ILogger logger,
        string? applicationName);
}
