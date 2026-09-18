using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace NevaxisZentra.Desktop.Runtime;

public sealed class EmbeddedPostgreSqlRuntime : IAsyncDisposable
{
    private readonly PackagedRuntimeBundle bundle;
    private readonly IList<string> diagnostics;
    private RuntimeProcess? ownedProcess;

    public EmbeddedPostgreSqlRuntime(PackagedRuntimeBundle bundle, IList<string> diagnostics)
    {
        this.bundle = bundle;
        this.diagnostics = diagnostics;
    }

    public async Task EnsureReadyAsync(int port, CancellationToken cancellationToken)
    {
        var dataDirectory = Path.Combine(PackagedRuntimeBundle.GetPersistentDataRoot(), "database", "data");
        var logDirectory = Path.Combine(PackagedRuntimeBundle.GetPersistentDataRoot(), "database", "logs");

        Directory.CreateDirectory(PackagedRuntimeBundle.GetPersistentDataRoot());
        Directory.CreateDirectory(logDirectory);

        if (!Directory.Exists(bundle.PostgresBinDirectory))
        {
            throw new InvalidOperationException(
                $"Embedded PostgreSQL binaries missing: {bundle.PostgresBinDirectory}");
        }

        if (!File.Exists(Path.Combine(dataDirectory, "PG_VERSION")))
        {
            await InitializeFromTemplateAsync(dataDirectory, cancellationToken).ConfigureAwait(false);
        }

        ConfigurePort(dataDirectory, port);
        await StartAsync(dataDirectory, logDirectory, port, cancellationToken).ConfigureAwait(false);

        var ready = await RuntimeReadinessProbe.IsTcpPortOpenAsync(
            "127.0.0.1",
            port,
            TimeSpan.FromSeconds(90),
            cancellationToken).ConfigureAwait(false);
        if (!ready)
        {
            throw new InvalidOperationException("Embedded PostgreSQL failed to start.");
        }

        diagnostics.Add($"postgres=embedded-started port={port.ToString(CultureInfo.InvariantCulture)}");
    }

    private async Task InitializeFromTemplateAsync(string dataDirectory, CancellationToken cancellationToken)
    {
        if (Directory.Exists(bundle.PostgresTemplateDirectory))
        {
            diagnostics.Add("postgres=copy-template");
            CopyDirectory(bundle.PostgresTemplateDirectory, dataDirectory);
            return;
        }

        diagnostics.Add("postgres=initdb");
        var initdb = Path.Combine(bundle.PostgresBinDirectory, "initdb.exe");
        if (!File.Exists(initdb))
        {
            throw new InvalidOperationException($"initdb.exe not found: {initdb}");
        }

        Directory.CreateDirectory(dataDirectory);
        await RunUtilityAsync(
            initdb,
            $"-D \"{dataDirectory}\" -U postgres --encoding=UTF8 --locale=C",
            bundle.PostgresBinDirectory,
            cancellationToken).ConfigureAwait(false);
    }

    private static void ConfigurePort(string dataDirectory, int port)
    {
        var confPath = Path.Combine(dataDirectory, "postgresql.conf");
        if (!File.Exists(confPath))
        {
            return;
        }

        var lines = File.ReadAllLines(confPath).ToList();
        SetOrAddConfig(lines, "port", port.ToString(CultureInfo.InvariantCulture));
        SetOrAddConfig(lines, "listen_addresses", "'127.0.0.1'");
        File.WriteAllLines(confPath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var hbaPath = Path.Combine(dataDirectory, "pg_hba.conf");
        if (File.Exists(hbaPath))
        {
            var hba = File.ReadAllText(hbaPath, Encoding.UTF8);
            if (!hba.Contains("127.0.0.1/32", StringComparison.Ordinal))
            {
                File.AppendAllText(
                    hbaPath,
                    Environment.NewLine + "host all all 127.0.0.1/32 scram-sha-256" + Environment.NewLine,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }
    }

    private async Task StartAsync(
        string dataDirectory,
        string logDirectory,
        int port,
        CancellationToken cancellationToken)
    {
        if (await RuntimeReadinessProbe.IsTcpPortOpenAsync(
                "127.0.0.1",
                port,
                TimeSpan.FromSeconds(1),
                cancellationToken).ConfigureAwait(false))
        {
            diagnostics.Add("postgres=attached-existing");
            return;
        }

        var pgCtl = Path.Combine(bundle.PostgresBinDirectory, "pg_ctl.exe");
        if (!File.Exists(pgCtl))
        {
            throw new InvalidOperationException($"pg_ctl.exe not found: {pgCtl}");
        }

        var logFile = Path.Combine(logDirectory, "postgresql.log");
        ownedProcess = RuntimeProcess.StartHidden(
            pgCtl,
            $"start -D \"{dataDirectory}\" -l \"{logFile}\" -w -o \"-p {port.ToString(CultureInfo.InvariantCulture)}\"",
            bundle.PostgresBinDirectory,
            null,
            logFile);
        diagnostics.Add($"postgres=pg_ctl-started pid={ownedProcess.ProcessId}");
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<string?> QueryScalarAsync(
        int port,
        string database,
        string username,
        string password,
        string query,
        CancellationToken cancellationToken)
    {
        var psql = Path.Combine(bundle.PostgresBinDirectory, "psql.exe");
        if (!File.Exists(psql))
        {
            return null;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = psql,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = bundle.PostgresBinDirectory,
        };
        startInfo.ArgumentList.Add("-h");
        startInfo.ArgumentList.Add("127.0.0.1");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(port.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-U");
        startInfo.ArgumentList.Add(username);
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(database);
        startInfo.ArgumentList.Add("-tA");
        startInfo.Environment["PGPASSWORD"] = password;

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        await process.StandardInput.WriteLineAsync(query).ConfigureAwait(false);
        process.StandardInput.Close();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0 ? output.Trim() : null;
    }

    public async ValueTask DisposeAsync()
    {
        if (ownedProcess is null)
        {
            return;
        }

        var dataDirectory = Path.Combine(PackagedRuntimeBundle.GetPersistentDataRoot(), "database", "data");
        try
        {
            var pgCtl = Path.Combine(bundle.PostgresBinDirectory, "pg_ctl.exe");
            if (File.Exists(pgCtl))
            {
                await RunUtilityAsync(
                    pgCtl,
                    $"stop -D \"{dataDirectory}\" -m fast",
                    bundle.PostgresBinDirectory,
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch
        {
            ownedProcess.Dispose();
        }

        ownedProcess = null;
    }

    private static void SetOrAddConfig(List<string> lines, string key, string value)
    {
        var prefix = key + " =";
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith(prefix, StringComparison.Ordinal))
            {
                lines[i] = $"{key} = {value}";
                return;
            }
        }

        lines.Add($"{key} = {value}");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = file.Replace(source, destination, StringComparison.OrdinalIgnoreCase);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static async Task RunUtilityAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}");
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"{fileName} failed: {stderr}");
        }
    }
}
