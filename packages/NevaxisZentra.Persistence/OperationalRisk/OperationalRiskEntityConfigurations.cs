using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.OperationalRisk;

internal sealed class OperationalRiskCaseEntityConfiguration : IEntityTypeConfiguration<OperationalRiskCase>
{
    public void Configure(EntityTypeBuilder<OperationalRiskCase> entity)
    {
        entity.ToTable("operational_risk_cases");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.CaseReference).HasColumnName("case_reference").HasMaxLength(100).IsRequired();
        entity.Property(c => c.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        entity.Property(c => c.ScopeType).HasColumnName("scope_type").HasMaxLength(50).IsRequired();
        entity.Property(c => c.ScopeReference).HasColumnName("scope_reference").HasMaxLength(200);
        entity.Property(c => c.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        entity.Property(c => c.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();
        entity.Property(c => c.EvidenceState).HasColumnName("evidence_state").HasMaxLength(30).IsRequired();
        entity.Property(c => c.LikelihoodLevel).HasColumnName("likelihood_level");
        entity.Property(c => c.ImpactLevel).HasColumnName("impact_level");
        entity.Property(c => c.RiskScore).HasColumnName("risk_score");
        entity.Property(c => c.SeverityBand).HasColumnName("severity_band").HasMaxLength(20);
        entity.Property(c => c.AssessmentOutcome).HasColumnName("assessment_outcome").HasMaxLength(30).IsRequired();
        entity.Property(c => c.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(30).IsRequired();
        entity.Property(c => c.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(c => c.SourceModule).HasColumnName("source_module").HasMaxLength(100);
        entity.Property(c => c.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(c => c.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(c => c.DetectionKey).HasColumnName("detection_key").HasMaxLength(64);
        entity.Property(c => c.OccurrenceSequence).HasColumnName("occurrence_sequence");
        entity.Property(c => c.DetectionRuleCode).HasColumnName("detection_rule_code").HasMaxLength(100);
        entity.Property(c => c.FirstDetectedAtUtc).HasColumnName("first_detected_at_utc");
        entity.Property(c => c.LastObservedAtUtc).HasColumnName("last_observed_at_utc");
        entity.Property(c => c.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(c => c.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(c => c.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(c => c.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(c => c.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(c => new { c.TenantId, c.CaseReference })
            .HasDatabaseName("ux_operational_risk_cases_tenant_reference")
            .IsUnique();
        entity.HasIndex(c => new { c.TenantId, c.IdempotencyKey })
            .HasDatabaseName("ux_operational_risk_cases_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId })
            .HasDatabaseName("ix_operational_risk_cases_tenant_facility");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.Category })
            .HasDatabaseName("ix_operational_risk_cases_tenant_facility_category");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.LifecycleStatus })
            .HasDatabaseName("ix_operational_risk_cases_tenant_facility_status");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.SeverityBand })
            .HasDatabaseName("ix_operational_risk_cases_tenant_facility_severity");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.ScopeType, c.ScopeReference })
            .HasDatabaseName("ix_operational_risk_cases_tenant_facility_scope");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.FirstDetectedAtUtc })
            .HasDatabaseName("ix_operational_risk_cases_tenant_facility_first_detected");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.LastObservedAtUtc })
            .HasDatabaseName("ix_operational_risk_cases_tenant_facility_last_observed");
        entity.HasIndex(c => new { c.TenantId, c.DetectionKey })
            .HasDatabaseName("ux_operational_risk_cases_tenant_detection_key_open")
            .IsUnique()
            .HasFilter("detection_key IS NOT NULL AND lifecycle_status NOT IN ('resolved','dismissed')");
    }
}

internal sealed class OperationalRiskEvidenceEntityConfiguration : IEntityTypeConfiguration<OperationalRiskEvidence>
{
    public void Configure(EntityTypeBuilder<OperationalRiskEvidence> entity)
    {
        entity.ToTable("operational_risk_evidence");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.CaseId).HasColumnName("case_id");
        entity.Property(e => e.EvidenceTypeCode).HasColumnName("evidence_type_code").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EvidenceState).HasColumnName("evidence_state").HasMaxLength(30).IsRequired();
        entity.Property(e => e.ObservedAtUtc).HasColumnName("observed_at_utc");
        entity.Property(e => e.SourceModule).HasColumnName("source_module").HasMaxLength(100);
        entity.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000).IsRequired();
        entity.Property(e => e.NumericValue).HasColumnName("numeric_value").HasPrecision(18, 6);
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(50);
        entity.Property(e => e.ComparisonReference).HasColumnName("comparison_reference").HasMaxLength(200);
        entity.Property(e => e.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(e => e.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Case).WithMany(c => c.Evidence)
            .HasForeignKey(e => e.CaseId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey })
            .HasDatabaseName("ux_operational_risk_evidence_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.CaseId })
            .HasDatabaseName("ix_operational_risk_evidence_tenant_facility_case");
    }
}

internal sealed class OperationalRiskStatusHistoryEntityConfiguration : IEntityTypeConfiguration<OperationalRiskStatusHistory>
{
    public void Configure(EntityTypeBuilder<OperationalRiskStatusHistory> entity)
    {
        entity.ToTable("operational_risk_status_history");
        entity.HasKey(h => h.Id);
        entity.Property(h => h.Id).HasColumnName("id");
        entity.Property(h => h.TenantId).HasColumnName("tenant_id");
        entity.Property(h => h.FacilityId).HasColumnName("facility_id");
        entity.Property(h => h.CaseId).HasColumnName("case_id");
        entity.Property(h => h.PreviousStatus).HasColumnName("previous_status").HasMaxLength(30).IsRequired();
        entity.Property(h => h.NewStatus).HasColumnName("new_status").HasMaxLength(30).IsRequired();
        entity.Property(h => h.ReasonNote).HasColumnName("reason_note").HasMaxLength(2000);
        entity.Property(h => h.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(h => h.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(h => h.ExpectedCaseVersion).HasColumnName("expected_case_version");
        entity.Property(h => h.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(h => h.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(h => new { h.TenantId, h.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(h => h.Case).WithMany(c => c.StatusHistory)
            .HasForeignKey(h => h.CaseId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(h => new { h.TenantId, h.FacilityId, h.CaseId, h.OccurredAtUtc })
            .HasDatabaseName("ix_operational_risk_status_history_tenant_facility_case");
    }
}

internal sealed class OperationalRiskAuditEventEntityConfiguration : IEntityTypeConfiguration<OperationalRiskAuditEvent>
{
    public void Configure(EntityTypeBuilder<OperationalRiskAuditEvent> entity)
    {
        entity.ToTable("operational_risk_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityId).HasColumnName("entity_id");
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        entity.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.OccurredAtUtc })
            .HasDatabaseName("ix_operational_risk_audit_events_tenant_facility_occurred");
    }
}
