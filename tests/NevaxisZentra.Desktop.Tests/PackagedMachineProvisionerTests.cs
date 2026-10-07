using System.IO;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

public sealed class PackagedMachineProvisionerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"zentra-prov-{Guid.NewGuid():N}");
    private readonly MachineProvisioningLayout layout;

    public PackagedMachineProvisionerTests()
    {
        var template = Path.Combine(root, "template");
        Directory.CreateDirectory(Path.Combine(template, "base", "1"));
        File.WriteAllText(Path.Combine(template, "PG_VERSION"), "17");
        for (var i = 0; i < 20; i++)
        {
            File.WriteAllText(Path.Combine(template, "base", "1", $"f{i}"), "x");
        }

        layout = new MachineProvisioningLayout(
            template,
            Path.Combine(root, "database", "data"),
            Path.Combine(root, "database", "provisioning-state.json"),
            Path.Combine(root, "config", "runtime.env.local"));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FakeBootstrapper : IPostgresRoleBootstrapper
    {
        public List<(string Migration, string Runtime)> Calls { get; } = [];

        public Exception? FailNextWith { get; set; }

        public Task SyncRolePasswordsAsync(string dataDirectory, string migrationPassword, string runtimePassword, CancellationToken cancellationToken)
        {
            if (FailNextWith is { } failure)
            {
                FailNextWith = null;
                throw failure;
            }

            Assert.True(File.Exists(Path.Combine(dataDirectory, "PG_VERSION")));
            Assert.True(Directory.Exists(Path.Combine(dataDirectory, "pg_notify")), "required dirs must exist before the sync");
            Calls.Add((migrationPassword, runtimePassword));
            return Task.CompletedTask;
        }
    }

    private PackagedMachineProvisioner NewProvisioner(FakeBootstrapper bootstrapper, List<string>? diagnostics = null) =>
        new(layout, bootstrapper, diagnostics ?? []);

    private bool CreateSecrets() => PackagedMachineSecrets.EnsureCreated(layout.SecretsPath, null, false, []);

    [Fact]
    public async Task FirstRunCopiesTemplateRecreatesDirectoriesSyncsGeneratedCredentialsThenCompletes()
    {
        var bootstrapper = new FakeBootstrapper();
        var provisioner = NewProvisioner(bootstrapper);
        var created = CreateSecrets();

        await provisioner.EnsureEmbeddedDatabaseAsync(created, CancellationToken.None);

        Assert.Equal(ProvisioningStage.RolesSynced, provisioner.ReadStage());
        foreach (var relative in EmbeddedPostgresDataDirectory.RequiredEmptyDirectories)
        {
            Assert.True(Directory.Exists(Path.Combine(layout.DataDirectory, relative)), relative);
        }

        var secrets = PackagedMachineSecrets.Load(layout.SecretsPath);
        var call = Assert.Single(bootstrapper.Calls);
        Assert.Equal(secrets[PackagedMachineSecrets.MigrationPasswordKey], call.Migration);
        Assert.Equal(secrets[PackagedMachineSecrets.RuntimePasswordKey], call.Runtime);
        Assert.False(Directory.Exists(layout.DataDirectory + ".provisioning"));

        provisioner.MarkComplete();
        Assert.Equal(ProvisioningStage.Complete, provisioner.ReadStage());
    }

    [Fact]
    public async Task MarkCompleteBeforeCredentialSyncIsIgnored()
    {
        var provisioner = NewProvisioner(new FakeBootstrapper());
        provisioner.MarkComplete();
        Assert.Equal(ProvisioningStage.None, provisioner.ReadStage());
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RepeatedStartupDoesNotRotateCredentialsOrResync()
    {
        var bootstrapper = new FakeBootstrapper();
        var provisioner = NewProvisioner(bootstrapper);
        await provisioner.EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);
        provisioner.MarkComplete();
        var secretsBefore = File.ReadAllBytes(layout.SecretsPath);

        for (var i = 0; i < 3; i++)
        {
            var created = CreateSecrets();
            await NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(created, CancellationToken.None);
        }

        Assert.Single(bootstrapper.Calls);
        Assert.Equal(secretsBefore, File.ReadAllBytes(layout.SecretsPath));
        Assert.Equal(ProvisioningStage.Complete, provisioner.ReadStage());
    }

    [Fact]
    public async Task InterruptedCredentialSyncResumesWithTheSameCredentials()
    {
        var bootstrapper = new FakeBootstrapper { FailNextWith = new InvalidOperationException("killed mid-sync") };
        var created = CreateSecrets();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(created, CancellationToken.None));
        Assert.Equal(ProvisioningStage.TemplateCopied, NewProvisioner(bootstrapper).ReadStage());
        var secretsAfterCrash = File.ReadAllBytes(layout.SecretsPath);

        // Next launch: secrets already exist (created == false), stage resumes at the sync step.
        await NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);

        Assert.Equal(ProvisioningStage.RolesSynced, NewProvisioner(bootstrapper).ReadStage());
        var call = Assert.Single(bootstrapper.Calls);
        Assert.Equal(PackagedMachineSecrets.Load(layout.SecretsPath)[PackagedMachineSecrets.RuntimePasswordKey], call.Runtime);
        Assert.Equal(secretsAfterCrash, File.ReadAllBytes(layout.SecretsPath));
    }

    [Fact]
    public async Task StaleTemporaryCopyFromAKilledFirstRunIsDiscarded()
    {
        var stale = layout.DataDirectory + ".provisioning";
        Directory.CreateDirectory(stale);
        File.WriteAllText(Path.Combine(stale, "PG_VERSION"), "half-copied");
        File.WriteAllText(Path.Combine(stale, "junk"), "junk");

        await NewProvisioner(new FakeBootstrapper()).EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);

        Assert.False(Directory.Exists(stale));
        Assert.False(File.Exists(Path.Combine(layout.DataDirectory, "junk")));
        Assert.Equal("17", File.ReadAllText(Path.Combine(layout.DataDirectory, "PG_VERSION")));
    }

    [Fact]
    public async Task MissingEmptyDirectoriesAreRecreatedOnLaterStartups()
    {
        var bootstrapper = new FakeBootstrapper();
        await NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);
        NewProvisioner(bootstrapper).MarkComplete();
        Directory.Delete(Path.Combine(layout.DataDirectory, "pg_notify"));
        Directory.Delete(Path.Combine(layout.DataDirectory, "pg_logical", "snapshots"));

        await NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);

        Assert.True(Directory.Exists(Path.Combine(layout.DataDirectory, "pg_notify")));
        Assert.True(Directory.Exists(Path.Combine(layout.DataDirectory, "pg_logical", "snapshots")));
        Assert.Single(bootstrapper.Calls);
    }

    [Fact]
    public async Task ExistingProvisionedInstallationIsAdoptedWithoutAnyChange()
    {
        Directory.CreateDirectory(layout.DataDirectory);
        File.WriteAllText(Path.Combine(layout.DataDirectory, "PG_VERSION"), "17");
        Directory.CreateDirectory(Path.GetDirectoryName(layout.SecretsPath)!);
        File.WriteAllText(layout.SecretsPath, "NEVAXIS_FACTOS__PERSISTENCE__PASSWORD=existing-runtime\nNEVAXIS_FACTOS__MIGRATIONS__PASSWORD=existing-migration\n");
        var before = File.ReadAllBytes(layout.SecretsPath);
        var bootstrapper = new FakeBootstrapper();

        var diagnostics = new List<string>();
        await NewProvisioner(bootstrapper, diagnostics).EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);

        Assert.Empty(bootstrapper.Calls);
        Assert.Equal(before, File.ReadAllBytes(layout.SecretsPath));
        Assert.Equal(ProvisioningStage.Complete, NewProvisioner(bootstrapper).ReadStage());
        Assert.Contains("provisioning=legacy-install-adopted", diagnostics);
        Assert.True(Directory.Exists(Path.Combine(layout.DataDirectory, "pg_notify")));
    }

    [Fact]
    public async Task LostMachineSecretsAreRegeneratedAndTheRolesResynced()
    {
        var bootstrapper = new FakeBootstrapper();
        await NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);
        NewProvisioner(bootstrapper).MarkComplete();
        var original = PackagedMachineSecrets.Load(layout.SecretsPath)[PackagedMachineSecrets.RuntimePasswordKey];
        File.Delete(layout.SecretsPath);

        var created = CreateSecrets();
        await NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(created, CancellationToken.None);

        Assert.True(created);
        Assert.Equal(2, bootstrapper.Calls.Count);
        Assert.NotEqual(original, bootstrapper.Calls[1].Runtime);
    }

    [Fact]
    public async Task WipedDataDirectoryIsReprovisionedWithTheExistingCredentials()
    {
        var bootstrapper = new FakeBootstrapper();
        await NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);
        NewProvisioner(bootstrapper).MarkComplete();
        Directory.Delete(layout.DataDirectory, recursive: true);

        await NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(CreateSecrets(), CancellationToken.None);

        Assert.Equal(2, bootstrapper.Calls.Count);
        Assert.Equal(bootstrapper.Calls[0], bootstrapper.Calls[1]);
        Assert.True(File.Exists(Path.Combine(layout.DataDirectory, "PG_VERSION")));
    }

    [Fact]
    public async Task MissingCredentialsStopProvisioningInsteadOfSyncingBlankPasswords()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(layout.SecretsPath)!);
        File.WriteAllText(layout.SecretsPath, "SOMETHING=else\n");
        var bootstrapper = new FakeBootstrapper();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewProvisioner(bootstrapper).EnsureEmbeddedDatabaseAsync(false, CancellationToken.None));

        Assert.Empty(bootstrapper.Calls);
    }

    [Fact]
    public async Task StateFileNeverContainsSecrets()
    {
        var provisioner = NewProvisioner(new FakeBootstrapper());
        var created = CreateSecrets();
        await provisioner.EnsureEmbeddedDatabaseAsync(created, CancellationToken.None);

        var state = File.ReadAllText(layout.StateFilePath);
        foreach (var value in PackagedMachineSecrets.Load(layout.SecretsPath).Values)
        {
            Assert.DoesNotContain(value, state, StringComparison.Ordinal);
        }
    }
}
