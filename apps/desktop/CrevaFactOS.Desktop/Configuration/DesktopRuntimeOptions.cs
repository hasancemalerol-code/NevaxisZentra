namespace CrevaFactOS.Desktop.Configuration;

using CrevaFactOS.Desktop.Runtime;

public enum DesktopRuntimeProfile
{
    SalesDemo,
}

public sealed class DesktopRuntimeOptions
{
    public DesktopRuntimeProfile Profile { get; init; } = DesktopRuntimeProfile.SalesDemo;

    public int ApiPort { get; init; } = 5000;

    public int WebPort { get; init; } = 3001;

    public string WebEntryPath { get; init; } = "/dashboard";

    public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromMinutes(3);

    public bool ManageInfrastructure { get; init; } = true;

    public bool ManageApi { get; init; } = true;

    public bool ManageWeb { get; init; } = true;

    public bool HideShellChrome { get; init; } = true;

    public DesktopRuntimeMode Mode { get; init; } = DesktopRuntimeMode.Development;

    public static DesktopRuntimeOptions SalesDemo { get; } = new()
    {
        Profile = DesktopRuntimeProfile.SalesDemo,
        ApiPort = 5000,
        WebPort = 3001,
        PreferredWebPort = 3001,
        WebEntryPath = "/dashboard",
        StartupTimeout = TimeSpan.FromMinutes(3),
        ManageInfrastructure = true,
        ManageApi = true,
        ManageWeb = true,
        HideShellChrome = true,
    };

    public const string EntryPathEnvironmentVariable = "CREVAFACTOS_DESKTOP_ENTRY_PATH";

    public static DesktopRuntimeOptions ResolveSalesDemoOptions()
    {
        var mode = RuntimePaths.ResolveRuntimeMode();
        var entryPath = Environment.GetEnvironmentVariable(EntryPathEnvironmentVariable);
        var baseOptions = SalesDemo;
        var preferredWebPort = ResolvePreferredWebPort(mode, baseOptions.WebPort);

        return new DesktopRuntimeOptions
        {
            Profile = baseOptions.Profile,
            ApiPort = baseOptions.ApiPort,
            WebPort = preferredWebPort,
            PreferredWebPort = preferredWebPort,
            WebEntryPath = string.IsNullOrWhiteSpace(entryPath)
                ? baseOptions.WebEntryPath
                : entryPath.StartsWith('/') ? entryPath : $"/{entryPath}",
            StartupTimeout = baseOptions.StartupTimeout,
            ManageInfrastructure = baseOptions.ManageInfrastructure,
            ManageApi = baseOptions.ManageApi,
            ManageWeb = baseOptions.ManageWeb,
            HideShellChrome = baseOptions.HideShellChrome,
            Mode = mode,
        };
    }

    public int PreferredWebPort { get; init; } = 3001;

    public DesktopRuntimeOptions WithWebPort(int webPort) =>
        new()
        {
            Profile = Profile,
            ApiPort = ApiPort,
            WebPort = webPort,
            PreferredWebPort = PreferredWebPort,
            WebEntryPath = WebEntryPath,
            StartupTimeout = StartupTimeout,
            ManageInfrastructure = ManageInfrastructure,
            ManageApi = ManageApi,
            ManageWeb = ManageWeb,
            HideShellChrome = HideShellChrome,
            Mode = Mode,
        };

    private static int ResolvePreferredWebPort(DesktopRuntimeMode mode, int defaultWebPort)
    {
        if (mode != DesktopRuntimeMode.Packaged)
        {
            return defaultWebPort;
        }

        try
        {
            var bundle = PackagedRuntimeBundle.TryResolveFromDesktopExecutable();
            if (bundle is null)
            {
                return defaultWebPort;
            }

            var manifest = bundle.TryReadManifest();
            return manifest?.WebPort ?? defaultWebPort;
        }
        catch
        {
            return defaultWebPort;
        }
    }

    public Uri ApiHealthUri => new($"http://127.0.0.1:{ApiPort}/health/ready");

    public Uri WebOriginUri => new($"http://127.0.0.1:{WebPort}");

    public Uri WebReadinessUri => new(WebOriginUri, "/giris");

    public Uri WebEntryUri => new(WebOriginUri, WebEntryPath);
}
