using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportComplianceEvaluationEntityConfiguration
    : IEntityTypeConfiguration<ExportComplianceEvaluation>
{
    private static readonly Dictionary<ExportComplianceEvaluationResult, string> ResultValues =
        new()
        {
            [ExportComplianceEvaluationResult.Ready] = "ready",
            [ExportComplianceEvaluationResult.Attention] = "attention",
            [ExportComplianceEvaluationResult.Blocked] = "blocked",
            [ExportComplianceEvaluationResult.UnableToEvaluate] = "unable-to-evaluate",
        };

    public void Configure(EntityTypeBuilder<ExportComplianceEvaluation> entity)
    {
        entity.ToTable("export_compliance_evaluations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.ShipmentId).HasColumnName("shipment_id");
        entity.Property(e => e.EvaluationNumber).HasColumnName("evaluation_number");
        entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        entity.Property(e => e.OriginCountryCode).HasColumnName("origin_country_code").HasMaxLength(2);
        entity.Property(e => e.HsCodeSnapshot).HasColumnName("hs_code_snapshot").HasMaxLength(20);
        entity.Property(e => e.RulePackId).HasColumnName("rule_pack_id");
        entity.Property(e => e.RulePackVersionNumber).HasColumnName("rule_pack_version_number");
        entity.Property(e => e.Result)
            .HasColumnName("result")
            .HasConversion(
                value => ResultValues[value],
                value => ResultValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.ReadinessPercent).HasColumnName("readiness_percent");
        entity.Property(e => e.IsStale).HasColumnName("is_stale");
        entity.Property(e => e.RevalidationRequired).HasColumnName("revalidation_required");
        entity.Property(e => e.SupersedesEvaluationId).HasColumnName("supersedes_evaluation_id");
        entity.Property(e => e.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(e => e.EvaluatedBySubjectId).HasColumnName("evaluated_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId, e.EvaluationNumber })
            .IsUnique()
            .HasDatabaseName("uix_export_compliance_evaluations_case_number");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_compliance_evaluations_tenant_facility_case");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportCase>()
            .WithMany()
            .HasForeignKey(e => e.ExportCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_compliance_evaluations_result",
            "result IN ('ready', 'attention', 'blocked', 'unable-to-evaluate')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_compliance_evaluations_readiness_percent",
            "readiness_percent >= 0 AND readiness_percent <= 100"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_compliance_evaluations_evaluation_number",
            "evaluation_number >= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_compliance_evaluations_version",
            "version >= 1"));
    }
}
