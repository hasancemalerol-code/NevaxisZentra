using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.CommercialIntelligence;

internal sealed class CommercialSalesPriceProfileEntityConfiguration : IEntityTypeConfiguration<CommercialSalesPriceProfile>
{
    public void Configure(EntityTypeBuilder<CommercialSalesPriceProfile> entity)
    {
        entity.ToTable("commercial_sales_price_profiles");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.ProfileCode).HasColumnName("profile_code").HasMaxLength(100).IsRequired();
        entity.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        entity.Property(p => p.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(p => p.ComparisonDimension).HasColumnName("comparison_dimension").HasMaxLength(50).IsRequired();
        entity.Property(p => p.ReferenceWindowDurationDays).HasColumnName("reference_window_duration_days");
        entity.Property(p => p.MinimumComparableObservations).HasColumnName("minimum_comparable_observations");
        entity.Property(p => p.MaximumPositiveVarianceRatio).HasColumnName("maximum_positive_variance_ratio").HasPrecision(18, 6);
        entity.Property(p => p.CustomerSpreadThreshold).HasColumnName("customer_spread_threshold").HasPrecision(18, 6);
        entity.Property(p => p.AllowedEvidenceStatuses).HasColumnName("allowed_evidence_statuses").HasMaxLength(200).IsRequired();
        entity.Property(p => p.AllowedSourceEvidenceKinds).HasColumnName("allowed_source_evidence_kinds").HasMaxLength(200).IsRequired();
        entity.Property(p => p.RequiredTaxInclusion).HasColumnName("required_tax_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredDiscountInclusion).HasColumnName("required_discount_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredFreightInclusion).HasColumnName("required_freight_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredGrossNetBasis).HasColumnName("required_gross_net_basis").HasMaxLength(20).IsRequired();
        entity.Property(p => p.ReferenceCenter).HasColumnName("reference_center").HasMaxLength(20).IsRequired();
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
            .HasDatabaseName("ux_commercial_sales_price_profiles_tenant_code")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.CreateIdempotencyKey })
            .HasDatabaseName("ux_commercial_sales_price_profiles_tenant_create_idempotency")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Status })
            .HasDatabaseName("ix_commercial_sales_price_profiles_tenant_facility_status");
    }
}

