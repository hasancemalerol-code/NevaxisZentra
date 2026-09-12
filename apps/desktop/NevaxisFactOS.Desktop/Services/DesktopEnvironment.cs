using NevaxisFactOS.Desktop.Configuration;

namespace NevaxisFactOS.Desktop.Services;

public static class DesktopEnvironment
{
    public const string DevToolsEnvironmentVariable = "NEVAXIS_FACTOS_DESKTOP_ENABLE_DEVTOOLS";

    public static bool IsDevelopment =>
#if DEBUG
        true;
#else
        DesktopBuildConfiguration.DevelopmentDistribution;
#endif

    public static bool IsDevToolsEnabled =>
        IsDevelopment
        && string.Equals(
            Environment.GetEnvironmentVariable(DevToolsEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
