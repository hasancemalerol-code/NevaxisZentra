namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public interface IBulkDataOnboardingTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class BulkDataImportJob : IBulkDataOnboardingTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = BulkDataImportJobStatuses.Created;
    public string SourceProviderKind { get; set; } = BulkDataSourceProviderKinds.SyntheticStructured;
    public string SourceKind { get; set; } = BulkDataSourceKinds.File;
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = "1";
    public int ImportVersionNumber { get; set; } = 1;
    public string ImportVersionKind { get; set; } = BulkImportVersionKinds.FullSnapshot;
    public string SafetyMode { get; set; } = BulkOnboardingSafetyModes.Observe;
    public Guid? ConnectorId { get; set; }
    public Guid? MappingProfileId { get; set; }
    public string? CheckpointJson { get; set; }
    public string? WatermarkJson { get; set; }
    public string? ImpactPreviewJson { get; set; }
    public string? ReconciliationJson { get; set; }
    public string? ObservabilityJson { get; set; }
    public int TotalSourceRows { get; set; }
    public int StagedRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int DuplicateRows { get; set; }
    public int QuarantinedRows { get; set; }
    public int ImportedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int FailedRows { get; set; }
    public string? IdempotencyKey { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? FailureCode { get; set; }
    public string? SafeFailureMessage { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class FactoryDataMappingProfile : IBulkDataOnboardingTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = "1";
    public string Status { get; set; } = "draft";
    public string MappingJson { get; set; } = "[]";
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class BulkImportSourceCatalogEntry : IBulkDataOnboardingTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ImportJobId { get; set; }
    public string SourceDatabase { get; set; } = string.Empty;
    public string SourceSchema { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public string RoleHint { get; set; } = BulkSchemaRoleHints.Unknown;
    public string ColumnsJson { get; set; } = "[]";
    public long EstimatedRowCount { get; set; }
    public string? PrimaryKeyColumns { get; set; }
    public string? ForeignKeyJson { get; set; }
    public string? TimestampColumns { get; set; }
    public string? StatisticsJson { get; set; }
    public DateTimeOffset DiscoveredAtUtc { get; set; }
}

public sealed class BulkImportStagingRecord : IBulkDataOnboardingTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ImportJobId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceDatabase { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public string RecordChecksum { get; set; } = string.Empty;
    public string CanonicalEntityType { get; set; } = string.Empty;
    public string ValidationState { get; set; } = BulkStagingValidationStates.Pending;
    public string? ValidationReasonCode { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string? SourceTimestampRaw { get; set; }
    public DateTimeOffset? SourceTimestampUtc { get; set; }
    public DateTimeOffset IngestedAtUtc { get; set; }
    public string HistoricalTier { get; set; } = BulkHistoricalDataTiers.Hot;
    public int BatchNumber { get; set; }
}

public sealed class BulkImportQuarantineRecord : IBulkDataOnboardingTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ImportJobId { get; set; }
    public Guid? StagingRecordId { get; set; }
    public string SourceTable { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string SafeReason { get; set; } = string.Empty;
    public string Severity { get; set; } = BulkQuarantineSeverities.Warning;
    public string Status { get; set; } = BulkQuarantineStatuses.Open;
    public string? MappingContextJson { get; set; }
    public string? ResolutionJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolvedBySubjectId { get; set; }
}

public sealed class BulkImportLineageRecord : IBulkDataOnboardingTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ImportJobId { get; set; }
    public string CanonicalEntityType { get; set; } = string.Empty;
    public Guid CanonicalEntityId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceDatabase { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class BulkImportEntityMutation : IBulkDataOnboardingTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ImportJobId { get; set; }
    public string CanonicalEntityType { get; set; } = string.Empty;
    public Guid CanonicalEntityId { get; set; }
    public string MutationKind { get; set; } = "created";
    public DateTimeOffset RecordedAtUtc { get; set; }
    public bool RollbackEligible { get; set; } = true;
    public bool RolledBack { get; set; }
}
