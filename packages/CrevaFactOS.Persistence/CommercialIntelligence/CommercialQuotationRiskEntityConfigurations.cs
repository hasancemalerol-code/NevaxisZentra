using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

internal sealed class CommercialQuotationRiskProfileEntityConfiguration : IEntityTypeConfiguration<CommercialQuotationRiskProfile>
{
    public void Configure(EntityTypeBuilder<CommercialQuotationRiskProfile> entity)
    {
        entity.ToTable("commercial_quotation_risk_profiles");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.ProfileCode).HasColumnName("profile_code").HasMaxLength(100).IsRequired();
        entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(p => p.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(p => p.ReferenceWindowDurationDays).HasColumnName("reference_window_duration_days");
        entity.Property(p => p.MinimumComparableObservations).HasColumnName("minimum_comparable_observations");
        entity.Property(p => p.MaximumNegativePriceVarianceRatio).HasColumnName("maximum_negative_price_variance_ratio").HasPrecision(18, 6);
        entity.Property(p => p.MaximumPositivePriceVarianceRatio).HasColumnName("maximum_positive_price_variance_ratio").HasPrecision(18, 6);
        entity.Property(p => p.MaximumPaymentTermDays).HasColumnName("maximum_payment_term_days");
        entity.Property(p => p.ExpiryWarningDays).HasColumnName("expiry_warning_days");
        entity.Property(p => p.MaximumEvidenceAgeDays).HasColumnName("maximum_evidence_age_days");
        entity.Property(p => p.MaximumRiskIncidenceDelta).HasColumnName("maximum_risk_incidence_delta").HasPrecision(18, 6);
        entity.Property(p => p.AllowedEvidenceStatuses).HasColumnName("allowed_evidence_statuses").HasMaxLength(200).IsRequired();
        entity.Property(p => p.AllowedSalesEvidenceKinds).HasColumnName("allowed_sales_evidence_kinds").HasMaxLength(200).IsRequired();
        entity.Property(p => p.RequiredTaxInclusion).HasColumnName("required_tax_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredDiscountInclusion).HasColumnName("required_discount_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredFreightInclusion).HasColumnName("required_freight_inclusion").HasMaxLength(20).IsRequired();
        entity.Property(p => p.RequiredGrossNetBasis).HasColumnName("required_gross_net_basis").HasMaxLength(20).IsRequired();
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
            .HasDatabaseName("ux_commercial_quotation_risk_profiles_tenant_code")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.CreateIdempotencyKey })
            .HasDatabaseName("ux_commercial_quotation_risk_profiles_tenant_create_idempotency")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Status })
            .HasDatabaseName("ix_commercial_quotation_risk_profiles_tenant_facility_status");
    }
}

