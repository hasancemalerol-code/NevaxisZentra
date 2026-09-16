using System.IO;

namespace NevaxisFactOS.Desktop.Runtime;

public sealed record FactOsSelfTestResult(string Component, string State, string Detail);

public static class FactOsSelfTestService
{
    public static async Task<IReadOnlyList<FactOsSelfTestResult>> RunAsync(
        PackagedRuntimeBundle? bundle,
        CancellationToken cancellationToken = default)
    {
        var results = new List<FactOsSelfTestResult>();

        results.Add(Check("installation", bundle is not null || RuntimePaths.TryResolveRepositoryRoot() is not null,
            bundle?.BundleRoot ?? RuntimePaths.TryResolveRepositoryRoot() ?? "missing"));

        results.Add(Check("filesystem", Directory.Exists(PackagedRuntimePaths.DataRoot),
            PackagedRuntimePaths.DataRoot));

        if (bundle is not null)
        {
            results.Add(Check("database-runtime", Directory.Exists(bundle.PostgresBinDirectory), bundle.PostgresBinDirectory));
            results.Add(Check("api-binary", File.Exists(bundle.ApiExecutablePath), bundle.ApiExecutablePath));
            results.Add(Check("web-server", File.Exists(bundle.WebServerScriptPath), bundle.WebServerScriptPath));
        }

        var apiHealthy = await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
            new Uri("http://127.0.0.1:5000/health/live"),
            TimeSpan.FromSeconds(3),
            cancellationToken).ConfigureAwait(false);
        results.Add(Check("api-health", apiHealthy, "http://127.0.0.1:5000/health/live"));

        var webHealthy = await RuntimeReadinessProbe.WaitForHttpSuccessAsync(
            new Uri("http://127.0.0.1:3001/giris"),
            TimeSpan.FromSeconds(3),
            cancellationToken).ConfigureAwait(false);
        results.Add(Check("web-health", webHealthy, "http://127.0.0.1:3001/giris"));

        return results;
    }

    private static FactOsSelfTestResult Check(string component, bool ok, string detail) =>
        new(component, ok ? "HEALTHY" : "FAILED", detail);
}
