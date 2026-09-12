using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Energy;

internal sealed class EnergyBaselineProfileEntityConfiguration : IEntityTypeConfiguration<EnergyBaselineProfile>
{
    public void Configure(EntityTypeBuilder<EnergyBaselineProfile> entity)
    {
        entity.ToTable("energy_baseline_profiles");
        entity.HasKey(b => b.Id);
        entity.Property(b => b.Id).HasColumnName("id");
        entity.Property(b => b.TenantId).HasColumnName("tenant_id");
        entity.Property(b => b.FacilityId).HasColumnName("facility_id");
        entity.Property(b => b.BaselineReference).HasColumnName("baseline_reference").HasMaxLength(100).IsRequired();
        entity.Property(b => b.ComparisonDimension).HasColumnName("comparison_dimension").HasMaxLength(50).IsRequired();
        entity.Property(b => b.MeasurementPointId).HasColumnName("measurement_point_id");
        entity.Property(b => b.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(b => b.ShiftReference).HasColumnName("shift_reference").HasMaxLength(200);
        entity.Property(b => b.ProductionContextReference).HasColumnName("production_context_reference").HasMaxLength(200);
        entity.Property(b => b.ReportingWindowDurationSeconds).HasColumnName("reporting_window_duration_seconds");
        entity.Property(b => b.SampleCount).HasColumnName("sample_count");
        entity.Property(b => b.BaselineConsumedKwh).HasColumnName("baseline_consumed_kwh").HasPrecision(18, 4);
        entity.Property(b => b.BaselineMaximumDemandKw).HasColumnName("baseline_maximum_demand_kw").HasPrecision(18, 4);
        entity.Property(b => b.BaselineKwhPerKg).HasColumnName("baseline_kwh_per_kg").HasPrecision(18, 4);
        entity.Property(b => b.BaselineKwhPer1000Parts).HasColumnName("baseline_kwh_per_1000_parts").HasPrecision(18, 4);
        entity.Property(b => b.BaselineKwhPer1000GoodParts).HasColumnName("baseline_kwh_per_1000_good_parts").HasPrecision(18, 4);
        entity.Property(b => b.MedianCenter).HasColumnName("median_center").HasPrecision(18, 4);
        entity.Property(b => b.MadDispersion).HasColumnName("mad_dispersion").HasPrecision(18, 4);
        entity.Property(b => b.NormalizedDispersion).HasColumnName("normalized_dispersion").HasPrecision(18, 4);
        entity.Property(b => b.RelativeTolerance).HasColumnName("relative_tolerance").HasPrecision(18, 6);
        entity.Property(b => b.UpperThresholdConsumedKwh).HasColumnName("upper_threshold_consumed_kwh").HasPrecision(18, 4);
        entity.Property(b => b.UpperThresholdDemandKw).HasColumnName("upper_threshold_demand_kw").HasPrecision(18, 4);
        entity.Property(b => b.CalculationMethod).HasColumnName("calculation_method").HasMaxLength(50).IsRequired();
        entity.Property(b => b.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(b => b.SourceWindowReferencesJson).HasColumnName("source_window_references_json").IsRequired();
        entity.Property(b => b.ValidFromUtc).HasColumnName("valid_from_utc");
        entity.Property(b => b.ValidToUtc).HasColumnName("valid_to_utc");
        entity.Property(b => b.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(b => b.Provenance).HasColumnName("provenance").HasMaxLength(500).IsRequired();
        entity.Property(b => b.EvaluateIdempotencyKey).HasColumnName("evaluate_idempotency_key").HasMaxLength(128);
        entity.Property(b => b.EvaluateIdempotencyPayloadFingerprint).HasColumnName("evaluate_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(b => b.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(b => b.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(b => b.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(b => b.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(b => b.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(b => b.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(b => new { b.TenantId, b.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<EnergyMeasurementPoint>().WithMany()
            .HasForeignKey(b => b.MeasurementPointId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(b => new { b.TenantId, b.FacilityId, b.BaselineReference })
            .HasDatabaseName("ux_energy_baseline_profiles_tenant_facility_reference")
            .IsUnique();
        entity.HasIndex(b => new { b.TenantId, b.EvaluateIdempotencyKey })
            .HasDatabaseName("ux_energy_baseline_profiles_tenant_evaluate_idempotency")
            .IsUnique()
            .HasFilter("evaluate_idempotency_key IS NOT NULL");
    }
}

internal sealed class EnergyTargetEntityConfiguration : IEntityTypeConfiguration<EnergyTarget>
{
    public void Configure(EntityTypeBuilder<EnergyTarget> entity)
    {
        entity.ToTable("energy_targets");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).HasColumnName("id");
        entity.Property(t => t.TenantId).HasColumnName("tenant_id");
        entity.Property(t => t.FacilityId).HasColumnName("facility_id");
        entity.Property(t => t.TargetReference).HasColumnName("target_reference").HasMaxLength(100).IsRequired();
        entity.Property(t => t.TargetScope).HasColumnName("target_scope").HasMaxLength(50).IsRequired();
        entity.Property(t => t.ScopeReference).HasColumnName("scope_reference").HasMaxLength(200).IsRequired();
        entity.Property(t => t.TargetMetric).HasColumnName("target_metric").HasMaxLength(50).IsRequired();
        entity.Property(t => t.TargetValue).HasColumnName("target_value").HasPrecision(18, 4);
        entity.Property(t => t.Unit).HasColumnName("unit").HasMaxLength(30).IsRequired();
        entity.Property(t => t.Tolerance).HasColumnName("tolerance").HasPrecision(18, 6);
        entity.Property(t => t.EffectiveFromUtc).HasColumnName("effective_from_utc");
        entity.Property(t => t.EffectiveToUtc).HasColumnName("effective_to_utc");
        entity.Property(t => t.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(t => t.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(t => t.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(t => t.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(t => t.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(t => t.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(t => new { t.TenantId, t.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.TargetReference })
            .HasDatabaseName("ux_energy_targets_tenant_facility_reference")
            .IsUnique();
        entity.HasIndex(t => new { t.TenantId, t.CreateIdempotencyKey })
            .HasDatabaseName("ux_energy_targets_tenant_create_idempotency")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
    }
}

internal sealed class EnergyIntelligenceAssessmentEntityConfiguration : IEntityTypeConfiguration<EnergyIntelligenceAssessment>
{
    public void Configure(EntityTypeBuilder<EnergyIntelligenceAssessment> entity)
    {
        entity.ToTable("energy_intelligence_assessments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.AssessmentReference).HasColumnName("assessment_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.ComparisonDimension).HasColumnName("comparison_dimension").HasMaxLength(50).IsRequired();
        entity.Property(a => a.MeasurementPointId).HasColumnName("measurement_point_id");
        entity.Property(a => a.ConsumptionWindowId).HasColumnName("consumption_window_id");
        entity.Property(a => a.BaselineProfileId).HasColumnName("baseline_profile_id");
        entity.Property(a => a.TargetId).HasColumnName("target_id");
        entity.Property(a => a.EvaluationWindowStartUtc).HasColumnName("evaluation_window_start_utc");
        entity.Property(a => a.EvaluationWindowEndUtc).HasColumnName("evaluation_window_end_utc");
        entity.Property(a => a.ActualConsumedKwh).HasColumnName("actual_consumed_kwh").HasPrecision(18, 4);
        entity.Property(a => a.ActualMaximumDemandKw).HasColumnName("actual_maximum_demand_kw").HasPrecision(18, 4);
        entity.Property(a => a.ActualKwhPer1000GoodParts).HasColumnName("actual_kwh_per_1000_good_parts").HasPrecision(18, 4);
        entity.Property(a => a.VarianceConsumedKwh).HasColumnName("variance_consumed_kwh").HasPrecision(18, 4);
        entity.Property(a => a.VariancePercent).HasColumnName("variance_percent").HasPrecision(18, 4);
        entity.Property(a => a.PotentialReducibleKwh).HasColumnName("potential_reducible_kwh").HasPrecision(18, 4);
        entity.Property(a => a.EstimatedCostImpact).HasColumnName("estimated_cost_impact").HasPrecision(18, 4);
        entity.Property(a => a.CostCurrencyCode).HasColumnName("cost_currency_code").HasMaxLength(10);
        entity.Property(a => a.OpportunityLabel).HasColumnName("opportunity_label").HasMaxLength(100);
        entity.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        entity.Property(a => a.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(100);
        entity.Property(a => a.LeadingDriverId).HasColumnName("leading_driver_id");
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(a => a.CalculationMethod).HasColumnName("calculation_method").HasMaxLength(50).IsRequired();
        entity.Property(a => a.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20).IsRequired();
        entity.Property(a => a.EvaluateIdempotencyKey).HasColumnName("evaluate_idempotency_key").HasMaxLength(128);
        entity.Property(a => a.EvaluateIdempotencyPayloadFingerprint).HasColumnName("evaluate_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<EnergyBaselineProfile>().WithMany()
            .HasForeignKey(a => a.BaselineProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<EnergyTarget>().WithMany()
            .HasForeignKey(a => a.TargetId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<EnergyConsumptionWindow>().WithMany()
            .HasForeignKey(a => a.ConsumptionWindowId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.AssessmentReference })
            .HasDatabaseName("ux_energy_intelligence_assessments_tenant_facility_reference")
            .IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.EvaluateIdempotencyKey })
            .HasDatabaseName("ux_energy_intelligence_assessments_tenant_evaluate_idempotency")
            .IsUnique()
            .HasFilter("evaluate_idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.EvaluatedAtUtc })
            .HasDatabaseName("ix_energy_intelligence_assessments_tenant_facility_evaluated");
    }
}

internal sealed class EnergyIntelligenceDriverEntityConfiguration : IEntityTypeConfiguration<EnergyIntelligenceDriver>
{
    public void Configure(EntityTypeBuilder<EnergyIntelligenceDriver> entity)
    {
        entity.ToTable("energy_intelligence_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.AssessmentId).HasColumnName("assessment_id");
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.LabelTr).HasColumnName("label_tr").HasMaxLength(200).IsRequired();
        entity.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(d => d.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        entity.Property(d => d.ActualValue).HasColumnName("actual_value").HasMaxLength(100);
        entity.Property(d => d.ActualUnit).HasColumnName("actual_unit").HasMaxLength(30);
        entity.Property(d => d.ComparisonValue).HasColumnName("comparison_value").HasMaxLength(100);
        entity.Property(d => d.ComparisonUnit).HasColumnName("comparison_unit").HasMaxLength(30);
        entity.Property(d => d.NormalizedVariance).HasColumnName("normalized_variance").HasPrecision(18, 4);
        entity.Property(d => d.StructuredEvidenceJson).HasColumnName("structured_evidence_json").IsRequired();
        entity.Property(d => d.EvidenceSource).HasColumnName("evidence_source").HasMaxLength(100).IsRequired();
        entity.Property(d => d.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20);
        entity.Property(d => d.Explanation).HasColumnName("explanation").HasMaxLength(500).IsRequired();
        entity.Property(d => d.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(d => d.IsLeadingDriver).HasColumnName("is_leading_driver");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(d => d.Assessment).WithMany(a => a.Drivers)
            .HasForeignKey(d => d.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.AssessmentId, d.ReasonCode })
            .HasDatabaseName("ix_energy_intelligence_drivers_assessment_reason");
    }
}
