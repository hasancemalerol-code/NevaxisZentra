using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class MaterialRequirementProfileEntityConfiguration : IEntityTypeConfiguration<Material.MaterialRequirementProfile>
{
    public void Configure(EntityTypeBuilder<Material.MaterialRequirementProfile> entity)
    {
        entity.ToTable("material_requirement_profiles");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.MaterialId).HasColumnName("material_id");
        entity.Property(p => p.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200);
        entity.Property(p => p.ProductReference).HasColumnName("product_reference").HasMaxLength(200);
        entity.Property(p => p.MoldReference).HasColumnName("mold_reference").HasMaxLength(200);
        entity.Property(p => p.RequirementType).HasColumnName("requirement_type").HasMaxLength(30)
            .HasConversion(
                v => Material.MaterialRequirementValueConversions.ToStorage(v),
                v => Material.MaterialRequirementValueConversions.RequirementTypeFromStorage(v));
        entity.Property(p => p.PlannedProductionQuantity).HasColumnName("planned_production_quantity").HasPrecision(18, 4);
        entity.Property(p => p.GoodPartTargetQuantity).HasColumnName("good_part_target_quantity").HasPrecision(18, 4);
        entity.Property(p => p.PartWeightGrams).HasColumnName("part_weight_grams").HasPrecision(18, 4);
        entity.Property(p => p.RunnerWeightGramsPerShot).HasColumnName("runner_weight_grams_per_shot").HasPrecision(18, 4);
        entity.Property(p => p.EffectiveCavityCount).HasColumnName("effective_cavity_count");
        entity.Property(p => p.SetupAllowanceGrams).HasColumnName("setup_allowance_grams").HasPrecision(18, 4);
        entity.Property(p => p.ProcessLossAllowancePercent).HasColumnName("process_loss_allowance_percent").HasPrecision(8, 4);
        entity.Property(p => p.PlannedVirginPercent).HasColumnName("planned_virgin_percent").HasPrecision(8, 4);
        entity.Property(p => p.PlannedRegrindPercent).HasColumnName("planned_regrind_percent").HasPrecision(8, 4);
        entity.Property(p => p.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(
                v => Material.MaterialValueConversions.ToStorage(v),
                v => Material.MaterialValueConversions.SourceFromStorage(v));
        entity.Property(p => p.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(p => p.DataRecordedAtUtc).HasColumnName("data_recorded_at_utc");
        entity.Property(p => p.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(p => p.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(p => p.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.MaterialId });
        entity.HasIndex(p => new { p.TenantId, p.CreateIdempotencyKey }).IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");

        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(p => p.Material).WithMany()
            .HasForeignKey(p => p.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaterialConsumptionObservationEntityConfiguration : IEntityTypeConfiguration<Material.MaterialConsumptionObservation>
{
    public void Configure(EntityTypeBuilder<Material.MaterialConsumptionObservation> entity)
    {
        entity.ToTable("material_consumption_observations");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.MaterialId).HasColumnName("material_id");
        entity.Property(o => o.MaterialLotId).HasColumnName("material_lot_id");
        entity.Property(o => o.RequirementProfileId).HasColumnName("requirement_profile_id");
        entity.Property(o => o.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200);
        entity.Property(o => o.ProductReference).HasColumnName("product_reference").HasMaxLength(200);
        entity.Property(o => o.MoldReference).HasColumnName("mold_reference").HasMaxLength(200);
        entity.Property(o => o.ObservationType).HasColumnName("observation_type").HasMaxLength(40)
            .HasConversion(
                v => Material.MaterialRequirementValueConversions.ToStorage(v),
                v => Material.MaterialRequirementValueConversions.ObservationTypeFromStorage(v));
        entity.Property(o => o.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        entity.Property(o => o.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20);
        entity.Property(o => o.VirginQuantity).HasColumnName("virgin_quantity").HasPrecision(18, 4);
        entity.Property(o => o.RegrindQuantity).HasColumnName("regrind_quantity").HasPrecision(18, 4);
        entity.Property(o => o.GoodPartQuantity).HasColumnName("good_part_quantity").HasPrecision(18, 4);
        entity.Property(o => o.ScrapQuantity).HasColumnName("scrap_quantity").HasPrecision(18, 4);
        entity.Property(o => o.ObservedAtUtc).HasColumnName("observed_at_utc");
        entity.Property(o => o.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(
                v => Material.MaterialValueConversions.ToStorage(v),
                v => Material.MaterialValueConversions.SourceFromStorage(v));
        entity.Property(o => o.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(o => o.DataRecordedAtUtc).HasColumnName("data_recorded_at_utc");
        entity.Property(o => o.Notes).HasColumnName("notes").HasMaxLength(2000);
        entity.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(o => o.PayloadFingerprint).HasColumnName("payload_fingerprint").HasMaxLength(64);
        entity.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(o => o.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(o => o.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(o => new { o.TenantId, o.MaterialId, o.IdempotencyKey }).IsUnique();
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.MaterialId, o.ObservedAtUtc });

        entity.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(o => o.Material).WithMany()
            .HasForeignKey(o => o.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(o => o.MaterialLot).WithMany()
            .HasForeignKey(o => o.MaterialLotId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(o => o.RequirementProfile).WithMany()
            .HasForeignKey(o => o.RequirementProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaterialRequirementAssessmentEntityConfiguration : IEntityTypeConfiguration<Material.MaterialRequirementAssessment>
{
    public void Configure(EntityTypeBuilder<Material.MaterialRequirementAssessment> entity)
    {
        entity.ToTable("material_requirement_assessments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.MaterialId).HasColumnName("material_id");
        entity.Property(a => a.RequirementProfileId).HasColumnName("requirement_profile_id");
        entity.Property(a => a.AssessmentType).HasColumnName("assessment_type").HasMaxLength(30)
            .HasConversion(
                v => Material.MaterialRequirementValueConversions.ToStorage(v),
                v => Material.MaterialRequirementValueConversions.AssessmentTypeFromStorage(v));
        entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50);
        entity.Property(a => a.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(64);
        entity.Property(a => a.StructuredDrivers).HasColumnName("structured_drivers").HasColumnType("jsonb");
        entity.Property(a => a.StructuredEvidence).HasColumnName("structured_evidence").HasColumnType("jsonb");
        entity.Property(a => a.TheoreticalRequirementGrams).HasColumnName("theoretical_requirement_grams").HasPrecision(18, 4);
        entity.Property(a => a.ObservedConsumptionGrams).HasColumnName("observed_consumption_grams").HasPrecision(18, 4);
        entity.Property(a => a.VarianceGrams).HasColumnName("variance_grams").HasPrecision(18, 4);
        entity.Property(a => a.VariancePercent).HasColumnName("variance_percent").HasPrecision(8, 4);
        entity.Property(a => a.VirginRequirementGrams).HasColumnName("virgin_requirement_grams").HasPrecision(18, 4);
        entity.Property(a => a.RegrindRequirementGrams).HasColumnName("regrind_requirement_grams").HasPrecision(18, 4);
        entity.Property(a => a.ErpAvailableQuantity).HasColumnName("erp_available_quantity").HasPrecision(18, 4);
        entity.Property(a => a.ErpReservedQuantity).HasColumnName("erp_reserved_quantity").HasPrecision(18, 4);
        entity.Property(a => a.ErpUnit).HasColumnName("erp_unit").HasMaxLength(20);
        entity.Property(a => a.AvailableAfterReservationQuantity).HasColumnName("available_after_reservation_quantity").HasPrecision(18, 4);
        entity.Property(a => a.ShortageQuantity).HasColumnName("shortage_quantity").HasPrecision(18, 4);
        entity.Property(a => a.ErpSourceUpdatedAtUtc).HasColumnName("erp_source_updated_at_utc");
        entity.Property(a => a.SourceFreshness).HasColumnName("source_freshness").HasMaxLength(20);
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(a => a.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(a => a.FollowUpStatus).HasColumnName("follow_up_status").HasMaxLength(30);
        entity.Property(a => a.FollowUpUpdatedBy).HasColumnName("follow_up_updated_by").HasMaxLength(200);

        entity.HasIndex(a => new { a.TenantId, a.MaterialId, a.AssessmentType, a.InputFingerprint }).IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.MaterialId, a.IdempotencyKey }).IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.AssessmentType, a.Status, a.EvaluatedAtUtc });
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.MaterialId, a.EvaluatedAtUtc });

        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Material).WithMany()
            .HasForeignKey(a => a.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.RequirementProfile).WithMany()
            .HasForeignKey(a => a.RequirementProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaterialAvailabilityRiskFollowUpAuditEventEntityConfiguration
    : IEntityTypeConfiguration<Material.MaterialAvailabilityRiskFollowUpAuditEvent>
{
    public void Configure(EntityTypeBuilder<Material.MaterialAvailabilityRiskFollowUpAuditEvent> entity)
    {
        entity.ToTable("material_availability_risk_follow_up_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50);
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
        entity.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(200);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.AssessmentId, e.OccurredAtUtc });
        entity.HasOne(e => e.Assessment).WithMany()
            .HasForeignKey(e => e.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
