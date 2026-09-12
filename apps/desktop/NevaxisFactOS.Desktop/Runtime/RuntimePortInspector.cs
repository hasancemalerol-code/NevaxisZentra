using System.Diagnostics;
using System.Net.NetworkInformation;

namespace NevaxisFactOS.Desktop.Runtime;

public static class RuntimePortInspector
{
    public static readonly int[] PackagedWebPortCandidates = [3001, 3011, 3021, 3031];

    public static string? TryGetProcessCommandLine(int processId)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-NoProfile -NonInteractive -Command \"(Get-CimInstance Win32_Process -Filter 'ProcessId={processId}').CommandLine\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return string.IsNullOrWhiteSpace(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }

    public static int? TryGetParentProcessId(int processId)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-NoProfile -NonInteractive -Command \"(Get-CimInstance Win32_Process -Filter 'ProcessId={processId}').ParentProcessId\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return int.TryParse(output, out var parentId) && parentId > 0 ? parentId : null;
        }
        catch
        {
            return null;
        }
    }

    public static string? TryGetProcessName(int processId)
    {
        try
        {
            return Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            return null;
        }
    }

    public static WebListenerClassification ClassifyWebListener(
        int processId,
        string? repositoryRoot = null,
        string? packagedWebDirectory = null)
    {
        if (IsNevaxisFactOSWebListener(processId, repositoryRoot, packagedWebDirectory))
        {
            var parentId = TryGetParentProcessId(processId);
            var parentName = parentId is null ? null : TryGetProcessName(parentId.Value);
            if (parentName is "powershell" or "pwsh" or "cmd")
            {
                return WebListenerClassification.NevaxisFactOSVerifierOrphan;
            }

            return WebListenerClassification.NevaxisFactOSOwned;
        }

        var commandLine = TryGetProcessCommandLine(processId);
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return WebListenerClassification.Unknown;
        }

        if (LooksLikeWebDevServer(commandLine))
        {
            return WebListenerClassification.Foreign;
        }

        return WebListenerClassification.Unknown;
    }

    public static bool IsNevaxisFactOSWebListener(
        int processId,
        string? repositoryRoot = null,
        string? packagedWebDirectory = null)
    {
        var commandLine = TryGetProcessCommandLine(processId);
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        if (!LooksLikeWebDevServer(commandLine))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(packagedWebDirectory)
            && commandLine.Contains(packagedWebDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (commandLine.Contains("desktop-runtime", StringComparison.OrdinalIgnoreCase)
            && commandLine.Contains("crevafactos", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (commandLine.Contains("crevafactos", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(repositoryRoot)
            && commandLine.Contains(repositoryRoot, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsPackagedServerJsWithoutPath(commandLine)
            && IsInNevaxisFactOSDesktopLineage(processId, packagedWebDirectory, repositoryRoot))
        {
            return true;
        }

        return false;
    }

    public static bool TryTerminateNevaxisFactOSWebListener(
        int processId,
        string? repositoryRoot,
        string? packagedWebDirectory,
        IList<string> diagnostics)
    {
        var classification = ClassifyWebListener(processId, repositoryRoot, packagedWebDirectory);
        if (classification is not WebListenerClassification.NevaxisFactOSOwned
            and not WebListenerClassification.NevaxisFactOSVerifierOrphan)
        {
            diagnostics.Add($"web=skip-terminate-foreign-listener pid={processId} classification={classification}");
            return false;
        }

        return TryTerminateProcessTree(processId, diagnostics);
    }

    public static bool TryTerminateNevaxisFactOSWebListener(
        int processId,
        string? repositoryRoot,
        IList<string> diagnostics) =>
        TryTerminateNevaxisFactOSWebListener(processId, repositoryRoot, null, diagnostics);

    public static bool IsPortListening(int port)
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        return properties.GetActiveTcpListeners().Any(listener => listener.Port == port);
    }

    public static int? TryGetListeningProcessId(int port)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.Contains("LISTENING", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!trimmed.Contains($":{port}", StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 || !int.TryParse(parts[^1], out var pid))
                {
                    continue;
                }

                return pid;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public static bool TryTerminateProcessTree(int processId, IList<string> diagnostics)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            diagnostics.Add($"web=terminating-listener pid={processId} name={process.ProcessName}");
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
            return true;
        }
        catch (Exception ex)
        {
            diagnostics.Add($"web=terminate-listener-failed pid={processId} error={ex.Message}");
            return false;
        }
    }

    private static bool LooksLikeWebDevServer(string commandLine) =>
        commandLine.Contains("next", StringComparison.OrdinalIgnoreCase)
        || commandLine.Contains("@nevaxisfactos/web", StringComparison.OrdinalIgnoreCase)
        || commandLine.Contains("start-server.js", StringComparison.OrdinalIgnoreCase)
        || commandLine.Contains("server.js", StringComparison.OrdinalIgnoreCase);

    private static bool IsPackagedServerJsWithoutPath(string commandLine) =>
        commandLine.Contains("server.js", StringComparison.OrdinalIgnoreCase)
        && !commandLine.Contains("next dev", StringComparison.OrdinalIgnoreCase)
        && !commandLine.Contains("next\\dist", StringComparison.OrdinalIgnoreCase)
        && !commandLine.Contains("next/dist", StringComparison.OrdinalIgnoreCase);

    private static bool IsInNevaxisFactOSDesktopLineage(
        int processId,
        string? packagedWebDirectory,
        string? repositoryRoot,
        int maxDepth = 8)
    {
        var current = processId;
        for (var depth = 0; depth < maxDepth; depth++)
        {
            var processName = TryGetProcessName(current);
            if (processName is "crevafactos" or "NevaxisFactOS.Desktop")
            {
                return true;
            }

            var commandLine = TryGetProcessCommandLine(current);
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                if (!string.IsNullOrWhiteSpace(packagedWebDirectory)
                    && commandLine.Contains(packagedWebDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (commandLine.Contains("desktop-runtime", StringComparison.OrdinalIgnoreCase)
                    && commandLine.Contains("crevafactos", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(repositoryRoot)
                    && commandLine.Contains(repositoryRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var parentId = TryGetParentProcessId(current);
            if (parentId is null || parentId.Value <= 0)
            {
                break;
            }

            current = parentId.Value;
        }

        return false;
    }
}
