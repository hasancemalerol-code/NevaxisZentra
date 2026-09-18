using NevaxisZentra.Desktop.Services;

namespace NevaxisZentra.Desktop.Tests;

public sealed class SiteReachabilityProbeTests
{
    [Fact]
    public async Task UnreachableLocalPortReturnsFalse()
    {
        var reachable = await SiteReachabilityProbe.CanReachAsync(
            new Uri("http://127.0.0.1:59999"),
            TimeSpan.FromSeconds(2));

        Assert.False(reachable);
    }

    [Fact]
    public async Task ReachableLocalWebReturnsTrue()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        try
        {
            var reachable = await SiteReachabilityProbe.CanReachAsync(
                new Uri($"http://127.0.0.1:{port.ToString(System.Globalization.CultureInfo.InvariantCulture)}"),
                TimeSpan.FromSeconds(5));

            Assert.True(reachable);
        }
        finally
        {
            listener.Stop();
        }
    }
}
