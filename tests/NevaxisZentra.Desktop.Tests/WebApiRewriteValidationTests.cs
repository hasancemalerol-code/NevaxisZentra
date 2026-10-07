using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace NevaxisZentra.Desktop.Tests;

public sealed class WebApiRewriteValidationTests : IDisposable
{
    private const string Api = "http://127.0.0.1:5000";
    private readonly string root = Path.Combine(Path.GetTempPath(), $"zentra-rewrites-{Guid.NewGuid():N}");

    public WebApiRewriteValidationTests() => Directory.CreateDirectory(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private string WriteManifest(IEnumerable<(string Source, string Destination)> afterFiles)
    {
        var path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".json");
        var manifest = new
        {
            version = 3,
            rewrites = new
            {
                beforeFiles = Array.Empty<object>(),
                afterFiles = afterFiles.Select(r => new { source = r.Source, destination = r.Destination, regex = "x" }).ToArray(),
                fallback = Array.Empty<object>(),
            },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(manifest));
        return path;
    }

    private static (string Source, string Destination)[] AllRequired() =>
    [
        ("/api/identity/:path*", $"{Api}/api/identity/:path*"),
        ("/api/admin-access/:path*", $"{Api}/api/admin-access/:path*"),
        ("/api/saas-subscription/:path*", $"{Api}/api/saas-subscription/:path*"),
        ("/api/public/ai-task-access/:path*", $"{Api}/api/public/ai-task-access/:path*"),
    ];

    private static (int Exit, string Output) RunGate(string manifestPath)
    {
        var repo = RealPostgres.RepositoryRoot ?? throw new InvalidOperationException("repository root not found");
        var script = Path.Combine(repo, "scripts", "release", "validate-web-api-rewrites.ps1");
        var startInfo = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, "-ManifestPath", manifestPath, "-ApiBaseUrl", Api })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }

    [Fact]
    public void PresentRewritesPassTheGate()
    {
        var (exit, output) = RunGate(WriteManifest(AllRequired()));

        Assert.Equal(0, exit);
        Assert.Contains("WEB_API_REWRITES_VALIDATION: PASS", output, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingRewritesFailTheGate()
    {
        var (exit, output) = RunGate(WriteManifest([]));

        Assert.NotEqual(0, exit);
        Assert.Contains("/api/identity/:path*", output, StringComparison.Ordinal);
    }

    [Fact]
    public void AnySingleMissingRewriteFailsTheGate()
    {
        var (exit, output) = RunGate(WriteManifest(AllRequired().Where(r => !r.Source.Contains("identity", StringComparison.Ordinal))));

        Assert.NotEqual(0, exit);
        Assert.Contains("identity", output, StringComparison.Ordinal);
    }

    [Fact]
    public void RewritesPointingAtTheWrongApiFailTheGate()
    {
        var wrong = AllRequired().Select(r => (r.Source, r.Destination.Replace(":5000", ":9999", StringComparison.Ordinal)));

        var (exit, _) = RunGate(WriteManifest(wrong));

        Assert.NotEqual(0, exit);
    }

    [Fact]
    public void AbsentManifestFailsTheGate()
    {
        var (exit, _) = RunGate(Path.Combine(root, "does-not-exist.json"));

        Assert.NotEqual(0, exit);
    }
}
