namespace NevaxisFactOS.Persistence.Integration;

public sealed class IntegrationConnector
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConnectorKind { get; set; } = IntegrationConnectorKinds.FileImport;
    public string SourceSystemType { get; set; } = string.Empty;
    public string SourceSystemName { get; set; } = string.Empty;
    public string Environment { get; set; } = IntegrationEnvironments.Development;
    public string Direction { get; set; } = IntegrationDirections.IngestOnly;
    public string Status { get; set; } = IntegrationConnectorStatuses.Draft;
    public bool Enabled { get; set; }
    public string AuthenticationMode { get; set; } = IntegrationAuthenticationModes.None;
    public string? SecretReference { get; set; }
    public string? EndpointOrLocation { get; set; }
    public string? IngressKey { get; set; }
    public Guid? ActiveMappingVersionId { get; set; }
    public string SynchronizationMode { get; set; } = IntegrationSynchronizationModes.Manual;
    public string? ScheduleExpression { get; set; }
    public string? CheckpointJson { get; set; }
    public string HealthStatus { get; set; } = IntegrationHealthStatuses.Unknown;
    public string FreshnessState { get; set; } = IntegrationFreshnessStates.Unknown;
    public string QualityState { get; set; } = IntegrationQualityStates.Unknown;
    public DateTimeOffset? LastSuccessfulSyncAtUtc { get; set; }
    public DateTimeOffset? LastAttemptedSyncAtUtc { get; set; }
    public DateTimeOffset? LastSuccessAtUtc { get; set; }
    public DateTimeOffset? LastFailureAtUtc { get; set; }
    public string? FailureStatus { get; set; }
    public string RetryState { get; set; } = IntegrationRetryStates.Idle;
    public string? RateLimitState { get; set; }
    public string DomainBindingKind { get; set; } = IntegrationDomainBindingKinds.Standalone;
    public Guid? DomainBindingId { get; set; }
    public string OwnerSubjectId { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = "{}";
    public string ObservabilityJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public IntegrationMappingVersion? ActiveMappingVersion { get; set; }
    public ICollection<IntegrationMappingVersion> MappingVersions { get; set; } = new List<IntegrationMappingVersion>();
    public ICollection<IntegrationSyncRun> SyncRuns { get; set; } = new List<IntegrationSyncRun>();
}

public sealed class IntegrationMappingVersion
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectorId { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = IntegrationMappingVersionStatuses.Draft;
    public string TargetCanonicalEntityType { get; set; } = IntegrationCanonicalProjectionTypes.Material;
    public string SourceSchemaFingerprint { get; set; } = string.Empty;
    public string MappingJson { get; set; } = "{}";
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ValidatedAtUtc { get; set; }
    public string? ActivatedBySubjectId { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public Guid? SupersededByMappingVersionId { get; set; }
    public uint Version { get; set; } = 1;

    public IntegrationConnector? Connector { get; set; }
}

public sealed class IntegrationSyncRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectorId { get; set; }
    public Guid? MappingVersionId { get; set; }
    public string SourceType { get; set; } = IntegrationConnectorKinds.FileImport;
    public string? SourceFingerprint { get; set; }
    public string? ImportIdempotencyKey { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Status { get; set; } = IntegrationSyncRunStatuses.Running;
    public string? CheckpointBeforeJson { get; set; }
    public string? CheckpointAfterJson { get; set; }
    public int RecordsRead { get; set; }
    public int RecordsAccepted { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsDuplicate { get; set; }
    public int RecordsRejected { get; set; }
    public int RecordsQuarantined { get; set; }
    public string? FailureCategory { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string TriggeredBySubjectId { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public Guid? RetriedFromSyncRunId { get; set; }

    public IntegrationConnector? Connector { get; set; }
}

public sealed class IntegrationIngestionQuarantine
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectorId { get; set; }
    public Guid? SyncRunId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public string? SourceRecordId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string SafeReason { get; set; } = string.Empty;
    public string? ValidationIssue { get; set; }
    public string Status { get; set; } = IntegrationQuarantineStatuses.Open;
    public string EvidenceClass { get; set; } = IntegrationEvidenceClasses.Unknown;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolvedBySubjectId { get; set; }

    public IntegrationConnector? Connector { get; set; }
    public IntegrationSyncRun? SyncRun { get; set; }
}

public sealed class IntegrationSourceIdentity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectorId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public string SourceRecordId { get; set; } = string.Empty;
    public string? SourceVersion { get; set; }
    public string? ContentFingerprint { get; set; }
    public string? InternalProjectionType { get; set; }
    public Guid? InternalProjectionId { get; set; }
    public string EvidenceClass { get; set; } = IntegrationEvidenceClasses.Imported;
    public DateTimeOffset FirstSeenAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }

    public IntegrationConnector? Connector { get; set; }
}
