using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NevaxisFactOS.Desktop.Services;

public sealed class SingleInstanceManager : IDisposable
{
    public const string MutexName = "Local\\NevaxisFactOS.Desktop.SingleInstance";
    public const string ActivationPipeName = "NevaxisFactOS.Desktop.Activate";

    private readonly Mutex _mutex;
    private readonly bool _ownsMutex;
    private CancellationTokenSource? _listenerCancellation;

    public SingleInstanceManager()
    {
        _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out _ownsMutex);
    }

    public bool IsPrimaryInstance => _ownsMutex;

    public void StartActivationListener(Window mainWindow)
    {
        if (!_ownsMutex)
        {
            return;
        }

        _listenerCancellation = new CancellationTokenSource();
        _ = ListenForActivationRequestsAsync(mainWindow, _listenerCancellation.Token);
    }

    public static bool TryActivateExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(
                ".",
                ActivationPipeName,
                PipeDirection.Out,
                PipeOptions.None);

            client.Connect(timeout: 1500);
            using var writer = new StreamWriter(client);
            writer.WriteLine("activate");
            writer.Flush();
            return true;
        }
        catch (TimeoutException)
        {
            return ActivateExistingWindowByProcessScan();
        }
        catch (IOException)
        {
            return ActivateExistingWindowByProcessScan();
        }
    }

    public void Dispose()
    {
        _listenerCancellation?.Cancel();
        _listenerCancellation?.Dispose();

        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }

    private static async Task ListenForActivationRequestsAsync(Window mainWindow, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    ActivationPipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                using var reader = new StreamReader(server);
                _ = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                await mainWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                    {
                        mainWindow.WindowState = WindowState.Normal;
                    }

                    mainWindow.Show();
                    mainWindow.Activate();

                    var handle = new WindowInteropHelper(mainWindow).Handle;
                    if (handle != IntPtr.Zero)
                    {
                        _ = SetForegroundWindow(handle);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool ActivateExistingWindowByProcessScan()
    {
        var currentProcessId = Environment.ProcessId;
        var processes = Process.GetProcessesByName("NevaxisFactOS");
        if (processes.Length == 0)
        {
            processes = Process.GetProcessesByName("crevafactos");
        }
        if (processes.Length == 0)
        {
            processes = Process.GetProcessesByName("NevaxisFactOS.Desktop");
        }

        foreach (var process in processes)
        {
            using (process)
            {
                if (process.Id == currentProcessId)
                {
                    continue;
                }

                var handle = process.MainWindowHandle;
                if (handle == IntPtr.Zero)
                {
                    continue;
                }

                _ = ShowWindow(handle, SwRestore);
                _ = SetForegroundWindow(handle);
                return true;
            }
        }

        return false;
    }

    private const int SwRestore = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
