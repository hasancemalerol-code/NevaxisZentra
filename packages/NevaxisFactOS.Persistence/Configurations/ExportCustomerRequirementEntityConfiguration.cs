using NevaxisFactOS.Persistence.Export;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class ExportCustomerRequirementEntityConfiguration
    : IEntityTypeConfiguration<ExportCustomerRequirement>
{
    private static readonly Dictionary<ExportComplianceRuleType, string> RequirementTypeValues =
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

    private static readonly Dictionary<ExportCustomerRequirementStatus, string> StatusValues =
        new()
        {
            [ExportCustomerRequirementStatus.Draft] = "draft",
            [ExportCustomerRequirementStatus.Verified] = "verified",
            [ExportCustomerRequirementStatus.Superseded] = "superseded",
            [ExportCustomerRequirementStatus.Archived] = "archived",
        };

    public void Configure(EntityTypeBuilder<ExportCustomerRequirement> entity)
    {
        entity.ToTable("export_customer_requirements");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.CustomerReference).HasColumnName("customer_reference").HasMaxLength(200);
        entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        entity.Property(e => e.ProductScope).HasColumnName("product_scope").HasMaxLength(200);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.RequirementType)
            .HasColumnName("requirement_type")
            .HasConversion(
                value => RequirementTypeValues[value],
                value => RequirementTypeValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasConversion(
                value => SeverityValues[value],
                value => SeverityValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.Blocking).HasColumnName("blocking");
        entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
        entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.VerifiedBySubjectId).HasColumnName("verified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.CustomerReference, e.CountryCode, e.Title })
            .HasDatabaseName("ix_export_customer_requirements_tenant_customer");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_customer_requirements_status",
            "status IN ('draft', 'verified', 'superseded', 'archived')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_customer_requirements_version",
            "version >= 1"));
    }
}
