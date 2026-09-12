using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace NevaxisFactOS.Desktop.Runtime;

public static class RuntimeReadinessProbe
{
    public static async Task<bool> WaitForHttpSuccessAsync(
        Uri url,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                {
                    return true;
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // HttpClient timeout — retry.
            }
            catch (HttpRequestException)
            {
                // Service not ready yet.
            }
            catch (SocketException)
            {
                // Service not ready yet.
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    public static async Task<bool> IsTcpPortOpenAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(timeout);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellation.Token).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
