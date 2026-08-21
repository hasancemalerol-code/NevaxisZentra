namespace CrevaFactOS.Desktop.Services;

public static class WebViewProfilePaths
{
    public const string ProfileFolderName = "WebView2";

    public static string GetUserDataFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(localAppData, "CrevaFactOS", "Desktop", ProfileFolderName);
    }
}
