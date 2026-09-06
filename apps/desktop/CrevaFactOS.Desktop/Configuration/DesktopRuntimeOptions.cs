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

        return new DesktopRuntimeOptions
        {
            Profile = baseOptions.Profile,
            ApiPort = baseOptions.ApiPort,
            WebPort = baseOptions.WebPort,
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

    public Uri ApiHealthUri => new($"http://127.0.0.1:{ApiPort}/health/ready");

    public Uri WebOriginUri => new($"http://127.0.0.1:{WebPort}");

    public Uri WebReadinessUri => new(WebOriginUri, "/giris");

    public Uri WebEntryUri => new(WebOriginUri, WebEntryPath);
}
