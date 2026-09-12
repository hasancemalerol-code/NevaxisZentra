using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.ProcessOee;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

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

internal sealed class ProcessOeeLossDriverEntityConfiguration : IEntityTypeConfiguration<ProcessOeeLossDriver>
{
    public void Configure(EntityTypeBuilder<ProcessOeeLossDriver> entity)
    {
        entity.ToTable("process_oee_loss_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.AssessmentId).HasColumnName("assessment_id");
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.Category).HasColumnName("category").HasMaxLength(40).IsRequired();
        entity.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(d => d.OperationalPriority).HasColumnName("operational_priority");
        entity.Property(d => d.ObservedValue).HasColumnName("observed_value").HasPrecision(18, 4);
        entity.Property(d => d.ExpectedValue).HasColumnName("expected_value").HasPrecision(18, 4);
        entity.Property(d => d.AbsoluteLossValue).HasColumnName("absolute_loss_value").HasPrecision(18, 4);
        entity.Property(d => d.AffectedDurationSeconds).HasColumnName("affected_duration_seconds");
        entity.Property(d => d.AffectedQuantity).HasColumnName("affected_quantity");
        entity.Property(d => d.SourceReferencesJson).HasColumnName("source_references_json").IsRequired();
        entity.Property(d => d.EvidenceTimestampUtc).HasColumnName("evidence_timestamp_utc");
        entity.Property(d => d.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20).IsRequired();
        entity.Property(d => d.ExplanationCode).HasColumnName("explanation_code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.DataLimitationsJson).HasColumnName("data_limitations_json").IsRequired();
        entity.Property(d => d.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50).IsRequired();
        entity.Property(d => d.IsLeadingLoss).HasColumnName("is_leading_loss");
        entity.Property(d => d.NormalizedImpactPoints).HasColumnName("normalized_impact_points").HasPrecision(8, 2);
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(d => d.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.HasOne(d => d.Assessment)
            .WithMany(a => a.LossDrivers)
            .HasForeignKey(d => d.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.AssessmentId, d.ReasonCode })
            .HasDatabaseName("ix_process_oee_loss_drivers_tenant_assessment_reason")
            .IsUnique();
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.AssessmentId })
            .HasDatabaseName("ix_process_oee_loss_drivers_tenant_facility_assessment");
    }
}

