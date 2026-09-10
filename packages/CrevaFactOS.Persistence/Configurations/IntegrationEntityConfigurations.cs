using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Integration;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class IntegrationConnectorEntityConfiguration : IEntityTypeConfiguration<IntegrationConnector>
{
    public void Configure(EntityTypeBuilder<IntegrationConnector> entity)
    {
        entity.ToTable("integration_connectors");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        entity.Property(e => e.ConnectorKind).HasColumnName("connector_kind").HasMaxLength(50);
        entity.Property(e => e.SourceSystemType).HasColumnName("source_system_type").HasMaxLength(100);
        entity.Property(e => e.SourceSystemName).HasColumnName("source_system_name").HasMaxLength(200);
        entity.Property(e => e.Environment).HasColumnName("environment").HasMaxLength(30);
        entity.Property(e => e.Direction).HasColumnName("direction").HasMaxLength(30);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.Enabled).HasColumnName("enabled");
        entity.Property(e => e.AuthenticationMode).HasColumnName("authentication_mode").HasMaxLength(50);
        entity.Property(e => e.SecretReference).HasColumnName("secret_reference").HasMaxLength(200);
        entity.Property(e => e.EndpointOrLocation).HasColumnName("endpoint_or_location").HasMaxLength(500);
        entity.Property(e => e.IngressKey).HasColumnName("ingress_key").HasMaxLength(100);
        entity.Property(e => e.ActiveMappingVersionId).HasColumnName("active_mapping_version_id");
        entity.Property(e => e.SynchronizationMode).HasColumnName("synchronization_mode").HasMaxLength(30);
        entity.Property(e => e.ScheduleExpression).HasColumnName("schedule_expression").HasMaxLength(200);
        entity.Property(e => e.CheckpointJson).HasColumnName("checkpoint_json");
        entity.Property(e => e.HealthStatus).HasColumnName("health_status").HasMaxLength(40);
        entity.Property(e => e.FreshnessState).HasColumnName("freshness_state").HasMaxLength(30);
        entity.Property(e => e.QualityState).HasColumnName("quality_state").HasMaxLength(30);
        entity.Property(e => e.LastSuccessfulSyncAtUtc).HasColumnName("last_successful_sync_at_utc");
        entity.Property(e => e.LastAttemptedSyncAtUtc).HasColumnName("last_attempted_sync_at_utc");
        entity.Property(e => e.LastSuccessAtUtc).HasColumnName("last_success_at_utc");
        entity.Property(e => e.LastFailureAtUtc).HasColumnName("last_failure_at_utc");
        entity.Property(e => e.FailureStatus).HasColumnName("failure_status").HasMaxLength(100);
        entity.Property(e => e.RetryState).HasColumnName("retry_state").HasMaxLength(30);
        entity.Property(e => e.RateLimitState).HasColumnName("rate_limit_state").HasMaxLength(100);
        entity.Property(e => e.DomainBindingKind).HasColumnName("domain_binding_kind").HasMaxLength(50);
        entity.Property(e => e.DomainBindingId).HasColumnName("domain_binding_id");
        entity.Property(e => e.OwnerSubjectId).HasColumnName("owner_subject_id").HasMaxLength(200);
        entity.Property(e => e.ConfigurationJson).HasColumnName("configuration_json");
        entity.Property(e => e.ObservabilityJson).HasColumnName("observability_json");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.Name }).IsUnique();
        entity.HasIndex(e => e.IngressKey)
            .IsUnique()
            .HasFilter("ingress_key IS NOT NULL");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.ActiveMappingVersion).WithMany()
            .HasForeignKey(e => e.ActiveMappingVersionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class IntegrationMappingVersionEntityConfiguration : IEntityTypeConfiguration<IntegrationMappingVersion>
{
    public void Configure(EntityTypeBuilder<IntegrationMappingVersion> entity)
    {
        entity.ToTable("integration_mapping_versions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectorId).HasColumnName("connector_id");
        entity.Property(e => e.VersionNumber).HasColumnName("version_number");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.TargetCanonicalEntityType).HasColumnName("target_canonical_entity_type").HasMaxLength(100);
        entity.Property(e => e.SourceSchemaFingerprint).HasColumnName("source_schema_fingerprint").HasMaxLength(128);
        entity.Property(e => e.MappingJson).HasColumnName("mapping_json");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.ValidatedAtUtc).HasColumnName("validated_at_utc");
        entity.Property(e => e.ActivatedBySubjectId).HasColumnName("activated_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.ActivatedAtUtc).HasColumnName("activated_at_utc");
        entity.Property(e => e.SupersededByMappingVersionId).HasColumnName("superseded_by_mapping_version_id");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.ConnectorId, e.VersionNumber }).IsUnique();
        entity.HasOne(e => e.Connector).WithMany(c => c.MappingVersions)
            .HasForeignKey(e => e.ConnectorId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IntegrationSyncRunEntityConfiguration : IEntityTypeConfiguration<IntegrationSyncRun>
{
    public void Configure(EntityTypeBuilder<IntegrationSyncRun> entity)
    {
        entity.ToTable("integration_sync_runs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectorId).HasColumnName("connector_id");
        entity.Property(e => e.MappingVersionId).HasColumnName("mapping_version_id");
        entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(50);
        entity.Property(e => e.SourceFingerprint).HasColumnName("source_fingerprint").HasMaxLength(128);
        entity.Property(e => e.ImportIdempotencyKey).HasColumnName("import_idempotency_key").HasMaxLength(200);
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.CheckpointBeforeJson).HasColumnName("checkpoint_before_json");
        entity.Property(e => e.CheckpointAfterJson).HasColumnName("checkpoint_after_json");
        entity.Property(e => e.RecordsRead).HasColumnName("records_read");
        entity.Property(e => e.RecordsAccepted).HasColumnName("records_accepted");
        entity.Property(e => e.RecordsUpdated).HasColumnName("records_updated");
        entity.Property(e => e.RecordsDuplicate).HasColumnName("records_duplicate");
        entity.Property(e => e.RecordsRejected).HasColumnName("records_rejected");
        entity.Property(e => e.RecordsQuarantined).HasColumnName("records_quarantined");
        entity.Property(e => e.FailureCategory).HasColumnName("failure_category").HasMaxLength(50);
        entity.Property(e => e.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(500);
        entity.Property(e => e.TriggeredBySubjectId).HasColumnName("triggered_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.RetriedFromSyncRunId).HasColumnName("retried_from_sync_run_id");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.StartedAtUtc });
        entity.HasIndex(e => new { e.ConnectorId, e.ImportIdempotencyKey })
            .IsUnique()
            .HasFilter("import_idempotency_key IS NOT NULL");
        entity.HasOne(e => e.Connector).WithMany(c => c.SyncRuns)
            .HasForeignKey(e => e.ConnectorId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<IntegrationMappingVersion>().WithMany()
            .HasForeignKey(e => e.MappingVersionId)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IntegrationIngestionQuarantineEntityConfiguration : IEntityTypeConfiguration<IntegrationIngestionQuarantine>
{
    public void Configure(EntityTypeBuilder<IntegrationIngestionQuarantine> entity)
    {
        entity.ToTable("integration_ingestion_quarantine");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectorId).HasColumnName("connector_id");
        entity.Property(e => e.SyncRunId).HasColumnName("sync_run_id");
        entity.Property(e => e.SourceSystem).HasColumnName("source_system").HasMaxLength(100);
        entity.Property(e => e.SourceEntityType).HasColumnName("source_entity_type").HasMaxLength(100);
        entity.Property(e => e.SourceRecordId).HasColumnName("source_record_id").HasMaxLength(200);
        entity.Property(e => e.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        entity.Property(e => e.SafeReason).HasColumnName("safe_reason").HasMaxLength(500);
        entity.Property(e => e.ValidationIssue).HasColumnName("validation_issue").HasMaxLength(500);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.EvidenceClass).HasColumnName("evidence_class").HasMaxLength(30);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        entity.Property(e => e.ResolvedBySubjectId).HasColumnName("resolved_by_subject_id").HasMaxLength(200);

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.OccurredAtUtc });
        entity.HasOne(e => e.Connector).WithMany()
            .HasForeignKey(e => e.ConnectorId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.SyncRun).WithMany()
            .HasForeignKey(e => e.SyncRunId)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IntegrationSourceIdentityEntityConfiguration : IEntityTypeConfiguration<IntegrationSourceIdentity>
{
    public void Configure(EntityTypeBuilder<IntegrationSourceIdentity> entity)
    {
        entity.ToTable("integration_source_identities");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.ConnectorId).HasColumnName("connector_id");
        entity.Property(e => e.SourceSystem).HasColumnName("source_system").HasMaxLength(100);
        entity.Property(e => e.SourceEntityType).HasColumnName("source_entity_type").HasMaxLength(100);
        entity.Property(e => e.SourceRecordId).HasColumnName("source_record_id").HasMaxLength(200);
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.ContentFingerprint).HasColumnName("content_fingerprint").HasMaxLength(128);
        entity.Property(e => e.InternalProjectionType).HasColumnName("internal_projection_type").HasMaxLength(100);
        entity.Property(e => e.InternalProjectionId).HasColumnName("internal_projection_id");
        entity.Property(e => e.EvidenceClass).HasColumnName("evidence_class").HasMaxLength(30);
        entity.Property(e => e.FirstSeenAtUtc).HasColumnName("first_seen_at_utc");
        entity.Property(e => e.LastSeenAtUtc).HasColumnName("last_seen_at_utc");

        entity.HasIndex(e => new { e.ConnectorId, e.SourceEntityType, e.SourceRecordId }).IsUnique();
        entity.HasOne(e => e.Connector).WithMany()
            .HasForeignKey(e => e.ConnectorId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
