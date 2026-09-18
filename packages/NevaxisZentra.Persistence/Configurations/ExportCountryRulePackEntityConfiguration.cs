using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportCountryRulePackEntityConfiguration : IEntityTypeConfiguration<ExportCountryRulePack>
{
    private static readonly Dictionary<ExportCountryRulePackStatus, string> StatusValues =
        new()
        {
            [ExportCountryRulePackStatus.Draft] = "draft",
            [ExportCountryRulePackStatus.UnderReview] = "under-review",
            [ExportCountryRulePackStatus.Verified] = "verified",
            [ExportCountryRulePackStatus.Superseded] = "superseded",
            [ExportCountryRulePackStatus.Archived] = "archived",
        };

    public void Configure(EntityTypeBuilder<ExportCountryRulePack> entity)
    {
        entity.ToTable("export_country_rule_packs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.VersionNumber).HasColumnName("version_number");
        entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
        entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        entity.Property(e => e.SourceTitle).HasColumnName("source_title").HasMaxLength(500);
        entity.Property(e => e.SourceUrl).HasColumnName("source_url").HasMaxLength(1000);
        entity.Property(e => e.SourcePublisher).HasColumnName("source_publisher").HasMaxLength(200);
        entity.Property(e => e.SourcePublicationDate).HasColumnName("source_publication_date");
        entity.Property(e => e.SourceAccessedAtUtc).HasColumnName("source_accessed_at_utc");
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.VerifiedBySubjectId).HasColumnName("verified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.SupersedesRulePackId).HasColumnName("supersedes_rule_pack_id");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.CountryCode, e.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uix_export_country_rule_packs_tenant_country_version");
        entity.HasIndex(e => new { e.TenantId, e.CountryCode, e.Status })
            .HasDatabaseName("ix_export_country_rule_packs_tenant_country_status");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_rule_packs_status",
            "status IN ('draft', 'under-review', 'verified', 'superseded', 'archived')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_rule_packs_version",
            "version >= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_rule_packs_version_number",
            "version_number >= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_rule_packs_country_code",
            "country_code ~ '^[A-Z]{2}$'"));
    }
}
