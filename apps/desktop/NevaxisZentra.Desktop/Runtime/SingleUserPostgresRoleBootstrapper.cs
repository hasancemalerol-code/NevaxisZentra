using System.Diagnostics;
using System.IO;
using System.Text;

namespace NevaxisZentra.Desktop.Runtime;

/// <summary>
/// Synchronizes role passwords using PostgreSQL single-user mode (postgres --single). The backend
/// reads statements from stdin, bypasses client authentication, and opens no network listener,
/// so no reusable bootstrap credential exists and nothing is exposed during bootstrap.
/// </summary>
public sealed class SingleUserPostgresRoleBootstrapper : IPostgresRoleBootstrapper
{
    public const string MigrationRole = "nevaxiszentra";
    public const string RuntimeRole = "crevafactos_runtime";

    private readonly string postgresBinDirectory;
    private readonly TimeSpan timeout;

    public SingleUserPostgresRoleBootstrapper(string postgresBinDirectory, TimeSpan? timeout = null)
    {
        this.postgresBinDirectory = postgresBinDirectory;
        this.timeout = timeout ?? TimeSpan.FromSeconds(90);
    }

    public async Task SyncRolePasswordsAsync(
        string dataDirectory,
        string migrationPassword,
        string runtimePassword,
        CancellationToken cancellationToken)
    {
        var postgres = Path.Combine(postgresBinDirectory, "postgres.exe");
        if (!File.Exists(postgres))
        {
            throw new FileNotFoundException("Embedded PostgreSQL server binary is missing.", postgres);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = postgres,
            WorkingDirectory = postgresBinDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--single");
        startInfo.ArgumentList.Add("-D");
        startInfo.ArgumentList.Add(dataDirectory);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("password_encryption=scram-sha-256");
        startInfo.ArgumentList.Add("postgres");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Database bootstrap process could not be started.");

        // Statements travel over stdin only: never in arguments, environment or logs.
        var script = new StringBuilder()
            .Append(BuildAlterRole(MigrationRole, migrationPassword)).Append('\n')
            .Append(BuildAlterRole(RuntimeRole, runtimePassword)).Append('\n')
            .ToString();
        await process.StandardInput.WriteAsync(script.AsMemory(), cancellationToken).ConfigureAwait(false);
        process.StandardInput.Close();

        // The backend echoes failing statements (including the secret) on stderr: capture, never surface.
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException("Database credential bootstrap timed out.");
        }

        var error = await stderr.ConfigureAwait(false);
        await stdout.ConfigureAwait(false);
        if (process.ExitCode != 0 || error.Contains("ERROR:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Database credential bootstrap failed (exit code {process.ExitCode}).");
        }
    }

    internal static string BuildAlterRole(string role, string password)
    {
        if (password.Contains('\n') || password.Contains('\r') || password.Contains('\0'))
        {
            throw new ArgumentException("Password contains characters that cannot be bootstrapped.", nameof(password));
        }

        return $"ALTER ROLE \"{role}\" WITH LOGIN PASSWORD '{password.Replace("'", "''", StringComparison.Ordinal)}'";
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
