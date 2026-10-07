using System.Diagnostics;
using System.IO;

namespace NevaxisZentra.Desktop.Tests;

public sealed class CredentialScanTests : IDisposable
{
    private const string Planted = "Planted-Reusable-Credential-9f3a7c";
    private readonly string payload = Path.Combine(Path.GetTempPath(), $"zentra-scan-{Guid.NewGuid():N}");
    private readonly string knownSecrets;

    public CredentialScanTests()
    {
        Directory.CreateDirectory(Path.Combine(payload, "config"));
        File.WriteAllText(Path.Combine(payload, "config", "runtime.env"), "NEVAXIS_FACTOS__PERSISTENCE__HOST=127.0.0.1\n");
        knownSecrets = Path.Combine(payload, "..", $"known-{Guid.NewGuid():N}.env");
        File.WriteAllText(knownSecrets, $"NEVAXIS_FACTOS__MIGRATIONS__PASSWORD={Planted}\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(payload))
        {
            Directory.Delete(payload, recursive: true);
        }

        File.Delete(knownSecrets);
    }

    private (int Exit, string Output) Scan(string channel = "PRODUCTION")
    {
        var script = Path.Combine(RealPostgres.RepositoryRoot!, "scripts", "release", "scan-release-credentials.ps1");
        var startInfo = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[]
                 {
                     "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script,
                     "-PayloadRoot", payload, "-DeploymentChannel", channel,
                     "-KnownSecretSources", knownSecrets,
                 })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }

    [Fact]
    public void CleanPayloadPasses()
    {
        var (exit, output) = Scan();

        Assert.Equal(0, exit);
        Assert.Contains("CREDENTIAL_SCAN: PASS", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PlantedCredentialIsDetectedAndNeverPrinted()
    {
        File.WriteAllText(Path.Combine(payload, "config", "leak.txt"), $"x={Planted}");

        var (exit, output) = Scan();

        Assert.NotEqual(0, exit);
        Assert.Contains("leak.txt", output, StringComparison.Ordinal);
        Assert.DoesNotContain(Planted, output, StringComparison.Ordinal);
    }

    [Fact]
    public void DemoSecretsFileAndDevelopmentSettingsFailAProductionPayload()
    {
        File.WriteAllText(Path.Combine(payload, "config", "release-demo-secrets.env"), "DevelopmentSaasIdentity__Enabled=true\n");
        Directory.CreateDirectory(Path.Combine(payload, "api"));
        File.WriteAllText(Path.Combine(payload, "api", "appsettings.Development.json"), "{}");

        var (exit, output) = Scan();

        Assert.NotEqual(0, exit);
        Assert.Contains("release-demo-secrets.env", output, StringComparison.Ordinal);
        Assert.Contains("appsettings.Development.json", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("NevaxisDemo2026!")]
    [InlineData("admin@demo.nevaxiszentra.local")]
    [InlineData("active-user@dev.local")]
    public void DemoCredentialLiteralsInAnyProductionBinaryOrArtifactFailTheGate(string literal)
    {
        Directory.CreateDirectory(Path.Combine(payload, "desktop"));
        File.WriteAllBytes(
            Path.Combine(payload, "desktop", "NevaxisZentra.dll"),
            [.. new byte[] { 0x4D, 0x5A, 0x00 }, .. System.Text.Encoding.Unicode.GetBytes(literal), .. new byte[] { 0x00, 0x00 }]);

        var (exit, output) = Scan();

        Assert.NotEqual(0, exit);
        Assert.Contains("demo credential literal found in production artifact", output, StringComparison.Ordinal);
        Assert.Contains("NevaxisZentra.dll", output, StringComparison.Ordinal);
        Assert.DoesNotContain(literal, output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("local_dev_only_change_me")]
    [InlineData("local_runtime_only_change_me")]
    public void DevelopmentDefaultPasswordsInAProductionBinaryFailTheGateButNotOtherChannels(string literal)
    {
        Directory.CreateDirectory(Path.Combine(payload, "tools"));
        File.WriteAllBytes(
            Path.Combine(payload, "tools", "NevaxisZentra.DatabaseTool.dll"),
            [.. new byte[] { 0x4D, 0x5A, 0x00 }, .. System.Text.Encoding.Unicode.GetBytes(literal), .. new byte[] { 0x00, 0x00 }]);

        var (productionExit, productionOutput) = Scan("PRODUCTION");
        var (demoExit, _) = Scan("DEMO");

        Assert.NotEqual(0, productionExit);
        Assert.Contains("development default password literal found in production artifact", productionOutput, StringComparison.Ordinal);
        Assert.Contains("NevaxisZentra.DatabaseTool.dll", productionOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(literal, productionOutput, StringComparison.Ordinal);
        Assert.Equal(0, demoExit);
    }

    [Fact]
    public void DemoLiteralsAreOnlyEnforcedForTheProductionChannel()
    {
        Directory.CreateDirectory(Path.Combine(payload, "api"));
        File.WriteAllText(Path.Combine(payload, "api", "demo-fixture.json"), "{\"email\":\"admin@demo.nevaxiszentra.local\"}");

        var (productionExit, _) = Scan("PRODUCTION");
        var (demoExit, demoOutput) = Scan("DEMO");

        Assert.NotEqual(0, productionExit);
        Assert.Equal(0, demoExit);
        Assert.Contains("CREDENTIAL_SCAN: PASS", demoOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void SecretLikeConfigKeyWithAValueFails()
    {
        File.WriteAllText(Path.Combine(payload, "config", "runtime.env"), "NEVAXIS_FACTOS__PERSISTENCE__PASSWORD=whatever-value\n");

        var (exit, output) = Scan();

        Assert.NotEqual(0, exit);
        Assert.DoesNotContain("whatever-value", output, StringComparison.Ordinal);
    }
}
