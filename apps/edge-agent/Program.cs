using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using NevaxisFactOS.Configuration;
using NevaxisFactOS.EdgeBuffer;

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
        builder.Services.AddSingleton<EdgeReliabilityState>();
        builder.Services.AddSingleton(sp =>
        {
            var state = sp.GetRequiredService<EdgeReliabilityState>();
            var storePath = builder.Configuration["EdgeAgent:BufferStorePath"]
                ?? EdgeBufferStore.ResolveDefaultDbPath();
            return new EdgeBufferStore(storePath, state);
        });
        builder.Services.AddHttpClient("EdgeDelivery");
        builder.Services.AddHostedService<EdgeDeliveryWorker>();
        builder.Services.AddHostedService<EdgeAgentHeartbeatReporter>();
        builder.Services.AddHostedService<EdgeAgentHealthReporter>();

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

internal sealed partial class EdgeAgentHealthReporter(
    EdgeBufferStore bufferStore,
    ILogger<EdgeAgentHealthReporter> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var health = bufferStore.GetHealth(DateTimeOffset.UtcNow);
            LogBufferHealth(
                logger,
                health.PendingCount,
                health.DeadLetterCount,
                health.BufferUtilization,
                health.ReplayStatus);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Edge buffer pending={Pending} deadLetter={DeadLetter} utilization={Utilization:P0} replay={Replay}")]
    private static partial void LogBufferHealth(
        ILogger logger,
        int pending,
        int deadLetter,
        double utilization,
        string replay);
}

internal sealed class EdgeAgentHeartbeatReporter(
    IHttpClientFactory httpClientFactory,
    EdgeBufferStore bufferStore,
    EdgeReliabilityState state,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ReportHeartbeatAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ReportHeartbeatAsync(CancellationToken cancellationToken)
    {
        var apiBase = configuration["NEVAXIS_FACTOS_API_URL"]
            ?? Environment.GetEnvironmentVariable("NEVAXIS_FACTOS_API_URL");
        if (string.IsNullOrWhiteSpace(apiBase))
        {
            return;
        }

        var health = bufferStore.GetHealth(DateTimeOffset.UtcNow);
        var token = configuration["NEVAXIS_FACTOS_DEV_TOKEN"]
            ?? Environment.GetEnvironmentVariable("NEVAXIS_FACTOS_DEV_TOKEN");
        var tenant = configuration["EdgeAgent:TenantSlug"] ?? "dev-pilot-tenant";
        var facilityId = configuration["EdgeAgent:FacilityId"] ?? "bb000001-0005-4000-8000-000000000005";
        var agentKey = configuration["EdgeAgent:AgentKey"] ?? "pilot-edge-agent-001";

        using var client = httpClientFactory.CreateClient("EdgeDelivery");
        client.Timeout = TimeSpan.FromSeconds(10);
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        client.DefaultRequestHeaders.Add("X-NevaxisFactOS-Tenant", tenant);
        client.DefaultRequestHeaders.Add("X-crevafactos-dev-Issuer", "crevafactos-dev");
        client.DefaultRequestHeaders.Add("X-crevafactos-dev-Subject", "edge-agent");

        var payload = new
        {
            agentKey,
            facilityId,
            displayName = configuration["EdgeAgent:DisplayName"] ?? "FactOS Edge Agent",
            agentVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            connectivityStatus = state.IsApiAvailable() ? "healthy" : "unavailable",
            bufferBacklogCount = health.PendingCount + health.RetryCount,
            bufferCapacity = health.MaxBufferedEvents,
            machineScopeJson = configuration["EdgeAgent:MachineScopeJson"] ?? "[]",
        };

        try
        {
            var response = await client.PostAsJsonAsync(
                $"{apiBase.TrimEnd('/')}/api/system-status/edge/heartbeat",
                payload,
                cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                state.SetApiAvailable(true);
            }
            else if ((int)response.StatusCode >= 500)
            {
                state.SetApiAvailable(false);
            }
        }
        catch
        {
            state.SetApiAvailable(false);
        }
    }
}