internal sealed class CommercialQuotationRiskEvidenceObservationEntityConfiguration
    : IEntityTypeConfiguration<CommercialQuotationRiskEvidenceObservation>
{
    public void Configure(EntityTypeBuilder<CommercialQuotationRiskEvidenceObservation> entity)
    {
        entity.ToTable("commercial_quotation_risk_evidence_observations");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.QuotationReference).HasColumnName("quotation_reference").HasMaxLength(100).IsRequired();
        entity.Property(o => o.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100).IsRequired();
        entity.Property(o => o.MaterialId).HasColumnName("material_id");
        entity.Property(o => o.SourceKind).HasColumnName("source_kind").HasMaxLength(50).IsRequired();
        entity.Property(o => o.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(o => o.QuotedAtUtc).HasColumnName("quoted_at_utc");
        entity.Property(o => o.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(o => o.QuotedUnitPrice).HasColumnName("quoted_unit_price").HasPrecision(18, 6);
        entity.Property(o => o.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(o => o.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(50).IsRequired();
        entity.Property(o => o.GrossNetBasis).HasColumnName("gross_net_basis").HasMaxLength(20).IsRequired();
        entity.Property(o => o.TaxBasis).HasColumnName("tax_basis").HasMaxLength(20).IsRequired();
        entity.Property(o => o.DiscountBasis).HasColumnName("discount_basis").HasMaxLength(20).IsRequired();
        entity.Property(o => o.FreightBasis).HasColumnName("freight_basis").HasMaxLength(20).IsRequired();
        entity.Property(o => o.PaymentTermDays).HasColumnName("payment_term_days");
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
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.MaterialId, o.QuotedAtUtc })
            .HasDatabaseName("ix_commquote1_evidence_tenant_facility_material_quoted");
        entity.HasIndex(o => new { o.TenantId, o.SourceFingerprint })
            .HasDatabaseName("ux_commquote1_evidence_tenant_source_fingerprint")
            .IsUnique();
    }
}

internal sealed class CommercialQuotationRiskAssessmentEntityConfiguration : IEntityTypeConfiguration<CommercialQuotationRiskAssessment>
{
    public void Configure(EntityTypeBuilder<CommercialQuotationRiskAssessment> entity)
    {
        entity.ToTable(
            "commercial_quotation_risk_assessments",
            table => table.HasCheckConstraint(
                "ck_commercial_quotation_risk_assessments_signed_variance",
                "signed_price_variance_ratio IS NULL OR signed_price_variance_ratio > -1"));
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.AssessmentReference).HasColumnName("assessment_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.ProfileId).HasColumnName("profile_id");
        entity.Property(a => a.ProfileVersion).HasColumnName("profile_version").HasMaxLength(20).IsRequired();
        entity.Property(a => a.EvidenceObservationId).HasColumnName("evidence_observation_id");
        entity.Property(a => a.QuotationReference).HasColumnName("quotation_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.MaterialId).HasColumnName("material_id");
        entity.Property(a => a.ReferenceWindowStartUtc).HasColumnName("reference_window_start_utc");
        entity.Property(a => a.ReferenceWindowEndUtc).HasColumnName("reference_window_end_utc");
        entity.Property(a => a.ReferenceMedianUnitPrice).HasColumnName("reference_median_unit_price").HasPrecision(18, 6);
        entity.Property(a => a.QuotedComparableUnitPrice).HasColumnName("quoted_comparable_unit_price").HasPrecision(18, 6);
        entity.Property(a => a.SignedPriceVarianceRatio).HasColumnName("signed_price_variance_ratio").HasPrecision(18, 6);
        entity.Property(a => a.CandidateObservationCount).HasColumnName("candidate_observation_count");
        entity.Property(a => a.ComparableObservationCount).HasColumnName("comparable_observation_count");
        entity.Property(a => a.ExcludedObservationCount).HasColumnName("excluded_observation_count");
        entity.Property(a => a.PaymentTermDays).HasColumnName("payment_term_days");
        entity.Property(a => a.RemainingValidityDays).HasColumnName("remaining_validity_days");
        entity.Property(a => a.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(a => a.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(50).IsRequired();
        entity.Property(a => a.GrossNetBasis).HasColumnName("gross_net_basis").HasMaxLength(20).IsRequired();
        entity.Property(a => a.TaxBasis).HasColumnName("tax_basis").HasMaxLength(20).IsRequired();
        entity.Property(a => a.DiscountBasis).HasColumnName("discount_basis").HasMaxLength(20).IsRequired();
        entity.Property(a => a.FreightBasis).HasColumnName("freight_basis").HasMaxLength(20).IsRequired();
        entity.Property(a => a.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        entity.Property(a => a.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(50);
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(a => a.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(a => a.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(a => a.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne(a => a.Profile).WithMany().HasForeignKey(a => a.ProfileId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.EvidenceObservation).WithMany().HasForeignKey(a => a.EvidenceObservationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.MaterialId, a.Outcome })
            .HasDatabaseName("ix_commquote1_assessment_tenant_facility_material_outcome");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.ReferenceWindowStartUtc, a.ReferenceWindowEndUtc })
            .HasDatabaseName("ix_commquote1_assessment_tenant_facility_window");
        entity.HasIndex(a => new { a.TenantId, a.IdempotencyKey })
            .HasDatabaseName("ux_commercial_quotation_risk_assessments_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.AssessmentReference })
            .HasDatabaseName("ux_commercial_quotation_risk_assessments_tenant_reference")
            .IsUnique();
    }
}

internal sealed class CommercialQuotationRiskAssessmentDriverEntityConfiguration
    : IEntityTypeConfiguration<CommercialQuotationRiskAssessmentDriver>
{
    public void Configure(EntityTypeBuilder<CommercialQuotationRiskAssessmentDriver> entity)
    {
        entity.ToTable("commercial_quotation_risk_assessment_drivers");
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
        entity.HasOne(d => d.Assessment).WithMany(a => a.Drivers).HasForeignKey(d => d.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.AssessmentId, d.SortOrder })
            .HasDatabaseName("ix_commquote1_driver_tenant_facility_assessment");
    }
}
