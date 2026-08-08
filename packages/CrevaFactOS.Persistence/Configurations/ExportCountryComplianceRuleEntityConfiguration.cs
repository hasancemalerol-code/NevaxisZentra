using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ExportCountryComplianceRuleEntityConfiguration
    : IEntityTypeConfiguration<ExportCountryComplianceRule>
{
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

    private static readonly Dictionary<ExportComplianceRequirementMode, string> RequirementModeValues =
        new()
        {
            [ExportComplianceRequirementMode.Mandatory] = "mandatory",
            [ExportComplianceRequirementMode.Conditional] = "conditional",
            [ExportComplianceRequirementMode.Informational] = "informational",
        };

    public void Configure(EntityTypeBuilder<ExportCountryComplianceRule> entity)
    {
        entity.ToTable("export_country_compliance_rules");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.RulePackId).HasColumnName("rule_pack_id");
        entity.Property(e => e.RuleCode).HasColumnName("rule_code").HasMaxLength(80);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.RuleType)
            .HasColumnName("rule_type")
            .HasConversion(
                value => RuleTypeValues[value],
                value => RuleTypeValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasConversion(
                value => SeverityValues[value],
                value => SeverityValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.RequirementMode)
            .HasColumnName("requirement_mode")
            .HasConversion(
                value => RequirementModeValues[value],
                value => RequirementModeValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.ProductScope).HasColumnName("product_scope").HasMaxLength(200);
        entity.Property(e => e.ProductGroup).HasColumnName("product_group").HasMaxLength(100);
        entity.Property(e => e.HsCodePattern).HasColumnName("hs_code_pattern").HasMaxLength(20);
        entity.Property(e => e.OriginCountryCode).HasColumnName("origin_country_code").HasMaxLength(2);
        entity.Property(e => e.TransportMode).HasColumnName("transport_mode").HasMaxLength(20);
        entity.Property(e => e.IncotermCode).HasColumnName("incoterm_code").HasMaxLength(20);
        entity.Property(e => e.PackagingType).HasColumnName("packaging_type").HasMaxLength(40);
        entity.Property(e => e.CustomerCondition).HasColumnName("customer_condition").HasMaxLength(200);
        entity.Property(e => e.RequiredDocumentType).HasColumnName("required_document_type").HasMaxLength(60);
        entity.Property(e => e.RequiredField).HasColumnName("required_field").HasMaxLength(100);
        entity.Property(e => e.RequiredCertification).HasColumnName("required_certification").HasMaxLength(200);
        entity.Property(e => e.Blocking).HasColumnName("blocking");
        entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
        entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        entity.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(500);
        entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);

        entity.HasIndex(e => new { e.RulePackId, e.RuleCode })
            .IsUnique()
            .HasDatabaseName("uix_export_country_compliance_rules_pack_code");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportCountryRulePack>()
            .WithMany()
            .HasForeignKey(e => e.RulePackId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_compliance_rules_rule_code",
            "rule_code <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_country_compliance_rules_version",
            "version >= 1"));
    }
}
