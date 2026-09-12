using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Production;
using NevaxisFactOS.Persistence.ShiftWorkforce;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.ShiftAssignment;

internal sealed class ShiftOperatorEligibilityAssessmentEntityConfiguration : IEntityTypeConfiguration<ShiftOperatorEligibilityAssessment>
{
    public void Configure(EntityTypeBuilder<ShiftOperatorEligibilityAssessment> entity)
    {
        entity.ToTable("shift_operator_eligibility_assessments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200).IsRequired();
        entity.Property(a => a.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(a => a.ProductionShiftId).HasColumnName("production_shift_id");
        entity.Property(a => a.ShiftDate).HasColumnName("shift_date");
        entity.Property(a => a.PlannedStartUtc).HasColumnName("planned_start_utc");
        entity.Property(a => a.PlannedEndUtc).HasColumnName("planned_end_utc");
        entity.Property(a => a.RequiredCompetencyCode).HasColumnName("required_competency_code").HasMaxLength(100).IsRequired();
        entity.Property(a => a.MachineFamilyScope).HasColumnName("machine_family_scope").HasMaxLength(200);
        entity.Property(a => a.ProcessScope).HasColumnName("process_scope").HasMaxLength(200);
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(a => a.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionShift>().WithMany()
            .HasForeignKey(a => a.ProductionShiftId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.InputFingerprint })
            .HasDatabaseName("ix_shift_operator_eligibility_assessments_tenant_facility_fingerprint");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.EvaluatedAtUtc })
            .HasDatabaseName("ix_shift_operator_eligibility_assessments_tenant_facility_evaluated");
    }
}

internal sealed class ShiftOperatorEligibilityResultEntityConfiguration : IEntityTypeConfiguration<ShiftOperatorEligibilityResult>
{
    public void Configure(EntityTypeBuilder<ShiftOperatorEligibilityResult> entity)
    {
        entity.ToTable("shift_operator_eligibility_results");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.AssessmentId).HasColumnName("assessment_id");
        entity.Property(r => r.WorkforcePersonId).HasColumnName("workforce_person_id");
        entity.Property(r => r.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        entity.Property(r => r.PrimaryReasonCode).HasColumnName("primary_reason_code").HasMaxLength(100);
        entity.Property(r => r.EmployeeCodeSnapshot).HasColumnName("employee_code_snapshot").HasMaxLength(100).IsRequired();
        entity.Property(r => r.DisplayNameSnapshot).HasColumnName("display_name_snapshot").HasMaxLength(200).IsRequired();
        entity.HasOne(r => r.Assessment)
            .WithMany(a => a.Results)
            .HasForeignKey(r => r.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ShiftWorkforcePerson>().WithMany()
            .HasForeignKey(r => r.WorkforcePersonId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.AssessmentId, r.WorkforcePersonId })
            .HasDatabaseName("ix_shift_operator_eligibility_results_tenant_assessment_person")
            .IsUnique();
    }
}

internal sealed class ShiftOperatorEligibilityReasonEntityConfiguration : IEntityTypeConfiguration<ShiftOperatorEligibilityReason>
{
    public void Configure(EntityTypeBuilder<ShiftOperatorEligibilityReason> entity)
    {
        entity.ToTable("shift_operator_eligibility_reasons");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.ResultId).HasColumnName("result_id");
        entity.Property(r => r.ReasonCode).HasColumnName("reason_code").HasMaxLength(100).IsRequired();
        entity.Property(r => r.LabelTr).HasColumnName("label_tr").HasMaxLength(200).IsRequired();
        entity.Property(r => r.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(r => r.RequiredValue).HasColumnName("required_value").HasMaxLength(500);
        entity.Property(r => r.ObservedValue).HasColumnName("observed_value").HasMaxLength(500);
        entity.Property(r => r.EvidenceSource).HasColumnName("evidence_source").HasMaxLength(100).IsRequired();
        entity.Property(r => r.EvidenceTimestampUtc).HasColumnName("evidence_timestamp_utc");
        entity.Property(r => r.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20);
        entity.HasOne(r => r.Result)
            .WithMany(result => result.Reasons)
            .HasForeignKey(r => r.ResultId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.ResultId })
            .HasDatabaseName("ix_shift_operator_eligibility_reasons_tenant_result");
    }
}

