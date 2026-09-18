using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportCountryKnowledgeEntryEntityConfiguration
    : IEntityTypeConfiguration<ExportCountryKnowledgeEntry>
{
    private static readonly Dictionary<ExportCountryResearchFindingCategory, string> CategoryValues =
        new()
        {
            [ExportCountryResearchFindingCategory.Customs] = "customs",
            [ExportCountryResearchFindingCategory.Tariff] = "tariff",
            [ExportCountryResearchFindingCategory.Documentation] = "documentation",
            [ExportCountryResearchFindingCategory.Origin] = "origin",
            [ExportCountryResearchFindingCategory.Technical] = "technical",
            [ExportCountryResearchFindingCategory.Certification] = "certification",
            [ExportCountryResearchFindingCategory.Labeling] = "labeling",
            [ExportCountryResearchFindingCategory.Packaging] = "packaging",
            [ExportCountryResearchFindingCategory.RestrictedGoods] = "restricted-goods",
            [ExportCountryResearchFindingCategory.TradeAgreement] = "trade-agreement",
            [ExportCountryResearchFindingCategory.Logistics] = "logistics",
            [ExportCountryResearchFindingCategory.BrokerQuestion] = "broker-question",
            [ExportCountryResearchFindingCategory.Other] = "other",
        };

    private static readonly Dictionary<ExportCountryKnowledgeAuthority, string> AuthorityValues =
        new()
        {
            [ExportCountryKnowledgeAuthority.VerifiedRule] = "verified-rule",
            [ExportCountryKnowledgeAuthority.HistoricalObservation] = "historical-observation",
            [ExportCountryKnowledgeAuthority.CustomerRequirement] = "customer-requirement",
            [ExportCountryKnowledgeAuthority.BrokerInput] = "broker-input",
            [ExportCountryKnowledgeAuthority.ManualEntry] = "manual-entry",
        };

    private static readonly Dictionary<ExportSourceVerificationStatus, string> VerificationStatusValues =
        new()
        {
            [ExportSourceVerificationStatus.Unverified] = "unverified",
            [ExportSourceVerificationStatus.UnderReview] = "under-review",
            [ExportSourceVerificationStatus.Verified] = "verified",
            [ExportSourceVerificationStatus.Superseded] = "superseded",
            [ExportSourceVerificationStatus.RevalidationRequired] = "revalidation-required",
        };

    public void Configure(EntityTypeBuilder<ExportCountryKnowledgeEntry> entity)
    {
        entity.ToTable("export_country_knowledge_entries");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        entity.Property(e => e.ProductScope).HasColumnName("product_scope").HasMaxLength(200);
        entity.Property(e => e.ProductGroup).HasColumnName("product_group").HasMaxLength(100);
        entity.Property(e => e.HsCodePattern).HasColumnName("hs_code_pattern").HasMaxLength(20);
        entity.Property(e => e.CustomerReference).HasColumnName("customer_reference").HasMaxLength(200);
        entity.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion(
                value => CategoryValues[value],
                value => CategoryValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(4000);
        entity.Property(e => e.SourcePublisher).HasColumnName("source_publisher").HasMaxLength(200);
        entity.Property(e => e.SourceTitle).HasColumnName("source_title").HasMaxLength(500);
        entity.Property(e => e.SourceUrl).HasColumnName("source_url").HasMaxLength(1000);
        entity.Property(e => e.SourcePublicationDate).HasColumnName("source_publication_date");
        entity.Property(e => e.SourceAccessedAtUtc).HasColumnName("source_accessed_at_utc");
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.Authority)
            .HasColumnName("authority")
            .HasConversion(
                value => AuthorityValues[value],
                value => AuthorityValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(e => e.VerificationStatus)
            .HasColumnName("verification_status")
            .HasConversion(
                value => VerificationStatusValues[value],
                value => VerificationStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(25);
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.VerifiedBySubjectId).HasColumnName("verified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
        entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        entity.Property(e => e.SupersededById).HasColumnName("superseded_by_id");
        entity.Property(e => e.CreatedFromResearchCaseId).HasColumnName("created_from_research_case_id");
        entity.Property(e => e.CreatedFromExportCaseId).HasColumnName("created_from_export_case_id");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.CountryCode, e.Category })
            .HasDatabaseName("ix_export_country_knowledge_entries_tenant_country");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_knowledge_entries_version",
            "version >= 1"));
    }
}
