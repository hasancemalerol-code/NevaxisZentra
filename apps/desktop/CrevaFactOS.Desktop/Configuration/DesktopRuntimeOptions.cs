namespace CrevaFactOS.Desktop.Configuration;

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
        var entryPath = Environment.GetEnvironmentVariable(EntryPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(entryPath))
        {
            return SalesDemo;
        }

        return new DesktopRuntimeOptions
        {
            Profile = SalesDemo.Profile,
            ApiPort = SalesDemo.ApiPort,
            WebPort = SalesDemo.WebPort,
            WebEntryPath = entryPath.StartsWith('/') ? entryPath : $"/{entryPath}",
            StartupTimeout = SalesDemo.StartupTimeout,
            ManageInfrastructure = SalesDemo.ManageInfrastructure,
            ManageApi = SalesDemo.ManageApi,
            ManageWeb = SalesDemo.ManageWeb,
            HideShellChrome = SalesDemo.HideShellChrome,
        };
    }

    public Uri ApiHealthUri => new($"http://127.0.0.1:{ApiPort}/health/ready");

    public Uri WebOriginUri => new($"http://127.0.0.1:{WebPort}");

    public Uri WebReadinessUri => new(WebOriginUri, "/giris");

    public Uri WebEntryUri => new(WebOriginUri, WebEntryPath);
}
