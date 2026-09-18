using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportComplianceRequirementEntityConfiguration
    : IEntityTypeConfiguration<ExportComplianceRequirement>
{
    private static readonly Dictionary<ExportComplianceRequirementStatus, string> StatusValues =
        new()
        {
            [ExportComplianceRequirementStatus.Missing] = "missing",
            [ExportComplianceRequirementStatus.Pending] = "pending",
            [ExportComplianceRequirementStatus.Satisfied] = "satisfied",
            [ExportComplianceRequirementStatus.NotApplicable] = "not-applicable",
            [ExportComplianceRequirementStatus.UnableToEvaluate] = "unable-to-evaluate",
        };

    private static readonly Dictionary<ExportComplianceSeverity, string> SeverityValues =
        new()
        {
            [ExportComplianceSeverity.Info] = "info",
            [ExportComplianceSeverity.Warning] = "warning",
            [ExportComplianceSeverity.Blocking] = "blocking",
        };

    public void Configure(EntityTypeBuilder<ExportComplianceRequirement> entity)
    {
        entity.ToTable("export_compliance_requirements");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
        entity.Property(e => e.ShipmentId).HasColumnName("shipment_id");
        entity.Property(e => e.RulePackId).HasColumnName("rule_pack_id");
        entity.Property(e => e.RuleId).HasColumnName("rule_id");
        entity.Property(e => e.RequirementCode).HasColumnName("requirement_code").HasMaxLength(80);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasConversion(
                value => SeverityValues[value],
                value => SeverityValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.Blocking).HasColumnName("blocking");
        entity.Property(e => e.RequiredDocumentType).HasColumnName("required_document_type").HasMaxLength(60);
        entity.Property(e => e.RelatedDocumentId).HasColumnName("related_document_id");
        entity.Property(e => e.ResolutionType).HasColumnName("resolution_type").HasMaxLength(60);
        entity.Property(e => e.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(2000);
        entity.Property(e => e.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(e => e.SatisfiedAtUtc).HasColumnName("satisfied_at_utc");
        entity.Property(e => e.SatisfiedBySubjectId).HasColumnName("satisfied_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(500);
        entity.Property(e => e.RuleVersionSnapshot).HasColumnName("rule_version_snapshot");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.EvaluationId, e.RequirementCode })
            .IsUnique()
            .HasDatabaseName("uix_export_compliance_requirements_evaluation_code");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_compliance_requirements_tenant_facility_case");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportComplianceEvaluation>()
            .WithMany()
            .HasForeignKey(e => e.EvaluationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_compliance_requirements_status",
            "status IN ('missing', 'pending', 'satisfied', 'not-applicable', 'unable-to-evaluate')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_compliance_requirements_version",
            "version >= 1"));
    }
}
