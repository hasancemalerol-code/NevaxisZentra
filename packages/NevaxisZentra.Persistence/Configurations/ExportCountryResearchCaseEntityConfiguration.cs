using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportCountryResearchCaseEntityConfiguration
    : IEntityTypeConfiguration<ExportCountryResearchCase>
{
    private static readonly Dictionary<ExportCountryResearchCaseStatus, string> StatusValues =
        new()
        {
            [ExportCountryResearchCaseStatus.Draft] = "draft",
            [ExportCountryResearchCaseStatus.Researching] = "researching",
            [ExportCountryResearchCaseStatus.ReviewRequired] = "review-required",
            [ExportCountryResearchCaseStatus.Verified] = "verified",
            [ExportCountryResearchCaseStatus.Completed] = "completed",
            [ExportCountryResearchCaseStatus.Archived] = "archived",
        };

    public void Configure(EntityTypeBuilder<ExportCountryResearchCase> entity)
    {
        entity.ToTable("export_country_research_cases");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ResearchNumber).HasColumnName("research_number").HasMaxLength(40);
        entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        entity.Property(e => e.ProductReference).HasColumnName("product_reference").HasMaxLength(200);
        entity.Property(e => e.ProductDescription).HasColumnName("product_description").HasMaxLength(500);
        entity.Property(e => e.ProductGroup).HasColumnName("product_group").HasMaxLength(100);
        entity.Property(e => e.HsCode).HasColumnName("hs_code").HasMaxLength(20);
        entity.Property(e => e.OriginCountryCode).HasColumnName("origin_country_code").HasMaxLength(2);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.PurposeNotes).HasColumnName("purpose_notes").HasMaxLength(2000);
        entity.Property(e => e.LinkedExportCaseId).HasColumnName("linked_export_case_id");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.ReviewedAtUtc).HasColumnName("reviewed_at_utc");
        entity.Property(e => e.ReviewedBySubjectId).HasColumnName("reviewed_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ResearchNumber })
            .IsUnique()
            .HasDatabaseName("uix_export_country_research_cases_tenant_facility_number");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.CountryCode })
            .HasDatabaseName("ix_export_country_research_cases_tenant_facility_country");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_research_cases_status",
            "status IN ('draft', 'researching', 'review-required', 'verified', 'completed', 'archived')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_research_cases_research_number",
            "research_number <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_research_cases_version",
            "version >= 1"));
    }
}
