using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.CommercialIntelligence;

internal sealed class CommercialProfitabilityProfileEntityConfiguration : IEntityTypeConfiguration<CommercialProfitabilityProfile>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityProfile> entity)
    {
        entity.ToTable("commercial_profitability_profiles");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.ProfileCode).HasColumnName("profile_code").HasMaxLength(100).IsRequired();
        entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(p => p.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(p => p.MinimumContributionMarginRatio).HasColumnName("minimum_contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(p => p.MinimumComparableAssessmentCountPerWindow).HasColumnName("minimum_comparable_assessment_count_per_window");
        entity.Property(p => p.MaximumAdverseContributionMarginDelta).HasColumnName("maximum_adverse_contribution_margin_delta").HasPrecision(18, 6);
        entity.Property(p => p.MinimumFavorableContributionMarginDelta).HasColumnName("minimum_favorable_contribution_margin_delta").HasPrecision(18, 6);
        entity.Property(p => p.MaximumCostShareIncrease).HasColumnName("maximum_cost_share_increase").HasPrecision(18, 6);
        entity.Property(p => p.MaximumEvidenceAgeDays).HasColumnName("maximum_evidence_age_days");
        entity.Property(p => p.RequiredCostComponentKinds).HasColumnName("required_cost_component_kinds").HasMaxLength(500).IsRequired();
        entity.Property(p => p.RequiredCurrencyCode).HasColumnName("required_currency_code").HasMaxLength(10).IsRequired();
        entity.Property(p => p.RequiredUnitOfMeasure).HasColumnName("required_unit_of_measure").HasMaxLength(50).IsRequired();
        entity.Property(p => p.RequiredGrossNetBasis).HasColumnName("required_gross_net_basis").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredTaxInclusion).HasColumnName("required_tax_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredDiscountInclusion).HasColumnName("required_discount_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredFreightInclusion).HasColumnName("required_freight_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.AllowedEvidenceStatuses).HasColumnName("allowed_evidence_statuses").HasMaxLength(200).IsRequired();
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
            .HasDatabaseName("ux_commercial_profitability_profiles_tenant_code")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.CreateIdempotencyKey })
            .HasDatabaseName("ux_commercial_profitability_profiles_tenant_create_idempotency")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Status })
            .HasDatabaseName("ix_commercial_profitability_profiles_tenant_facility_status");
    }
}

