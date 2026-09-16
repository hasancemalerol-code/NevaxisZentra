using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NevaxisFactOS.Desktop.Runtime;

internal static class PackagedFirstRunInitializer
{
    internal const string ReleaseDemoOwnerEmail = "admin@demo.nevaxisfactos.local";
    internal const string ReleaseDemoOwnerPassword = "NevaxisDemo2026!";

    internal static void EnsureMachineSecrets(PackagedRuntimeBundle bundle, IList<string> diagnostics)
    {
        var secretsPath = PackagedRuntimeBundle.GetMachineLocalSecretsPath();
        if (File.Exists(secretsPath))
        {
            diagnostics.Add("secrets=existing");
            return;
        }

        var manifest = bundle.TryReadManifest();
        var isDemoChannel = manifest?.IsDemoChannel ?? true;
        diagnostics.Add($"deployment-channel={(manifest?.DeploymentChannel ?? "DEMO")}");

        if (isDemoChannel)
        {
            var templatePath = Path.Combine(bundle.ConfigDirectory, "release-demo-secrets.env");
            if (File.Exists(templatePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);
                File.Copy(templatePath, secretsPath, overwrite: false);
                diagnostics.Add("secrets=installed-from-template");
                return;
            }
        }

        var runtimePassword = GenerateSecret(32);
        var migrationPassword = GenerateSecret(32);
        var lines = new List<string>
        {
            "# Machine-local packaged runtime secrets. DO NOT COMMIT.",
            $"NEVAXIS_FACTOS__PERSISTENCE__PASSWORD={runtimePassword}",
            $"NEVAXIS_FACTOS__MIGRATIONS__PASSWORD={migrationPassword}",
        };

        if (isDemoChannel)
        {
            var mfaSecret = GenerateSecret(24);
            lines.Add($"DevelopmentSaasIdentity__TenantOwnerPassword={ReleaseDemoOwnerPassword}");
            lines.Add($"DevelopmentSaasIdentity__ActiveUserPassword={ReleaseDemoOwnerPassword}");
            lines.Add($"DevelopmentSaasIdentity__DeactivatedUserPassword={ReleaseDemoOwnerPassword}");
            lines.Add($"DevelopmentSaasIdentity__NoMembershipUserPassword={ReleaseDemoOwnerPassword}");
            lines.Add($"DevelopmentSaasIdentity__MfaSharedSecret={mfaSecret}");
            lines.Add("DevelopmentSaasIdentity__Enabled=true");
        }
        else
        {
            lines.Add("DevelopmentSaasIdentity__Enabled=false");
        }

        lines.Add(string.Empty);
        Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);
        File.WriteAllLines(secretsPath, lines, Encoding.UTF8);
        diagnostics.Add(isDemoChannel ? "secrets=generated-demo" : "secrets=generated-production");
    }

    private static string GenerateSecret(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes);
    }
}
