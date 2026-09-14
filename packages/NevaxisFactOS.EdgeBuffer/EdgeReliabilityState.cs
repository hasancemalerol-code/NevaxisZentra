namespace NevaxisFactOS.EdgeBuffer;

/// <summary>
/// Mutable runtime state for deterministic reliability scenario control.
/// Thread-safe for simulator HTTP endpoints and delivery worker.
/// </summary>
public sealed class EdgeReliabilityState
{
    private readonly object _lock = new();

    public bool ApiAvailable { get; private set; } = true;
    public string ActiveScenario { get; private set; } = "normal";
    public bool ForceDuplicateReplay { get; private set; }
    public int? TestMaxBufferedEvents { get; private set; }
    public HashSet<string> NonRetryableIdempotencyKeys { get; } = new(StringComparer.Ordinal);
    public HashSet<string> RetryableOnceIdempotencyKeys { get; } = new(StringComparer.Ordinal);
    public int SimulatedHttpStatus { get; private set; }

    public void Reset(string scenario = "normal")
    {
        lock (_lock)
        {
            ApiAvailable = true;
            ActiveScenario = scenario;
            ForceDuplicateReplay = false;
            TestMaxBufferedEvents = null;
            NonRetryableIdempotencyKeys.Clear();
            RetryableOnceIdempotencyKeys.Clear();
            SimulatedHttpStatus = 0;
        }
    }

    public void SetApiAvailable(bool available)
    {
        lock (_lock) { ApiAvailable = available; }
    }

    public void SetScenario(string scenario)
    {
        lock (_lock) { ActiveScenario = scenario; }
    }

    public void EnableDuplicateReplay()
    {
        lock (_lock) { ForceDuplicateReplay = true; }
    }

    public void SetTestMaxBufferedEvents(int max)
    {
        lock (_lock) { TestMaxBufferedEvents = max; }
    }

    public void MarkNonRetryable(string idempotencyKey)
    {
        lock (_lock) { NonRetryableIdempotencyKeys.Add(idempotencyKey); }
    }

    public void MarkRetryableOnce(string idempotencyKey)
    {
        lock (_lock) { RetryableOnceIdempotencyKeys.Add(idempotencyKey); }
    }

    public void SetSimulatedHttpStatus(int status)
    {
        lock (_lock) { SimulatedHttpStatus = status; }
    }

    public bool IsApiAvailable()
    {
        lock (_lock) { return ApiAvailable; }
    }

    public string GetActiveScenario()
    {
        lock (_lock) { return ActiveScenario; }
    }

    public bool ShouldForceDuplicateReplay()
    {
        lock (_lock) { return ForceDuplicateReplay; }
    }

    public int GetEffectiveMaxBufferedEvents()
    {
        lock (_lock) { return TestMaxBufferedEvents ?? EdgeBufferConstants.DefaultMaxBufferedEvents; }
    }

    public bool IsNonRetryableKey(string key)
    {
        lock (_lock) { return NonRetryableIdempotencyKeys.Contains(key); }
    }

    public bool TryConsumeRetryableOnce(string key)
    {
        lock (_lock) { return RetryableOnceIdempotencyKeys.Remove(key); }
    }

    public int GetSimulatedHttpStatus()
    {
        lock (_lock) { return SimulatedHttpStatus; }
    }
}
