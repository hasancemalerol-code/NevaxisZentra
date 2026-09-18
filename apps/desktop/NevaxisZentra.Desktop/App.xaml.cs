using System.Windows;
using System.Windows.Threading;
using NevaxisZentra.Desktop.Configuration;
using NevaxisZentra.Desktop.Runtime;
using NevaxisZentra.Desktop.Services;
using NevaxisZentra.Desktop.Views;

namespace NevaxisZentra.Desktop;

public partial class App : Application, IAsyncDisposable
{
    private SingleInstanceManager? _singleInstanceManager;
    private DesktopRuntimeSupervisor? _runtimeSupervisor;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        RegisterExceptionHandlers();
        DesktopLifecycleLogger.Initialize();
        DesktopLifecycleLogger.Write("application-startup");
        DesktopAutomationContext.ConfigureFromStartup(e.Args);

        _singleInstanceManager = new SingleInstanceManager();
        if (!_singleInstanceManager.IsPrimaryInstance)
        {
            DesktopLifecycleLogger.Write("single-instance-secondary-exit");
            _ = SingleInstanceManager.TryActivateExistingInstance();
            _singleInstanceManager.Dispose();
            _singleInstanceManager = null;
            Shutdown();
            return;
        }

        _ = LaunchApplicationAsync();
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            DesktopLifecycleLogger.WriteException("dispatcher-unhandled", args.Exception);
            args.Handled = true;
            ShowFatalStartupFailure("Beklenmeyen bir masaüstü hatası oluştu.", args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                DesktopLifecycleLogger.WriteException("appdomain-unhandled", exception);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            DesktopLifecycleLogger.WriteException("task-unobserved", args.Exception);
            args.SetObserved();
        };
    }

    private async Task LaunchApplicationAsync()
    {
        while (true)
        {
            var startupWindow = new StartupWindow();
            startupWindow.Show();
            DesktopLifecycleLogger.Write("startup-window-shown");

            try
            {
                if (_runtimeSupervisor is not null)
                {
                    await _runtimeSupervisor.DisposeAsync().ConfigureAwait(true);
                }

                _runtimeSupervisor = new DesktopRuntimeSupervisor();
                _runtimeSupervisor.StatusChanged += status =>
                    startupWindow.Dispatcher.Invoke(() => startupWindow.SetStatus(status));

                var result = await _runtimeSupervisor.StartAsync(DesktopRuntimeOptions.ResolveSalesDemoOptions()).ConfigureAwait(true);
                DesktopLifecycleLogger.Write(
                    $"runtime-ready mode={result.RuntimeMode} root={result.RepositoryRoot} selectedWebPort={result.SelectedWebPort} preferredWebPort={result.PreferredWebPort} webOrigin={result.WebOriginUri}");

                var mainWindow = new MainWindow(result.WebEntryUri, hideShellChrome: DesktopRuntimeOptions.SalesDemo.HideShellChrome);
                _singleInstanceManager!.StartActivationListener(mainWindow);
                await MainWindowStartupHandoff.ShowMainWindowAndCloseSplashAsync(startupWindow, mainWindow).ConfigureAwait(true);

                ShutdownMode = ShutdownMode.OnMainWindowClose;
                DesktopLifecycleLogger.Write("application-ready-main-window");
                return;
            }
            catch (Exception ex)
            {
                DesktopLifecycleLogger.WriteException("startup-failed", ex);
                startupWindow.Hide();

                var errorCode = FactOsStartupErrorCodes.MapException(ex);
                var diagnostics = new List<string>(_runtimeSupervisor?.Diagnostics ?? [])
                {
                    ex.GetType().Name,
                    ex.Message,
                };

                if (ex is StartupPreflightException preflight)
                {
                    diagnostics.AddRange(preflight.Checks);
                }

                var failureWindow = new StartupFailureWindow(
                    FactOsStartupErrorCodes.ToCustomerMessage(errorCode),
                    diagnostics,
                    errorCode,
                    _runtimeSupervisor?.Session);
                var retryRequested = false;
                failureWindow.RetryRequested += (_, _) => retryRequested = true;
                failureWindow.ShowDialog();
                startupWindow.Close();

                if (!retryRequested)
                {
                    DesktopLifecycleLogger.Write("startup-failure-closed");
                    Shutdown();
                    return;
                }

                DesktopLifecycleLogger.Write("startup-retry-requested");
                RuntimePaths.RefreshRuntimeRootEnvironmentForRetry();
            }
        }
    }

    private void ShowFatalStartupFailure(string summary, Exception exception)
    {
        try
        {
            var errorCode = FactOsStartupErrorCodes.MapException(exception);
            var failureWindow = new StartupFailureWindow(
                FactOsStartupErrorCodes.ToCustomerMessage(errorCode),
                [exception.GetType().Name, exception.Message],
                errorCode);
            failureWindow.ShowDialog();
        }
        catch (Exception dialogException)
        {
            DesktopLifecycleLogger.WriteException("fatal-failure-dialog", dialogException);
        }
        finally
        {
            Shutdown();
        }
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        DesktopLifecycleLogger.Write($"application-exit code={e.ApplicationExitCode}");
        await DisposeAsync().ConfigureAwait(true);
    }

    public async ValueTask DisposeAsync()
    {
        if (_runtimeSupervisor is not null)
        {
            await _runtimeSupervisor.DisposeAsync().ConfigureAwait(true);
            _runtimeSupervisor = null;
        }

        _singleInstanceManager?.Dispose();
        _singleInstanceManager = null;
        GC.SuppressFinalize(this);
    }
}
