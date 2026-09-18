using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.BulkDataOnboarding;

internal static class BulkDataOnboardingEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IBulkDataOnboardingTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class BulkDataImportJobConfiguration : IEntityTypeConfiguration<BulkDataImportJob>
{
    public void Configure(EntityTypeBuilder<BulkDataImportJob> builder)
    {
        builder.ToTable("bulk_data_import_jobs");
        builder.HasKey(e => e.Id);
        BulkDataOnboardingEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.SourceProviderKind).HasColumnName("source_provider_kind").HasMaxLength(64).IsRequired();
        builder.Property(e => e.SourceKind).HasColumnName("source_kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.SourceSystem).HasColumnName("source_system").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.ImportVersionNumber).HasColumnName("import_version_number");
        builder.Property(e => e.ImportVersionKind).HasColumnName("import_version_kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.SafetyMode).HasColumnName("safety_mode").HasMaxLength(32).IsRequired();
        builder.Property(e => e.ConnectorId).HasColumnName("connector_id");
        builder.Property(e => e.MappingProfileId).HasColumnName("mapping_profile_id");
        builder.Property(e => e.CheckpointJson).HasColumnName("checkpoint_json");
        builder.Property(e => e.WatermarkJson).HasColumnName("watermark_json");
        builder.Property(e => e.ImpactPreviewJson).HasColumnName("impact_preview_json");
        builder.Property(e => e.ReconciliationJson).HasColumnName("reconciliation_json");
        builder.Property(e => e.ObservabilityJson).HasColumnName("observability_json");
        builder.Property(e => e.TotalSourceRows).HasColumnName("total_source_rows");
        builder.Property(e => e.StagedRows).HasColumnName("staged_rows");
        builder.Property(e => e.ValidRows).HasColumnName("valid_rows");
        builder.Property(e => e.InvalidRows).HasColumnName("invalid_rows");
        builder.Property(e => e.DuplicateRows).HasColumnName("duplicate_rows");
        builder.Property(e => e.QuarantinedRows).HasColumnName("quarantined_rows");
        builder.Property(e => e.ImportedRows).HasColumnName("imported_rows");
        builder.Property(e => e.UpdatedRows).HasColumnName("updated_rows");
        builder.Property(e => e.FailedRows).HasColumnName("failed_rows");
        builder.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(256);
        builder.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(128).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(e => e.FailureCode).HasColumnName("failure_code").HasMaxLength(64);
        builder.Property(e => e.SafeFailureMessage).HasColumnName("safe_failure_message").HasMaxLength(512);
        builder.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(e => new { e.TenantId, e.FacilityId, e.IdempotencyKey });
    }
}

public sealed class FactoryDataMappingProfileConfiguration : IEntityTypeConfiguration<FactoryDataMappingProfile>
{
    public void Configure(EntityTypeBuilder<FactoryDataMappingProfile> builder)
    {
        builder.ToTable("factory_data_mapping_profiles");
        builder.HasKey(e => e.Id);
        BulkDataOnboardingEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.SourceSystem).HasColumnName("source_system").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.MappingJson).HasColumnName("mapping_json").IsRequired();
        builder.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(128).IsRequired();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(e => new { e.TenantId, e.FacilityId, e.SourceSystem, e.SourceVersion });
    }
}

public sealed class BulkImportSourceCatalogEntryConfiguration : IEntityTypeConfiguration<BulkImportSourceCatalogEntry>
{
    public void Configure(EntityTypeBuilder<BulkImportSourceCatalogEntry> builder)
    {
        builder.ToTable("bulk_import_source_catalog_entries");
        builder.HasKey(e => e.Id);
        BulkDataOnboardingEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(e => e.ImportJobId).HasColumnName("import_job_id");
        builder.Property(e => e.SourceDatabase).HasColumnName("source_database").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceSchema).HasColumnName("source_schema").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceTable).HasColumnName("source_table").HasMaxLength(128).IsRequired();
        builder.Property(e => e.RoleHint).HasColumnName("role_hint").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ColumnsJson).HasColumnName("columns_json").IsRequired();
        builder.Property(e => e.EstimatedRowCount).HasColumnName("estimated_row_count");
        builder.Property(e => e.PrimaryKeyColumns).HasColumnName("primary_key_columns").HasMaxLength(256);
        builder.Property(e => e.ForeignKeyJson).HasColumnName("foreign_key_json");
        builder.Property(e => e.TimestampColumns).HasColumnName("timestamp_columns").HasMaxLength(256);
        builder.Property(e => e.StatisticsJson).HasColumnName("statistics_json");
        builder.Property(e => e.DiscoveredAtUtc).HasColumnName("discovered_at_utc");
        builder.HasIndex(e => new { e.TenantId, e.ImportJobId, e.SourceTable });
    }
}

