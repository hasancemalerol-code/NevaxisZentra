using System.IO;
using NevaxisFactOS.Desktop.Services;

namespace NevaxisFactOS.Desktop.Runtime;

public static class StartupPreflightValidator
{
    private const long MinimumFreeDiskBytes = 2L * 1024 * 1024 * 1024;

    public static void Validate(StartupSession session, PackagedRuntimeBundle? bundle)
    {
        var checks = new List<string>();
        var failures = new List<string>();

        void Pass(string check) => checks.Add($"PASS {check}");
        void Fail(string check, string detail)
        {
            checks.Add($"FAIL {check}: {detail}");
            failures.Add($"{check}: {detail}");
        }

        session.SetStage("pre-flight");

        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            Fail("os", "Windows gerekli");
        }
        else
        {
            Pass("os=windows");
        }

        try
        {
            PackagedRuntimePaths.EnsureCustomerDirectories();
            var probe = Path.Combine(PackagedRuntimePaths.RuntimeDirectory, ".write-probe");
            File.WriteAllText(probe, DateTimeOffset.UtcNow.ToString("O"));
            File.Delete(probe);
            Pass("data-directories-writable");
        }
        catch (Exception ex)
        {
            Fail("data-directories", ex.Message);
        }

        var drive = new DriveInfo(Path.GetPathRoot(PackagedRuntimePaths.DataRoot) ?? "C:\\");
        if (drive.AvailableFreeSpace < MinimumFreeDiskBytes)
        {
            Fail("disk-space", $"Yetersiz disk alanı ({drive.AvailableFreeSpace / (1024 * 1024)} MB)");
        }
        else
        {
            Pass($"disk-space={drive.AvailableFreeSpace / (1024 * 1024)}MB");
        }

        if (bundle is null)
        {
            if (RuntimePaths.ResolveRuntimeMode() == DesktopRuntimeMode.Packaged)
            {
                Fail("bundle", "runtime-manifest.json bulunamadı");
            }
            else
            {
                Pass("mode=development");
            }
        }
        else
        {
            if (!File.Exists(bundle.ManifestPath))
            {
                Fail("manifest", bundle.ManifestPath);
            }
            else
            {
                Pass("manifest=present");
            }

            if (!File.Exists(bundle.ApiExecutablePath))
            {
                Fail("api-binary", bundle.ApiExecutablePath);
            }
            else
            {
                Pass("api-binary=present");
            }

            if (!File.Exists(bundle.WebServerScriptPath))
            {
                Fail("web-server", bundle.WebServerScriptPath);
            }
            else
            {
                Pass("web-server=present");
            }

            if (!File.Exists(bundle.NodeExecutablePath))
            {
                Fail("node-runtime", bundle.NodeExecutablePath);
            }
            else
            {
                Pass("node-runtime=present");
            }

            if (!Directory.Exists(bundle.PostgresBinDirectory))
            {
                Fail("postgres-bin", bundle.PostgresBinDirectory);
            }
            else
            {
                Pass("postgres-bin=present");
            }
        }

        foreach (var check in checks)
        {
            DesktopLifecycleLogger.WriteSession(session.SessionId, check);
        }

        if (failures.Count > 0)
        {
            var errorCode = bundle is null && RuntimePaths.ResolveRuntimeMode() == DesktopRuntimeMode.Packaged
                ? FactOsStartupErrorCodes.BundleMissing
                : FactOsStartupErrorCodes.PreflightFailed;
            session.SetFailure(errorCode, string.Join("; ", failures));
            throw new StartupPreflightException(errorCode, FactOsStartupErrorCodes.ToCustomerMessage(errorCode), checks);
        }
    }
}
