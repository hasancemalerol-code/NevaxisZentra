using NevaxisFactOS.Persistence.Export;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class ExportPackingItemEntityConfiguration : IEntityTypeConfiguration<ExportPackingItem>
{
    public void Configure(EntityTypeBuilder<ExportPackingItem> entity)
    {
        entity.ToTable("export_packing_items");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.PackingUnitId).HasColumnName("packing_unit_id");
        entity.Property(e => e.SourceOrderLineId).HasColumnName("source_order_line_id");
        entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(100);
        entity.Property(e => e.ProductName).HasColumnName("product_name").HasMaxLength(300);
        entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 6);
        entity.Property(e => e.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(40);
        entity.Property(e => e.LotBatchReference).HasColumnName("lot_batch_reference").HasMaxLength(200);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_packing_items_tenant_facility_case");
        entity.HasIndex(e => e.PackingUnitId)
            .HasDatabaseName("ix_export_packing_items_packing_unit");

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
        entity.HasOne<ExportPackingUnit>()
            .WithMany()
            .HasForeignKey(e => e.PackingUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_packing_items_product_code",
            "product_code <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_packing_items_quantity",
            "quantity > 0"));
    }
}
