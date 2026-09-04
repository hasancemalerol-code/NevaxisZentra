using System.IO;
using CrevaFactOS.Desktop.Configuration;
using CrevaFactOS.Desktop.Runtime;

namespace CrevaFactOS.Desktop.Tests;

public sealed class WebReadinessTests
{
    [Fact]
    public void SalesDemoWebContractUsesLoopbackHostAndFixedPort()
    {
        var options = DesktopRuntimeOptions.SalesDemo;

        Assert.Equal(3001, options.WebPort);
        Assert.Equal("127.0.0.1", options.WebOriginUri.Host);
        Assert.Equal("/giris", options.WebReadinessUri.AbsolutePath);
        Assert.Equal("http://127.0.0.1:3001/giris", options.WebReadinessUri.ToString());
    }

    [Fact]
    public void RuntimePortInspectorDetectsListeningPortState()
    {
        Assert.False(RuntimePortInspector.IsPortListening(37655));
    }

    [Fact]
    public void IsCrevaFactOSWebListenerRejectsUnrelatedProcessName()
    {
        Assert.False(RuntimePortInspector.IsCrevaFactOSWebListener(-1, FindRepositoryRoot()));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CrevaFactOS.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
