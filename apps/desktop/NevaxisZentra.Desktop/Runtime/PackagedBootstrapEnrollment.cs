using System.IO;
using NevaxisZentra.Configuration.Bootstrap;

namespace NevaxisZentra.Desktop.Runtime;

/// <summary>What the API/web child processes may receive regarding the one-time bootstrap privilege.</summary>
internal sealed record BootstrapApiAccess(bool IsProduction, bool Pending, string? Directory)
{
    internal static readonly string[] MigrationCredentialKeys =
    [
        "NEVAXIS_ZENTRA__MIGRATIONS__PASSWORD",
        "NEVAXIS_FACTOS__MIGRATIONS__PASSWORD",
        "CREVAFACTOS__MIGRATIONS__PASSWORD",
    ];

    internal static readonly BootstrapApiAccess None = new(false, false, null);

    /// <summary>
    /// PRODUCTION: the API holds the (superuser) migration credential only while first-account
    /// enrollment is pending; afterwards it is removed from the inherited environment.
    /// </summary>
    internal IReadOnlyCollection<string> ApiKeysToRemove =>
        !IsProduction
            ? []
            : Pending
                ? []
                : [.. MigrationCredentialKeys, BootstrapEnrollmentStore.DirectoryEnvironmentVariable];

    /// <summary>The web server never needs database credentials of the migration role.</summary>
    internal IReadOnlyCollection<string> WebKeysToRemove =>
        IsProduction ? MigrationCredentialKeys : [];
}

internal static class PackagedBootstrapEnrollment
{
    internal static string ResolveDirectory() =>
        Path.GetDirectoryName(PackagedRuntimeBundle.GetMachineLocalSecretsPath())!;

    /// <summary>
    /// Step between "database credentials valid" and "start API": decides whether a first-account
    /// enrollment token must exist. The tenant count (database is authoritative) closes existing
    /// installations; an unused expired token is replaced; a completed enrollment is never reopened.
    /// The raw token goes only to first-run-token.txt; diagnostics carry the path, never the token.
    /// </summary>
    internal static async Task<BootstrapApiAccess> PrepareAsync(
        bool isProduction,
        string directory,
        Func<Task<int?>> tenantCountQuery,
        IList<string> diagnostics,
        TimeProvider? time = null)
    {
        if (!isProduction)
        {
            return BootstrapApiAccess.None;
        }

        var tenantCount = await tenantCountQuery().ConfigureAwait(false);
        if (tenantCount is null)
        {
            // Never guess: without an authoritative answer the bootstrap path stays closed.
            diagnostics.Add("bootstrap=skipped-tenant-count-unavailable");
            return new BootstrapApiAccess(true, false, directory);
        }

        var store = new BootstrapEnrollmentStore(directory, time);
        var preparation = store.Prepare(tenantCount.Value);
        diagnostics.Add($"bootstrap=stage:{preparation.Stage}");
        if (preparation.TokenIssued)
        {
            diagnostics.Add(
                $"bootstrap=token-{(preparation.TokenReplaced ? "replaced" : "issued")} file={preparation.TokenFilePath}");
        }

        return new BootstrapApiAccess(
            true,
            preparation.Stage != BootstrapEnrollmentStage.Completed,
            directory);
    }
}
