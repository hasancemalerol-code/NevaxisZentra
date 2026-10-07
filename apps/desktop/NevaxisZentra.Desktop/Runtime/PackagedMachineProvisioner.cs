using System.Globalization;
using System.IO;
using System.Text.Json;

namespace NevaxisZentra.Desktop.Runtime;

public enum ProvisioningStage
{
    None = 0,
    Started = 1,
    SecretsReady = 2,
    TemplateCopied = 3,
    RolesSynced = 4,
    Complete = 5,
}

public interface IPostgresRoleBootstrapper
{
    /// <summary>
    /// Sets the login passwords of the migration and runtime roles on a data directory that no
    /// server is using. Must not require any pre-existing credential and must not open a listener.
    /// </summary>
    Task SyncRolePasswordsAsync(
        string dataDirectory,
        string migrationPassword,
        string runtimePassword,
        CancellationToken cancellationToken);
}

public sealed record MachineProvisioningLayout(
    string TemplateDirectory,
    string DataDirectory,
    string StateFilePath,
    string SecretsPath);

/// <summary>
/// Resumable first-run provisioning of the embedded database:
/// template copy (atomic) -> required directories -> role credential sync -> (caller starts the
/// stack) -> mark complete. Every step is idempotent and progress is persisted so an interrupted
/// first run resumes safely. Credentials are never regenerated once the secrets file exists.
/// </summary>
public sealed class PackagedMachineProvisioner
{
    private readonly MachineProvisioningLayout layout;
    private readonly IPostgresRoleBootstrapper bootstrapper;
    private readonly IList<string> diagnostics;

    public PackagedMachineProvisioner(
        MachineProvisioningLayout layout,
        IPostgresRoleBootstrapper bootstrapper,
        IList<string> diagnostics)
    {
        this.layout = layout;
        this.bootstrapper = bootstrapper;
        this.diagnostics = diagnostics;
    }

    private string TemporaryDataDirectory => layout.DataDirectory.TrimEnd('\\', '/') + ".provisioning";

    private bool HasData => File.Exists(Path.Combine(layout.DataDirectory, "PG_VERSION"));

    public ProvisioningStage ReadStage()
    {
        try
        {
            if (!File.Exists(layout.StateFilePath))
            {
                return ProvisioningStage.None;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(layout.StateFilePath));
            return document.RootElement.TryGetProperty("stage", out var stage)
                && Enum.TryParse<ProvisioningStage>(stage.GetString(), ignoreCase: false, out var parsed)
                ? parsed
                : ProvisioningStage.None;
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            // A torn or unreadable state file is treated as "unknown": provisioning is idempotent.
            return ProvisioningStage.None;
        }
    }

    public async Task EnsureEmbeddedDatabaseAsync(bool secretsCreatedThisRun, CancellationToken cancellationToken)
    {
        var stage = ReadStage();

        if (stage == ProvisioningStage.None && HasData && !secretsCreatedThisRun && File.Exists(layout.SecretsPath))
        {
            // Installation provisioned before first-run state tracking existed: adopt, never rotate.
            EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(layout.DataDirectory);
            WriteStage(ProvisioningStage.Complete);
            diagnostics.Add("provisioning=legacy-install-adopted");
            return;
        }

        if (stage == ProvisioningStage.Complete && HasData && !secretsCreatedThisRun)
        {
            EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(layout.DataDirectory);
            diagnostics.Add("provisioning=complete");
            return;
        }

        if (stage == ProvisioningStage.None)
        {
            WriteStage(ProvisioningStage.Started);
            stage = ProvisioningStage.Started;
        }

        // New credentials, or a wiped data directory, always require the roles to be re-synced.
        if ((secretsCreatedThisRun && stage >= ProvisioningStage.RolesSynced)
            || (!HasData && stage >= ProvisioningStage.TemplateCopied))
        {
            stage = ProvisioningStage.SecretsReady;
            WriteStage(stage);
        }

        var secrets = PackagedMachineSecrets.Load(layout.SecretsPath);
        if (!secrets.TryGetValue(PackagedMachineSecrets.MigrationPasswordKey, out var migrationPassword)
            || !secrets.TryGetValue(PackagedMachineSecrets.RuntimePasswordKey, out var runtimePassword)
            || string.IsNullOrWhiteSpace(migrationPassword)
            || string.IsNullOrWhiteSpace(runtimePassword))
        {
            throw new InvalidOperationException("Machine database credentials are missing; provisioning cannot continue.");
        }

        if (stage < ProvisioningStage.SecretsReady)
        {
            stage = ProvisioningStage.SecretsReady;
            WriteStage(stage);
        }

        if (!HasData)
        {
            CopyTemplateAtomically();
            diagnostics.Add("provisioning=template-copied");
        }

        if (stage < ProvisioningStage.TemplateCopied)
        {
            stage = ProvisioningStage.TemplateCopied;
            WriteStage(stage);
        }

        EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(layout.DataDirectory);

        if (stage < ProvisioningStage.RolesSynced)
        {
            await bootstrapper.SyncRolePasswordsAsync(
                layout.DataDirectory,
                migrationPassword,
                runtimePassword,
                cancellationToken).ConfigureAwait(false);
            WriteStage(ProvisioningStage.RolesSynced);
            diagnostics.Add("provisioning=roles-synced");
        }
    }

    /// <summary>Call only after the database, API and web have all started successfully.</summary>
    public void MarkComplete()
    {
        if (ReadStage() >= ProvisioningStage.RolesSynced)
        {
            WriteStage(ProvisioningStage.Complete);
            diagnostics.Add("provisioning=marked-complete");
        }
    }

    private void CopyTemplateAtomically()
    {
        if (!Directory.Exists(layout.TemplateDirectory))
        {
            throw new DirectoryNotFoundException("Embedded database template is missing.");
        }

        var temporary = TemporaryDataDirectory;
        if (Directory.Exists(temporary))
        {
            Directory.Delete(temporary, recursive: true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(layout.DataDirectory)!);
        EmbeddedPostgresDataDirectory.CopyTemplate(layout.TemplateDirectory, temporary);
        EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(temporary);

        if (Directory.Exists(layout.DataDirectory))
        {
            // Data directory without PG_VERSION is an unusable remnant, never a live database.
            Directory.Delete(layout.DataDirectory, recursive: true);
        }

        Directory.Move(temporary, layout.DataDirectory);
    }

    private void WriteStage(ProvisioningStage stage)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(layout.StateFilePath)!);
        var temporary = layout.StateFilePath + ".tmp";
        var payload = JsonSerializer.Serialize(new
        {
            version = 1,
            stage = stage.ToString(),
            updatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        });
        File.WriteAllText(temporary, payload);
        File.Move(temporary, layout.StateFilePath, overwrite: true);
    }
}