internal sealed class ShiftOperatorAssignmentEntityConfiguration : IEntityTypeConfiguration<ShiftOperatorAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftOperatorAssignment> entity)
    {
        entity.ToTable("shift_operator_assignments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200).IsRequired();
        entity.Property(a => a.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(a => a.ProductionShiftId).HasColumnName("production_shift_id");
        entity.Property(a => a.ShiftDate).HasColumnName("shift_date");
        entity.Property(a => a.WorkforcePersonId).HasColumnName("workforce_person_id");
        entity.Property(a => a.PlannedStartUtc).HasColumnName("planned_start_utc");
        entity.Property(a => a.PlannedEndUtc).HasColumnName("planned_end_utc");
        entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.EligibilityAssessmentId).HasColumnName("eligibility_assessment_id");
        entity.Property(a => a.EligibilityResultId).HasColumnName("eligibility_result_id");
        entity.Property(a => a.RequiredCompetencyCode).HasColumnName("required_competency_code").HasMaxLength(100);
        entity.Property(a => a.MachineFamilyScope).HasColumnName("machine_family_scope").HasMaxLength(200);
        entity.Property(a => a.ProcessScope).HasColumnName("process_scope").HasMaxLength(200);
        entity.Property(a => a.ApprovedBySubjectId).HasColumnName("approved_by_subject_id").HasMaxLength(200);
        entity.Property(a => a.ApprovedAtUtc).HasColumnName("approved_at_utc");
        entity.Property(a => a.CancelledBySubjectId).HasColumnName("cancelled_by_subject_id").HasMaxLength(200);
        entity.Property(a => a.CancelledAtUtc).HasColumnName("cancelled_at_utc");
        entity.Property(a => a.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(a => a.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(a => a.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(a => a.ApproveIdempotencyKey).HasColumnName("approve_idempotency_key").HasMaxLength(128);
        entity.Property(a => a.ApproveIdempotencyPayloadFingerprint).HasColumnName("approve_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(a => a.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ShiftWorkforcePerson>().WithMany()
            .HasForeignKey(a => a.WorkforcePersonId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionShift>().WithMany()
            .HasForeignKey(a => a.ProductionShiftId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ShiftOperatorEligibilityAssessment>().WithMany()
            .HasForeignKey(a => a.EligibilityAssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ShiftOperatorEligibilityResult>().WithMany()
            .HasForeignKey(a => a.EligibilityResultId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.CreateIdempotencyKey })
            .HasDatabaseName("ix_shift_operator_assignments_tenant_create_idempotency")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.Status, a.ShiftDate })
            .HasDatabaseName("ix_shift_operator_assignments_tenant_facility_status_date");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.ProductionOrderReference })
            .HasDatabaseName("ix_shift_operator_assignments_tenant_facility_order_ref");
        entity.HasIndex(a => new { a.TenantId, a.WorkforcePersonId, a.PlannedStartUtc, a.PlannedEndUtc })
            .HasDatabaseName("ix_shift_operator_assignments_tenant_person_window")
            .IsUnique()
            .HasFilter("status = 'approved'");
    }
}

internal sealed class ShiftOperatorAssignmentAuditEventEntityConfiguration : IEntityTypeConfiguration<ShiftOperatorAssignmentAuditEvent>
{
    public void Configure(EntityTypeBuilder<ShiftOperatorAssignmentAuditEvent> entity)
    {
        entity.ToTable("shift_operator_assignment_audit_events");
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
            .HasDatabaseName("ix_shift_operator_assignment_audit_events_tenant_facility_occurred");
        entity.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId })
            .HasDatabaseName("ix_shift_operator_assignment_audit_events_tenant_entity");
    }
}
