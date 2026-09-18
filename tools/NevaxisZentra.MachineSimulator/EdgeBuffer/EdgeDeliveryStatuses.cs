namespace NevaxisZentra.MachineSimulator.EdgeBuffer;

public static class EdgeDeliveryStatuses
{
    public const string Pending = "pending";
    public const string InFlight = "in-flight";
    public const string Delivered = "delivered";
    public const string RetryWait = "retry-wait";
    public const string DeadLetter = "dead-letter";
}

public static class EdgeBufferErrorCodes
{
    public const string CapacityExceeded = "edge.buffer.capacity-exceeded";
    public const string NetworkUnavailable = "edge.delivery.network-unavailable";
    public const string Timeout = "edge.delivery.timeout";
    public const string RetryableHttp = "edge.delivery.retryable-http";
    public const string NonRetryableHttp = "edge.delivery.non-retryable";
    public const string UnsupportedPayloadVersion = "edge.delivery.unsupported-payload-version";
    public const string MalformedEvent = "edge.delivery.malformed-event";
    public const string AuthorizationDenied = "edge.delivery.authorization-denied";
    public const string StaleInFlightRecovered = "edge.delivery.stale-in-flight-recovered";
}

public static class EdgeBufferConstants
{
    public const int SchemaVersion = 1;
    public const int SupportedPayloadVersion = 1;
    public const int DefaultMaxBufferedEvents = 10_000;
    public const int DefaultBatchSize = 25;
    public const int DefaultMaxAttempts = 8;
    public static readonly TimeSpan InFlightLeaseTimeout = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan WorkerPollInterval = TimeSpan.FromSeconds(2);
}
