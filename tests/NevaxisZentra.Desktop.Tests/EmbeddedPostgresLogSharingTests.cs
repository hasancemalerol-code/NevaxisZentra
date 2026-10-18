using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

/// <summary>
/// Release blocker regression: on a clean PC the supervisor held postgresql.log open (FileShare.Read) while
/// <c>pg_ctl -l</c> tried to open the same file, so PostgreSQL failed with "Permission denied", and the diagnostics
/// bundle could not read the log it was supposed to collect.
/// </summary>
public sealed class EmbeddedPostgresLogSharingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"zentra-logshare-{Guid.NewGuid():N}");

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a temp folder.
        }
    }

    [Fact]
    public void ServerLogAndSupervisorCaptureLogAreDifferentFiles()
    {
        var logs = Path.Combine(root, "logs");

        Assert.NotEqual(
            EmbeddedPostgresLogFiles.ServerLog(logs),
            EmbeddedPostgresLogFiles.SupervisorCaptureLog(logs),
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal("postgresql.log", Path.GetFileName(EmbeddedPostgresLogFiles.ServerLog(logs)));
        Assert.Contains(
            $"-l \"{EmbeddedPostgresLogFiles.ServerLog(logs)}\"",
            EmbeddedPostgresLogFiles.BuildStartArguments(Path.Combine(root, "data"), logs, 55432),
            StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsCollectLogsWhileTheirWritersStillHoldThemOpen()
    {
        var appLogs = Directory.CreateDirectory(Path.Combine(root, "Logs")).FullName;
        var dbLogs = Directory.CreateDirectory(Path.Combine(root, "database", "logs")).FullName;
        using var appWriter = OpenAsLiveWriter(Path.Combine(appLogs, "desktop.log"), "desktop line");
        using var serverWriter = OpenAsLiveWriter(EmbeddedPostgresLogFiles.ServerLog(dbLogs), "server line");
        using var captureWriter = OpenAsLiveWriter(EmbeddedPostgresLogFiles.SupervisorCaptureLog(dbLogs), "capture line");

        var bundle = FactOsDiagnosticsBundleService.CreateSupportBundle(
            null, ["diagnostic=test"], "ZENTRA-STARTUP-DB-001", Path.Combine(root, "Diagnostics"), [appLogs], dbLogs);

        using var archive = ZipFile.OpenRead(bundle);
        Assert.Equal("server line", ReadEntry(archive, "logs/postgresql.log"));
        Assert.Equal("capture line", ReadEntry(archive, "logs/pg_ctl-supervisor.log"));
        Assert.Equal("desktop line", ReadEntry(archive, "logs/Logs/desktop.log"));
        Assert.DoesNotContain(archive.Entries, entry => entry.FullName.EndsWith(".unreadable.txt", StringComparison.Ordinal));
    }

    [Fact]
    public void AnUnreadableLogIsRecordedAndDoesNotFailTheBundle()
    {
        var dbLogs = Directory.CreateDirectory(Path.Combine(root, "database", "logs")).FullName;
        var locked = EmbeddedPostgresLogFiles.ServerLog(dbLogs);
        File.WriteAllText(locked, "exclusive");
        using var exclusiveHandle = new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        File.WriteAllText(EmbeddedPostgresLogFiles.SupervisorCaptureLog(dbLogs), "readable");

        var bundle = FactOsDiagnosticsBundleService.CreateSupportBundle(
            null, [], null, Path.Combine(root, "Diagnostics"), [], dbLogs);

        using var archive = ZipFile.OpenRead(bundle);
        Assert.Contains(archive.Entries, entry => entry.FullName == "logs/postgresql.log.unreadable.txt");
        Assert.Equal("readable", ReadEntry(archive, "logs/pg_ctl-supervisor.log"));
    }

    [RealPostgresFact]
    public async Task BundledPostgresStartsWithAPreExistingLogFileAndDiagnosticsCollectWhileItRuns()
    {
        var pgRoot = RealPostgres.Root!;
        var pgCtl = Path.Combine(pgRoot, "bin", "pg_ctl.exe");
        var data = Path.Combine(root, "database", "data");
        var logs = Directory.CreateDirectory(Path.Combine(root, "database", "logs")).FullName;
        EmbeddedPostgresDataDirectory.CopyTemplate(Path.Combine(pgRoot, "template"), data);
        EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(data);

        // A log left behind by an earlier launch, an uninstall or an upgrade (preserved customer data).
        File.WriteAllText(EmbeddedPostgresLogFiles.ServerLog(logs), "previous run" + Environment.NewLine);
        File.WriteAllText(EmbeddedPostgresLogFiles.SupervisorCaptureLog(logs), "previous capture" + Environment.NewLine);

        var port = FreeTcpPort();
        try
        {
            using var launched = EmbeddedPostgresLogFiles.LaunchPgCtlStart(
                pgCtl, Path.Combine(pgRoot, "bin"), data, logs, port);

            var ready = await RuntimeReadinessProbe.IsTcpPortOpenAsync(
                "127.0.0.1", port, TimeSpan.FromSeconds(90), CancellationToken.None);
            Assert.True(ready, "The bundled PostgreSQL did not start with a pre-existing postgresql.log.");

            var bundle = FactOsDiagnosticsBundleService.CreateSupportBundle(
                null, [], null, Path.Combine(root, "Diagnostics"), [], logs);

            using var archive = ZipFile.OpenRead(bundle);
            var serverLog = ReadEntry(archive, "logs/postgresql.log");
            Assert.StartsWith("previous run", serverLog, StringComparison.Ordinal);
            Assert.Contains("database system is ready to accept connections", serverLog, StringComparison.Ordinal);
            Assert.Contains("previous capture", ReadEntry(archive, "logs/pg_ctl-supervisor.log"), StringComparison.Ordinal);
            Assert.DoesNotContain(archive.Entries, entry => entry.FullName.EndsWith(".unreadable.txt", StringComparison.Ordinal));
        }
        finally
        {
            StopServer(pgCtl, data);
        }
    }

    private static FileStream OpenAsLiveWriter(string path, string content)
    {
        // Exactly how the supervisor's own log writer opens a log: append, write access, other readers only.
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
        return stream;
    }

    private static string ReadEntry(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name) ?? throw new InvalidOperationException($"Bundle entry missing: {name}");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static int FreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void StopServer(string pgCtl, string dataDirectory)
    {
        using var stop = Process.Start(new ProcessStartInfo
        {
            FileName = pgCtl,
            Arguments = $"stop -D \"{dataDirectory}\" -m fast",
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        stop?.WaitForExit(60_000);
    }
}