internal sealed class CommercialProfitabilityEvidenceObservationEntityConfiguration
    : IEntityTypeConfiguration<CommercialProfitabilityEvidenceObservation>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityEvidenceObservation> entity)
    {
        entity.ToTable("commercial_profitability_evidence_observations");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.ObservationKind).HasColumnName("observation_kind").HasMaxLength(30).IsRequired();
        entity.Property(o => o.EvidenceSetReference).HasColumnName("evidence_set_reference").HasMaxLength(100).IsRequired();
        entity.Property(o => o.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100).IsRequired();
        entity.Property(o => o.MaterialId).HasColumnName("material_id");
        entity.Property(o => o.OrderReference).HasColumnName("order_reference").HasMaxLength(100);
        entity.Property(o => o.QuotationReference).HasColumnName("quotation_reference").HasMaxLength(100);
        entity.Property(o => o.ComparableNetSalesUnitPrice).HasColumnName("comparable_net_sales_unit_price").HasPrecision(18, 6);
        entity.Property(o => o.ComponentKind).HasColumnName("component_kind").HasMaxLength(50);
        entity.Property(o => o.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 6);
        entity.Property(o => o.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(o => o.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(50).IsRequired();
        entity.Property(o => o.GrossNetBasis).HasColumnName("gross_net_basis").HasMaxLength(20).IsRequired();
        entity.Property(o => o.TaxBasis).HasColumnName("tax_basis").HasMaxLength(20).IsRequired();
        entity.Property(o => o.DiscountBasis).HasColumnName("discount_basis").HasMaxLength(20).IsRequired();
        entity.Property(o => o.FreightBasis).HasColumnName("freight_basis").HasMaxLength(20).IsRequired();
        entity.Property(o => o.CostBasis).HasColumnName("cost_basis").HasMaxLength(20);
        entity.Property(o => o.AllocationBasis).HasColumnName("allocation_basis").HasMaxLength(100);
        entity.Property(o => o.SourceKind).HasColumnName("source_kind").HasMaxLength(50).IsRequired();
        entity.Property(o => o.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(o => o.EvidenceTimestampUtc).HasColumnName("evidence_timestamp_utc");
        entity.Property(o => o.IsCancelledOrReturned).HasColumnName("is_cancelled_or_returned");
        entity.Property(o => o.SourceReference).HasColumnName("source_reference").HasMaxLength(200).IsRequired();
        entity.Property(o => o.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(o => o.SourceFingerprint).HasColumnName("source_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(o => o.IngestIdempotencyKey).HasColumnName("ingest_idempotency_key").HasMaxLength(128);
        entity.Property(o => o.IngestIdempotencyPayloadFingerprint).HasColumnName("ingest_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(o => o.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(o => o.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.EvidenceSetReference, o.ObservationKind })
            .HasDatabaseName("ix_commprofit1_evidence_tenant_facility_set_kind");
        entity.HasIndex(o => new { o.TenantId, o.SourceFingerprint })
            .HasDatabaseName("ux_commprofit1_evidence_tenant_source_fingerprint")
            .IsUnique();
    }
}

internal sealed class CommercialProfitabilityAssessmentEntityConfiguration : IEntityTypeConfiguration<CommercialProfitabilityAssessment>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityAssessment> entity)
    {
        entity.ToTable(
            "commercial_profitability_assessments",
            table => table.HasCheckConstraint(
                "ck_commercial_profitability_assessments_margin_ratio",
                "contribution_margin_ratio IS NULL OR contribution_margin_ratio < 1"));
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.AssessmentReference).HasColumnName("assessment_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.ProfileId).HasColumnName("profile_id");
        entity.Property(a => a.ProfileVersion).HasColumnName("profile_version").HasMaxLength(20).IsRequired();
        entity.Property(a => a.SalesEvidenceObservationId).HasColumnName("sales_evidence_observation_id");
        entity.Property(a => a.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.MaterialId).HasColumnName("material_id");
        entity.Property(a => a.OrderReference).HasColumnName("order_reference").HasMaxLength(100);
        entity.Property(a => a.QuotationReference).HasColumnName("quotation_reference").HasMaxLength(100);
        entity.Property(a => a.EvidenceSetReference).HasColumnName("evidence_set_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.ComparableNetSalesUnitPrice).HasColumnName("comparable_net_sales_unit_price").HasPrecision(18, 6);
        entity.Property(a => a.TotalIncludedUnitCost).HasColumnName("total_included_unit_cost").HasPrecision(18, 6);
        entity.Property(a => a.UnitContribution).HasColumnName("unit_contribution").HasPrecision(18, 6);
        entity.Property(a => a.ContributionMarginRatio).HasColumnName("contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(a => a.MarkupRatio).HasColumnName("markup_ratio").HasPrecision(18, 6);
        entity.Property(a => a.CandidateCostComponentCount).HasColumnName("candidate_cost_component_count");
        entity.Property(a => a.IncludedCostComponentCount).HasColumnName("included_cost_component_count");
        entity.Property(a => a.ExcludedCostComponentCount).HasColumnName("excluded_cost_component_count");
        entity.Property(a => a.RequiredCostComponentCount).HasColumnName("required_cost_component_count");
        entity.Property(a => a.AvailableRequiredCostComponentCount).HasColumnName("available_required_cost_component_count");
        entity.Property(a => a.MissingRequiredCostComponentCount).HasColumnName("missing_required_cost_component_count");
        entity.Property(a => a.MissingRequiredComponentKinds).HasColumnName("missing_required_component_kinds").HasMaxLength(500).IsRequired();
        entity.Property(a => a.CostCoverageStatus).HasColumnName("cost_coverage_status").HasMaxLength(50).IsRequired();
        entity.Property(a => a.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(a => a.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(50).IsRequired();
        entity.Property(a => a.GrossNetBasis).HasColumnName("gross_net_basis").HasMaxLength(20).IsRequired();
        entity.Property(a => a.TaxBasis).HasColumnName("tax_basis").HasMaxLength(20).IsRequired();
        entity.Property(a => a.DiscountBasis).HasColumnName("discount_basis").HasMaxLength(20).IsRequired();
        entity.Property(a => a.FreightBasis).HasColumnName("freight_basis").HasMaxLength(20).IsRequired();
        entity.Property(a => a.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        entity.Property(a => a.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(100);
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(a => a.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(a => a.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(a => a.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne(a => a.Profile).WithMany().HasForeignKey(a => a.ProfileId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.SalesEvidenceObservation).WithMany().HasForeignKey(a => a.SalesEvidenceObservationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.IdempotencyKey })
            .HasDatabaseName("ux_commercial_profitability_assessments_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.AssessmentReference })
            .HasDatabaseName("ux_commercial_profitability_assessments_tenant_reference")
            .IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.MaterialId, a.Outcome })
            .HasDatabaseName("ix_commprofit1_assessments_tenant_facility_material_outcome");
    }
}

internal sealed class CommercialProfitabilityAssessmentCostComponentEntityConfiguration
    : IEntityTypeConfiguration<CommercialProfitabilityAssessmentCostComponent>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityAssessmentCostComponent> entity)
    {
        entity.ToTable("commercial_profitability_assessment_cost_components");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.AssessmentId).HasColumnName("assessment_id");
        entity.Property(c => c.SourceEvidenceObservationId).HasColumnName("source_evidence_observation_id");
        entity.Property(c => c.ComponentKind).HasColumnName("component_kind").HasMaxLength(50).IsRequired();
        entity.Property(c => c.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 6);
        entity.Property(c => c.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(c => c.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(50).IsRequired();
        entity.Property(c => c.CostBasis).HasColumnName("cost_basis").HasMaxLength(20).IsRequired();
        entity.Property(c => c.AllocationBasis).HasColumnName("allocation_basis").HasMaxLength(100);
        entity.Property(c => c.EvidenceTimestampUtc).HasColumnName("evidence_timestamp_utc");
        entity.Property(c => c.SourceKind).HasColumnName("source_kind").HasMaxLength(50).IsRequired();
        entity.Property(c => c.SourceReference).HasColumnName("source_reference").HasMaxLength(200).IsRequired();
        entity.Property(c => c.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(c => c.InclusionStatus).HasColumnName("inclusion_status").HasMaxLength(20).IsRequired();
        entity.Property(c => c.ExclusionReason).HasColumnName("exclusion_reason").HasMaxLength(100);
        entity.Property(c => c.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(c => c.SortOrder).HasColumnName("sort_order");
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne(c => c.Assessment).WithMany(a => a.CostComponents).HasForeignKey(c => c.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.AssessmentId, c.SortOrder })
            .HasDatabaseName("ix_commprofit1_components_tenant_facility_assessment_sort")
            .IsUnique();
    }
}

internal sealed class CommercialProfitabilityAssessmentDriverEntityConfiguration
    : IEntityTypeConfiguration<CommercialProfitabilityAssessmentDriver>
{
    public void Configure(EntityTypeBuilder<CommercialProfitabilityAssessmentDriver> entity)
    {
        entity.ToTable("commercial_profitability_assessment_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.AssessmentId).HasColumnName("assessment_id");
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        entity.Property(d => d.ActualValue).HasColumnName("actual_value").HasPrecision(18, 6);
        entity.Property(d => d.ThresholdValue).HasColumnName("threshold_value").HasPrecision(18, 6);
        entity.Property(d => d.Unit).HasColumnName("unit").HasMaxLength(50);
        entity.Property(d => d.NormalizedVariance).HasColumnName("normalized_variance").HasPrecision(18, 6);
        entity.Property(d => d.AffectedComponentCount).HasColumnName("affected_component_count");
        entity.Property(d => d.SupportingText).HasColumnName("supporting_text").HasMaxLength(500).IsRequired();
        entity.Property(d => d.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(d => d.SortOrder).HasColumnName("sort_order");
        entity.Property(d => d.IsLeadingDriver).HasColumnName("is_leading_driver");
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne(d => d.Assessment).WithMany(a => a.Drivers).HasForeignKey(d => d.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.AssessmentId, d.SortOrder })
            .HasDatabaseName("ix_commprofit1_drivers_tenant_facility_assessment_sort")
            .IsUnique();
    }
}
