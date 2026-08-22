using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.IotPlc;

internal sealed class IotPlcQualityProfileEntityConfiguration : IEntityTypeConfiguration<IotPlcQualityProfile>
{
    public void Configure(EntityTypeBuilder<IotPlcQualityProfile> builder)
    {
        builder.ToTable("iot_plc_quality_profiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.FacilityId).HasColumnName("facility_id");
        builder.Property(p => p.ProfileCode).HasColumnName("profile_code").HasMaxLength(100);
        builder.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(p => p.ExpectedObservationIntervalSeconds).HasColumnName("expected_observation_interval_seconds");
        builder.Property(p => p.FreshnessToleranceSeconds).HasColumnName("freshness_tolerance_seconds");
        builder.Property(p => p.StaleThresholdSeconds).HasColumnName("stale_threshold_seconds");
        builder.Property(p => p.MaximumClockSkewToleranceSeconds).HasColumnName("maximum_clock_skew_tolerance_seconds");
        builder.Property(p => p.MinimumExpectedObservationCount).HasColumnName("minimum_expected_observation_count");
        builder.Property(p => p.MinimumGoodQualityRatio).HasColumnName("minimum_good_quality_ratio").HasPrecision(5, 4);
        builder.Property(p => p.MaximumBadQualityRatio).HasColumnName("maximum_bad_quality_ratio").HasPrecision(5, 4);
        builder.Property(p => p.MaximumUncertainQualityRatio).HasColumnName("maximum_uncertain_quality_ratio").HasPrecision(5, 4);
        builder.Property(p => p.MaximumSourceConflictRatio).HasColumnName("maximum_source_conflict_ratio").HasPrecision(5, 4);
        builder.Property(p => p.SequenceGapEvaluationEnabled).HasColumnName("sequence_gap_evaluation_enabled");
        builder.Property(p => p.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(p => p.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500);
        builder.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);

        builder.HasIndex(p => new { p.TenantId, p.FacilityId, p.ProfileCode })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_quality_profiles_tenant_facility_code");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IotPlcConnectivityAssessmentEntityConfiguration : IEntityTypeConfiguration<IotPlcConnectivityAssessment>
{
    public void Configure(EntityTypeBuilder<IotPlcConnectivityAssessment> builder)
    {
        builder.ToTable("iot_plc_connectivity_assessments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.FacilityId).HasColumnName("facility_id");
        builder.Property(a => a.TargetType).HasColumnName("target_type").HasMaxLength(50);
        builder.Property(a => a.TargetId).HasColumnName("target_id");
        builder.Property(a => a.EvaluationWindowStartUtc).HasColumnName("evaluation_window_start_utc");
        builder.Property(a => a.EvaluationWindowEndUtc).HasColumnName("evaluation_window_end_utc");
        builder.Property(a => a.QualityProfileId).HasColumnName("quality_profile_id");
        builder.Property(a => a.EvidenceMode).HasColumnName("evidence_mode").HasMaxLength(20);
        builder.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30);
        builder.Property(a => a.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(100);
        builder.Property(a => a.LeadingDriverId).HasColumnName("leading_driver_id");
        builder.Property(a => a.LatestHealthState).HasColumnName("latest_health_state").HasMaxLength(20);
        builder.Property(a => a.HealthObservationAvailabilityRatio).HasColumnName("health_observation_availability_ratio").HasPrecision(5, 2);
        builder.Property(a => a.FreshnessSeconds).HasColumnName("freshness_seconds");
        builder.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50);
        builder.Property(a => a.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500);
        builder.Property(a => a.EvaluateIdempotencyKey).HasColumnName("evaluate_idempotency_key").HasMaxLength(128);
        builder.Property(a => a.EvaluateIdempotencyPayloadFingerprint).HasColumnName("evaluate_idempotency_payload_fingerprint").HasMaxLength(64);
        builder.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        builder.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200);

        builder.HasIndex(a => new { a.TenantId, a.EvaluateIdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_connectivity_assessments_tenant_idempotency")
            .HasFilter("evaluate_idempotency_key IS NOT NULL");

        builder.HasIndex(a => new { a.TargetId, a.EvaluatedAtUtc })
            .HasDatabaseName("ix_iot_plc_connectivity_assessments_target_evaluated");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IotPlcQualityProfile>().WithMany()
            .HasForeignKey(a => a.QualityProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IotPlcSignalQualityAssessmentEntityConfiguration : IEntityTypeConfiguration<IotPlcSignalQualityAssessment>
{
    public void Configure(EntityTypeBuilder<IotPlcSignalQualityAssessment> builder)
    {
        builder.ToTable("iot_plc_signal_quality_assessments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.FacilityId).HasColumnName("facility_id");
        builder.Property(a => a.TargetType).HasColumnName("target_type").HasMaxLength(50);
        builder.Property(a => a.TargetId).HasColumnName("target_id");
        builder.Property(a => a.SignalDefinitionId).HasColumnName("signal_definition_id");
        builder.Property(a => a.EndpointId).HasColumnName("endpoint_id");
        builder.Property(a => a.EvaluationWindowStartUtc).HasColumnName("evaluation_window_start_utc");
        builder.Property(a => a.EvaluationWindowEndUtc).HasColumnName("evaluation_window_end_utc");
        builder.Property(a => a.QualityProfileId).HasColumnName("quality_profile_id");
        builder.Property(a => a.EvidenceMode).HasColumnName("evidence_mode").HasMaxLength(20);
        builder.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30);
        builder.Property(a => a.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(100);
        builder.Property(a => a.LeadingDriverId).HasColumnName("leading_driver_id");
        builder.Property(a => a.ActualObservationCount).HasColumnName("actual_observation_count");
        builder.Property(a => a.ExpectedObservationCount).HasColumnName("expected_observation_count");
        builder.Property(a => a.CompletenessRatio).HasColumnName("completeness_ratio").HasPrecision(5, 4);
        builder.Property(a => a.GoodCount).HasColumnName("good_count");
        builder.Property(a => a.UncertainCount).HasColumnName("uncertain_count");
        builder.Property(a => a.BadCount).HasColumnName("bad_count");
        builder.Property(a => a.StaleCount).HasColumnName("stale_count");
        builder.Property(a => a.SourceConflictCount).HasColumnName("source_conflict_count");
        builder.Property(a => a.GoodRatio).HasColumnName("good_ratio").HasPrecision(5, 2);
        builder.Property(a => a.UncertainRatio).HasColumnName("uncertain_ratio").HasPrecision(5, 2);
        builder.Property(a => a.BadRatio).HasColumnName("bad_ratio").HasPrecision(5, 2);
        builder.Property(a => a.StaleRatio).HasColumnName("stale_ratio").HasPrecision(5, 2);
        builder.Property(a => a.SourceConflictRatio).HasColumnName("source_conflict_ratio").HasPrecision(5, 2);
        builder.Property(a => a.LatestValidObservedAtUtc).HasColumnName("latest_valid_observed_at_utc");
        builder.Property(a => a.FreshnessSeconds).HasColumnName("freshness_seconds");
        builder.Property(a => a.MaxClockSkewSeconds).HasColumnName("max_clock_skew_seconds");
        builder.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50);
        builder.Property(a => a.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500);
        builder.Property(a => a.EvaluateIdempotencyKey).HasColumnName("evaluate_idempotency_key").HasMaxLength(128);
        builder.Property(a => a.EvaluateIdempotencyPayloadFingerprint).HasColumnName("evaluate_idempotency_payload_fingerprint").HasMaxLength(64);
        builder.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        builder.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200);

        builder.HasIndex(a => new { a.TenantId, a.EvaluateIdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_signal_quality_assessments_tenant_idempotency")
            .HasFilter("evaluate_idempotency_key IS NOT NULL");

        builder.HasIndex(a => new { a.SignalDefinitionId, a.EvaluatedAtUtc })
            .HasDatabaseName("ix_iot_plc_signal_quality_assessments_signal_evaluated");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IotPlcQualityProfile>().WithMany()
            .HasForeignKey(a => a.QualityProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IotPlcSignal>().WithMany()
            .HasForeignKey(a => a.SignalDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IotPlcIntelligenceDriverEntityConfiguration : IEntityTypeConfiguration<IotPlcIntelligenceDriver>
{
    public void Configure(EntityTypeBuilder<IotPlcIntelligenceDriver> builder)
    {
        builder.ToTable("iot_plc_intelligence_drivers");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.TenantId).HasColumnName("tenant_id");
        builder.Property(d => d.FacilityId).HasColumnName("facility_id");
        builder.Property(d => d.AssessmentKind).HasColumnName("assessment_kind").HasMaxLength(30);
        builder.Property(d => d.AssessmentId).HasColumnName("assessment_id");
        builder.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        builder.Property(d => d.OutcomeSeverity).HasColumnName("outcome_severity");
        builder.Property(d => d.ThresholdVariance).HasColumnName("threshold_variance").HasPrecision(18, 6);
        builder.Property(d => d.AffectedObservationCount).HasColumnName("affected_observation_count");
        builder.Property(d => d.StructuredEvidenceJson).HasColumnName("structured_evidence_json");
        builder.Property(d => d.Explanation).HasColumnName("explanation").HasMaxLength(500);
        builder.Property(d => d.IsLeadingDriver).HasColumnName("is_leading_driver");
        builder.Property(d => d.SortOrder).HasColumnName("sort_order");
        builder.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(d => d.CreatedBy).HasColumnName("created_by").HasMaxLength(200);

        builder.HasIndex(d => new { d.AssessmentKind, d.AssessmentId })
            .HasDatabaseName("ix_iot_plc_intelligence_drivers_assessment");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
