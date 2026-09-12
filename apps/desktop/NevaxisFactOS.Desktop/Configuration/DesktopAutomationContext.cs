using System.IO;

namespace NevaxisFactOS.Desktop.Configuration;

public static class DesktopAutomationContext
{
    public const string ScreenshotPathEnvironmentVariable = "NEVAXIS_FACTOS_DESKTOP_SCREENSHOT_PATH";
    public const string ScreenshotPathArgumentPrefix = "--desktop-screenshot=";

    public static string? ScreenshotPath { get; private set; }

    public static void ConfigureFromStartup(string[] startupArguments)
    {
        ScreenshotPath = Environment.GetEnvironmentVariable(ScreenshotPathEnvironmentVariable);

        foreach (var argument in startupArguments)
        {
            if (argument.StartsWith(ScreenshotPathArgumentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                ScreenshotPath = argument[ScreenshotPathArgumentPrefix.Length..].Trim('"');
                break;
            }
        }

        if (!string.IsNullOrWhiteSpace(ScreenshotPath))
        {
            try
            {
                var directory = Path.GetDirectoryName(ScreenshotPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText($"{ScreenshotPath}.configured", ScreenshotPath);
            }
            catch
            {
                // Best-effort automation only.
            }
        }
    }
}
