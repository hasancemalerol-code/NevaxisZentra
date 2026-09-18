using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportBrokerCorrectionEntityConfiguration
    : IEntityTypeConfiguration<ExportBrokerCorrection>
{
    private static readonly Dictionary<ExportBrokerCorrectionSeverity, string> SeverityValues =
        new()
        {
            [ExportBrokerCorrectionSeverity.Info] = "info",
            [ExportBrokerCorrectionSeverity.Warning] = "warning",
            [ExportBrokerCorrectionSeverity.Blocking] = "blocking",
        };

    private static readonly Dictionary<ExportBrokerCorrectionStatus, string> StatusValues =
        new()
        {
            [ExportBrokerCorrectionStatus.Open] = "open",
            [ExportBrokerCorrectionStatus.Resolved] = "resolved",
            [ExportBrokerCorrectionStatus.Cancelled] = "cancelled",
        };

    public void Configure(EntityTypeBuilder<ExportBrokerCorrection> entity)
    {
        entity.ToTable("export_broker_corrections");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.BrokerWorkspaceId).HasColumnName("broker_workspace_id");
        entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasConversion(
                value => SeverityValues[value],
                value => SeverityValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.RelatedDocumentId).HasColumnName("related_document_id");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        entity.Property(e => e.ResolvedBySubjectId).HasColumnName("resolved_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(1000);

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_broker_corrections_tenant_facility_case");
        entity.HasIndex(e => new { e.BrokerWorkspaceId, e.Status })
            .HasDatabaseName("ix_export_broker_corrections_workspace_status");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportBrokerWorkspace>()
            .WithMany()
            .HasForeignKey(e => e.BrokerWorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_broker_corrections_severity",
            "severity IN ('info', 'warning', 'blocking')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_broker_corrections_status",
            "status IN ('open', 'resolved', 'cancelled')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_broker_corrections_title",
            "title <> ''"));
    }
}
