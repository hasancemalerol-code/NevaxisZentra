using System.Globalization;
using System.IO;
using System.Text;
using NevaxisFactOS.Desktop.Runtime;

namespace NevaxisFactOS.Desktop.Services;

public static class DesktopLifecycleLogger
{
    private static readonly object Sync = new();
    private static string? _primaryLogPath;
    private static string? _artifactLogPath;
    private static string? _programDataLogPath;
    private static string? _startupLogPath;

    public static void Initialize()
    {
        lock (Sync)
        {
            PackagedRuntimePaths.EnsureCustomerDirectories();
            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            _programDataLogPath = Path.Combine(PackagedRuntimePaths.LogsDirectory, $"desktop-{timestamp}.log");
            _startupLogPath = Path.Combine(PackagedRuntimePaths.LogsDirectory, "startup.log");
            _primaryLogPath = RuntimePaths.CreateLogFilePath("desktop-lifecycle");
            try
            {
                var repositoryRoot = RuntimePaths.TryResolveRepositoryRoot();
                if (!string.IsNullOrWhiteSpace(repositoryRoot))
                {
                    var artifactLogDir = Path.Combine(
                        repositoryRoot,
                        "artifacts",
                        "desktop",
                        "NevaxisFactOS",
                        "runtime",
                        "logs");
                    Directory.CreateDirectory(artifactLogDir);
                    _artifactLogPath = Path.Combine(artifactLogDir, $"desktop-lifecycle-{timestamp}.log");
                }
            }
            catch
            {
                _artifactLogPath = null;
            }

            Write("desktop-lifecycle-initialized");
            Write($"baseDirectory={AppContext.BaseDirectory}");
            Write($"currentDirectory={Environment.CurrentDirectory}");
        }
    }

    public static void Write(string message)
    {
        var line = $"{DateTimeOffset.Now:O} {message}";
        lock (Sync)
        {
            AppendLine(_primaryLogPath, line);
            AppendLine(_artifactLogPath, line);
            AppendLine(_programDataLogPath, line);
            AppendLine(_startupLogPath, line);
        }
    }

    public static void WriteSession(string sessionId, string message) =>
        Write($"session={sessionId} {message}");

    public static void WriteException(string context, Exception exception)
    {
        Write($"{context}: {exception.GetType().Name}: {exception.Message}");
        Write($"{context}: {exception.StackTrace}");
    }

    private static void AppendLine(string? path, string line)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Logging must never crash the shell.
        }
    }
}
