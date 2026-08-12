using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.ProcessOee;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ProcessOeeEvaluationWindowEntityConfiguration : IEntityTypeConfiguration<ProcessOeeEvaluationWindow>
{
    public void Configure(EntityTypeBuilder<ProcessOeeEvaluationWindow> entity)
    {
        entity.ToTable("process_oee_evaluation_windows");
        entity.HasKey(w => w.Id);
        entity.Property(w => w.Id).HasColumnName("id");
        entity.Property(w => w.TenantId).HasColumnName("tenant_id");
        entity.Property(w => w.FacilityId).HasColumnName("facility_id");
        entity.Property(w => w.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(w => w.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200);
        entity.Property(w => w.MoldReference).HasColumnName("mold_reference").HasMaxLength(200);
        entity.Property(w => w.WindowStartUtc).HasColumnName("window_start_utc");
        entity.Property(w => w.WindowEndUtc).HasColumnName("window_end_utc");
        entity.Property(w => w.PlannedProductionSeconds).HasColumnName("planned_production_seconds");
        entity.Property(w => w.IdealCycleTimeSeconds).HasColumnName("ideal_cycle_time_seconds").HasPrecision(18, 4);
        entity.Property(w => w.EffectiveCavityCount).HasColumnName("effective_cavity_count");
        entity.Property(w => w.SourceType).HasColumnName("source_type").HasMaxLength(20).IsRequired();
        entity.Property(w => w.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(w => w.DataRecordedAtUtc).HasColumnName("data_recorded_at_utc");
        entity.Property(w => w.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(w => w.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(w => w.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(w => w.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(w => w.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(w => w.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(w => w.Version).HasColumnName("version");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(w => w.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(w => new { w.TenantId, w.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(w => new { w.TenantId, w.CreateIdempotencyKey })
            .HasDatabaseName("ix_process_oee_windows_tenant_create_idempotency_key")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(w => new { w.TenantId, w.FacilityId, w.MachineAssetId })
            .HasDatabaseName("ix_process_oee_windows_tenant_facility_machine");
        entity.HasIndex(w => new { w.TenantId, w.FacilityId, w.WindowEndUtc })
            .HasDatabaseName("ix_process_oee_windows_tenant_facility_window_end");
    }
}

internal sealed class ProcessOeeAssessmentEntityConfiguration : IEntityTypeConfiguration<ProcessOeeAssessment>
{
    public void Configure(EntityTypeBuilder<ProcessOeeAssessment> entity)
    {
        entity.ToTable("process_oee_assessments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(a => a.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200);
        entity.Property(a => a.MoldReference).HasColumnName("mold_reference").HasMaxLength(200);
        entity.Property(a => a.EvaluationWindowId).HasColumnName("evaluation_window_id");
        entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50).IsRequired();
        entity.Property(a => a.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(a => a.WindowStartUtc).HasColumnName("window_start_utc");
        entity.Property(a => a.WindowEndUtc).HasColumnName("window_end_utc");
        entity.Property(a => a.PlannedProductionSeconds).HasColumnName("planned_production_seconds");
        entity.Property(a => a.OperatingSeconds).HasColumnName("operating_seconds");
        entity.Property(a => a.DowntimeSeconds).HasColumnName("downtime_seconds");
        entity.Property(a => a.UnknownSeconds).HasColumnName("unknown_seconds");
        entity.Property(a => a.ObservedCycleCount).HasColumnName("observed_cycle_count");
        entity.Property(a => a.DerivedShotCount).HasColumnName("derived_shot_count");
        entity.Property(a => a.TotalPieceCount).HasColumnName("total_piece_count");
        entity.Property(a => a.GoodPieceCount).HasColumnName("good_piece_count");
        entity.Property(a => a.ScrapPieceCount).HasColumnName("scrap_piece_count");
        entity.Property(a => a.AvailabilityPercent).HasColumnName("availability_percent").HasPrecision(8, 2);
        entity.Property(a => a.PerformanceRawPercent).HasColumnName("performance_raw_percent").HasPrecision(8, 2);
        entity.Property(a => a.PerformancePercent).HasColumnName("performance_percent").HasPrecision(8, 2);
        entity.Property(a => a.QualityPercent).HasColumnName("quality_percent").HasPrecision(8, 2);
        entity.Property(a => a.OeePercent).HasColumnName("oee_percent").HasPrecision(8, 2);
        entity.Property(a => a.StructuredDrivers).HasColumnName("structured_drivers").IsRequired();
        entity.Property(a => a.StructuredEvidence).HasColumnName("structured_evidence").IsRequired();
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(a => a.Version).HasColumnName("version");
        entity.HasOne(a => a.EvaluationWindow)
            .WithMany()
            .HasForeignKey(a => a.EvaluationWindowId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.MachineAssetId, a.InputFingerprint })
            .HasDatabaseName("ix_process_oee_assessments_tenant_machine_fingerprint")
            .IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.MachineAssetId, a.IdempotencyKey })
            .HasDatabaseName("ix_process_oee_assessments_tenant_machine_idempotency_key")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.WindowEndUtc, a.EvaluatedAtUtc })
            .HasDatabaseName("ix_process_oee_assessments_tenant_facility_window_evaluated");
        entity.HasIndex(a => a.EvaluationWindowId)
            .HasDatabaseName("ix_process_oee_assessments_evaluation_window_id");
    }
}
