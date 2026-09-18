using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.EnterpriseReliability;

internal sealed class EnterpriseReliabilityPolicyEntityConfiguration : IEntityTypeConfiguration<EnterpriseReliabilityPolicy>
{
    public void Configure(EntityTypeBuilder<EnterpriseReliabilityPolicy> entity)
    {
        entity.ToTable("enterprise_reliability_policies");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.OperatingMode).HasColumnName("operating_mode").HasMaxLength(40);
        entity.Property(e => e.PolicyEnvelopeJson).HasColumnName("policy_envelope_json");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token")
            .IsConcurrencyToken();
        entity.HasIndex(e => e.TenantId).IsUnique();
    }
}

internal sealed class SystemComponentHealthSnapshotEntityConfiguration
    : IEntityTypeConfiguration<SystemComponentHealthSnapshot>
{
    public void Configure(EntityTypeBuilder<SystemComponentHealthSnapshot> entity)
    {
        entity.ToTable("system_component_health_snapshots");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ComponentKind).HasColumnName("component_kind").HasMaxLength(40);
        entity.Property(e => e.ComponentKey).HasColumnName("component_key").HasMaxLength(120);
        entity.Property(e => e.HealthStatus).HasColumnName("health_status").HasMaxLength(20);
        entity.Property(e => e.DetailJson).HasColumnName("detail_json");
        entity.Property(e => e.ObservedAtUtc).HasColumnName("observed_at_utc");
        entity.Property(e => e.LastHealthyAtUtc).HasColumnName("last_healthy_at_utc");
        entity.Property(e => e.LastFailureAtUtc).HasColumnName("last_failure_at_utc");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        entity.HasIndex(e => new { e.TenantId, e.ComponentKind, e.ComponentKey, e.ObservedAtUtc });
    }
}

internal sealed class IntegrationCircuitBreakerStateEntityConfiguration
    : IEntityTypeConfiguration<IntegrationCircuitBreakerState>
{
    public void Configure(EntityTypeBuilder<IntegrationCircuitBreakerState> entity)
    {
        entity.ToTable("integration_circuit_breaker_states");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectorId).HasColumnName("connector_id");
        entity.Property(e => e.State).HasColumnName("state").HasMaxLength(20);
        entity.Property(e => e.ConsecutiveFailures).HasColumnName("consecutive_failures");
        entity.Property(e => e.FailureThreshold).HasColumnName("failure_threshold");
        entity.Property(e => e.OpenedAtUtc).HasColumnName("opened_at_utc");
        entity.Property(e => e.CooldownUntilUtc).HasColumnName("cooldown_until_utc");
        entity.Property(e => e.LastProbeAtUtc).HasColumnName("last_probe_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasIndex(e => e.ConnectorId).IsUnique();
    }
}

internal sealed class RecoveryReconciliationRunEntityConfiguration
    : IEntityTypeConfiguration<RecoveryReconciliationRun>
{
    public void Configure(EntityTypeBuilder<RecoveryReconciliationRun> entity)
    {
        entity.ToTable("recovery_reconciliation_runs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(40);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(e => e.SummaryJson).HasColumnName("summary_json");
        entity.Property(e => e.ConflictJson).HasColumnName("conflict_json");
        entity.Property(e => e.TriggeredBySubjectId).HasColumnName("triggered_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.StartedAtUtc });
    }
}

internal sealed class BackgroundJobLeaseEntityConfiguration : IEntityTypeConfiguration<BackgroundJobLease>
{
    public void Configure(EntityTypeBuilder<BackgroundJobLease> entity)
    {
        entity.ToTable("background_job_leases");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.JobKey).HasColumnName("job_key").HasMaxLength(120);
        entity.Property(e => e.OwnerInstanceId).HasColumnName("owner_instance_id").HasMaxLength(120);
        entity.Property(e => e.AcquiredAtUtc).HasColumnName("acquired_at_utc");
        entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        entity.HasIndex(e => new { e.TenantId, e.JobKey }).IsUnique();
    }
}

internal sealed class TechnicalOperationalAlertEntityConfiguration
    : IEntityTypeConfiguration<TechnicalOperationalAlert>
{
    public void Configure(EntityTypeBuilder<TechnicalOperationalAlert> entity)
    {
        entity.ToTable("technical_operational_alerts");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.AlertKind).HasColumnName("alert_kind").HasMaxLength(80);
        entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.DetailJson).HasColumnName("detail_json");
        entity.Property(e => e.DedupKey).HasColumnName("dedup_key").HasMaxLength(200);
        entity.Property(e => e.OpenedAtUtc).HasColumnName("opened_at_utc");
        entity.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.DedupKey, e.Status });
    }
}

internal sealed class EdgeAgentRegistrationEntityConfiguration : IEntityTypeConfiguration<EdgeAgentRegistration>
{
    public void Configure(EntityTypeBuilder<EdgeAgentRegistration> entity)
    {
        entity.ToTable("edge_agent_registrations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.AgentKey).HasColumnName("agent_key").HasMaxLength(120);
        entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        entity.Property(e => e.AgentVersion).HasColumnName("agent_version").HasMaxLength(40);
        entity.Property(e => e.ConnectivityStatus).HasColumnName("connectivity_status").HasMaxLength(20);
        entity.Property(e => e.BufferBacklogCount).HasColumnName("buffer_backlog_count");
        entity.Property(e => e.BufferCapacity).HasColumnName("buffer_capacity");
        entity.Property(e => e.LastHeartbeatAtUtc).HasColumnName("last_heartbeat_at_utc");
        entity.Property(e => e.LastSyncAtUtc).HasColumnName("last_sync_at_utc");
        entity.Property(e => e.MachineScopeJson).HasColumnName("machine_scope_json");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.AgentKey }).IsUnique();
    }
}

internal sealed class BackupVerificationRunEntityConfiguration : IEntityTypeConfiguration<BackupVerificationRun>
{
    public void Configure(EntityTypeBuilder<BackupVerificationRun> entity)
    {
        entity.ToTable("backup_verification_runs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.BackupArtifactReference).HasColumnName("backup_artifact_reference").HasMaxLength(500);
        entity.Property(e => e.RestoreTargetReference).HasColumnName("restore_target_reference").HasMaxLength(500);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(e => e.ResultJson).HasColumnName("result_json");
        entity.Property(e => e.TriggeredBySubjectId).HasColumnName("triggered_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
    }
}

internal sealed class IntegrationQuarantineReplayAuditEntityConfiguration
    : IEntityTypeConfiguration<IntegrationQuarantineReplayAudit>
{
    public void Configure(EntityTypeBuilder<IntegrationQuarantineReplayAudit> entity)
    {
        entity.ToTable("integration_quarantine_replay_audit");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.QuarantineId).HasColumnName("quarantine_id");
        entity.Property(e => e.ReplayIdempotencyKey).HasColumnName("replay_idempotency_key").HasMaxLength(200);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(e => e.TriggeredBySubjectId).HasColumnName("triggered_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.ReplayedAtUtc).HasColumnName("replayed_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.ReplayIdempotencyKey }).IsUnique();
    }
}
