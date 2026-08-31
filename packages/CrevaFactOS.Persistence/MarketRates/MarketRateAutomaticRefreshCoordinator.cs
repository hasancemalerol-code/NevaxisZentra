using System.Collections.Concurrent;

namespace CrevaFactOS.Persistence.MarketRates;

internal static class MarketRateAutomaticRefreshCoordinator
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RefreshGates = new(StringComparer.Ordinal);

    public static async Task ExecuteExclusiveAsync(
        string scopeKey,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var gate = RefreshGates.GetOrAdd(scopeKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }
}
