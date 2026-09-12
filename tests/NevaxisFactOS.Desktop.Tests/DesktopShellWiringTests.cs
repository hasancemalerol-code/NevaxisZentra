using System.Windows;
using System.Windows.Input;
using NevaxisFactOS.Desktop.Services;

namespace NevaxisFactOS.Desktop.Tests;

public sealed class DesktopShellWiringTests
{
    [Fact]
    public void SingleInstanceContractUsesPerUserLocalMutexName()
    {
        Assert.Equal("Local\\NevaxisFactOS.Desktop.SingleInstance", SingleInstanceManager.MutexName);
    }

    [Fact]
    public void PersistentProfileIsOutsideRepository()
    {
        var profilePath = WebViewProfilePaths.GetUserDataFolder();

        Assert.Contains("crevafactos", profilePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Projects", profilePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NevaxisFactOS.Desktop", profilePath, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionRetryCoordinatorTracksRetryAttempts()
    {
        var coordinator = new ConnectionRetryCoordinator();

        Assert.Equal(0, coordinator.RetryCount);
        coordinator.RecordRetryAttempt();
        coordinator.RecordRetryAttempt();
        Assert.Equal(2, coordinator.RetryCount);
        coordinator.Reset();
        Assert.Equal(0, coordinator.RetryCount);
    }

    [Fact]
    public void KeyboardCommandsWireNativeNavigationShortcuts()
    {
        RunSta(() =>
        {
            if (Application.Current is null)
            {
                var app = new App();
                app.InitializeComponent();
            }

            var window = new MainWindow();
            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.True(KeyboardCommands.TryHandle(Key.F11, ModifierKeys.None, window));
                Assert.True(window.IsFullscreen);
                Assert.Equal(WindowStyle.None, window.CurrentWindowStyle);
                Assert.Equal(ResizeMode.NoResize, window.CurrentResizeMode);
                Assert.Equal(Visibility.Collapsed, window.CurrentViewMenuVisibility);

                Assert.True(KeyboardCommands.TryHandle(Key.Escape, ModifierKeys.None, window));
                Assert.False(window.IsFullscreen);
                Assert.Equal(WindowStyle.SingleBorderWindow, window.CurrentWindowStyle);
                Assert.Equal(ResizeMode.CanResize, window.CurrentResizeMode);
                Assert.Equal(Visibility.Visible, window.CurrentViewMenuVisibility);
                Assert.True(KeyboardCommands.TryHandle(Key.F5, ModifierKeys.None, window));
                Assert.True(KeyboardCommands.TryHandle(Key.R, ModifierKeys.Control, window));
                Assert.True(KeyboardCommands.TryHandle(Key.Left, ModifierKeys.Alt, window));
                Assert.True(KeyboardCommands.TryHandle(Key.Right, ModifierKeys.Alt, window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static void RunSta(Action action)
    {
        RunStaStatic(action);
    }

    internal static void RunStaStatic(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (captured is not null)
        {
            throw captured;
        }
    }
}
