using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ExportShipmentEntityConfiguration : IEntityTypeConfiguration<ExportShipment>
{
    private static readonly Dictionary<ExportShipmentStatus, string> ShipmentStatusValues =
        new()
        {
            [ExportShipmentStatus.Draft] = "draft",
            [ExportShipmentStatus.Preparing] = "preparing",
            [ExportShipmentStatus.Ready] = "ready",
            [ExportShipmentStatus.HandedToBroker] = "handed-to-broker",
            [ExportShipmentStatus.ClearedForDispatch] = "cleared-for-dispatch",
            [ExportShipmentStatus.Dispatched] = "dispatched",
            [ExportShipmentStatus.Closed] = "closed",
            [ExportShipmentStatus.Cancelled] = "cancelled",
        };

    public void Configure(EntityTypeBuilder<ExportShipment> entity)
    {
        entity.ToTable("export_shipments");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.ShipmentReference).HasColumnName("shipment_reference").HasMaxLength(100);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => ShipmentStatusValues[value],
                value => ShipmentStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(e => e.TransportMode).HasColumnName("transport_mode").HasMaxLength(20);
        entity.Property(e => e.IncotermCode).HasColumnName("incoterm_code").HasMaxLength(20);
        entity.Property(e => e.DestinationCountryCode).HasColumnName("destination_country_code").HasMaxLength(2);
        entity.Property(e => e.DestinationCity).HasColumnName("destination_city").HasMaxLength(100);
        entity.Property(e => e.DeliveryAddress).HasColumnName("delivery_address").HasMaxLength(500);
        entity.Property(e => e.ForwarderName).HasColumnName("forwarder_name").HasMaxLength(200);
        entity.Property(e => e.CarrierName).HasColumnName("carrier_name").HasMaxLength(200);
        entity.Property(e => e.PlannedPickupAtUtc).HasColumnName("planned_pickup_at_utc");
        entity.Property(e => e.PlannedDepartureAtUtc).HasColumnName("planned_departure_at_utc");
        entity.Property(e => e.PlannedArrivalAtUtc).HasColumnName("planned_arrival_at_utc");
        entity.Property(e => e.ActualPickupAtUtc).HasColumnName("actual_pickup_at_utc");
        entity.Property(e => e.ActualDepartureAtUtc).HasColumnName("actual_departure_at_utc");
        entity.Property(e => e.ActualArrivalAtUtc).HasColumnName("actual_arrival_at_utc");
        entity.Property(e => e.BookingReference).HasColumnName("booking_reference").HasMaxLength(100);
        entity.Property(e => e.TrackingReference).HasColumnName("tracking_reference").HasMaxLength(200);
        entity.Property(e => e.TotalGrossWeight).HasColumnName("total_gross_weight").HasPrecision(14, 4);
        entity.Property(e => e.TotalNetWeight).HasColumnName("total_net_weight").HasPrecision(14, 4);
        entity.Property(e => e.TotalPackageCount).HasColumnName("total_package_count");
        entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId, e.ShipmentReference })
            .IsUnique()
            .HasDatabaseName("uix_export_shipments_tenant_facility_case_reference");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_shipments_tenant_facility_case");

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
            "ck_export_shipments_status",
            "status IN ('draft', 'preparing', 'ready', 'handed-to-broker', 'cleared-for-dispatch', 'dispatched', 'closed', 'cancelled')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_shipments_reference",
            "shipment_reference <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_shipments_version",
            "version >= 1"));
    }
}
