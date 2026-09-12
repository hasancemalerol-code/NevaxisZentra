using System.Windows;
using NevaxisFactOS.Desktop.Views;

namespace NevaxisFactOS.Desktop.Services;

internal static class MainWindowStartupHandoff
{
    public static async Task ShowMainWindowAndCloseSplashAsync(Window startupWindow, MainWindow mainWindow)
    {
        DesktopLifecycleLogger.Write("handoff:create-main-window-complete");

        var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnContentRendered(object? sender, EventArgs e)
        {
            mainWindow.ContentRendered -= OnContentRendered;
            rendered.TrySetResult();
        }

        mainWindow.ContentRendered += OnContentRendered;
        Application.Current.MainWindow = mainWindow;
        DesktopLifecycleLogger.Write("handoff:main-window-assigned");

        mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        mainWindow.Show();
        mainWindow.Activate();
        mainWindow.Focus();
        DesktopLifecycleLogger.Write("handoff:main-window-shown");

        if (!rendered.Task.IsCompleted)
        {
            await rendered.Task.ConfigureAwait(true);
        }

        DesktopLifecycleLogger.Write("handoff:main-window-content-rendered");
        startupWindow.Close();
        DesktopLifecycleLogger.Write("handoff:splash-closed");

        mainWindow.Activate();
        mainWindow.Focus();
    }
}
