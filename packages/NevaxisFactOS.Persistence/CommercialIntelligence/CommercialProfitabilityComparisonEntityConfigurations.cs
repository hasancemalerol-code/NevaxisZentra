using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.CommercialIntelligence;

internal sealed class CommercialProfitabilityComparisonRunEntityConfiguration
    : IEntityTypeConfiguration<CommercialProfitabilityComparisonRun>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityComparisonRun> entity)
    {
        entity.ToTable(
            "commercial_profitability_comparison_runs",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_runs_assessment_counts",
                    "candidate_assessment_count = comparable_assessment_count + excluded_assessment_count");
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_runs_reference_margin",
                    "reference_median_contribution_margin_ratio IS NULL OR reference_median_contribution_margin_ratio < 1");
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_runs_current_margin",
                    "current_median_contribution_margin_ratio IS NULL OR current_median_contribution_margin_ratio < 1");
            });
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ComparisonReference).HasColumnName("comparison_reference").HasMaxLength(100).IsRequired();
        entity.Property(r => r.ProfileId).HasColumnName("profile_id");
        entity.Property(r => r.ProfileVersion).HasColumnName("profile_version").HasMaxLength(20).IsRequired();
        entity.Property(r => r.ComparisonType).HasColumnName("comparison_type").HasMaxLength(50).IsRequired();
        entity.Property(r => r.MaterialId).HasColumnName("material_id");
        entity.Property(r => r.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100);
        entity.Property(r => r.ReferenceWindowStartUtc).HasColumnName("reference_window_start_utc");
        entity.Property(r => r.ReferenceWindowEndUtc).HasColumnName("reference_window_end_utc");
        entity.Property(r => r.CurrentWindowStartUtc).HasColumnName("current_window_start_utc");
        entity.Property(r => r.CurrentWindowEndUtc).HasColumnName("current_window_end_utc");
        entity.Property(r => r.AggregationMethod).HasColumnName("aggregation_method").HasMaxLength(50).IsRequired();
        entity.Property(r => r.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(r => r.ComparisonOutcome).HasColumnName("comparison_outcome").HasMaxLength(30).IsRequired();
        entity.Property(r => r.CandidateAssessmentCount).HasColumnName("candidate_assessment_count");
        entity.Property(r => r.ComparableAssessmentCount).HasColumnName("comparable_assessment_count");
        entity.Property(r => r.ExcludedAssessmentCount).HasColumnName("excluded_assessment_count");
        entity.Property(r => r.CompleteCoverageCount).HasColumnName("complete_coverage_count");
        entity.Property(r => r.PartialCoverageCount).HasColumnName("partial_coverage_count");
        entity.Property(r => r.InsufficientDataCount).HasColumnName("insufficient_data_count");
        entity.Property(r => r.SourceConflictCount).HasColumnName("source_conflict_count");
        entity.Property(r => r.NegativeContributionCount).HasColumnName("negative_contribution_count");
        entity.Property(r => r.BelowThresholdCount).HasColumnName("below_threshold_count");
        entity.Property(r => r.ReferenceCandidateAssessmentCount).HasColumnName("reference_candidate_assessment_count");
        entity.Property(r => r.ReferenceComparableAssessmentCount).HasColumnName("reference_comparable_assessment_count");
        entity.Property(r => r.ReferenceExcludedAssessmentCount).HasColumnName("reference_excluded_assessment_count");
        entity.Property(r => r.ReferenceCompleteCoverageCount).HasColumnName("reference_complete_coverage_count");
        entity.Property(r => r.ReferencePartialCoverageCount).HasColumnName("reference_partial_coverage_count");
        entity.Property(r => r.ReferenceInsufficientDataCount).HasColumnName("reference_insufficient_data_count");
        entity.Property(r => r.ReferenceSourceConflictCount).HasColumnName("reference_source_conflict_count");
        entity.Property(r => r.ReferenceNegativeContributionCount).HasColumnName("reference_negative_contribution_count");
        entity.Property(r => r.ReferenceBelowThresholdCount).HasColumnName("reference_below_threshold_count");
        entity.Property(r => r.CurrentCandidateAssessmentCount).HasColumnName("current_candidate_assessment_count");
        entity.Property(r => r.CurrentComparableAssessmentCount).HasColumnName("current_comparable_assessment_count");
        entity.Property(r => r.CurrentExcludedAssessmentCount).HasColumnName("current_excluded_assessment_count");
        entity.Property(r => r.CurrentCompleteCoverageCount).HasColumnName("current_complete_coverage_count");
        entity.Property(r => r.CurrentPartialCoverageCount).HasColumnName("current_partial_coverage_count");
        entity.Property(r => r.CurrentInsufficientDataCount).HasColumnName("current_insufficient_data_count");
        entity.Property(r => r.CurrentSourceConflictCount).HasColumnName("current_source_conflict_count");
        entity.Property(r => r.CurrentNegativeContributionCount).HasColumnName("current_negative_contribution_count");
        entity.Property(r => r.CurrentBelowThresholdCount).HasColumnName("current_below_threshold_count");
        entity.Property(r => r.ReferenceMedianContributionMarginRatio).HasColumnName("reference_median_contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(r => r.CurrentMedianContributionMarginRatio).HasColumnName("current_median_contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(r => r.ContributionMarginDelta).HasColumnName("contribution_margin_delta").HasPrecision(18, 6);
        entity.Property(r => r.ReferenceMedianUnitContribution).HasColumnName("reference_median_unit_contribution").HasPrecision(18, 6);
        entity.Property(r => r.CurrentMedianUnitContribution).HasColumnName("current_median_unit_contribution").HasPrecision(18, 6);
        entity.Property(r => r.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(50);
        entity.Property(r => r.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(r => r.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(r => r.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(r => r.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne(r => r.Profile).WithMany().HasForeignKey(r => r.ProfileId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.ComparisonReference })
            .HasDatabaseName("ux_commprofit2_comparison_runs_tenant_reference")
            .IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.IdempotencyKey })
            .HasDatabaseName("ux_commprofit2_comparison_runs_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.ComparisonType, r.ComparisonOutcome })
            .HasDatabaseName("ix_commprofit2_comparison_runs_tenant_facility_type_outcome");
    }
}

internal sealed class CommercialProfitabilityComparisonResultEntityConfiguration
    : IEntityTypeConfiguration<CommercialProfitabilityComparisonResult>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityComparisonResult> entity)
    {
        entity.ToTable(
            "commercial_profitability_comparison_results",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_results_assessment_counts",
                    "candidate_assessment_count = comparable_assessment_count + excluded_assessment_count");
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_results_reference_margin",
                    "reference_median_contribution_margin_ratio IS NULL OR reference_median_contribution_margin_ratio < 1");
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_results_current_margin",
                    "current_median_contribution_margin_ratio IS NULL OR current_median_contribution_margin_ratio < 1");
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_results_unit_cost_ratio",
                    "signed_unit_cost_change_ratio IS NULL OR signed_unit_cost_change_ratio > -1");
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_results_reference_share",
                    "reference_median_cost_share IS NULL OR (reference_median_cost_share >= 0 AND reference_median_cost_share <= 1)");
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_results_current_share",
                    "current_median_cost_share IS NULL OR (current_median_cost_share >= 0 AND current_median_cost_share <= 1)");
                table.HasCheckConstraint(
                    "ck_commprofit2_comparison_results_share_delta",
                    "cost_share_delta IS NULL OR (cost_share_delta >= -1 AND cost_share_delta <= 1)");
            });
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ComparisonRunId).HasColumnName("comparison_run_id");
        entity.Property(r => r.ResultDimension).HasColumnName("result_dimension").HasMaxLength(30).IsRequired();
        entity.Property(r => r.CanonicalResultReference).HasColumnName("canonical_result_reference").HasMaxLength(100).IsRequired();
        entity.Property(r => r.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100);
        entity.Property(r => r.MaterialId).HasColumnName("material_id");
        entity.Property(r => r.ComponentKind).HasColumnName("component_kind").HasMaxLength(50);
        entity.Property(r => r.CandidateAssessmentCount).HasColumnName("candidate_assessment_count");
        entity.Property(r => r.ComparableAssessmentCount).HasColumnName("comparable_assessment_count");
        entity.Property(r => r.ExcludedAssessmentCount).HasColumnName("excluded_assessment_count");
        entity.Property(r => r.CompleteCoverageCount).HasColumnName("complete_coverage_count");
        entity.Property(r => r.PartialCoverageCount).HasColumnName("partial_coverage_count");
        entity.Property(r => r.NegativeContributionCount).HasColumnName("negative_contribution_count");
        entity.Property(r => r.BelowThresholdCount).HasColumnName("below_threshold_count");
        entity.Property(r => r.ReferenceComparableAssessmentCount).HasColumnName("reference_comparable_assessment_count");
        entity.Property(r => r.CurrentComparableAssessmentCount).HasColumnName("current_comparable_assessment_count");
        entity.Property(r => r.ReferenceExcludedAssessmentCount).HasColumnName("reference_excluded_assessment_count");
        entity.Property(r => r.CurrentExcludedAssessmentCount).HasColumnName("current_excluded_assessment_count");
        entity.Property(r => r.ReferenceMedianContributionMarginRatio).HasColumnName("reference_median_contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(r => r.CurrentMedianContributionMarginRatio).HasColumnName("current_median_contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(r => r.ContributionMarginDelta).HasColumnName("contribution_margin_delta").HasPrecision(18, 6);
        entity.Property(r => r.ReferenceMedianUnitContribution).HasColumnName("reference_median_unit_contribution").HasPrecision(18, 6);
        entity.Property(r => r.CurrentMedianUnitContribution).HasColumnName("current_median_unit_contribution").HasPrecision(18, 6);
        entity.Property(r => r.ReferenceMedianUnitCost).HasColumnName("reference_median_unit_cost").HasPrecision(18, 6);
        entity.Property(r => r.CurrentMedianUnitCost).HasColumnName("current_median_unit_cost").HasPrecision(18, 6);
        entity.Property(r => r.SignedUnitCostChange).HasColumnName("signed_unit_cost_change").HasPrecision(18, 6);
        entity.Property(r => r.SignedUnitCostChangeRatio).HasColumnName("signed_unit_cost_change_ratio").HasPrecision(18, 6);
        entity.Property(r => r.ReferenceMedianCostShare).HasColumnName("reference_median_cost_share").HasPrecision(18, 6);
        entity.Property(r => r.CurrentMedianCostShare).HasColumnName("current_median_cost_share").HasPrecision(18, 6);
        entity.Property(r => r.CostShareDelta).HasColumnName("cost_share_delta").HasPrecision(18, 6);
        entity.Property(r => r.PositionState).HasColumnName("position_state").HasMaxLength(50).IsRequired();
        entity.Property(r => r.ContributionRank).HasColumnName("contribution_rank");
        entity.Property(r => r.IsTied).HasColumnName("is_tied");
        entity.Property(r => r.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(r => r.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(r => r.SortOrder).HasColumnName("sort_order");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne(r => r.ComparisonRun).WithMany(run => run.Results).HasForeignKey(r => r.ComparisonRunId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.ComparisonRunId, r.ResultDimension, r.CanonicalResultReference })
            .HasDatabaseName("ix_commprofit2_comparison_results_tenant_facility_run_dimension_ref");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.ComparisonRunId, r.ContributionRank })
            .HasDatabaseName("ix_commprofit2_comparison_results_tenant_facility_run_rank");
    }
}

