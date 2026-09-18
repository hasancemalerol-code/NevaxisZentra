using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class MaterialLotEntityConfiguration : IEntityTypeConfiguration<Material.MaterialLot>
{
    public void Configure(EntityTypeBuilder<Material.MaterialLot> entity)
    {
        entity.ToTable("material_lots");
        entity.HasKey(l => l.Id);
        entity.Property(l => l.Id).HasColumnName("id");
        entity.Property(l => l.TenantId).HasColumnName("tenant_id");
        entity.Property(l => l.FacilityId).HasColumnName("facility_id");
        entity.Property(l => l.MaterialId).HasColumnName("material_id");
        entity.Property(l => l.LotNumber).HasColumnName("lot_number").HasMaxLength(100);
        entity.Property(l => l.LotNumberNormalized).HasColumnName("lot_number_normalized").HasMaxLength(100);
        entity.Property(l => l.SupplierLotNumber).HasColumnName("supplier_lot_number").HasMaxLength(100);
        entity.Property(l => l.ManufacturerLotNumber).HasColumnName("manufacturer_lot_number").HasMaxLength(100);
        entity.Property(l => l.SupplierReference).HasColumnName("supplier_reference").HasMaxLength(200);
        entity.Property(l => l.PurchaseOrderReference).HasColumnName("purchase_order_reference").HasMaxLength(200);
        entity.Property(l => l.GoodsReceiptReference).HasColumnName("goods_receipt_reference").HasMaxLength(200);
        entity.Property(l => l.ExternalSystem).HasColumnName("external_system").HasMaxLength(100);
        entity.Property(l => l.ExternalLotReference).HasColumnName("external_lot_reference").HasMaxLength(200);
        entity.Property(l => l.ManufactureDate).HasColumnName("manufacture_date");
        entity.Property(l => l.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(l => l.ExpiryDate).HasColumnName("expiry_date");
        entity.Property(l => l.RetestDate).HasColumnName("retest_date");
        entity.Property(l => l.InitialQuantity).HasColumnName("initial_quantity").HasPrecision(18, 4);
        entity.Property(l => l.CurrentObservedQuantity).HasColumnName("current_observed_quantity").HasPrecision(18, 4);
        entity.Property(l => l.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20);
        entity.Property(l => l.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(
                v => Material.MaterialLotValueConversions.ToStorage(v),
                v => Material.MaterialLotValueConversions.LotStatusFromStorage(v));
        entity.Property(l => l.QualityStatus).HasColumnName("quality_status").HasMaxLength(30)
            .HasConversion(
                v => Material.MaterialLotValueConversions.ToStorage(v),
                v => Material.MaterialLotValueConversions.LotQualityStatusFromStorage(v));
        entity.Property(l => l.CurrentStorageLocationId).HasColumnName("current_storage_location_id");
        entity.Property(l => l.CurrentWarehouseReference).HasColumnName("current_warehouse_reference").HasMaxLength(100);
        entity.Property(l => l.CurrentLocationReference).HasColumnName("current_location_reference").HasMaxLength(100);
        entity.Property(l => l.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(
                v => Material.MaterialValueConversions.ToStorage(v),
                v => Material.MaterialValueConversions.SourceFromStorage(v));
        entity.Property(l => l.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(l => l.DataRecordedAtUtc).HasColumnName("data_recorded_at_utc");
        entity.Property(l => l.Notes).HasColumnName("notes").HasMaxLength(2000);
        entity.Property(l => l.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(l => l.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(l => l.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(l => l.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(l => l.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(l => l.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(l => l.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(l => new { l.TenantId, l.MaterialId, l.LotNumberNormalized }).IsUnique();
        entity.HasIndex(l => new { l.TenantId, l.CreateIdempotencyKey }).IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(l => new { l.TenantId, l.FacilityId, l.Status });
        entity.HasIndex(l => new { l.TenantId, l.FacilityId, l.QualityStatus });
        entity.HasIndex(l => new { l.TenantId, l.FacilityId, l.MaterialId });

        entity.HasOne<Tenant>().WithMany().HasForeignKey(l => l.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(l => new { l.TenantId, l.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(l => l.Material).WithMany()
            .HasForeignKey(l => l.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(l => l.CurrentStorageLocation).WithMany()
            .HasForeignKey(l => l.CurrentStorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaterialStorageLocationEntityConfiguration
    : IEntityTypeConfiguration<Material.MaterialStorageLocation>
{
    public void Configure(EntityTypeBuilder<Material.MaterialStorageLocation> entity)
    {
        entity.ToTable("material_storage_locations");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.TenantId).HasColumnName("tenant_id");
        entity.Property(s => s.FacilityId).HasColumnName("facility_id");
        entity.Property(s => s.WarehouseCode).HasColumnName("warehouse_code").HasMaxLength(50);
        entity.Property(s => s.WarehouseCodeNormalized).HasColumnName("warehouse_code_normalized").HasMaxLength(50);
        entity.Property(s => s.WarehouseName).HasColumnName("warehouse_name").HasMaxLength(200);
        entity.Property(s => s.LocationCode).HasColumnName("location_code").HasMaxLength(50);
        entity.Property(s => s.LocationCodeNormalized).HasColumnName("location_code_normalized").HasMaxLength(50);
        entity.Property(s => s.LocationName).HasColumnName("location_name").HasMaxLength(200);
        entity.Property(s => s.IsActive).HasColumnName("is_active");
        entity.Property(s => s.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(
                v => Material.MaterialValueConversions.ToStorage(v),
                v => Material.MaterialValueConversions.SourceFromStorage(v));
        entity.Property(s => s.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(s => s.DataRecordedAtUtc).HasColumnName("data_recorded_at_utc");
        entity.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(s => s.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(s => s.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(s => s.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(s => new { s.TenantId, s.FacilityId, s.WarehouseCodeNormalized, s.LocationCodeNormalized }).IsUnique();
        entity.HasIndex(s => new { s.TenantId, s.FacilityId, s.IsActive });

        entity.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(s => new { s.TenantId, s.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaterialLotTraceabilityEventEntityConfiguration
    : IEntityTypeConfiguration<Material.MaterialLotTraceabilityEvent>
{
    public void Configure(EntityTypeBuilder<Material.MaterialLotTraceabilityEvent> entity)
    {
        entity.ToTable("material_lot_traceability_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.MaterialLotId).HasColumnName("material_lot_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(30)
            .HasConversion(
                v => Material.MaterialLotValueConversions.ToStorage(v),
                v => Material.MaterialLotValueConversions.LotEventTypeFromStorage(v));
        entity.Property(e => e.EventAtUtc).HasColumnName("event_at_utc");
        entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        entity.Property(e => e.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20);
        entity.Property(e => e.FromWarehouseReference).HasColumnName("from_warehouse_reference").HasMaxLength(100);
        entity.Property(e => e.FromLocationReference).HasColumnName("from_location_reference").HasMaxLength(100);
        entity.Property(e => e.ToWarehouseReference).HasColumnName("to_warehouse_reference").HasMaxLength(100);
        entity.Property(e => e.ToLocationReference).HasColumnName("to_location_reference").HasMaxLength(100);
        entity.Property(e => e.StatusBefore).HasColumnName("status_before").HasMaxLength(20)
            .HasConversion(
                v => v.HasValue ? Material.MaterialLotValueConversions.ToStorage(v.Value) : null,
                v => v == null ? null : Material.MaterialLotValueConversions.LotStatusFromStorage(v));
        entity.Property(e => e.StatusAfter).HasColumnName("status_after").HasMaxLength(20)
            .HasConversion(
                v => v.HasValue ? Material.MaterialLotValueConversions.ToStorage(v.Value) : null,
                v => v == null ? null : Material.MaterialLotValueConversions.LotStatusFromStorage(v));
        entity.Property(e => e.QualityStatusBefore).HasColumnName("quality_status_before").HasMaxLength(30)
            .HasConversion(
                v => v.HasValue ? Material.MaterialLotValueConversions.ToStorage(v.Value) : null,
                v => v == null ? null : Material.MaterialLotValueConversions.LotQualityStatusFromStorage(v));
        entity.Property(e => e.QualityStatusAfter).HasColumnName("quality_status_after").HasMaxLength(30)
            .HasConversion(
                v => v.HasValue ? Material.MaterialLotValueConversions.ToStorage(v.Value) : null,
                v => v == null ? null : Material.MaterialLotValueConversions.LotQualityStatusFromStorage(v));
        entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1000);
        entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(
                v => Material.MaterialValueConversions.ToStorage(v),
                v => Material.MaterialValueConversions.SourceFromStorage(v));
        entity.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(e => e.ExternalEventReference).HasColumnName("external_event_reference").HasMaxLength(200);
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(e => e.PayloadFingerprint).HasColumnName("payload_fingerprint").HasMaxLength(64);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.MaterialLotId, e.IdempotencyKey }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MaterialLotId, e.EventAtUtc });

        entity.HasOne(e => e.MaterialLot).WithMany()
            .HasForeignKey(e => e.MaterialLotId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaterialLotCertificateReferenceEntityConfiguration
    : IEntityTypeConfiguration<Material.MaterialLotCertificateReference>
{
    public void Configure(EntityTypeBuilder<Material.MaterialLotCertificateReference> entity)
    {
        entity.ToTable("material_lot_certificate_references");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.MaterialLotId).HasColumnName("material_lot_id");
        entity.Property(c => c.CertificateType).HasColumnName("certificate_type").HasMaxLength(30)
            .HasConversion(
                v => Material.MaterialLotValueConversions.ToStorage(v),
                v => Material.MaterialLotValueConversions.LotCertificateTypeFromStorage(v));
        entity.Property(c => c.DocumentReference).HasColumnName("document_reference").HasMaxLength(500);
        entity.Property(c => c.DocumentNumber).HasColumnName("document_number").HasMaxLength(100);
        entity.Property(c => c.Issuer).HasColumnName("issuer").HasMaxLength(200);
        entity.Property(c => c.IssuedDate).HasColumnName("issued_date");
        entity.Property(c => c.ExpiryDate).HasColumnName("expiry_date");
        entity.Property(c => c.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(
                v => Material.MaterialValueConversions.ToStorage(v),
                v => Material.MaterialValueConversions.SourceFromStorage(v));
        entity.Property(c => c.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(c => c.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(c => c.PayloadFingerprint).HasColumnName("payload_fingerprint").HasMaxLength(64);
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(c => c.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(c => c.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(c => new { c.TenantId, c.MaterialLotId, c.IdempotencyKey }).IsUnique();
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.MaterialLotId });

        entity.HasOne(c => c.MaterialLot).WithMany()
            .HasForeignKey(c => c.MaterialLotId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
