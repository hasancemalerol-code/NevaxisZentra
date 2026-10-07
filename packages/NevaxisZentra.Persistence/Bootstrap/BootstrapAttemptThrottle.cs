namespace NevaxisZentra.Persistence.Bootstrap;

/// <summary>Blocks token guessing: too many failures in a window lock the endpoint until the window passes.</summary>
public sealed class BootstrapAttemptThrottle(
    int maxFailures = 5,
    TimeSpan? window = null,
    TimeProvider? time = null)
{
    private readonly object gate = new();
    private readonly Queue<DateTimeOffset> failures = new();
    private readonly TimeSpan window = window ?? TimeSpan.FromMinutes(10);
    private readonly TimeProvider time = time ?? TimeProvider.System;

    public bool IsBlocked()
    {
        lock (gate)
        {
            Trim();
            return failures.Count >= maxFailures;
        }
    }

    public void RecordFailure()
    {
        lock (gate)
        {
            Trim();
            failures.Enqueue(time.GetUtcNow());
        }
    }

    public void Reset()
    {
        lock (gate)
        {
            failures.Clear();
        }
    }

    private void Trim()
    {
        var cutoff = time.GetUtcNow() - window;
        while (failures.Count > 0 && failures.Peek() < cutoff)
        {
            failures.Dequeue();
        }
    }
}
