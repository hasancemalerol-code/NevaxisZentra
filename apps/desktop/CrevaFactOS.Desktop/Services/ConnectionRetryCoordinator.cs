namespace CrevaFactOS.Desktop.Services;

public sealed class ConnectionRetryCoordinator
{
    private int _retryCount;

    public int RetryCount => _retryCount;

    public void RecordRetryAttempt() => _retryCount++;

    public void Reset() => _retryCount = 0;
}
