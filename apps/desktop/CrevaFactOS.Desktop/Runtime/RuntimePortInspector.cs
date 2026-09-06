using System.Diagnostics;
using System.Net.NetworkInformation;

namespace CrevaFactOS.Desktop.Runtime;

public static class RuntimePortInspector
{
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

    public static bool IsCrevaFactOSWebListener(
        int processId,
        string? repositoryRoot = null,
        string? packagedWebDirectory = null)
    {
        var commandLine = TryGetProcessCommandLine(processId);
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        var isNextWeb =
            commandLine.Contains("next", StringComparison.OrdinalIgnoreCase)
            || commandLine.Contains("@crevafactos/web", StringComparison.OrdinalIgnoreCase)
            || commandLine.Contains("start-server.js", StringComparison.OrdinalIgnoreCase)
            || commandLine.Contains("server.js", StringComparison.OrdinalIgnoreCase);
        if (!isNextWeb)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(packagedWebDirectory)
            && commandLine.Contains(packagedWebDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (commandLine.Contains("CrevaFactOS", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(repositoryRoot)
            && commandLine.Contains(repositoryRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryTerminateCrevaFactOSWebListener(
        int processId,
        string? repositoryRoot,
        string? packagedWebDirectory,
        IList<string> diagnostics)
    {
        if (!IsCrevaFactOSWebListener(processId, repositoryRoot, packagedWebDirectory))
        {
            diagnostics.Add($"web=skip-terminate-foreign-listener pid={processId}");
            return false;
        }

        return TryTerminateProcessTree(processId, diagnostics);
    }

    public static bool TryTerminateCrevaFactOSWebListener(
        int processId,
        string? repositoryRoot,
        IList<string> diagnostics) =>
        TryTerminateCrevaFactOSWebListener(processId, repositoryRoot, null, diagnostics);

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
}