internal sealed class ProcessOeeStabilityAssessmentEntityConfiguration : IEntityTypeConfiguration<ProcessOeeStabilityAssessment>
{
    public void Configure(EntityTypeBuilder<ProcessOeeStabilityAssessment> entity)
    {
        entity.ToTable("process_oee_stability_assessments");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.TenantId).HasColumnName("tenant_id");
        entity.Property(s => s.FacilityId).HasColumnName("facility_id");
        entity.Property(s => s.AssessmentId).HasColumnName("assessment_id");
        entity.Property(s => s.ResultState).HasColumnName("result_state").HasMaxLength(30).IsRequired();
        entity.Property(s => s.MedianCycleMs).HasColumnName("median_cycle_ms");
        entity.Property(s => s.MadCycleMs).HasColumnName("mad_cycle_ms");
        entity.Property(s => s.SampleCount).HasColumnName("sample_count");
        entity.Property(s => s.DriftDirection).HasColumnName("drift_direction").HasMaxLength(20).IsRequired();
        entity.Property(s => s.MicroStopCount).HasColumnName("micro_stop_count");
        entity.Property(s => s.MicroStopTotalSeconds).HasColumnName("micro_stop_total_seconds");
        entity.Property(s => s.MicroStopMedianSeconds).HasColumnName("micro_stop_median_seconds");
        entity.Property(s => s.ClassifiedDowntimeSeconds).HasColumnName("classified_downtime_seconds");
        entity.Property(s => s.UnclassifiedDowntimeSeconds).HasColumnName("unclassified_downtime_seconds");
        entity.Property(s => s.EvidenceFingerprint).HasColumnName("evidence_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(s => s.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50).IsRequired();
        entity.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(s => s.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.HasOne(s => s.Assessment)
            .WithOne(a => a.StabilityAssessment)
            .HasForeignKey<ProcessOeeStabilityAssessment>(s => s.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(s => new { s.TenantId, s.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(s => new { s.TenantId, s.AssessmentId })
            .HasDatabaseName("ix_process_oee_stability_assessments_tenant_assessment")
            .IsUnique();
        entity.HasIndex(s => new { s.TenantId, s.EvidenceFingerprint })
            .HasDatabaseName("ix_process_oee_stability_assessments_tenant_fingerprint")
            .IsUnique();
        entity.HasIndex(s => new { s.TenantId, s.FacilityId, s.ResultState })
            .HasDatabaseName("ix_process_oee_stability_assessments_tenant_facility_state");
    }
}

internal sealed class ProcessOeeStabilityDriverEntityConfiguration : IEntityTypeConfiguration<ProcessOeeStabilityDriver>
{
    public void Configure(EntityTypeBuilder<ProcessOeeStabilityDriver> entity)
    {
        entity.ToTable("process_oee_stability_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.AssessmentId).HasColumnName("assessment_id");
        entity.Property(d => d.StabilityAssessmentId).HasColumnName("stability_assessment_id");
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.Category).HasColumnName("category").HasMaxLength(40).IsRequired();
        entity.Property(d => d.ResultState).HasColumnName("result_state").HasMaxLength(30).IsRequired();
        entity.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(d => d.OperationalPriority).HasColumnName("operational_priority");
        entity.Property(d => d.ObservedValue).HasColumnName("observed_value").HasPrecision(18, 4);
        entity.Property(d => d.ReferenceValue).HasColumnName("reference_value").HasPrecision(18, 4);
        entity.Property(d => d.Unit).HasColumnName("unit").HasMaxLength(20).IsRequired();
        entity.Property(d => d.SampleCount).HasColumnName("sample_count");
        entity.Property(d => d.TimeRangeStartUtc).HasColumnName("time_range_start_utc");
        entity.Property(d => d.TimeRangeEndUtc).HasColumnName("time_range_end_utc");
        entity.Property(d => d.SourceReferencesJson).HasColumnName("source_references_json").IsRequired();
        entity.Property(d => d.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20).IsRequired();
        entity.Property(d => d.ExplanationCode).HasColumnName("explanation_code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.DataLimitationsJson).HasColumnName("data_limitations_json").IsRequired();
        entity.Property(d => d.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50).IsRequired();
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(d => d.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.HasOne(d => d.Assessment)
            .WithMany(a => a.StabilityDrivers)
            .HasForeignKey(d => d.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(d => d.StabilityAssessment)
            .WithMany(s => s.Drivers)
            .HasForeignKey(d => d.StabilityAssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.AssessmentId, d.ReasonCode })
            .HasDatabaseName("ix_process_oee_stability_drivers_tenant_assessment_reason")
            .IsUnique();
    }
}

internal sealed class ProcessOeeDowntimeBreakdownRowEntityConfiguration : IEntityTypeConfiguration<ProcessOeeDowntimeBreakdownRow>
{
    public void Configure(EntityTypeBuilder<ProcessOeeDowntimeBreakdownRow> entity)
    {
        entity.ToTable("process_oee_downtime_breakdown_rows");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.AssessmentId).HasColumnName("assessment_id");
        entity.Property(r => r.StabilityAssessmentId).HasColumnName("stability_assessment_id");
        entity.Property(r => r.ReasonCode).HasColumnName("reason_code").HasMaxLength(100).IsRequired();
        entity.Property(r => r.DurationSeconds).HasColumnName("duration_seconds");
        entity.Property(r => r.OccurrenceCount).HasColumnName("occurrence_count");
        entity.Property(r => r.SortRank).HasColumnName("sort_rank");
        entity.Property(r => r.IsClassified).HasColumnName("is_classified");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.HasOne(r => r.Assessment)
            .WithMany(a => a.DowntimeBreakdownRows)
            .HasForeignKey(r => r.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.StabilityAssessment)
            .WithMany(s => s.DowntimeBreakdownRows)
            .HasForeignKey(r => r.StabilityAssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.AssessmentId, r.ReasonCode })
            .HasDatabaseName("ix_process_oee_downtime_breakdown_tenant_assessment_reason")
            .IsUnique();
    }
}

internal sealed class ProcessOeeShiftComparisonBatchEntityConfiguration : IEntityTypeConfiguration<ProcessOeeShiftComparisonBatch>
{
    public void Configure(EntityTypeBuilder<ProcessOeeShiftComparisonBatch> entity)
    {
        entity.ToTable("process_oee_shift_comparison_batches");
        entity.HasKey(b => b.Id);
        entity.Property(b => b.Id).HasColumnName("id");
        entity.Property(b => b.TenantId).HasColumnName("tenant_id");
        entity.Property(b => b.FacilityId).HasColumnName("facility_id");
        entity.Property(b => b.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(b => b.ComparisonPeriodStartUtc).HasColumnName("comparison_period_start_utc");
        entity.Property(b => b.ComparisonPeriodEndUtc).HasColumnName("comparison_period_end_utc");
        entity.Property(b => b.IsComparable).HasColumnName("is_comparable");
        entity.Property(b => b.ComparabilityWarningCode).HasColumnName("comparability_warning_code").HasMaxLength(100);
        entity.Property(b => b.SummaryJson).HasColumnName("summary_json").IsRequired();
        entity.Property(b => b.EvidenceFingerprint).HasColumnName("evidence_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(b => b.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50).IsRequired();
        entity.Property(b => b.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(b => b.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(b => b.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(b => b.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(b => new { b.TenantId, b.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(b => new { b.TenantId, b.IdempotencyKey })
            .HasDatabaseName("ix_process_oee_shift_comparison_batches_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
    }
}

internal sealed class ProcessOeeShiftComparisonRowEntityConfiguration : IEntityTypeConfiguration<ProcessOeeShiftComparisonRow>
{
    public void Configure(EntityTypeBuilder<ProcessOeeShiftComparisonRow> entity)
    {
        entity.ToTable("process_oee_shift_comparison_rows");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.BatchId).HasColumnName("batch_id");
        entity.Property(r => r.AssessmentId).HasColumnName("assessment_id");
        entity.Property(r => r.ShiftCode).HasColumnName("shift_code").HasMaxLength(50).IsRequired();
        entity.Property(r => r.ShiftLabel).HasColumnName("shift_label").HasMaxLength(200).IsRequired();
        entity.Property(r => r.WindowStartUtc).HasColumnName("window_start_utc");
        entity.Property(r => r.WindowEndUtc).HasColumnName("window_end_utc");
        entity.Property(r => r.PlannedProductionSeconds).HasColumnName("planned_production_seconds");
        entity.Property(r => r.OperatingSeconds).HasColumnName("operating_seconds");
        entity.Property(r => r.AvailabilityPercent).HasColumnName("availability_percent").HasPrecision(8, 2);
        entity.Property(r => r.PerformancePercent).HasColumnName("performance_percent").HasPrecision(8, 2);
        entity.Property(r => r.QualityPercent).HasColumnName("quality_percent").HasPrecision(8, 2);
        entity.Property(r => r.OeePercent).HasColumnName("oee_percent").HasPrecision(8, 2);
        entity.Property(r => r.StabilityResultState).HasColumnName("stability_result_state").HasMaxLength(30);
        entity.Property(r => r.DowntimeSeconds).HasColumnName("downtime_seconds");
        entity.Property(r => r.MicroStopCount).HasColumnName("micro_stop_count");
        entity.Property(r => r.DataSufficiency).HasColumnName("data_sufficiency").HasMaxLength(30).IsRequired();
        entity.Property(r => r.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20).IsRequired();
        entity.Property(r => r.IsComparable).HasColumnName("is_comparable");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.HasOne(r => r.Batch)
            .WithMany(b => b.Rows)
            .HasForeignKey(r => r.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.BatchId, r.AssessmentId })
            .HasDatabaseName("ix_process_oee_shift_comparison_rows_batch_assessment")
            .IsUnique();
    }
}
