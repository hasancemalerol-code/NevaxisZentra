using System.Net.Sockets;

namespace CrevaFactOS.Desktop.Services;

public static class SiteReachabilityProbe
{
    public static async Task<bool> CanReachAsync(Uri applicationUrl, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(
                applicationUrl.Host,
                applicationUrl.Port,
                cancellation.Token).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
