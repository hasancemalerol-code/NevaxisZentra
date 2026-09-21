namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public sealed record BulkDataOperationContext(
    Guid TenantId,
    Guid FacilityId,
    string SubjectId);

public sealed record BulkColumnDescriptor(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    string? ForeignKeyReference);

public sealed record BulkSourceTableDescriptor(
    string SourceDatabase,
    string SourceSchema,
    string SourceTable,
    IReadOnlyList<BulkColumnDescriptor> Columns,
    long EstimatedRowCount,
    IReadOnlyList<string> PrimaryKeyColumns,
    IReadOnlyList<string> TimestampColumns);

public sealed record BulkSourceDiscoveryResult(
    string SourceSystem,
    string SourceVersion,
    IReadOnlyList<BulkSourceTableDescriptor> Tables);

public sealed record BulkMappingRule(
    string SourceTable,
    string SourceColumn,
    string CanonicalEntityType,
    string CanonicalField,
    string MappingType,
    string? ConversionJson,
    bool Required);

public sealed record BulkMappingProfileDefinition(
    string SourceSystem,
    string SourceVersion,
    IReadOnlyList<BulkMappingRule> Rules);

public sealed record BulkMappedRecord(
    string SourceTable,
    string SourceKey,
    string CanonicalEntityType,
    IReadOnlyDictionary<string, string?> Fields,
    string? SourceTimestampRaw,
    DateTimeOffset? SourceTimestampUtc,
    DateTimeOffset IngestedAtUtc);

public sealed record BulkDataQualityIssue(
    string ReasonCode,
    string SafeMessage,
    string Severity);

public sealed record BulkDataQualityResult(
    bool Accepted,
    IReadOnlyList<BulkDataQualityIssue> Issues);

public sealed record BulkEntityResolutionResult(
    string Outcome,
    string? CanonicalCode,
    Guid? CanonicalEntityId,
    IReadOnlyList<string> Candidates);

public sealed record BulkImpactPreview(
    int AreasToCreate,
    int LinesToCreate,
    int StationsToCreate,
    int ResourcesToCreate,
    int MaterialsToCreate,
    int OrdersToCreate,
    int EventsToCreate,
    int QualityRecordsToCreate,
    int MaintenanceRecordsToCreate,
    int MoldsToCreate,
    int ExistingMatches,
    int Updates,
    int Conflicts,
    int QuarantinedRows,
    int UnresolvedMappings);

public sealed record BulkDryRunResult(
    bool Success,
    int SourceRows,
    int ValidRows,
    int InvalidRows,
    int DuplicateRows,
    int QuarantinedRows,
    BulkImpactPreview Impact,
    string? ErrorCode,
    string? SafeMessage);

public sealed record BulkReconciliationSummary(
    long SourceRows,
    long StagedRows,
    long ValidRows,
    long ImportedRows,
    long UpdatedRows,
    long DuplicateRows,
    long QuarantinedRows,
    long FailedRows,
    bool Balanced);

public sealed record BulkImportCheckpoint(
    string CurrentTable,
    int CurrentBatch,
    int ProcessedRows,
    DateTimeOffset SavedAtUtc);

public sealed record BulkImportJobSummary(
    Guid Id,
    string Name,
    string Status,
    string SourceSystem,
    string SourceProviderKind,
    int TotalSourceRows,
    int ValidRows,
    int QuarantinedRows,
    int ImportedRows,
    string SafetyMode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record BulkImportJobWorkspace(
    BulkImportJobSummary Job,
    IReadOnlyList<BulkSourceTableDescriptor> Catalog,
    BulkImpactPreview? ImpactPreview,
    BulkReconciliationSummary? Reconciliation,
    BulkImportCheckpoint? Checkpoint,
    IReadOnlyList<BulkQuarantineSummary> Quarantine,
    IReadOnlyList<BulkMappingRule> MappingRules,
    BulkImportFileManifest UploadedFiles);

public sealed record CreateBulkImportJobRequest(
    string Name,
    string SourceProviderKind,
    string SourceSystem,
    string? IdempotencyKey,
    Guid? MappingProfileId);

public sealed record BulkSyntheticImportPerformance(
    int TableCount,
    int TotalRows,
    long DryRunDurationMs,
    long ImportDurationMs,
    double RowsPerSecond,
    int QuarantineCount,
    bool ReconciliationBalanced);
