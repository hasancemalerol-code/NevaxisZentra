namespace NevaxisFactOS.Persistence.BulkDataOnboarding;

public static class BulkDataImportJobStatuses
{
    public const string Created = "created";
    public const string Discovering = "discovering";
    public const string Profiling = "profiling";
    public const string MappingRequired = "mapping-required";
    public const string Validating = "validating";
    public const string ReadyForDryRun = "ready-for-dry-run";
    public const string DryRunning = "dry-running";
    public const string Quarantined = "quarantined";
    public const string ReadyToCommit = "ready-to-commit";
    public const string Importing = "importing";
    public const string Reconciling = "reconciling";
    public const string Verifying = "verifying";
    public const string Completed = "completed";
    public const string Partial = "partial";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}

public static class BulkDataSourceProviderKinds
{
    public const string SyntheticStructured = "synthetic-structured";
    public const string CsvFile = "csv-file";
    public const string XlsxFile = "xlsx-file";
    public const string DatabaseExport = "database-export";
    public const string ReadOnlyDatabase = "read-only-database";
}

public static class BulkDataSourceKinds
{
    public const string Database = "database";
    public const string File = "file";
    public const string Export = "export";
    public const string ReadOnlyConnection = "read-only-connection";
}

public static class BulkMappingRuleTypes
{
    public const string Direct = "direct";
    public const string Rename = "rename";
    public const string TypeConversion = "type-conversion";
    public const string EnumMap = "enum-map";
    public const string Lookup = "lookup";
    public const string Default = "default";
    public const string Derived = "derived";
    public const string UnitConversion = "unit-conversion";
    public const string Ignore = "ignore";
}

public static class BulkCanonicalEntityTypes
{
    public const string ManufacturingArea = "manufacturing-area";
    public const string ProductionLine = "production-line";
    public const string ProductionStation = "production-station";
    public const string ManufacturingResource = "manufacturing-resource";
    public const string Material = "material";
    public const string ProductionOrder = "production-order";
    public const string ProductionEvent = "production-event";
    public const string QualityRecord = "quality-record";
    public const string MaintenanceRecord = "maintenance-record";
    public const string MoldTool = "mold-tool";
    public const string Operator = "operator";
    public const string Shift = "shift";
}

public static class BulkStagingValidationStates
{
    public const string Pending = "pending";
    public const string Valid = "valid";
    public const string Invalid = "invalid";
    public const string Duplicate = "duplicate";
    public const string Quarantined = "quarantined";
}

public static class BulkQuarantineStatuses
{
    public const string Open = "open";
    public const string Reviewed = "reviewed";
    public const string Corrected = "corrected";
    public const string Ignored = "ignored";
    public const string Reprocessed = "reprocessed";
}

public static class BulkQuarantineSeverities
{
    public const string Critical = "critical";
    public const string Warning = "warning";
    public const string Info = "info";
}

public static class BulkDataQualityReasonCodes
{
    public const string MissingRequiredKey = "missing-required-key";
    public const string InvalidTimestamp = "invalid-timestamp";
    public const string InvalidNumber = "invalid-number";
    public const string InvalidEnum = "invalid-enum";
    public const string UnknownResource = "unknown-resource";
    public const string UnknownOrder = "unknown-order";
    public const string UnknownMaterial = "unknown-material";
    public const string BrokenRelation = "broken-relation";
    public const string Duplicate = "duplicate";
    public const string Conflict = "conflict";
    public const string FutureTimestamp = "future-timestamp";
    public const string StaleRecord = "stale-record";
    public const string InvalidUnit = "invalid-unit";
    public const string InvalidEncoding = "invalid-encoding";
    public const string TenantMismatch = "tenant-mismatch";
    public const string FacilityMismatch = "facility-mismatch";
    public const string InvalidMapping = "invalid-mapping";
    public const string AmbiguousEntity = "ambiguous-entity";
    public const string UnresolvedEntity = "unresolved-entity";
}

public static class BulkEntityResolutionOutcomes
{
    public const string Exact = "exact";
    public const string Mapped = "mapped";
    public const string Ambiguous = "ambiguous";
    public const string Unresolved = "unresolved";
}

public static class BulkHistoricalDataTiers
{
    public const string Hot = "hot";
    public const string Historical = "historical";
    public const string Archive = "archive";
}

public static class BulkOnboardingSafetyModes
{
    public const string Observe = "observe";
    public const string Draft = "draft";
}

public static class BulkImportVersionKinds
{
    public const string FullSnapshot = "full-snapshot";
    public const string CorrectedSnapshot = "corrected-snapshot";
    public const string Delta = "delta";
}

public static class BulkFailureCodes
{
    public const string NotFound = "bulk-onboarding.not-found";
    public const string ValidationFailed = "bulk-onboarding.validation-failed";
    public const string InvalidState = "bulk-onboarding.invalid-state";
    public const string TenantMismatch = "bulk-onboarding.tenant-mismatch";
    public const string FacilityMismatch = "bulk-onboarding.facility-mismatch";
    public const string RollbackBlocked = "bulk-onboarding.rollback-blocked";
    public const string FileRejected = "bulk-onboarding.file-rejected";
}

public static class BulkSchemaRoleHints
{
    public const string MachinesResources = "machines-resources";
    public const string ProductionOrders = "production-orders";
    public const string Products = "products";
    public const string EventsHistory = "events-history";
    public const string Materials = "materials";
    public const string Quality = "quality";
    public const string Maintenance = "maintenance";
    public const string MoldsTools = "molds-tools";
    public const string Operators = "operators";
    public const string Shifts = "shifts";
    public const string LinesStations = "lines-stations";
    public const string Unknown = "unknown";
}
