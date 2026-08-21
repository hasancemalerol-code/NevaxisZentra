using CrevaFactOS.Desktop.Services;

namespace CrevaFactOS.Desktop.Tests;

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
        var reachable = await SiteReachabilityProbe.CanReachAsync(
            new Uri("http://127.0.0.1:3001"),
            TimeSpan.FromSeconds(5));

        Assert.True(reachable);
    }
}
