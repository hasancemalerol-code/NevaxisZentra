using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.CommercialIntelligence;

internal sealed class CommercialProfitabilityAlertEntityConfiguration
    : IEntityTypeConfiguration<CommercialProfitabilityAlert>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityAlert> entity)
    {
        entity.ToTable("commercial_profitability_alerts");
        entity.HasKey(alert => alert.Id);
        entity.Property(alert => alert.Id).HasColumnName("id");
        entity.Property(alert => alert.TenantId).HasColumnName("tenant_id");
        entity.Property(alert => alert.FacilityId).HasColumnName("facility_id");
        entity.Property(alert => alert.AlertReference).HasColumnName("alert_reference").HasMaxLength(100).IsRequired();
        entity.Property(alert => alert.AlertCategory).HasColumnName("alert_category").HasMaxLength(50).IsRequired();
        entity.Property(alert => alert.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(alert => alert.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(30).IsRequired();
        entity.Property(alert => alert.SourceComparisonId).HasColumnName("source_comparison_id");
        entity.Property(alert => alert.SourceComparisonType).HasColumnName("source_comparison_type").HasMaxLength(50).IsRequired();
        entity.Property(alert => alert.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        entity.Property(alert => alert.EvidenceSummary).HasColumnName("evidence_summary").HasMaxLength(2000).IsRequired();
        entity.Property(alert => alert.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(alert => alert.ComparisonOutcome).HasColumnName("comparison_outcome").HasMaxLength(30).IsRequired();
        entity.Property(alert => alert.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(50);
        entity.Property(alert => alert.SourceSnapshotJson).HasColumnName("source_snapshot_json").HasColumnType("jsonb").IsRequired();
        entity.Property(alert => alert.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        entity.Property(alert => alert.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(alert => alert.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(alert => alert.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(alert => alert.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(alert => alert.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(alert => alert.MaterializedAtUtc).HasColumnName("materialized_at_utc");
        entity.Property(alert => alert.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(alert => alert.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(alert => alert.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();

        entity.HasIndex(alert => new { alert.TenantId, alert.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_commprofit6_alerts_tenant_idempotency");

        entity.HasIndex(alert => new { alert.TenantId, alert.FacilityId, alert.SourceComparisonId, alert.AlertCategory })
            .IsUnique()
            .HasFilter("lifecycle_status = 'active'")
            .HasDatabaseName("ux_commprofit6_alerts_active_comparison_category");

        entity.HasIndex(alert => new { alert.TenantId, alert.FacilityId, alert.LifecycleStatus })
            .HasDatabaseName("ix_commprofit6_alerts_tenant_facility_lifecycle");

        entity.HasIndex(alert => new { alert.TenantId, alert.FacilityId, alert.Severity })
            .HasDatabaseName("ix_commprofit6_alerts_tenant_facility_severity");

        entity.HasIndex(alert => new { alert.TenantId, alert.FacilityId, alert.AlertCategory })
            .HasDatabaseName("ix_commprofit6_alerts_tenant_facility_category");

        entity.HasOne(alert => alert.SourceComparison)
            .WithMany()
            .HasForeignKey(alert => alert.SourceComparisonId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(alert => alert.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(alert => new { alert.TenantId, alert.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CommercialProfitabilityAlertAuditEventEntityConfiguration
    : IEntityTypeConfiguration<CommercialProfitabilityAlertAuditEvent>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityAlertAuditEvent> entity)
    {
        entity.ToTable("commercial_profitability_alert_audit_events");
        entity.HasKey(auditEvent => auditEvent.Id);
        entity.Property(auditEvent => auditEvent.Id).HasColumnName("id");
        entity.Property(auditEvent => auditEvent.TenantId).HasColumnName("tenant_id");
        entity.Property(auditEvent => auditEvent.FacilityId).HasColumnName("facility_id");
        entity.Property(auditEvent => auditEvent.AlertId).HasColumnName("alert_id");
        entity.Property(auditEvent => auditEvent.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        entity.Property(auditEvent => auditEvent.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
        entity.Property(auditEvent => auditEvent.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(auditEvent => auditEvent.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(auditEvent => auditEvent.CorrelationId).HasColumnName("correlation_id");

        entity.HasIndex(auditEvent => new { auditEvent.TenantId, auditEvent.FacilityId, auditEvent.AlertId, auditEvent.OccurredAtUtc })
            .HasDatabaseName("ix_commprofit6_alert_audit_tenant_facility_alert_occurred");

        entity.HasOne(auditEvent => auditEvent.Alert)
            .WithMany(alert => alert.AuditEvents)
            .HasForeignKey(auditEvent => auditEvent.AlertId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(auditEvent => new { auditEvent.TenantId, auditEvent.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
