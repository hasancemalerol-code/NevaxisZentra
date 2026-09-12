using NevaxisFactOS.EdgeAgent;
using NevaxisFactOS.Worker;

namespace NevaxisFactOS.SmokeTests;

public sealed class HostConstructionTests
{
    [Fact]
    public async Task WorkerHostStartsAndStops()
    {
        using var host = WorkerApplication.BuildHost([]);

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public async Task EdgeAgentHostStartsAndStops()
    {
        using var host = EdgeAgentApplication.BuildHost([]);

        await host.StartAsync();
        await host.StopAsync();
    }
}
