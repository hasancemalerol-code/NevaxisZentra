using System.IO;

namespace NevaxisZentra.Desktop.Runtime;

internal static class PackagedFirstRunInitializer
{
#if !ZENTRA_PRODUCTION_BUILD
    // Demo-only identity. Compiled out of customer PRODUCTION builds (see Directory.Build.props).
    internal const string ReleaseDemoOwnerEmail = "admin@demo.nevaxiszentra.local";
    internal const string ReleaseDemoOwnerPassword = "NevaxisDemo2026!";
#endif

    /// <summary>
    /// Step 2-4 of first run: generate machine-local secrets (when absent), persist them with an
    /// owner-only ACL. Returns true when new secrets were created in this call.
    /// </summary>
    internal static bool EnsureMachineSecrets(PackagedRuntimeBundle bundle, IList<string> diagnostics) =>
        EnsureMachineSecrets(bundle, diagnostics, PackagedRuntimeBundle.GetMachineLocalSecretsPath());

    internal static bool EnsureMachineSecrets(
        PackagedRuntimeBundle bundle,
        IList<string> diagnostics,
        string secretsPath)
    {
        var manifest = bundle.TryReadManifest();
#if ZENTRA_PRODUCTION_BUILD
        // A production binary never provisions a demo identity, whatever the manifest says.
        const bool isDemoChannel = false;
#else
        var isDemoChannel = manifest?.IsDemoChannel ?? true;
#endif
        diagnostics.Add($"deployment-channel={(manifest?.DeploymentChannel ?? "DEMO")}");

        return PackagedMachineSecrets.EnsureCreated(
            secretsPath,
            Path.Combine(bundle.ConfigDirectory, "release-demo-secrets.env"),
            isDemoChannel,
            diagnostics);
    }
}
