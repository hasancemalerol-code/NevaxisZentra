namespace NevaxisZentra.Desktop.Runtime;

public sealed class RuntimeWatchdog : IDisposable
{
    private readonly List<RuntimeProcess> _processes;
    private readonly IList<string> _diagnostics;
    private readonly Timer _timer;
    private int _restartAttempts;

    public RuntimeWatchdog(IEnumerable<RuntimeProcess> processes, IList<string> diagnostics)
    {
        _processes = processes.ToList();
        _diagnostics = diagnostics;
        _timer = new Timer(CheckProcesses, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public event Action<string>? ComponentFailed;

    private void CheckProcesses(object? state)
    {
        foreach (var process in _processes)
        {
            if (process.HasExited)
            {
                _restartAttempts++;
                var message = $"watchdog=component-exited name={process.Name} pid={process.ProcessId} attempts={_restartAttempts}";
                _diagnostics.Add(message);
                ComponentFailed?.Invoke(message);

                if (_restartAttempts >= 3)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
