using System.IO;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

public sealed class PackagedRuntimeBundleLayoutTests
{
    [Fact]
    public void TryResolveFromCanonicalDesktopSubdirectoryLayout()
    {
        var bundleRoot = CreateBundleRoot();
        var desktopDirectory = Path.Combine(bundleRoot, "desktop");
        Directory.CreateDirectory(desktopDirectory);
        WriteManifest(bundleRoot);

        var result = PackagedRuntimeBundle.TryResolveFromDesktopExecutable(desktopDirectory);

        Assert.NotNull(result);
        Assert.Equal(bundleRoot, result!.BundleRoot);
        Assert.Equal(desktopDirectory, result.DesktopDirectory);
    }

    [Fact]
    public void TryResolveFromLegacyFlatInstallerLayout()
    {
        var bundleRoot = CreateBundleRoot();
        WriteManifest(bundleRoot);

        var result = PackagedRuntimeBundle.TryResolveFromDesktopExecutable(bundleRoot);

        Assert.NotNull(result);
        Assert.Equal(bundleRoot, result!.BundleRoot);
    }

    [Fact]
    public void PreflightFailsWhenPackagedBundleFilesMissing()
    {
        var bundleRoot = CreateBundleRoot();
        var desktopDirectory = Path.Combine(bundleRoot, "desktop");
        Directory.CreateDirectory(desktopDirectory);
        WriteManifest(bundleRoot);
        var bundle = PackagedRuntimeBundle.Create(bundleRoot, desktopDirectory);
        var session = new StartupSession();

        var exception = Assert.Throws<StartupPreflightException>(() =>
            StartupPreflightValidator.Validate(session, bundle));

        Assert.Equal(FactOsStartupErrorCodes.PreflightFailed, exception.ErrorCode);
    }

    private static string CreateBundleRoot()
    {
        var bundleRoot = Path.Combine(Path.GetTempPath(), $"factos-bundle-{Guid.NewGuid():N}");
        Directory.CreateDirectory(bundleRoot);
        return bundleRoot;
    }

    private static void WriteManifest(string bundleRoot)
    {
        var manifests = Path.Combine(bundleRoot, "manifests");
        Directory.CreateDirectory(manifests);
        File.WriteAllText(
            Path.Combine(manifests, "runtime-manifest.json"),
            """{"webPort":3001,"databaseArchitecture":"EMBEDDED_LOCAL"}""");
    }
}

public sealed class DeploymentChannelTests
{
    [Fact]
    public void DemoChannelEnablesDemoIdentitySecrets()
    {
        var bundleRoot = Path.Combine(Path.GetTempPath(), $"factos-channel-{Guid.NewGuid():N}");
        var configDir = Path.Combine(bundleRoot, "config");
        Directory.CreateDirectory(configDir);
        var manifestsDir = Path.Combine(bundleRoot, "manifests");
        Directory.CreateDirectory(manifestsDir);
        File.WriteAllText(
            Path.Combine(manifestsDir, "runtime-manifest.json"),
            """{"deploymentChannel":"DEMO"}""");
        var bundle = PackagedRuntimeBundle.Create(bundleRoot);
        var diagnostics = new List<string>();
        var secretsPath = PackagedRuntimeBundle.GetMachineLocalSecretsPath();
        var backup = secretsPath + ".bak";
        if (File.Exists(secretsPath))
        {
            File.Copy(secretsPath, backup, overwrite: true);
            File.Delete(secretsPath);
        }

        try
        {
            PackagedFirstRunInitializer.EnsureMachineSecrets(bundle, diagnostics);
            var content = File.ReadAllText(secretsPath);
            Assert.Contains("DevelopmentSaasIdentity__Enabled=true", content, StringComparison.Ordinal);
            Assert.Contains(PackagedFirstRunInitializer.ReleaseDemoOwnerPassword, content, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(secretsPath))
            {
                File.Delete(secretsPath);
            }

            if (File.Exists(backup))
            {
                File.Move(backup, secretsPath, overwrite: true);
            }

            if (Directory.Exists(bundleRoot))
            {
                Directory.Delete(bundleRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ProductionChannelOmitsDemoCredentials()
    {
        var bundleRoot = Path.Combine(Path.GetTempPath(), $"factos-channel-{Guid.NewGuid():N}");
        var manifestsDir = Path.Combine(bundleRoot, "manifests");
        Directory.CreateDirectory(manifestsDir);
        File.WriteAllText(
            Path.Combine(manifestsDir, "runtime-manifest.json"),
            """{"deploymentChannel":"PRODUCTION"}""");
        var bundle = PackagedRuntimeBundle.Create(bundleRoot);
        var diagnostics = new List<string>();
        var secretsPath = PackagedRuntimeBundle.GetMachineLocalSecretsPath();
        var backup = secretsPath + ".bak";
        if (File.Exists(secretsPath))
        {
            File.Copy(secretsPath, backup, overwrite: true);
            File.Delete(secretsPath);
        }

        try
        {
            PackagedFirstRunInitializer.EnsureMachineSecrets(bundle, diagnostics);
            var content = File.ReadAllText(secretsPath);
            Assert.Contains("DevelopmentSaasIdentity__Enabled=false", content, StringComparison.Ordinal);
            Assert.DoesNotContain(PackagedFirstRunInitializer.ReleaseDemoOwnerPassword, content, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(secretsPath))
            {
                File.Delete(secretsPath);
            }

            if (File.Exists(backup))
            {
                File.Move(backup, secretsPath, overwrite: true);
            }

            if (Directory.Exists(bundleRoot))
            {
                Directory.Delete(bundleRoot, recursive: true);
            }
        }
    }
}

public sealed class FactOsStartupErrorCodeTests
{
    [Fact]
    public void BundleMissingMapsToCustomerMessage()
    {
        var message = FactOsStartupErrorCodes.ToCustomerMessage(FactOsStartupErrorCodes.BundleMissing);
        Assert.Contains("Kurulum", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DatabaseFailureMapsToDedicatedCode()
    {
        var code = FactOsStartupErrorCodes.MapException(
            new InvalidOperationException("Embedded PostgreSQL failed to start."));
        Assert.Equal(FactOsStartupErrorCodes.DatabaseFailed, code);
    }
}

public sealed class FactOsDatabaseBackupServiceTests
{
    [Fact]
    public void BackupAndRestoreRoundTripPreservesFiles()
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), $"factos-backup-{Guid.NewGuid():N}");
        var dataDirectory = Path.Combine(dataRoot, "database", "data");
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(Path.Combine(dataDirectory, "marker.txt"), "baseline");

        try
        {
            Environment.SetEnvironmentVariable("FACTOS_TEST_DATA_ROOT", dataRoot);
            var backupFolder = FactOsDatabaseBackupService.CreateBackup("test-schema");
            File.WriteAllText(Path.Combine(dataDirectory, "marker.txt"), "modified");

            FactOsDatabaseBackupService.RestoreBackup(backupFolder);

            Assert.Equal("baseline", File.ReadAllText(Path.Combine(dataDirectory, "marker.txt")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("FACTOS_TEST_DATA_ROOT", null);
            if (Directory.Exists(dataRoot))
            {
                Directory.Delete(dataRoot, recursive: true);
            }
        }
    }
}
