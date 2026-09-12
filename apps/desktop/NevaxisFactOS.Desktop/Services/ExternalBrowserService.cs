using System.Diagnostics;

namespace NevaxisFactOS.Desktop.Services;

public static class ExternalBrowserService
{
    public static void Open(Uri targetUri)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = targetUri.AbsoluteUri,
            UseShellExecute = true,
        });
    }
}
