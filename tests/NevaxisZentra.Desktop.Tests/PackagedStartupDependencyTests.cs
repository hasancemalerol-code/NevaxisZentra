using System.IO;
using System.Reflection;
using NevaxisZentra.Desktop.Configuration;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

/// <summary>The behaviour test below changes the process-wide PATH, so it must not run alongside other tests.</summary>
[CollectionDefinition("ProcessEnvironmentMutation", DisableParallelization = true)]
public sealed class ProcessEnvironmentMutationGroup;

/// <summary>
/// Release blocker regression: a packaged PRODUCTION installation on a machine without Docker must reach
/// PostgreSQL through the bundled psql.exe and never look up the docker executable.
/// </summary>
[Collection("ProcessEnvironmentMutation")]
public sealed class PackagedStartupDependencyTests
{
    [Theory]
    [InlineData(DesktopRuntimeMode.Packaged, true, "BundledClient")]
    [InlineData(DesktopRuntimeMode.Development, true, "BundledClient")]
    [InlineData(DesktopRuntimeMode.Development, false, "DevelopmentDockerContainer")]
    public void RouteSelectionMatchesMode(DesktopRuntimeMode mode, bool bundledClient, string expected) =>
        Assert.Equal(expected, PostgresQueryRoutePolicy.Select(mode, bundledClient).ToString());

    [Fact]
    public void PackagedModeNeverSelectsDockerEvenWithoutBundledClient()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => PostgresQueryRoutePolicy.Select(DesktopRuntimeMode.Packaged, bundledClientAvailable: false));
        Assert.DoesNotContain("docker", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PackagedProductionWithoutDockerQueriesThroughBundledPostgresClient()
    {
        var bundleRoot = Path.Combine(Path.GetTempPath(), $"zentra-bundle-{Guid.NewGuid():N}");
        var emptyPath = Directory.CreateTempSubdirectory("no-docker-").FullName;
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Directory.CreateDirectory(Path.Combine(bundleRoot, "manifests"));
            Directory.CreateDirectory(Path.Combine(bundleRoot, "desktop"));
            Directory.CreateDirectory(Path.Combine(bundleRoot, "postgres", "bin"));
            File.WriteAllText(
                Path.Combine(bundleRoot, "manifests", "runtime-manifest.json"),
                """{"webPort":3001,"databaseArchitecture":"EMBEDDED_LOCAL","deploymentChannel":"PRODUCTION"}""");
            // Stand-in for the bundled psql.exe: any executable that exits 0 and prints a line.
            File.Copy(
                Path.Combine(Environment.SystemDirectory, "hostname.exe"),
                Path.Combine(bundleRoot, "postgres", "bin", "psql.exe"));

            var bundle = PackagedRuntimeBundle.TryResolveFromDesktopExecutable(Path.Combine(bundleRoot, "desktop"));
            Assert.NotNull(bundle);

            var supervisor = new DesktopRuntimeSupervisor();
            typeof(DesktopRuntimeSupervisor)
                .GetField("_packagedBundle", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(supervisor, bundle);

            Environment.SetEnvironmentVariable("PATH", emptyPath); // Docker is not installed.
            var route = typeof(DesktopRuntimeSupervisor).GetMethod(
                "QueryScalarRoutedAsync",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var options = new DesktopRuntimeOptions { Mode = DesktopRuntimeMode.Packaged };

            var task = (Task<string?>)route.Invoke(
                supervisor,
                [options, "127.0.0.1", 55432, "crevafactos_local", "user", "pw", "SELECT 1;", CancellationToken.None])!;
            await task; // would throw Win32Exception for "docker" if the Docker route were taken

            var diagnostics = (IEnumerable<string>)typeof(DesktopRuntimeSupervisor)
                .GetField("_diagnostics", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(supervisor)!;
            Assert.Contains("postgres=attached-existing-bundled-client", diagnostics);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Directory.Delete(emptyPath, recursive: true);
            Directory.Delete(bundleRoot, recursive: true);
        }
    }
}

public sealed class CustomerVisibleBrandingTests
{
    [Fact]
    public void StartupErrorCodesAndMessagesUseZentraBranding()
    {
        var codes = typeof(FactOsStartupErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        Assert.NotEmpty(codes);
        foreach (var code in codes)
        {
            Assert.StartsWith("ZENTRA-STARTUP-", code, StringComparison.Ordinal);
            Assert.DoesNotContain("factos", FactOsStartupErrorCodes.ToCustomerMessage(code), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void StartupWindowsShowNoLegacyFactOsBranding()
    {
        var views = FindViewsDirectory();
        foreach (var file in new[] { "StartupWindow.xaml", "StartupFailureWindow.xaml" })
        {
            var xaml = File.ReadAllText(Path.Combine(views, file));
            Assert.DoesNotContain("factos", xaml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Nevaxis Zentra", xaml, StringComparison.Ordinal);
        }
    }

    private static string FindViewsDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "apps", "desktop", "NevaxisZentra.Desktop", "Views");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Desktop Views directory not found from test base directory.");
    }
}
