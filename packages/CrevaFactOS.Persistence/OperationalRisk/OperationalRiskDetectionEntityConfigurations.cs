using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.OperationalRisk;

internal sealed class OperationalRiskDetectionProfileEntityConfiguration : IEntityTypeConfiguration<OperationalRiskDetectionProfile>
{
    public void Configure(EntityTypeBuilder<OperationalRiskDetectionProfile> entity)
    {
        entity.ToTable("operational_risk_detection_profiles");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.ProfileCode).HasColumnName("profile_code").HasMaxLength(100).IsRequired();
        entity.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        entity.Property(p => p.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(p => p.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(p => p.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(p => p.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(p => p.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(p => new { p.TenantId, p.ProfileCode })
            .HasDatabaseName("ux_operational_risk_detection_profiles_tenant_code")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.CreateIdempotencyKey })
            .HasDatabaseName("ux_operational_risk_detection_profiles_tenant_create_idempotency")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Status })
            .HasDatabaseName("ix_operational_risk_detection_profiles_tenant_facility_status");
    }
}

internal sealed class OperationalRiskDetectionRuleEntityConfiguration : IEntityTypeConfiguration<OperationalRiskDetectionRule>
{
    public void Configure(EntityTypeBuilder<OperationalRiskDetectionRule> entity)
    {
        entity.ToTable("operational_risk_detection_rules");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ProfileId).HasColumnName("profile_id");
        entity.Property(r => r.RuleCode).HasColumnName("rule_code").HasMaxLength(100).IsRequired();
        entity.Property(r => r.SourceModule).HasColumnName("source_module").HasMaxLength(50).IsRequired();
        entity.Property(r => r.SourceOutcome).HasColumnName("source_outcome").HasMaxLength(50).IsRequired();
        entity.Property(r => r.RiskCategory).HasColumnName("risk_category").HasMaxLength(50).IsRequired();
        entity.Property(r => r.DefaultLikelihood).HasColumnName("default_likelihood");
        entity.Property(r => r.DefaultImpact).HasColumnName("default_impact");
        entity.Property(r => r.ThresholdVarianceBand).HasColumnName("threshold_variance_band").HasPrecision(18, 6);
        entity.Property(r => r.DriverReasonCode).HasColumnName("driver_reason_code").HasMaxLength(50).IsRequired();
        entity.Property(r => r.PriorityOrder).HasColumnName("priority_order");
        entity.Property(r => r.IsActive).HasColumnName("is_active");
        entity.Property(r => r.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.Profile).WithMany(p => p.Rules)
            .HasForeignKey(r => r.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.ProfileId, r.RuleCode })
            .HasDatabaseName("ux_operational_risk_detection_rules_tenant_profile_code")
            .IsUnique();
    }
}

internal sealed class OperationalRiskDetectionRunEntityConfiguration : IEntityTypeConfiguration<OperationalRiskDetectionRun>
{
    public void Configure(EntityTypeBuilder<OperationalRiskDetectionRun> entity)
    {
        entity.ToTable("operational_risk_detection_runs");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.RunReference).HasColumnName("run_reference").HasMaxLength(100).IsRequired();
        entity.Property(r => r.ProfileId).HasColumnName("profile_id");
        entity.Property(r => r.ProfileVersion).HasColumnName("profile_version").HasMaxLength(20).IsRequired();
        entity.Property(r => r.WindowStartUtc).HasColumnName("window_start_utc");
        entity.Property(r => r.WindowEndUtc).HasColumnName("window_end_utc");
        entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(r => r.SourceCount).HasColumnName("source_count");
        entity.Property(r => r.ResultCount).HasColumnName("result_count");
        entity.Property(r => r.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(r => r.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(r => r.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(r => r.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.Profile).WithMany()
            .HasForeignKey(r => r.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.RunReference })
            .HasDatabaseName("ux_operational_risk_detection_runs_tenant_reference")
            .IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.IdempotencyKey })
            .HasDatabaseName("ux_operational_risk_detection_runs_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.WindowStartUtc, r.WindowEndUtc })
            .HasDatabaseName("ix_operational_risk_detection_runs_tenant_facility_window");
    }
}

