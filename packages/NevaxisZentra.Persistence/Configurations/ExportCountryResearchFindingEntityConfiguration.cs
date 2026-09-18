using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportCountryResearchFindingEntityConfiguration
    : IEntityTypeConfiguration<ExportCountryResearchFinding>
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

    private static readonly Dictionary<ExportComplianceRuleType, string> RuleTypeValues =
        new()
        {
            [ExportComplianceRuleType.Document] = "document",
            [ExportComplianceRuleType.Certification] = "certification",
            [ExportComplianceRuleType.Labeling] = "labeling",
            [ExportComplianceRuleType.Packaging] = "packaging",
            [ExportComplianceRuleType.Origin] = "origin",
            [ExportComplianceRuleType.Customs] = "customs",
            [ExportComplianceRuleType.Technical] = "technical",
            [ExportComplianceRuleType.Restriction] = "restriction",
            [ExportComplianceRuleType.Inspection] = "inspection",
            [ExportComplianceRuleType.Transport] = "transport",
            [ExportComplianceRuleType.Other] = "other",
        };

    private static readonly Dictionary<ExportComplianceSeverity, string> SeverityValues =
        new()
        {
            [ExportComplianceSeverity.Info] = "info",
            [ExportComplianceSeverity.Warning] = "warning",
            [ExportComplianceSeverity.Blocking] = "blocking",
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

    public void Configure(EntityTypeBuilder<ExportCountryResearchFinding> entity)
    {
        entity.ToTable("export_country_research_findings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.CountryResearchCaseId).HasColumnName("country_research_case_id");
        entity.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion(
                value => CategoryValues[value],
                value => CategoryValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(4000);
        entity.Property(e => e.RequirementType)
            .HasColumnName("requirement_type")
            .HasConversion(
                value => value.HasValue ? RuleTypeValues[value.Value] : null,
                value => string.IsNullOrEmpty(value)
                    ? null
                    : RuleTypeValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasConversion(
                value => value.HasValue ? SeverityValues[value.Value] : null,
                value => string.IsNullOrEmpty(value)
                    ? null
                    : SeverityValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.BlockingPotential).HasColumnName("blocking_potential");
        entity.Property(e => e.SourcePublisher).HasColumnName("source_publisher").HasMaxLength(200);
        entity.Property(e => e.SourceTitle).HasColumnName("source_title").HasMaxLength(500);
        entity.Property(e => e.SourceUrl).HasColumnName("source_url").HasMaxLength(1000);
        entity.Property(e => e.SourcePublicationDate).HasColumnName("source_publication_date");
        entity.Property(e => e.SourceAccessedAtUtc).HasColumnName("source_accessed_at_utc");
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.EffectiveDate).HasColumnName("effective_date");
        entity.Property(e => e.Confidence).HasColumnName("confidence");
        entity.Property(e => e.VerificationStatus)
            .HasColumnName("verification_status")
            .HasConversion(
                value => VerificationStatusValues[value],
                value => VerificationStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(25);
        entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => e.CountryResearchCaseId)
            .HasDatabaseName("ix_export_country_research_findings_case");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportCountryResearchCase>()
            .WithMany()
            .HasForeignKey(e => e.CountryResearchCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_research_findings_confidence",
            "confidence >= 0 AND confidence <= 100"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_research_findings_version",
            "version >= 1"));
    }
}
