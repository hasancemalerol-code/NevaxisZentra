using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.ShiftRisk;

internal sealed class ShiftCoverageRequirementEntityConfiguration : IEntityTypeConfiguration<ShiftCoverageRequirement>
{
    public void Configure(EntityTypeBuilder<ShiftCoverageRequirement> entity)
    {
        entity.ToTable("shift_coverage_requirements");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ProductionShiftId).HasColumnName("production_shift_id");
        entity.Property(r => r.ShiftDate).HasColumnName("shift_date");
        entity.Property(r => r.PlannedStartUtc).HasColumnName("planned_start_utc");
        entity.Property(r => r.PlannedEndUtc).HasColumnName("planned_end_utc");
        entity.Property(r => r.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200);
        entity.Property(r => r.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(r => r.MachineFamilyScope).HasColumnName("machine_family_scope").HasMaxLength(200);
        entity.Property(r => r.RequiredCompetencyCode).HasColumnName("required_competency_code").HasMaxLength(100).IsRequired();
        entity.Property(r => r.MinimumApprovedOperatorCount).HasColumnName("minimum_approved_operator_count");
        entity.Property(r => r.IsCriticalRole).HasColumnName("is_critical_role");
        entity.Property(r => r.SourceReference).HasColumnName("source_reference").HasMaxLength(200).IsRequired();
        entity.Property(r => r.EffectiveFrom).HasColumnName("effective_from");
        entity.Property(r => r.EffectiveUntil).HasColumnName("effective_until");
        entity.Property(r => r.IsActive).HasColumnName("is_active");
        entity.Property(r => r.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(r => r.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(r => r.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(r => r.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(r => r.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionShift>().WithMany()
            .HasForeignKey(r => r.ProductionShiftId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.CreateIdempotencyKey })
            .HasDatabaseName("ix_shift_coverage_requirements_tenant_create_idempotency")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.ProductionShiftId, r.ShiftDate })
            .HasDatabaseName("ix_shift_coverage_requirements_tenant_facility_shift_date");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.IsActive, r.ShiftDate })
            .HasDatabaseName("ix_shift_coverage_requirements_tenant_facility_active_date");
    }
}

internal sealed class ShiftRiskAssessmentEntityConfiguration : IEntityTypeConfiguration<ShiftRiskAssessment>
{
    public void Configure(EntityTypeBuilder<ShiftRiskAssessment> entity)
    {
        entity.ToTable("shift_risk_assessments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.CoverageRequirementId).HasColumnName("coverage_requirement_id");
        entity.Property(a => a.ProductionShiftId).HasColumnName("production_shift_id");
        entity.Property(a => a.ShiftDate).HasColumnName("shift_date");
        entity.Property(a => a.PlannedStartUtc).HasColumnName("planned_start_utc");
        entity.Property(a => a.PlannedEndUtc).HasColumnName("planned_end_utc");
        entity.Property(a => a.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200);
        entity.Property(a => a.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(a => a.MachineFamilyScope).HasColumnName("machine_family_scope").HasMaxLength(200);
        entity.Property(a => a.RequiredCompetencyCode).HasColumnName("required_competency_code").HasMaxLength(100);
        entity.Property(a => a.MinimumRequiredCount).HasColumnName("minimum_required_count");
        entity.Property(a => a.IsCriticalRole).HasColumnName("is_critical_role");
        entity.Property(a => a.ApprovedEligibleCount).HasColumnName("approved_eligible_count");
        entity.Property(a => a.CoverageGap).HasColumnName("coverage_gap");
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(a => a.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        entity.Property(a => a.LeadingDriverCategory).HasColumnName("leading_driver_category").HasMaxLength(50);
        entity.Property(a => a.LeadingReasonCode).HasColumnName("leading_reason_code").HasMaxLength(100);
        entity.Property(a => a.LeadingDriverId).HasColumnName("leading_driver_id");
        entity.Property(a => a.StructuredEvidenceJson).HasColumnName("structured_evidence_json").IsRequired();
        entity.Property(a => a.SourceFreshnessSnapshotJson).HasColumnName("source_freshness_snapshot_json").IsRequired();
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.EvaluateIdempotencyKey).HasColumnName("evaluate_idempotency_key").HasMaxLength(128);
        entity.Property(a => a.EvaluateIdempotencyPayloadFingerprint).HasColumnName("evaluate_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ShiftCoverageRequirement>().WithMany()
            .HasForeignKey(a => a.CoverageRequirementId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionShift>().WithMany()
            .HasForeignKey(a => a.ProductionShiftId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.EvaluateIdempotencyKey })
            .HasDatabaseName("ix_shift_risk_assessments_tenant_evaluate_idempotency")
            .IsUnique()
            .HasFilter("evaluate_idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.InputFingerprint })
            .HasDatabaseName("ix_shift_risk_assessments_tenant_facility_fingerprint");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.EvaluatedAtUtc })
            .HasDatabaseName("ix_shift_risk_assessments_tenant_facility_evaluated");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.Outcome, a.ShiftDate })
            .HasDatabaseName("ix_shift_risk_assessments_tenant_facility_outcome_date");
    }
}

internal sealed class ShiftRiskDriverEntityConfiguration : IEntityTypeConfiguration<ShiftRiskDriver>
{
    public void Configure(EntityTypeBuilder<ShiftRiskDriver> entity)
    {
        entity.ToTable("shift_risk_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.AssessmentId).HasColumnName("assessment_id");
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.LabelTr).HasColumnName("label_tr").HasMaxLength(200).IsRequired();
        entity.Property(d => d.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        entity.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(d => d.RequiredValue).HasColumnName("required_value").HasMaxLength(500);
        entity.Property(d => d.ObservedValue).HasColumnName("observed_value").HasMaxLength(500);
        entity.Property(d => d.GapValue).HasColumnName("gap_value").HasMaxLength(500);
        entity.Property(d => d.ProductionShiftId).HasColumnName("production_shift_id");
        entity.Property(d => d.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200);
        entity.Property(d => d.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(d => d.RequiredCompetencyCode).HasColumnName("required_competency_code").HasMaxLength(100);
        entity.Property(d => d.EvidenceSource).HasColumnName("evidence_source").HasMaxLength(100).IsRequired();
        entity.Property(d => d.EvidenceReference).HasColumnName("evidence_reference").HasMaxLength(200);
        entity.Property(d => d.EvidenceTimestampUtc).HasColumnName("evidence_timestamp_utc");
        entity.Property(d => d.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20);
        entity.Property(d => d.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(d => d.IsLeadingRisk).HasColumnName("is_leading_risk");
        entity.HasOne(d => d.Assessment)
            .WithMany(a => a.Drivers)
            .HasForeignKey(d => d.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.AssessmentId, d.ReasonCode })
            .HasDatabaseName("ix_shift_risk_drivers_tenant_assessment_reason")
            .IsUnique();
    }
}

internal sealed class ShiftRiskAuditEventEntityConfiguration : IEntityTypeConfiguration<ShiftRiskAuditEvent>
{
    public void Configure(EntityTypeBuilder<ShiftRiskAuditEvent> entity)
    {
        entity.ToTable("shift_risk_audit_events");
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
            .HasDatabaseName("ix_shift_risk_audit_events_tenant_facility_occurred");
        entity.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId })
            .HasDatabaseName("ix_shift_risk_audit_events_tenant_entity");
    }
}
