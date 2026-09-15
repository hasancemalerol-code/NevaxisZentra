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

        var templatePath = Path.Combine(bundle.ConfigDirectory, "release-demo-secrets.env");
        if (File.Exists(templatePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);
            File.Copy(templatePath, secretsPath, overwrite: false);
            diagnostics.Add("secrets=installed-from-template");
            return;
        }

        var runtimePassword = GenerateSecret(32);
        var migrationPassword = GenerateSecret(32);
        var mfaSecret = GenerateSecret(24);
        var lines = new[]
        {
            "# Machine-local packaged runtime secrets. DO NOT COMMIT.",
            $"DevelopmentSaasIdentity__TenantOwnerPassword={ReleaseDemoOwnerPassword}",
            $"DevelopmentSaasIdentity__ActiveUserPassword={ReleaseDemoOwnerPassword}",
            $"DevelopmentSaasIdentity__DeactivatedUserPassword={ReleaseDemoOwnerPassword}",
            $"DevelopmentSaasIdentity__NoMembershipUserPassword={ReleaseDemoOwnerPassword}",
            $"DevelopmentSaasIdentity__MfaSharedSecret={mfaSecret}",
            "DevelopmentSaasIdentity__Enabled=true",
            $"NEVAXIS_FACTOS__PERSISTENCE__PASSWORD={runtimePassword}",
            $"NEVAXIS_FACTOS__MIGRATIONS__PASSWORD={migrationPassword}",
            string.Empty,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);
        File.WriteAllLines(secretsPath, lines, Encoding.UTF8);
        diagnostics.Add("secrets=generated");
    }

    private static string GenerateSecret(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes);
    }
}
