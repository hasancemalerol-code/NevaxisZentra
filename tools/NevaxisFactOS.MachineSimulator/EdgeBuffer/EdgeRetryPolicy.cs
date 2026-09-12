namespace NevaxisFactOS.MachineSimulator.EdgeBuffer;

public sealed class EdgeRetryPolicy
{
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaximumDelay { get; init; } = TimeSpan.FromMinutes(5);
    public double BackoffMultiplier { get; init; } = 2.0;
    public int MaximumAttempts { get; init; } = EdgeBufferConstants.DefaultMaxAttempts;
    public bool UseDeterministicJitter { get; init; }

    public DateTimeOffset ComputeNextAttemptUtc(int attemptCount, DateTimeOffset nowUtc, string idempotencyKey)
    {
        var exponent = Math.Max(0, attemptCount - 1);
        var delayMs = InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, exponent);
        delayMs = Math.Min(delayMs, MaximumDelay.TotalMilliseconds);

        if (UseDeterministicJitter && !string.IsNullOrEmpty(idempotencyKey))
        {
            var jitterSeed = Math.Abs(idempotencyKey.GetHashCode(StringComparison.Ordinal)) % 500;
            delayMs += jitterSeed;
        }

        return nowUtc.AddMilliseconds(delayMs);
    }

    public bool ShouldDeadLetter(int attemptCount) => attemptCount >= MaximumAttempts;
}

public static class EdgeDeliveryClassifier
{
    public static bool IsRetryableHttpStatus(int statusCode) =>
        statusCode == 408 || statusCode == 429 || statusCode >= 500;

    public static bool IsNonRetryableHttpStatus(int statusCode) =>
        statusCode is 400 or 401 or 403 or 404 or 409 or 422;

    public static string ClassifyHttpStatus(int statusCode) =>
        IsRetryableHttpStatus(statusCode)
            ? EdgeBufferErrorCodes.RetryableHttp
            : EdgeBufferErrorCodes.NonRetryableHttp;
}
