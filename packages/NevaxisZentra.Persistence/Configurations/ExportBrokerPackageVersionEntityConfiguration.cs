using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportBrokerPackageVersionEntityConfiguration
    : IEntityTypeConfiguration<ExportBrokerPackageVersion>
{
    private static readonly Dictionary<ExportBrokerPackageVersionStatus, string> StatusValues =
        new()
        {
            [ExportBrokerPackageVersionStatus.Draft] = "draft",
            [ExportBrokerPackageVersionStatus.Sent] = "sent",
            [ExportBrokerPackageVersionStatus.Stale] = "stale",
        };

    public void Configure(EntityTypeBuilder<ExportBrokerPackageVersion> entity)
    {
        entity.ToTable("export_broker_package_versions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.BrokerWorkspaceId).HasColumnName("broker_workspace_id");
        entity.Property(e => e.VersionNumber).HasColumnName("version_number");
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.SentAtUtc).HasColumnName("sent_at_utc");
        entity.Property(e => e.SentBySubjectId).HasColumnName("sent_by_subject_id").HasMaxLength(200);

        entity.HasIndex(e => new { e.BrokerWorkspaceId, e.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uix_export_broker_package_versions_workspace_number");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_broker_package_versions_tenant_facility_case");

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
            "ck_export_broker_package_versions_status",
            "status IN ('draft', 'sent', 'stale')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_broker_package_versions_number",
            "version_number >= 1"));
    }
}
