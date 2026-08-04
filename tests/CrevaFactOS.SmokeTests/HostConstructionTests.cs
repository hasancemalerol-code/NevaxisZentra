using CrevaFactOS.EdgeAgent;
using CrevaFactOS.Worker;

namespace CrevaFactOS.SmokeTests;

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
