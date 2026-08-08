using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ExportCaseEntityConfiguration : IEntityTypeConfiguration<ExportCase>
{
    private static readonly Dictionary<ExportPackingStatus, string> PackingStatusValues =
        new()
        {
            [ExportPackingStatus.Draft] = "draft",
            [ExportPackingStatus.Packing] = "packing",
            [ExportPackingStatus.Complete] = "complete",
            [ExportPackingStatus.Verified] = "verified",
        };

    private static readonly Dictionary<ExportCaseStatus, string> CaseStatusValues =
        new()
        {
            [ExportCaseStatus.Open] = "open",
            [ExportCaseStatus.Preparing] = "preparing",
            [ExportCaseStatus.Blocked] = "blocked",
            [ExportCaseStatus.Ready] = "ready",
            [ExportCaseStatus.Closed] = "closed",
        };

    private static readonly Dictionary<ExportReadinessStatus, string> ReadinessStatusValues =
        new()
        {
            [ExportReadinessStatus.NotReady] = "not-ready",
            [ExportReadinessStatus.Attention] = "attention",
            [ExportReadinessStatus.Ready] = "ready",
        };

    public void Configure(EntityTypeBuilder<ExportCase> entity)
    {
        entity.ToTable("export_cases");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.SourceOrderId).HasColumnName("source_order_id");
        entity.Property(e => e.CaseNumber).HasColumnName("case_number").HasMaxLength(40);
        entity.Property(e => e.SourceOrderReference).HasColumnName("source_order_reference").HasMaxLength(200);
        entity.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(300);
        entity.Property(e => e.DestinationCountryCode).HasColumnName("destination_country_code").HasMaxLength(2);
        entity.Property(e => e.OriginCountryCode).HasColumnName("origin_country_code").HasMaxLength(2);
        entity.Property(e => e.IncotermCode).HasColumnName("incoterm_code").HasMaxLength(20);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => CaseStatusValues[value],
                value => CaseStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.ReadinessStatus)
            .HasColumnName("readiness_status")
            .HasConversion(
                value => ReadinessStatusValues[value],
                value => ReadinessStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.ReadinessPercent).HasColumnName("readiness_percent");
        entity.Property(e => e.BlockingIssueCount).HasColumnName("blocking_issue_count");
        entity.Property(e => e.MissingRequirementCount).HasColumnName("missing_requirement_count");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.PackingStatus)
            .HasColumnName("packing_status")
            .HasConversion(
                value => PackingStatusValues[value],
                value => PackingStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.PackingCompletedAtUtc).HasColumnName("packing_completed_at_utc");
        entity.Property(e => e.PackingCompletedBySubjectId)
            .HasColumnName("packing_completed_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.PackingVerifiedAtUtc).HasColumnName("packing_verified_at_utc");
        entity.Property(e => e.PackingVerifiedBySubjectId)
            .HasColumnName("packing_verified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.SourceOrderId })
            .IsUnique()
            .HasDatabaseName("uix_export_cases_tenant_facility_source_order");
        entity.HasIndex(e => new { e.TenantId, e.CaseNumber })
            .IsUnique()
            .HasDatabaseName("uix_export_cases_tenant_case_number");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.Status })
            .HasDatabaseName("ix_export_cases_tenant_facility_status");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.UpdatedAtUtc })
            .HasDatabaseName("ix_export_cases_tenant_facility_updated");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_cases_status",
            "status IN ('open', 'preparing', 'blocked', 'ready', 'closed')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_cases_readiness_status",
            "readiness_status IN ('not-ready', 'attention', 'ready')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_cases_readiness_percent",
            "readiness_percent >= 0 AND readiness_percent <= 100"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_cases_blocking_issue_count",
            "blocking_issue_count >= 0"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_cases_missing_requirement_count",
            "missing_requirement_count >= 0"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_cases_version",
            "version >= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_cases_case_number",
            "case_number <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_cases_destination_country",
            "destination_country_code ~ '^[A-Z]{2}$'"));
    }
}
