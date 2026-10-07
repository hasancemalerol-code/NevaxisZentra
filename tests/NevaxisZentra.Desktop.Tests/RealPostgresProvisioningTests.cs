using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

internal static class RealPostgres
{
    /// <summary>
    /// Directory holding bin\ and template\ of a packaged PostgreSQL. Set ZENTRA_TEST_PG_ROOT to
    /// point at an MSI-extracted payload; otherwise release\staging\postgres is used when present.
    /// </summary>
    internal static string? Root { get; } = Resolve();

    internal static string? RepositoryRoot { get; } = FindRepositoryRoot();

    private static string? Resolve()
    {
        var candidates = new List<string?> { Environment.GetEnvironmentVariable("ZENTRA_TEST_PG_ROOT") };
        var repo = FindRepositoryRoot();
        if (repo is not null)
        {
            candidates.Add(Path.Combine(repo, "release", "staging", "postgres"));
        }

        return candidates.FirstOrDefault(candidate =>
            !string.IsNullOrWhiteSpace(candidate)
            && File.Exists(Path.Combine(candidate, "bin", "postgres.exe"))
            && File.Exists(Path.Combine(candidate, "template", "PG_VERSION")));
    }

    internal static string? FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NevaxisZentra.sln")))
            {
                return directory.FullName;
            }
        }

        return null;
    }
}

public sealed class RealPostgresFactAttribute : FactAttribute
{
    public RealPostgresFactAttribute()
    {
        if (RealPostgres.Root is null)
        {
            Skip = "No packaged PostgreSQL (release\\staging\\postgres or ZENTRA_TEST_PG_ROOT) is available.";
        }
    }
}

public sealed class RealPostgresProvisioningTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"zentra-realpg-{Guid.NewGuid():N}");
    private string? runningDataDirectory;

    public void Dispose()
    {
        if (runningDataDirectory is not null && RealPostgres.Root is not null)
        {
            Run(Path.Combine(RealPostgres.Root, "bin", "pg_ctl.exe"), ["stop", "-D", runningDataDirectory, "-m", "fast"], null, null, out _);
        }

        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [RealPostgresFact]
    public async Task GeneratedCredentialsAuthenticateAndTemplateOrOldCredentialsAreRejected()
    {
        var pg = RealPostgres.Root!;
        var bin = Path.Combine(pg, "bin");
        var layout = new MachineProvisioningLayout(
            Path.Combine(pg, "template"),
            Path.Combine(root, "database", "data"),
            Path.Combine(root, "database", "provisioning-state.json"),
            Path.Combine(root, "config", "runtime.env.local"));
        var diagnostics = new List<string>();

        var created = PackagedMachineSecrets.EnsureCreated(layout.SecretsPath, null, false, diagnostics);
        var provisioner = new PackagedMachineProvisioner(layout, new SingleUserPostgresRoleBootstrapper(bin), diagnostics);
        await provisioner.EnsureEmbeddedDatabaseAsync(created, CancellationToken.None);

        var secrets = PackagedMachineSecrets.Load(layout.SecretsPath);
        var runtimePassword = secrets[PackagedMachineSecrets.RuntimePasswordKey];
        var migrationPassword = secrets[PackagedMachineSecrets.MigrationPasswordKey];

        var port = FreePort();
        runningDataDirectory = layout.DataDirectory;
        Assert.Equal(0, Run(
            Path.Combine(bin, "pg_ctl.exe"),
            ["start", "-D", layout.DataDirectory, "-l", Path.Combine(root, "pg.log"), "-w", "-o", $"-p {port}"],
            null, null, out _));

        // 1. Generated runtime credentials authenticate, as a least-privilege role.
        Assert.Equal("1", Query(bin, port, "crevafactos_runtime", runtimePassword, "select 1;"));
        Assert.Equal("false|false|false",Query(bin, port, "nevaxiszentra", migrationPassword,
            "select rolsuper::text||'|'||rolbypassrls::text||'|'||rolcreaterole::text from pg_roles where rolname='crevafactos_runtime';"));

        // 2. The runtime role can verify the schema head without any superuser credential.
        var head = Query(bin, port, "crevafactos_runtime", runtimePassword, "select max(\"MigrationId\") from \"__EFMigrationsHistory\";");
        Assert.False(string.IsNullOrWhiteSpace(head));

        // 3. Row-level security stays forced on tenants.
        Assert.Equal("true", Query(bin, port, "nevaxiszentra", migrationPassword,
            "select relforcerowsecurity::text from pg_class where relname='tenants' and relnamespace='public'::regnamespace;"));

        // 4. Template / old / foreign credentials are rejected.
        foreach (var rejected in new[] { string.Empty, "local_runtime_only_change_me", "NevaxisDemo2026!", migrationPassword })
        {
            Assert.Null(Query(bin, port, "crevafactos_runtime", rejected, "select 1;"));
        }

        Assert.Null(Query(bin, port, "nevaxiszentra", runtimePassword, "select 1;"));
        Assert.Null(Query(bin, port, "postgres", string.Empty, "select 1;"));
        Assert.Null(Query(bin, port, "postgres", "postgres", "select 1;"));

        // 5. A repeated startup neither re-syncs nor rotates: the same credentials keep working.
        provisioner.MarkComplete();
        var secondRun = PackagedMachineSecrets.EnsureCreated(layout.SecretsPath, null, false, diagnostics);
        await new PackagedMachineProvisioner(layout, new SingleUserPostgresRoleBootstrapper(bin), diagnostics)
            .EnsureEmbeddedDatabaseAsync(secondRun, CancellationToken.None);
        Assert.False(secondRun);
        Assert.Equal("1", Query(bin, port, "crevafactos_runtime", runtimePassword, "select 1;"));
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string? Query(string bin, int port, string user, string password, string sql)
    {
        var exit = Run(
            Path.Combine(bin, "psql.exe"),
            ["-w", "-h", "127.0.0.1", "-p", port.ToString(CultureInfo.InvariantCulture), "-U", user, "-d", "crevafactos_local", "-tA"],
            new Dictionary<string, string> { ["PGPASSWORD"] = password, ["PGCONNECT_TIMEOUT"] = "10" },
            sql,
            out var output);
        return exit == 0 ? output.Trim() : null;
    }

    private static int Run(
        string fileName,
        IEnumerable<string> arguments,
        IDictionary<string, string>? environment,
        string? stdin,
        out string stdout)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = stdin is not null,
            RedirectStandardOutput = stdin is not null,
            RedirectStandardError = stdin is not null,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        using var process = Process.Start(startInfo)!;
        stdout = string.Empty;
        if (stdin is not null)
        {
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
            var output = process.StandardOutput.ReadToEndAsync();
            process.StandardError.ReadToEndAsync().GetAwaiter().GetResult();
            stdout = output.GetAwaiter().GetResult();
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}
