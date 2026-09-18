namespace NevaxisZentra.Persistence.EnterpriseReliability;

public static class SystemHealthStatuses
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Unavailable = "unavailable";
    public const string Recovering = "recovering";
    public const string Unknown = "unknown";
}

public static class SystemOperatingModes
{
    public const string FullyOperational = "fully-operational";
    public const string DegradedReadOnly = "degraded-read-only";
    public const string DegradedBuffering = "degraded-buffering";
    public const string Recovering = "recovering";
}

public static class SystemComponentKinds
{
    public const string Api = "api";
    public const string Database = "database";
    public const string MessageBroker = "message-broker";
    public const string AiProvider = "ai-provider";
    public const string IntegrationConnector = "integration-connector";
    public const string EdgeAgent = "edge-agent";
    public const string MachineTelemetry = "machine-telemetry";
    public const string ErpAdapter = "erp-adapter";
    public const string FileWatch = "file-watch";
    public const string WebhookIngestion = "webhook-ingestion";
    public const string BackgroundJobs = "background-jobs";
}

public static class EdgeEventClassifications
{
    public const string Critical = "critical";
    public const string Important = "important";
    public const string Telemetry = "telemetry";
    public const string Disposable = "disposable";
}

public static class CircuitBreakerStates
{
    public const string Closed = "closed";
    public const string Open = "open";
    public const string HalfOpen = "half-open";
}

public static class IntegrationRetryCategories
{
    public const string Transient = "transient";
    public const string Permanent = "permanent";
    public const string Authorization = "authorization";
    public const string Validation = "validation";
    public const string RateLimit = "rate-limit";
    public const string Timeout = "timeout";
}

public static class EnterpriseErrorCategories
{
    public const string Validation = "validation";
    public const string Authorization = "authorization";
    public const string Dependency = "dependency";
    public const string Transient = "transient";
    public const string Conflict = "conflict";
    public const string StaleData = "stale-data";
    public const string Policy = "policy";
    public const string Safety = "safety";
    public const string Internal = "internal";
}

public static class ReconciliationRunStatuses
{
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Conflict = "conflict";
    public const string Failed = "failed";
}

public static class TechnicalAlertStatuses
{
    public const string Open = "open";
    public const string Acknowledged = "acknowledged";
    public const string Resolved = "resolved";
    public const string Suppressed = "suppressed";
}

public static class EnterpriseReliabilityFailureCodes
{
    public const string StaleData = "enterprise-reliability.stale-data";
    public const string DegradedMode = "enterprise-reliability.degraded-mode";
    public const string CircuitOpen = "enterprise-reliability.circuit-open";
    public const string Conflict = "enterprise-reliability.reconciliation-conflict";
    public const string Unauthorized = "enterprise-reliability.unauthorized";
    public const string BufferCapacity = "enterprise-reliability.buffer-capacity";
    public const string CriticalEventDropBlocked = "enterprise-reliability.critical-drop-blocked";
    public const string Validation = "enterprise-reliability.validation";
}

public sealed class EnterpriseReliabilityException : Exception
{
    public string FailureCode { get; }

    public EnterpriseReliabilityException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