internal sealed class CommercialProfitabilityComparisonDriverEntityConfiguration
    : IEntityTypeConfiguration<CommercialProfitabilityComparisonDriver>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityComparisonDriver> entity)
    {
        entity.ToTable("commercial_profitability_comparison_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.ComparisonRunId).HasColumnName("comparison_run_id");
        entity.Property(d => d.ComparisonResultId).HasColumnName("comparison_result_id");
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(50).IsRequired();
        entity.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(d => d.ActualValue).HasColumnName("actual_value").HasPrecision(18, 6);
        entity.Property(d => d.ReferenceValue).HasColumnName("reference_value").HasPrecision(18, 6);
        entity.Property(d => d.ThresholdValue).HasColumnName("threshold_value").HasPrecision(18, 6);
        entity.Property(d => d.Unit).HasColumnName("unit").HasMaxLength(50);
        entity.Property(d => d.NormalizedVariance).HasColumnName("normalized_variance").HasPrecision(18, 6);
        entity.Property(d => d.AffectedAssessmentCount).HasColumnName("affected_assessment_count");
        entity.Property(d => d.SupportingText).HasColumnName("supporting_text").HasMaxLength(2000).IsRequired();
        entity.Property(d => d.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(d => d.SortOrder).HasColumnName("sort_order");
        entity.Property(d => d.IsLeadingDriver).HasColumnName("is_leading_driver");
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne(d => d.ComparisonRun).WithMany(run => run.Drivers).HasForeignKey(d => d.ComparisonRunId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(d => d.ComparisonResult).WithMany(result => result.Drivers).HasForeignKey(d => d.ComparisonResultId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.ComparisonRunId, d.SortOrder })
            .HasDatabaseName("ix_commprofit2_comparison_drivers_tenant_facility_run_sort");
    }
}
