using System.Windows;
using CrevaFactOS.Desktop.Services;

namespace CrevaFactOS.Desktop;

public partial class App : Application, IDisposable
{
    private SingleInstanceManager? _singleInstanceManager;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        _singleInstanceManager = new SingleInstanceManager();
        if (!_singleInstanceManager.IsPrimaryInstance)
        {
            _ = SingleInstanceManager.TryActivateExistingInstance();
            _singleInstanceManager.Dispose();
            _singleInstanceManager = null;
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        _singleInstanceManager.StartActivationListener(mainWindow);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e) => Dispose();

    public void Dispose()
    {
        _singleInstanceManager?.Dispose();
        _singleInstanceManager = null;
        GC.SuppressFinalize(this);
    }
}
