using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;

namespace CrevaFactOS.Desktop.Runtime;

public sealed class RuntimeProcess : IDisposable
{
    private readonly Process? _process;
    private readonly StreamWriter? _stdoutWriter;
    private readonly StreamWriter? _stderrWriter;
    private bool _disposed;

    private RuntimeProcess(
        Process? process,
        StreamWriter? stdoutWriter,
        StreamWriter? stderrWriter,
        bool isOwned,
        int? processId)
    {
        _process = process;
        _stdoutWriter = stdoutWriter;
        _stderrWriter = stderrWriter;
        IsOwned = isOwned;
        ProcessId = processId ?? process?.Id;
    }

    public int? ProcessId { get; }

    public bool IsOwned { get; }

    public static RuntimeProcess AttachExisting(int processId) =>
        new(null, null, null, isOwned: false, processId);

    public static RuntimeProcess StartHidden(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        string stdoutLogFilePath,
        string? stderrLogFilePath = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(stdoutLogFilePath)!);
        var stdoutWriter = CreateLogWriter(stdoutLogFilePath);
        var stderrWriter = string.IsNullOrWhiteSpace(stderrLogFilePath)
            ? stdoutWriter
            : CreateLogWriter(stderrLogFilePath!);

        stdoutWriter.WriteLine($"[{DateTimeOffset.Now:O}] START {fileName} {arguments}");
        stdoutWriter.WriteLine($"[{DateTimeOffset.Now:O}] WORKING_DIRECTORY {workingDirectory}");
        if (environment is not null)
        {
            foreach (var pair in environment.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (pair.Key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.Contains("SECRET", StringComparison.OrdinalIgnoreCase))
                {
                    stdoutWriter.WriteLine($"[{DateTimeOffset.Now:O}] ENV {pair.Key}=***");
                    continue;
                }

                stdoutWriter.WriteLine($"[{DateTimeOffset.Now:O}] ENV {pair.Key}={pair.Value}");
            }
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                stdoutWriter.WriteLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                if (ReferenceEquals(stderrWriter, stdoutWriter))
                {
                    stdoutWriter.WriteLine($"[stderr] {args.Data}");
                }
                else
                {
                    stderrWriter.WriteLine(args.Data);
                }
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Çalışma zamanı süreci başlatılamadı: {fileName}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return new RuntimeProcess(process, stdoutWriter, stderrWriter, isOwned: true, process?.Id);
    }

    private static StreamWriter CreateLogWriter(string logFilePath) =>
        new(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true,
        };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (IsOwned && _process is not null && !_process.HasExited)
        {
            try
            {
                _process.CloseMainWindow();
            }
            catch
            {
                // Process may not have a main window.
            }

            if (!_process.WaitForExit(4000))
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort shutdown.
                }
            }
        }

        _process?.Dispose();
        _stdoutWriter?.Dispose();
        if (!ReferenceEquals(_stdoutWriter, _stderrWriter))
        {
            _stderrWriter?.Dispose();
        }
    }
}
