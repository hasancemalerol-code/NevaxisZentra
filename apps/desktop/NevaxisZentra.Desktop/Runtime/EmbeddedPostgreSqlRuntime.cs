using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace NevaxisZentra.Desktop.Runtime;

public sealed class EmbeddedPostgreSqlRuntime : IAsyncDisposable
{
    private readonly PackagedRuntimeBundle bundle;
    private readonly IList<string> diagnostics;
    private readonly bool machineSecretsCreatedThisRun;
    private RuntimeProcess? ownedProcess;
    private PackagedMachineProvisioner? provisioner;

    public EmbeddedPostgreSqlRuntime(
        PackagedRuntimeBundle bundle,
        IList<string> diagnostics,
        bool machineSecretsCreatedThisRun = false)
    {
        this.bundle = bundle;
        this.diagnostics = diagnostics;
        this.machineSecretsCreatedThisRun = machineSecretsCreatedThisRun;
    }

    public async Task EnsureReadyAsync(int port, CancellationToken cancellationToken)
    {
        var persistentRoot = PackagedRuntimeBundle.GetPersistentDataRoot();
        var dataDirectory = Path.Combine(persistentRoot, "database", "data");
        var logDirectory = Path.Combine(persistentRoot, "database", "logs");

        Directory.CreateDirectory(persistentRoot);
        Directory.CreateDirectory(logDirectory);

        if (!Directory.Exists(bundle.PostgresBinDirectory))
        {
            throw new InvalidOperationException(
                $"Embedded PostgreSQL binaries missing: {bundle.PostgresBinDirectory}");
        }

        if (Directory.Exists(bundle.PostgresTemplateDirectory))
        {
            // Template, required directories, then role credential sync: all before any server start.
            provisioner = new PackagedMachineProvisioner(
                new MachineProvisioningLayout(
                    bundle.PostgresTemplateDirectory,
                    dataDirectory,
                    Path.Combine(persistentRoot, "database", "provisioning-state.json"),
                    PackagedRuntimeBundle.GetMachineLocalSecretsPath()),
                new SingleUserPostgresRoleBootstrapper(bundle.PostgresBinDirectory),
                diagnostics);
            await provisioner.EnsureEmbeddedDatabaseAsync(machineSecretsCreatedThisRun, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            if (!File.Exists(Path.Combine(dataDirectory, "PG_VERSION")))
            {
                await InitializeWithInitdbAsync(dataDirectory, cancellationToken).ConfigureAwait(false);
            }

            EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(dataDirectory);
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

        if (provisioner is not null)
        {
            await ValidateRuntimeCredentialsAsync(port, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Marks first-run provisioning complete; call after API and web are healthy.</summary>
    public void MarkProvisioningComplete() => provisioner?.MarkComplete();

    private async Task ValidateRuntimeCredentialsAsync(int port, CancellationToken cancellationToken)
    {
        var database = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__DATABASE")
            ?? "crevafactos_local";
        var username = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__USERNAME")
            ?? SingleUserPostgresRoleBootstrapper.RuntimeRole;
        var password = Environment.GetEnvironmentVariable(PackagedMachineSecrets.RuntimePasswordKey) ?? string.Empty;

        var result = await QueryScalarAsync(port, database, username, password, "SELECT 1;", cancellationToken)
            .ConfigureAwait(false);
        if (result != "1")
        {
            throw new InvalidOperationException("Embedded database rejected the machine runtime credentials.");
        }

        diagnostics.Add("postgres=runtime-credentials-valid");
    }

    private async Task InitializeWithInitdbAsync(string dataDirectory, CancellationToken cancellationToken)
    {
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
