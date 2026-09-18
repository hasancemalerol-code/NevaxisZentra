namespace NevaxisZentra.Persistence.EnterpriseReliability;

public sealed class EnterpriseReliabilityPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string OperatingMode { get; set; } = SystemOperatingModes.FullyOperational;
    public string PolicyEnvelopeJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint ConcurrencyToken { get; set; } = 1;
}

public sealed class SystemComponentHealthSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string ComponentKind { get; set; } = SystemComponentKinds.Api;
    public string ComponentKey { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = SystemHealthStatuses.Unknown;
    public string? DetailJson { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset? LastHealthyAtUtc { get; set; }
    public DateTimeOffset? LastFailureAtUtc { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class IntegrationCircuitBreakerState
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectorId { get; set; }
    public string State { get; set; } = CircuitBreakerStates.Closed;
    public int ConsecutiveFailures { get; set; }
    public int FailureThreshold { get; set; } = 5;
    public DateTimeOffset? OpenedAtUtc { get; set; }
    public DateTimeOffset? CooldownUntilUtc { get; set; }
    public DateTimeOffset? LastProbeAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class RecoveryReconciliationRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string Scope { get; set; } = "integration";
    public string Status { get; set; } = ReconciliationRunStatuses.Running;
    public string SummaryJson { get; set; } = "{}";
    public string? ConflictJson { get; set; }
    public string TriggeredBySubjectId { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class BackgroundJobLease
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string JobKey { get; set; } = string.Empty;
    public string OwnerInstanceId { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string Status { get; set; } = "active";
}

public sealed class TechnicalOperationalAlert
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string AlertKind { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string Status { get; set; } = TechnicalAlertStatuses.Open;
    public string Title { get; set; } = string.Empty;
    public string DetailJson { get; set; } = "{}";
    public string DedupKey { get; set; } = string.Empty;
    public DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class EdgeAgentRegistration
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AgentKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public string ConnectivityStatus { get; set; } = SystemHealthStatuses.Unknown;
    public int BufferBacklogCount { get; set; }
    public int BufferCapacity { get; set; }
    public DateTimeOffset? LastHeartbeatAtUtc { get; set; }
    public DateTimeOffset? LastSyncAtUtc { get; set; }
    public string MachineScopeJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class BackupVerificationRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string BackupArtifactReference { get; set; } = string.Empty;
    public string RestoreTargetReference { get; set; } = string.Empty;
    public string Status { get; set; } = "running";
    public string ResultJson { get; set; } = "{}";
    public string TriggeredBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class IntegrationQuarantineReplayAudit
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid QuarantineId { get; set; }
    public string ReplayIdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "completed";
    public string TriggeredBySubjectId { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset ReplayedAtUtc { get; set; }
}