internal sealed class CommercialSalesPriceEvidenceObservationEntityConfiguration
    : IEntityTypeConfiguration<CommercialSalesPriceEvidenceObservation>
{
    public void Configure(EntityTypeBuilder<CommercialSalesPriceEvidenceObservation> entity)
    {
        entity.ToTable("commercial_sales_price_evidence_observations");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.MaterialId).HasColumnName("material_id");
        entity.Property(o => o.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100);
        entity.Property(o => o.SourceEvidenceKind).HasColumnName("source_evidence_kind").HasMaxLength(50).IsRequired();
        entity.Property(o => o.SourceDocumentReference).HasColumnName("source_document_reference").HasMaxLength(200).IsRequired();
        entity.Property(o => o.SourceLineReference).HasColumnName("source_line_reference").HasMaxLength(200).IsRequired();
        entity.Property(o => o.CommercialEventAtUtc).HasColumnName("commercial_event_at_utc");
        entity.Property(o => o.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(o => o.Quantity).HasColumnName("quantity").HasPrecision(18, 6);
        entity.Property(o => o.SourceUnit).HasColumnName("source_unit").HasMaxLength(50).IsRequired();
        entity.Property(o => o.NormalizedQuantity).HasColumnName("normalized_quantity").HasPrecision(18, 6);
        entity.Property(o => o.NormalizedUnit).HasColumnName("normalized_unit").HasMaxLength(50);
        entity.Property(o => o.OriginalUnitPrice).HasColumnName("original_unit_price").HasPrecision(18, 6);
        entity.Property(o => o.ComparableUnitPrice).HasColumnName("comparable_unit_price").HasPrecision(18, 6);
        entity.Property(o => o.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(o => o.PriceBasisGrossNet).HasColumnName("price_basis_gross_net").HasMaxLength(20).IsRequired();
        entity.Property(o => o.PriceBasisTaxInclusion).HasColumnName("price_basis_tax_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(o => o.PriceBasisDiscountInclusion).HasColumnName("price_basis_discount_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(o => o.PriceBasisFreightInclusion).HasColumnName("price_basis_freight_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(o => o.IsCancelledOrReturned).HasColumnName("is_cancelled_or_returned");
        entity.Property(o => o.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(o => o.SourceFingerprint).HasColumnName("source_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(o => o.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(o => o.ObservationSetCode).HasColumnName("observation_set_code").HasMaxLength(100).IsRequired();
        entity.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(o => o.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.MaterialId, o.CommercialEventAtUtc })
            .HasDatabaseName("ix_commsale1_evidence_tenant_facility_material_evt");
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.ObservationSetCode })
            .HasDatabaseName("ix_commsale1_evidence_tenant_facility_obs_set");
        entity.HasIndex(o => new { o.TenantId, o.SourceFingerprint })
            .HasDatabaseName("ux_commsale1_evidence_tenant_source_fingerprint")
            .IsUnique();
    }
}

internal sealed class CommercialSalesPriceAssessmentEntityConfiguration : IEntityTypeConfiguration<CommercialSalesPriceAssessment>
{
    public void Configure(EntityTypeBuilder<CommercialSalesPriceAssessment> entity)
    {
        entity.ToTable(
            "commercial_sales_price_assessments",
            table => table.HasCheckConstraint(
                "ck_commercial_sales_price_assessments_variance_ratio",
                "variance_ratio IS NULL OR variance_ratio > -1"));
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.AssessmentReference).HasColumnName("assessment_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.ProfileId).HasColumnName("profile_id");
        entity.Property(a => a.ProfileVersion).HasColumnName("profile_version").HasMaxLength(20).IsRequired();
        entity.Property(a => a.MaterialId).HasColumnName("material_id");
        entity.Property(a => a.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100);
        entity.Property(a => a.SourceEvidenceKind).HasColumnName("source_evidence_kind").HasMaxLength(50).IsRequired();
        entity.Property(a => a.ComparisonDimension).HasColumnName("comparison_dimension").HasMaxLength(50).IsRequired();
        entity.Property(a => a.CurrentUnitPrice).HasColumnName("current_unit_price").HasPrecision(18, 6);
        entity.Property(a => a.ReferenceMedianUnitPrice).HasColumnName("reference_median_unit_price").HasPrecision(18, 6);
        entity.Property(a => a.AbsoluteVariance).HasColumnName("absolute_variance").HasPrecision(18, 6);
        entity.Property(a => a.VarianceRatio).HasColumnName("variance_ratio").HasPrecision(18, 6);
        entity.Property(a => a.CandidateObservationCount).HasColumnName("candidate_observation_count");
        entity.Property(a => a.ComparableObservationCount).HasColumnName("comparable_observation_count");
        entity.Property(a => a.ExcludedObservationCount).HasColumnName("excluded_observation_count");
        entity.Property(a => a.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(a => a.NormalizedUnit).HasColumnName("normalized_unit").HasMaxLength(50).IsRequired();
        entity.Property(a => a.WindowStartUtc).HasColumnName("window_start_utc");
        entity.Property(a => a.WindowEndUtc).HasColumnName("window_end_utc");
        entity.Property(a => a.PriceBasisGrossNet).HasColumnName("price_basis_gross_net").HasMaxLength(20).IsRequired();
        entity.Property(a => a.PriceBasisTaxInclusion).HasColumnName("price_basis_tax_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(a => a.PriceBasisDiscountInclusion).HasColumnName("price_basis_discount_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(a => a.PriceBasisFreightInclusion).HasColumnName("price_basis_freight_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(a => a.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.AssessmentOutcome).HasColumnName("assessment_outcome").HasMaxLength(30).IsRequired();
        entity.Property(a => a.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(50);
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(a => a.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(a => a.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(a => a.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Profile).WithMany()
            .HasForeignKey(a => a.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.AssessmentReference })
            .HasDatabaseName("ux_commercial_sales_price_assessments_tenant_reference")
            .IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.IdempotencyKey })
            .HasDatabaseName("ux_commercial_sales_price_assessments_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.MaterialId, a.AssessmentOutcome })
            .HasDatabaseName("ix_commsale1_assessment_tenant_facility_material");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.WindowStartUtc, a.WindowEndUtc })
            .HasDatabaseName("ix_commsale1_assessment_tenant_facility_window");
    }
}

internal sealed class CommercialSalesPriceAssessmentDriverEntityConfiguration
    : IEntityTypeConfiguration<CommercialSalesPriceAssessmentDriver>
{
    public void Configure(EntityTypeBuilder<CommercialSalesPriceAssessmentDriver> entity)
    {
        entity.ToTable("commercial_sales_price_assessment_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.AssessmentId).HasColumnName("assessment_id");
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(50).IsRequired();
        entity.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(d => d.ActualValue).HasColumnName("actual_value").HasPrecision(18, 6);
        entity.Property(d => d.ThresholdValue).HasColumnName("threshold_value").HasPrecision(18, 6);
        entity.Property(d => d.Unit).HasColumnName("unit").HasMaxLength(50);
        entity.Property(d => d.NormalizedVariance).HasColumnName("normalized_variance").HasPrecision(18, 6);
        entity.Property(d => d.AffectedObservationCount).HasColumnName("affected_observation_count");
        entity.Property(d => d.SupportingText).HasColumnName("supporting_text").HasMaxLength(2000).IsRequired();
        entity.Property(d => d.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(d => d.SortOrder).HasColumnName("sort_order");
        entity.Property(d => d.IsLeadingDriver).HasColumnName("is_leading_driver");
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(d => d.Assessment).WithMany(a => a.Drivers)
            .HasForeignKey(d => d.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.AssessmentId, d.SortOrder })
            .HasDatabaseName("ix_commsale1_driver_tenant_facility_assessment");
    }
}

internal sealed class CommercialSalesPriceComparisonRunEntityConfiguration
    : IEntityTypeConfiguration<CommercialSalesPriceComparisonRun>
{
    public void Configure(EntityTypeBuilder<CommercialSalesPriceComparisonRun> entity)
    {
        entity.ToTable(
            "commercial_sales_price_comparison_runs",
            table => table.HasCheckConstraint(
                "ck_commercial_sales_price_comparison_runs_signed_variance",
                "signed_variance_ratio IS NULL OR signed_variance_ratio > -1"));
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
        entity.Property(r => r.SourceEvidenceKind).HasColumnName("source_evidence_kind").HasMaxLength(50).IsRequired();
        entity.Property(r => r.CurrentWindowStartUtc).HasColumnName("current_window_start_utc");
        entity.Property(r => r.CurrentWindowEndUtc).HasColumnName("current_window_end_utc");
        entity.Property(r => r.ReferenceWindowStartUtc).HasColumnName("reference_window_start_utc");
        entity.Property(r => r.ReferenceWindowEndUtc).HasColumnName("reference_window_end_utc");
        entity.Property(r => r.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(r => r.NormalizedUnit).HasColumnName("normalized_unit").HasMaxLength(50).IsRequired();
        entity.Property(r => r.PriceBasisGrossNet).HasColumnName("price_basis_gross_net").HasMaxLength(20).IsRequired();
        entity.Property(r => r.PriceBasisTaxInclusion).HasColumnName("price_basis_tax_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(r => r.PriceBasisDiscountInclusion).HasColumnName("price_basis_discount_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(r => r.PriceBasisFreightInclusion).HasColumnName("price_basis_freight_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(r => r.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(r => r.ComparisonOutcome).HasColumnName("comparison_outcome").HasMaxLength(30).IsRequired();
        entity.Property(r => r.CurrentMedianUnitPrice).HasColumnName("current_median_unit_price").HasPrecision(18, 6);
        entity.Property(r => r.ReferenceMedianUnitPrice).HasColumnName("reference_median_unit_price").HasPrecision(18, 6);
        entity.Property(r => r.SignedVarianceRatio).HasColumnName("signed_variance_ratio").HasPrecision(18, 6);
        entity.Property(r => r.CustomerSpreadRatio).HasColumnName("customer_spread_ratio").HasPrecision(18, 6);
        entity.Property(r => r.CandidateObservationCount).HasColumnName("candidate_observation_count");
        entity.Property(r => r.ComparableObservationCount).HasColumnName("comparable_observation_count");
        entity.Property(r => r.ExcludedObservationCount).HasColumnName("excluded_observation_count");
        entity.Property(r => r.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(50);
        entity.Property(r => r.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(r => r.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(r => r.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(r => r.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.Profile).WithMany()
            .HasForeignKey(r => r.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.ComparisonReference })
            .HasDatabaseName("ux_commercial_sales_price_comparison_runs_tenant_reference")
            .IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.IdempotencyKey })
            .HasDatabaseName("ux_commercial_sales_price_comparison_runs_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.ComparisonType, r.ComparisonOutcome })
            .HasDatabaseName("ix_commsale2_comparison_tenant_facility_type_outcome");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.MaterialId, r.CurrentWindowStartUtc, r.CurrentWindowEndUtc })
            .HasDatabaseName("ix_commsale2_comparison_tenant_facility_material_window");
    }
}

internal sealed class CommercialSalesPriceComparisonResultEntityConfiguration
    : IEntityTypeConfiguration<CommercialSalesPriceComparisonResult>
{
    public void Configure(EntityTypeBuilder<CommercialSalesPriceComparisonResult> entity)
    {
        entity.ToTable("commercial_sales_price_comparison_results");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ComparisonRunId).HasColumnName("comparison_run_id");
        entity.Property(r => r.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100);
        entity.Property(r => r.ComparableObservationCount).HasColumnName("comparable_observation_count");
        entity.Property(r => r.CustomerMedianUnitPrice).HasColumnName("customer_median_unit_price").HasPrecision(18, 6);
        entity.Property(r => r.LowestComparableMedianUnitPrice).HasColumnName("lowest_comparable_median_unit_price").HasPrecision(18, 6);
        entity.Property(r => r.RelativePricePositionRatio).HasColumnName("relative_price_position_ratio").HasPrecision(18, 6);
        entity.Property(r => r.PriceRank).HasColumnName("price_rank");
        entity.Property(r => r.IsTied).HasColumnName("is_tied");
        entity.Property(r => r.PositionState).HasColumnName("position_state").HasMaxLength(50).IsRequired();
        entity.Property(r => r.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.ComparisonRun).WithMany(run => run.Results)
            .HasForeignKey(r => r.ComparisonRunId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.ComparisonRunId, r.CustomerReference })
            .HasDatabaseName("ix_commsale2_result_tenant_facility_run_customer");
    }
}

internal sealed class CommercialSalesPriceComparisonDriverEntityConfiguration
    : IEntityTypeConfiguration<CommercialSalesPriceComparisonDriver>
{
    public void Configure(EntityTypeBuilder<CommercialSalesPriceComparisonDriver> entity)
    {
        entity.ToTable("commercial_sales_price_comparison_drivers");
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
        entity.Property(d => d.AffectedObservationCount).HasColumnName("affected_observation_count");
        entity.Property(d => d.SupportingText).HasColumnName("supporting_text").HasMaxLength(2000).IsRequired();
        entity.Property(d => d.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(d => d.SortOrder).HasColumnName("sort_order");
        entity.Property(d => d.IsLeadingDriver).HasColumnName("is_leading_driver");
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(d => d.ComparisonRun).WithMany(run => run.Drivers)
            .HasForeignKey(d => d.ComparisonRunId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(d => d.ComparisonResult).WithMany(result => result.Drivers)
            .HasForeignKey(d => d.ComparisonResultId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.ComparisonRunId, d.SortOrder })
            .HasDatabaseName("ix_commsale2_driver_tenant_facility_run");
    }
}
