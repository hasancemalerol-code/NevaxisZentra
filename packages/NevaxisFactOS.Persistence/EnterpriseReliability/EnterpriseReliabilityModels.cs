namespace NevaxisFactOS.Persistence.EnterpriseReliability;

public sealed record SystemStatusSnapshot(
    string OperatingMode,
    string ApplicationVersion,
    string BuildSha,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<ComponentHealthView> Components,
    IReadOnlyList<IntegrationHealthView> Integrations,
    IReadOnlyList<EdgeAgentHealthView> EdgeAgents,
    IReadOnlyList<TechnicalAlertView> Alerts,
    DependencyHealthView Dependencies,
    bool ShowsFalseHealth);

public sealed record ComponentHealthView(
    string ComponentKind,
    string ComponentKey,
    string HealthStatus,
    string? Detail,
    DateTimeOffset? ObservedAtUtc,
    DateTimeOffset? LastHealthyAtUtc,
    DateTimeOffset? LastFailureAtUtc);

public sealed record IntegrationHealthView(
    Guid ConnectorId,
    string Name,
    string ConnectorKind,
    string HealthStatus,
    string FreshnessState,
    string RetryState,
    string CircuitState,
    int ConsecutiveFailures,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset? LastFailureAtUtc,
    string? FailureStatus,
    string UiStatusLabel);

public sealed record EdgeAgentHealthView(
    Guid Id,
    string AgentKey,
    string DisplayName,
    Guid FacilityId,
    string ConnectivityStatus,
    int BufferBacklogCount,
    int BufferCapacity,
    string AgentVersion,
    DateTimeOffset? LastHeartbeatAtUtc,
    DateTimeOffset? LastSyncAtUtc);

public sealed record TechnicalAlertView(
    Guid Id,
    string AlertKind,
    string Severity,
    string Title,
    string Status,
    DateTimeOffset OpenedAtUtc);

public sealed record DependencyHealthView(
    string Database,
    string MessageBroker,
    string AiProvider,
    string BackgroundJobs);

public sealed record RecordConnectorOutcomeRequest(
    Guid ConnectorId,
    Guid FacilityId,
    bool Success,
    string? FailureCategory);

public sealed record EdgeAgentHeartbeatRequest(
    string AgentKey,
    Guid FacilityId,
    string DisplayName,
    string AgentVersion,
    string ConnectivityStatus,
    int BufferBacklogCount,
    int BufferCapacity,
    string MachineScopeJson);

public sealed record ReconcileRecoveryRequest(
    Guid? FacilityId,
    string Scope,
    int SourceCount,
    int AppliedCount,
    int BufferedCount,
    int UnresolvedCount,
    IReadOnlyList<string>? Conflicts);

public sealed record ReplayQuarantineRequest(
    Guid QuarantineId,
    Guid FacilityId,
    string ReplayIdempotencyKey);

public sealed record BackupVerificationRequest(
    string BackupArtifactReference,
    string RestoreTargetReference);

public sealed record SafeDiagnosticBundle(
    string ApplicationVersion,
    string BuildSha,
    DateTimeOffset GeneratedAtUtc,
    SystemStatusSnapshot Status,
    IReadOnlyDictionary<string, string> SafeConfigurationMetadata);

public sealed record OperabilityMetricsSnapshot(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyDictionary<string, double> Metrics);

public sealed record CustomerErrorView(
    string Category,
    string Title,
    string Message,
    string? AffectedScope,
    bool WillRetry,
    string? UserAction);
