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
        return CreateSupportBundle(
            session,
            diagnostics,
            errorCode,
            PackagedRuntimePaths.DiagnosticsDirectory,
            [PackagedRuntimePaths.LogsDirectory, RuntimePaths.GetLogDirectory()],
            Path.Combine(PackagedRuntimePaths.DataRoot, "database", "logs"));
    }

    internal static string CreateSupportBundle(
        StartupSession? session,
        IReadOnlyList<string> diagnostics,
        string? errorCode,
        string outputDirectory,
        IReadOnlyList<string> logDirectories,
        string databaseLogDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var bundlePath = Path.Combine(outputDirectory, $"Nevaxis-Zentra-Diagnostics-{timestamp}.zip");

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

        foreach (var logDirectory in logDirectories)
        {
            if (!Directory.Exists(logDirectory))
            {
                continue;
            }

            foreach (var logFile in Directory.GetFiles(logDirectory, "*.log").Take(20))
            {
                AddLogEntry(archive, logFile, $"logs/{Path.GetFileName(logDirectory)}/{Path.GetFileName(logFile)}");
            }
        }

        // The bundled PostgreSQL server log and the supervisor's pg_ctl capture log (kept under logs/ as before).
        if (Directory.Exists(databaseLogDirectory))
        {
            foreach (var logFile in Directory.GetFiles(databaseLogDirectory, "*.log").Take(20))
            {
                AddLogEntry(archive, logFile, $"logs/{Path.GetFileName(logFile)}");
            }
        }

        return bundlePath;
    }

    /// <summary>
    /// Copies a log into the bundle while its writer (the supervisor or PostgreSQL itself) may still hold it open:
    /// the file is opened with FileShare.ReadWrite | Delete so a live log never blocks diagnostics. A log that still
    /// cannot be read is recorded as a note in the bundle instead of failing the whole bundle.
    /// </summary>
    internal static void AddLogEntry(ZipArchive archive, string path, string entryName)
    {
        try
        {
            using var source = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var target = entry.Open();
            source.CopyTo(target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            AddTextEntry(
                archive,
                entryName + ".unreadable.txt",
                $"This log could not be read: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void AddTextEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(Encoding.UTF8.GetBytes(content));
    }
}