internal sealed class OperationalRiskDetectionResultEntityConfiguration : IEntityTypeConfiguration<OperationalRiskDetectionResult>
{
    public void Configure(EntityTypeBuilder<OperationalRiskDetectionResult> entity)
    {
        entity.ToTable("operational_risk_detection_results");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.RunId).HasColumnName("run_id");
        entity.Property(r => r.SourceModule).HasColumnName("source_module").HasMaxLength(50).IsRequired();
        entity.Property(r => r.SourceReference).HasColumnName("source_reference").HasMaxLength(200).IsRequired();
        entity.Property(r => r.RiskCategory).HasColumnName("risk_category").HasMaxLength(50).IsRequired();
        entity.Property(r => r.ScopeType).HasColumnName("scope_type").HasMaxLength(50).IsRequired();
        entity.Property(r => r.ScopeReference).HasColumnName("scope_reference").HasMaxLength(200);
        entity.Property(r => r.EvidenceState).HasColumnName("evidence_state").HasMaxLength(30).IsRequired();
        entity.Property(r => r.SourceOutcome).HasColumnName("source_outcome").HasMaxLength(50).IsRequired();
        entity.Property(r => r.DetectionOutcome).HasColumnName("detection_outcome").HasMaxLength(30).IsRequired();
        entity.Property(r => r.ActualValue).HasColumnName("actual_value").HasPrecision(18, 6);
        entity.Property(r => r.ThresholdValue).HasColumnName("threshold_value").HasPrecision(18, 6);
        entity.Property(r => r.Unit).HasColumnName("unit").HasMaxLength(50);
        entity.Property(r => r.NormalizedVariance).HasColumnName("normalized_variance").HasPrecision(18, 6);
        entity.Property(r => r.LikelihoodLevel).HasColumnName("likelihood_level");
        entity.Property(r => r.ImpactLevel).HasColumnName("impact_level");
        entity.Property(r => r.RiskScore).HasColumnName("risk_score");
        entity.Property(r => r.SeverityBand).HasColumnName("severity_band").HasMaxLength(20);
        entity.Property(r => r.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(50);
        entity.Property(r => r.LinkedCaseId).HasColumnName("linked_case_id");
        entity.Property(r => r.DetectionKey).HasColumnName("detection_key").HasMaxLength(64).IsRequired();
        entity.Property(r => r.SourceAssessmentReference).HasColumnName("source_assessment_reference").HasMaxLength(200).IsRequired();
        entity.Property(r => r.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.Run).WithMany(run => run.Results)
            .HasForeignKey(r => r.RunId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<OperationalRiskCase>().WithMany()
            .HasForeignKey(r => r.LinkedCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.DetectionKey })
            .HasDatabaseName("ix_operational_risk_detection_results_tenant_facility_detection_key");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.DetectionOutcome, r.SeverityBand })
            .HasDatabaseName("ix_operational_risk_detection_results_tenant_facility_outcome");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.SourceModule, r.SourceReference })
            .HasDatabaseName("ix_operational_risk_detection_results_tenant_facility_source");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.LinkedCaseId })
            .HasDatabaseName("ix_operational_risk_detection_results_tenant_facility_case");
    }
}

internal sealed class OperationalRiskDetectionDriverEntityConfiguration : IEntityTypeConfiguration<OperationalRiskDetectionDriver>
{
    public void Configure(EntityTypeBuilder<OperationalRiskDetectionDriver> entity)
    {
        entity.ToTable("operational_risk_detection_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.ResultId).HasColumnName("result_id");
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(50).IsRequired();
        entity.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(d => d.ActualValue).HasColumnName("actual_value").HasPrecision(18, 6);
        entity.Property(d => d.ThresholdValue).HasColumnName("threshold_value").HasPrecision(18, 6);
        entity.Property(d => d.Unit).HasColumnName("unit").HasMaxLength(50);
        entity.Property(d => d.NormalizedVariance).HasColumnName("normalized_variance").HasPrecision(18, 6);
        entity.Property(d => d.SupportingText).HasColumnName("supporting_text").HasMaxLength(2000).IsRequired();
        entity.Property(d => d.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(d => d.SortOrder).HasColumnName("sort_order");
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(d => d.Result).WithMany(r => r.Drivers)
            .HasForeignKey(d => d.ResultId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.ResultId, d.SortOrder })
            .HasDatabaseName("ix_operational_risk_detection_drivers_tenant_facility_result");
    }
}
