using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CrevaFactOS.Desktop.Security;
using CrevaFactOS.Desktop.Services;
using Microsoft.Web.WebView2.Core;

namespace CrevaFactOS.Desktop;

public partial class MainWindow : Window
{
    private readonly ConnectionRetryCoordinator _retryCoordinator = new();
    private NavigationPolicy? _navigationPolicy;
    private Uri? _applicationUrl;
    private bool _isFullscreen;
    private WindowState _previousWindowState = WindowState.Normal;
    private WindowStyle _previousWindowStyle = WindowStyle.SingleBorderWindow;
    private ResizeMode _previousResizeMode = ResizeMode.CanResize;
    private Visibility _previousMenuVisibility = Visibility.Visible;
    private NativeWindowBounds _savedNormalBounds;
    private bool _webViewInitialized;
    private bool _initialNavigationCompleted;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    public bool IsFullscreen => _isFullscreen;

    public WindowStyle CurrentWindowStyle => WindowStyle;

    public ResizeMode CurrentResizeMode => ResizeMode;

    public NativeWindowBounds SavedNormalBounds => _savedNormalBounds;

    public Visibility CurrentViewMenuVisibility => ViewMenu.Visibility;

    public ConnectionRetryCoordinator RetryCoordinator => _retryCoordinator;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _ = InitializeWebHostAsync();
    }

    private async Task InitializeWebHostAsync()
    {
        ShowLoadingState();

        var resolution = DesktopWebUrlPolicy.Resolve(DesktopEnvironment.IsDevelopment);
        if (resolution.Result != WebUrlValidationResult.Valid || resolution.Url is null)
        {
            ShowConnectionFailure(GetConfigurationFailureMessage(resolution.Result));
            return;
        }

        _applicationUrl = resolution.Url;
        _navigationPolicy = new NavigationPolicy(_applicationUrl);

        try
        {
            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: WebViewProfilePaths.GetUserDataFolder()).ConfigureAwait(true);

            await WebView.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
            ConfigureWebViewSettings(WebView.CoreWebView2.Settings);
            WireWebViewEvents(WebView.CoreWebView2);
            _webViewInitialized = true;
            NavigateToApplication();
        }
        catch
        {
            ShowConnectionFailure(
                "Yapılandırılmış uygulama hizmetine şu anda erişilemiyor. Lütfen servisin çalıştığını doğrulayın ve yeniden deneyin.");
        }
    }

    private static void ConfigureWebViewSettings(CoreWebView2Settings settings)
    {
        settings.AreDevToolsEnabled = DesktopEnvironment.IsDevToolsEnabled;
        settings.AreDefaultContextMenusEnabled = true;
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
    }

    private void WireWebViewEvents(CoreWebView2 coreWebView)
    {
        coreWebView.NavigationStarting += CoreWebView_NavigationStarting;
        coreWebView.NavigationCompleted += CoreWebView_NavigationCompleted;
        coreWebView.NewWindowRequested += CoreWebView_NewWindowRequested;
        coreWebView.ServerCertificateErrorDetected += CoreWebView_ServerCertificateErrorDetected;
        coreWebView.PermissionRequested += CoreWebView_PermissionRequested;
        coreWebView.DownloadStarting += CoreWebView_DownloadStarting;
    }

    private async Task NavigateToApplicationAsync()
    {
        if (_applicationUrl is null || WebView.CoreWebView2 is null)
        {
            ShowConnectionFailure(
                "Yapılandırılmış uygulama hizmetine şu anda erişilemiyor. Lütfen servisin çalıştığını doğrulayın ve yeniden deneyin.");
            return;
        }

        ShowLoadingState();

        var reachable = await SiteReachabilityProbe.CanReachAsync(
            _applicationUrl,
            TimeSpan.FromSeconds(5)).ConfigureAwait(true);

        if (!reachable)
        {
            ShowConnectionFailure(
                "Yapılandırılmış uygulama hizmetine şu anda erişilemiyor. Lütfen servisin çalıştığını doğrulayın ve yeniden deneyin.");
            return;
        }

        WebView.CoreWebView2.Navigate(_applicationUrl.AbsoluteUri);
    }

    private void NavigateToApplication()
    {
        _ = NavigateToApplicationAsync();
    }

    private void CoreWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (_navigationPolicy is null)
        {
            e.Cancel = true;
            return;
        }

        var decision = _navigationPolicy.Evaluate(e.Uri, e.IsUserInitiated);
        switch (decision)
        {
            case NavigationDecision.AllowInWebView:
                return;
            case NavigationDecision.OpenInSystemBrowser:
                e.Cancel = true;
                ExternalBrowserService.Open(new Uri(e.Uri));
                return;
            default:
                e.Cancel = true;
                break;
        }
    }

    private void CoreWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _initialNavigationCompleted = true;
            _retryCoordinator.Reset();
            ShowWebContentState();
            return;
        }

        ShowConnectionFailure(
            "Yapılandırılmış uygulama hizmetine şu anda erişilemiyor. Lütfen servisin çalıştığını doğrulayın ve yeniden deneyin.");
    }

    private void CoreWebView_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        if (_navigationPolicy is null)
        {
            e.Handled = true;
            return;
        }

        var decision = _navigationPolicy.Evaluate(e.Uri, isUserInitiated: true);
        switch (decision)
        {
            case NavigationDecision.AllowInWebView:
                e.Handled = true;
                WebView.CoreWebView2?.Navigate(e.Uri);
                return;
            case NavigationDecision.OpenInSystemBrowser:
                e.Handled = true;
                ExternalBrowserService.Open(new Uri(e.Uri));
                return;
            default:
                e.Handled = true;
                break;
        }
    }

    private static void CoreWebView_ServerCertificateErrorDetected(
        object? sender,
        CoreWebView2ServerCertificateErrorDetectedEventArgs e)
    {
        e.Action = CoreWebView2ServerCertificateErrorAction.Default;
    }

    private static void CoreWebView_PermissionRequested(
        object? sender,
        CoreWebView2PermissionRequestedEventArgs e)
    {
        e.State = CoreWebView2PermissionState.Deny;
        e.Handled = true;
    }

    private static void CoreWebView_DownloadStarting(
        object? sender,
        CoreWebView2DownloadStartingEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.ResultFilePath))
        {
            e.Cancel = true;
            e.Handled = true;
        }
    }

    private void ShowLoadingState()
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        ConnectionFailureOverlay.Visibility = Visibility.Collapsed;
        WebView.Visibility = Visibility.Collapsed;
    }

    private void ShowWebContentState()
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        ConnectionFailureOverlay.Visibility = Visibility.Collapsed;
        WebView.Visibility = Visibility.Visible;
    }

    private void ShowConnectionFailure(string detailMessage)
    {
        ConnectionFailureDetailText.Text = detailMessage;
        LoadingOverlay.Visibility = Visibility.Collapsed;
        ConnectionFailureOverlay.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Collapsed;
    }

    private static string GetConfigurationFailureMessage(WebUrlValidationResult result) =>
        result switch
        {
            WebUrlValidationResult.MissingInProduction =>
                "Üretim ortamında geçerli bir HTTPS uygulama adresi yapılandırılmalıdır.",
            WebUrlValidationResult.HttpsRequiredInProduction =>
                "Üretim ortamında yalnızca geçerli HTTPS uygulama adresleri kabul edilir.",
            WebUrlValidationResult.LocalhostNotAllowedInProduction =>
                "Üretim ortamında localhost adresleri kullanılamaz.",
            WebUrlValidationResult.UnsafeScheme =>
                "Yapılandırılmış uygulama adresi güvenli olmayan bir şema kullanıyor.",
            WebUrlValidationResult.InvalidUri =>
                "Yapılandırılmış uygulama adresi geçersiz.",
            _ =>
                "Yapılandırılmış uygulama hizmetine şu anda erişilemiyor. Lütfen servisin çalıştığını doğrulayın ve yeniden deneyin.",
        };

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        _retryCoordinator.RecordRetryAttempt();

        if (!_webViewInitialized)
        {
            _ = InitializeWebHostAsync();
            return;
        }

        NavigateToApplication();
    }

    private void GoBack_Click(object sender, RoutedEventArgs e) => ExecuteBack();

    private void GoForward_Click(object sender, RoutedEventArgs e) => ExecuteForward();

    private void Refresh_Click(object sender, RoutedEventArgs e) => ExecuteRefresh();

    private void ToggleFullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (KeyboardCommands.TryHandle(e, this))
        {
            e.Handled = true;
        }
    }

    internal void ExecuteBack()
    {
        if (WebView.CoreWebView2?.CanGoBack == true)
        {
            WebView.CoreWebView2.GoBack();
        }
    }

    internal void ExecuteForward()
    {
        if (WebView.CoreWebView2?.CanGoForward == true)
        {
            WebView.CoreWebView2.GoForward();
        }
    }

    internal void ExecuteRefresh()
    {
        if (_webViewInitialized && WebView.CoreWebView2 is not null)
        {
            if (_initialNavigationCompleted)
            {
                WebView.CoreWebView2.Reload();
            }
            else
            {
                NavigateToApplication();
            }

            return;
        }

        _ = InitializeWebHostAsync();
    }

    internal void ToggleFullscreen()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (!_isFullscreen)
        {
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;
            _previousMenuVisibility = ViewMenu.Visibility;
            _savedNormalBounds = NativeWindowInterop.GetExtendedFrameBounds(windowHandle);

            var monitorBounds = NativeWindowInterop.GetMonitorBounds(windowHandle);
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Normal;
            ViewMenu.Visibility = Visibility.Collapsed;
            _ = NativeWindowInterop.TrySetExactExtendedFrameBounds(windowHandle, monitorBounds);
            _isFullscreen = true;
            return;
        }

        ViewMenu.Visibility = _previousMenuVisibility;
        WindowStyle = _previousWindowStyle;
        ResizeMode = _previousResizeMode;
        WindowState = WindowState.Normal;
        if (!NativeWindowInterop.TrySetExactExtendedFrameBounds(windowHandle, _savedNormalBounds))
        {
            NativeWindowInterop.TrySetExactExtendedFrameBounds(
                windowHandle,
                _savedNormalBounds,
                maxAttempts: 24);
        }

        _isFullscreen = false;
    }
}
