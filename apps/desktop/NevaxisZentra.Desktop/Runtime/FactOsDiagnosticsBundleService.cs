using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace NevaxisZentra.Desktop.Runtime;

public static class FactOsDiagnosticsBundleService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static string CreateSupportBundle(
        StartupSession? session,
        IReadOnlyList<string> diagnostics,
        string? errorCode = null)
    {
        PackagedRuntimePaths.EnsureCustomerDirectories();
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var bundlePath = Path.Combine(
            PackagedRuntimePaths.DiagnosticsDirectory,
            $"Nevaxis-FactOS-Diagnostics-{timestamp}.zip");

        var summary = new
        {
            product = "Nevaxis Zentra",
            generatedAtUtc = DateTimeOffset.UtcNow,
            sessionId = session?.SessionId,
            errorCode,
            applicationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            osVersion = Environment.OSVersion.VersionString,
            machineName = Environment.MachineName,
            runtimeMode = RuntimePaths.ResolveRuntimeMode().ToString(),
            bundleRoot = PackagedRuntimeBundle.TryResolveFromDesktopExecutable()?.BundleRoot,
            diagnostics,
        };

        using var archive = ZipFile.Open(bundlePath, ZipArchiveMode.Create);
        AddTextEntry(archive, "summary.json", JsonSerializer.Serialize(summary, JsonOptions));

        foreach (var logDirectory in new[]
        {
            PackagedRuntimePaths.LogsDirectory,
            RuntimePaths.GetLogDirectory(),
        })
        {
            if (!Directory.Exists(logDirectory))
            {
                continue;
            }

            foreach (var logFile in Directory.GetFiles(logDirectory, "*.log").Take(20))
            {
                var entryName = $"logs/{Path.GetFileName(logDirectory)}/{Path.GetFileName(logFile)}";
                archive.CreateEntryFromFile(logFile, entryName, CompressionLevel.Optimal);
            }
        }

        var postgresLog = Path.Combine(PackagedRuntimePaths.DataRoot, "database", "logs", "postgresql.log");
        if (File.Exists(postgresLog))
        {
            archive.CreateEntryFromFile(postgresLog, "logs/postgresql.log", CompressionLevel.Optimal);
        }

        return bundlePath;
    }

    private static void AddTextEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(Encoding.UTF8.GetBytes(content));
    }
}
