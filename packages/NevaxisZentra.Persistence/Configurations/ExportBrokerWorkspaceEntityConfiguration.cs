using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportBrokerWorkspaceEntityConfiguration : IEntityTypeConfiguration<ExportBrokerWorkspace>
{
    private static readonly Dictionary<ExportBrokerWorkspaceStatus, string> WorkspaceStatusValues =
        new()
        {
            [ExportBrokerWorkspaceStatus.Draft] = "draft",
            [ExportBrokerWorkspaceStatus.PackageReady] = "package-ready",
            [ExportBrokerWorkspaceStatus.Sent] = "sent",
            [ExportBrokerWorkspaceStatus.Acknowledged] = "acknowledged",
            [ExportBrokerWorkspaceStatus.CorrectionRequested] = "correction-requested",
            [ExportBrokerWorkspaceStatus.Ready] = "ready",
            [ExportBrokerWorkspaceStatus.Closed] = "closed",
            [ExportBrokerWorkspaceStatus.Cancelled] = "cancelled",
        };

    public void Configure(EntityTypeBuilder<ExportBrokerWorkspace> entity)
    {
        entity.ToTable("export_broker_workspaces");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.ShipmentId).HasColumnName("shipment_id");
        entity.Property(e => e.BrokerCompanyName).HasColumnName("broker_company_name").HasMaxLength(300);
        entity.Property(e => e.BrokerContactName).HasColumnName("broker_contact_name").HasMaxLength(200);
        entity.Property(e => e.BrokerEmail).HasColumnName("broker_email").HasMaxLength(200);
        entity.Property(e => e.BrokerPhone).HasColumnName("broker_phone").HasMaxLength(50);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => WorkspaceStatusValues[value],
                value => WorkspaceStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(e => e.PreparedAtUtc).HasColumnName("prepared_at_utc");
        entity.Property(e => e.PreparedBySubjectId).HasColumnName("prepared_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.SentAtUtc).HasColumnName("sent_at_utc");
        entity.Property(e => e.SentBySubjectId).HasColumnName("sent_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.AcknowledgedAtUtc).HasColumnName("acknowledged_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.BrokerReference).HasColumnName("broker_reference").HasMaxLength(200);
        entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_broker_workspaces_tenant_facility_case");

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
            "ck_export_broker_workspaces_status",
            "status IN ('draft', 'package-ready', 'sent', 'acknowledged', 'correction-requested', 'ready', 'closed', 'cancelled')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_broker_workspaces_company",
            "broker_company_name <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_broker_workspaces_version",
            "version >= 1"));
    }
}
