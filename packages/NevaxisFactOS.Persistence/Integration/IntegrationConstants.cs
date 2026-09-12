namespace NevaxisFactOS.Persistence.Integration;

public static class IntegrationConnectorKinds
{
    public const string FileImport = "file-import";
    public const string FolderWatch = "folder-watch";
    public const string RestApi = "rest-api";
    public const string Webhook = "webhook";
    public const string ReadOnlyDatabase = "read-only-database";
    public const string ErpAdapter = "erp-adapter";
    public const string LocalConnector = "local-connector";
    public const string IotPlcBridge = "iot-plc-bridge";
    public const string EdgeBridge = "edge-bridge";
}

public static class IntegrationDirections
{
    public const string IngestOnly = "ingest-only";
}

public static class IntegrationEnvironments
{
    public const string Development = "development";
    public const string Simulated = "simulated";
    public const string Production = "production";
}

public static class IntegrationConnectorStatuses
{
    public const string Draft = "draft";
    public const string Configured = "configured";
    public const string Enabled = "enabled";
    public const string Disabled = "disabled";
}

public static class IntegrationHealthStatuses
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Offline = "offline";
    public const string Misconfigured = "misconfigured";
    public const string AuthenticationFailed = "authentication-failed";
    public const string RateLimited = "rate-limited";
    public const string Stale = "stale";
    public const string SyncFailed = "sync-failed";
    public const string Unknown = "unknown";
    public const string Disabled = "disabled";
}

public static class IntegrationFreshnessStates
{
    public const string Fresh = "fresh";
    public const string Aging = "aging";
    public const string Stale = "stale";
    public const string Unknown = "unknown";
    public const string Unavailable = "unavailable";
}

public static class IntegrationQualityStates
{
    public const string Good = "good";
    public const string Partial = "partial";
    public const string Invalid = "invalid";
    public const string Conflicting = "conflicting";
    public const string Stale = "stale";
    public const string Missing = "missing";
    public const string Unknown = "unknown";
}

public static class IntegrationEvidenceClasses
{
    public const string Measured = "measured";
    public const string Imported = "imported";
    public const string Derived = "derived";
    public const string Simulated = "simulated";
    public const string Manual = "manual";
    public const string Unknown = "unknown";
    public const string SourceConflict = "source-conflict";
}

public static class IntegrationMappingVersionStatuses
{
    public const string Draft = "draft";
    public const string Validated = "validated";
    public const string Active = "active";
    public const string Superseded = "superseded";
    public const string Retired = "retired";
}

public static class IntegrationCanonicalProjectionTypes
{
    public const string Material = "material";
}

public static class IntegrationFileKinds
{
    public const string Csv = "csv";
    public const string Xlsx = "xlsx";
    public const string Json = "json";
}

public static class IntegrationQuarantineReasonCodes
{
    public const string InvalidType = "invalid-type";
    public const string MissingRequiredField = "missing-required-field";
    public const string MappingFailure = "mapping-failure";
    public const string InvalidEnum = "invalid-enum";
    public const string MalformedDate = "malformed-date";
    public const string MalformedDecimal = "malformed-decimal";
    public const string DuplicateConflict = "duplicate-conflict";
    public const string UnsupportedData = "unsupported-data";
    public const string SchemaMismatch = "schema-mismatch";
    public const string IdempotencyConflict = "idempotency-conflict";
}

public static class IntegrationImportModes
{
    public const string DryRun = "dry-run";
    public const string Import = "import";
}

public static class IntegrationSyncRunStatuses
{
    public const string Running = "running";
    public const string Completed = "completed";
    public const string CompletedWithErrors = "completed-with-errors";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}

public static class IntegrationQuarantineStatuses
{
    public const string Open = "open";
    public const string Resolved = "resolved";
    public const string Ignored = "ignored";
}

public static class IntegrationDomainBindingKinds
{
    public const string Standalone = "standalone";
    public const string ErpConnectionProfile = "erp-connection-profile";
    public const string IotPlcEndpoint = "iot-plc-endpoint";
    public const string EdgeGateway = "edge-gateway";
}

public static class IntegrationAuthenticationModes
{
    public const string None = "none";
    public const string ApiKeyReference = "api-key-reference";
    public const string BearerSecretReference = "bearer-secret-reference";
    public const string BasicSecretReference = "basic-secret-reference";
    public const string DatabaseSecretReference = "database-secret-reference";
    public const string WebhookSignature = "webhook-signature";
    public const string LocalConnectorIdentity = "local-connector-identity";
}

public static class IntegrationSynchronizationModes
{
    public const string Manual = "manual";
    public const string Scheduled = "scheduled";
    public const string EventDriven = "event-driven";
    public const string Continuous = "continuous";
}

public static class IntegrationRetryStates
{
    public const string Idle = "idle";
    public const string Scheduled = "scheduled";
    public const string InProgress = "in-progress";
    public const string Exhausted = "exhausted";
}

public static class IntegrationFailureCategories
{
    public const string Transient = "transient";
    public const string RateLimit = "rate-limit";
    public const string Authentication = "authentication";
    public const string Configuration = "configuration";
    public const string Validation = "validation";
    public const string SourceUnavailable = "source-unavailable";
    public const string Network = "network";
    public const string Timeout = "timeout";
    public const string Permanent = "permanent";
}