public sealed class BulkImportStagingRecordConfiguration : IEntityTypeConfiguration<BulkImportStagingRecord>
{
    public void Configure(EntityTypeBuilder<BulkImportStagingRecord> builder)
    {
        builder.ToTable("bulk_import_staging_records");
        builder.HasKey(e => e.Id);
        BulkDataOnboardingEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(e => e.ImportJobId).HasColumnName("import_job_id");
        builder.Property(e => e.SourceSystem).HasColumnName("source_system").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceDatabase).HasColumnName("source_database").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceTable).HasColumnName("source_table").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceKey).HasColumnName("source_key").HasMaxLength(256).IsRequired();
        builder.Property(e => e.RecordChecksum).HasColumnName("record_checksum").HasMaxLength(128).IsRequired();
        builder.Property(e => e.CanonicalEntityType).HasColumnName("canonical_entity_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ValidationState).HasColumnName("validation_state").HasMaxLength(32).IsRequired();
        builder.Property(e => e.ValidationReasonCode).HasColumnName("validation_reason_code").HasMaxLength(64);
        builder.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        builder.Property(e => e.SourceTimestampRaw).HasColumnName("source_timestamp_raw").HasMaxLength(128);
        builder.Property(e => e.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        builder.Property(e => e.IngestedAtUtc).HasColumnName("ingested_at_utc");
        builder.Property(e => e.HistoricalTier).HasColumnName("historical_tier").HasMaxLength(32).IsRequired();
        builder.Property(e => e.BatchNumber).HasColumnName("batch_number");
        builder.HasIndex(e => new { e.TenantId, e.ImportJobId, e.SourceTable, e.SourceKey, e.RecordChecksum });
    }
}

public sealed class BulkImportQuarantineRecordConfiguration : IEntityTypeConfiguration<BulkImportQuarantineRecord>
{
    public void Configure(EntityTypeBuilder<BulkImportQuarantineRecord> builder)
    {
        builder.ToTable("bulk_import_quarantine_records");
        builder.HasKey(e => e.Id);
        BulkDataOnboardingEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(e => e.ImportJobId).HasColumnName("import_job_id");
        builder.Property(e => e.StagingRecordId).HasColumnName("staging_record_id");
        builder.Property(e => e.SourceTable).HasColumnName("source_table").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceKey).HasColumnName("source_key").HasMaxLength(256).IsRequired();
        builder.Property(e => e.ReasonCode).HasColumnName("reason_code").HasMaxLength(64).IsRequired();
        builder.Property(e => e.SafeReason).HasColumnName("safe_reason").HasMaxLength(512).IsRequired();
        builder.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(16).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.MappingContextJson).HasColumnName("mapping_context_json");
        builder.Property(e => e.ResolutionJson).HasColumnName("resolution_json");
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        builder.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        builder.Property(e => e.ResolvedBySubjectId).HasColumnName("resolved_by_subject_id").HasMaxLength(128);
        builder.HasIndex(e => new { e.TenantId, e.ImportJobId, e.Status });
    }
}

public sealed class BulkImportLineageRecordConfiguration : IEntityTypeConfiguration<BulkImportLineageRecord>
{
    public void Configure(EntityTypeBuilder<BulkImportLineageRecord> builder)
    {
        builder.ToTable("bulk_import_lineage_records");
        builder.HasKey(e => e.Id);
        BulkDataOnboardingEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(e => e.ImportJobId).HasColumnName("import_job_id");
        builder.Property(e => e.CanonicalEntityType).HasColumnName("canonical_entity_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.CanonicalEntityId).HasColumnName("canonical_entity_id");
        builder.Property(e => e.SourceSystem).HasColumnName("source_system").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceDatabase).HasColumnName("source_database").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceTable).HasColumnName("source_table").HasMaxLength(128).IsRequired();
        builder.Property(e => e.SourceKey).HasColumnName("source_key").HasMaxLength(256).IsRequired();
        builder.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
        builder.HasIndex(e => new { e.TenantId, e.CanonicalEntityType, e.CanonicalEntityId });
    }
}

public sealed class BulkImportEntityMutationConfiguration : IEntityTypeConfiguration<BulkImportEntityMutation>
{
    public void Configure(EntityTypeBuilder<BulkImportEntityMutation> builder)
    {
        builder.ToTable("bulk_import_entity_mutations");
        builder.HasKey(e => e.Id);
        BulkDataOnboardingEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(e => e.ImportJobId).HasColumnName("import_job_id");
        builder.Property(e => e.CanonicalEntityType).HasColumnName("canonical_entity_type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.CanonicalEntityId).HasColumnName("canonical_entity_id");
        builder.Property(e => e.MutationKind).HasColumnName("mutation_kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
        builder.Property(e => e.RollbackEligible).HasColumnName("rollback_eligible");
        builder.Property(e => e.RolledBack).HasColumnName("rolled_back");
        builder.HasIndex(e => new { e.TenantId, e.ImportJobId, e.CanonicalEntityId });
    }
}
