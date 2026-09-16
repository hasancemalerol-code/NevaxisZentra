namespace NevaxisFactOS.Persistence.Integration.FactoryEventSpine;

public static class FactoryEventSpineSchemaVersions
{
    public const string V1 = "factory-event-v1";
}

public static class FactoryInboundEventStatuses
{
    public const string Received = "received";
    public const string Validating = "validating";
    public const string Normalized = "normalized";
    public const string Quarantined = "quarantined";
    public const string Failed = "failed";
    public const string Processed = "processed";
}

public static class FactoryCanonicalEventStates
{
    public const string Persisted = "persisted";
    public const string DispatchPending = "dispatch-pending";
    public const string Processing = "processing";
    public const string Processed = "processed";
    public const string Failed = "failed";
    public const string DeadLetter = "dead-letter";
}

public static class FactoryEventErrorClassifications
{
    public const string Transient = "transient";
    public const string Permanent = "permanent";
    public const string Configuration = "configuration";
    public const string Data = "data";
}

public static class FactoryEventSpinePermissionKeys
{
    public const string Reprocess = "integration.reprocess";
    public const string EventReplay = "integration.event.replay";
}

public static class FactoryIntegrationHealthStates
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Offline = "offline";
    public const string Misconfigured = "misconfigured";
    public const string Paused = "paused";
}

public static class FactoryEventSpineConsumerNames
{
    public const string FactoryEventRouter = "factory-event-router";
}
