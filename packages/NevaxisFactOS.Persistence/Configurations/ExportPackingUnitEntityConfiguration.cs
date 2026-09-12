using NevaxisFactOS.Persistence.Export;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class ExportPackingUnitEntityConfiguration : IEntityTypeConfiguration<ExportPackingUnit>
{
    private static readonly Dictionary<ExportPackingStatus, string> PackingStatusValues =
        new()
        {
            [ExportPackingStatus.Draft] = "draft",
            [ExportPackingStatus.Packing] = "packing",
            [ExportPackingStatus.Complete] = "complete",
            [ExportPackingStatus.Verified] = "verified",
        };

    public void Configure(EntityTypeBuilder<ExportPackingUnit> entity)
    {
        entity.ToTable("export_packing_units");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.UnitCode).HasColumnName("unit_code").HasMaxLength(80);
        entity.Property(e => e.UnitType).HasColumnName("unit_type").HasMaxLength(40);
        entity.Property(e => e.ParentPackingUnitId).HasColumnName("parent_packing_unit_id");
        entity.Property(e => e.GrossWeight).HasColumnName("gross_weight").HasPrecision(14, 4);
        entity.Property(e => e.NetWeight).HasColumnName("net_weight").HasPrecision(14, 4);
        entity.Property(e => e.WeightUnit).HasColumnName("weight_unit").HasMaxLength(10);
        entity.Property(e => e.Length).HasColumnName("length").HasPrecision(14, 4);
        entity.Property(e => e.Width).HasColumnName("width").HasPrecision(14, 4);
        entity.Property(e => e.Height).HasColumnName("height").HasPrecision(14, 4);
        entity.Property(e => e.DimensionUnit).HasColumnName("dimension_unit").HasMaxLength(10);
        entity.Property(e => e.Volume).HasColumnName("volume").HasPrecision(14, 4);
        entity.Property(e => e.MarksAndNumbers).HasColumnName("marks_and_numbers").HasMaxLength(500);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => PackingStatusValues[value],
                value => PackingStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId, e.UnitCode })
            .IsUnique()
            .HasDatabaseName("uix_export_packing_units_tenant_facility_case_code");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_packing_units_tenant_facility_case");

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
            "ck_export_packing_units_status",
            "status IN ('draft', 'packing', 'complete', 'verified')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_packing_units_unit_code",
            "unit_code <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_packing_units_version",
            "version >= 1"));
    }
}
