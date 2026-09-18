using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Production;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ProductionCycleEntryEntityConfiguration
    : IEntityTypeConfiguration<ProductionCycleEntry>
{
    public void Configure(EntityTypeBuilder<ProductionCycleEntry> entity)
    {
        entity.ToTable("production_cycle_entries");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(c => c.CycleNumber).HasColumnName("cycle_number");
        entity.Property(c => c.CycleStartedAtUtc).HasColumnName("cycle_started_at_utc");
        entity.Property(c => c.CycleEndedAtUtc).HasColumnName("cycle_ended_at_utc");
        entity.Property(c => c.CycleDurationMilliseconds).HasColumnName("cycle_duration_ms");
        entity.Property(c => c.QuantityProduced).HasColumnName("quantity_produced");
        entity.Property(c => c.RecordedBySubjectId).HasColumnName("recorded_by_subject_id").HasMaxLength(200);
        entity.Property(c => c.RecordedAtUtc).HasColumnName("recorded_at_utc");
        entity.Property(c => c.Source).HasColumnName("source").HasMaxLength(20);
        entity.Property(c => c.Comment).HasColumnName("comment").HasMaxLength(1000);
        entity.Property(c => c.CorrelationId).HasColumnName("correlation_id");

        // Unique per-order cycle number
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.ProductionOrderId, c.CycleNumber })
            .IsUnique();
        entity.HasIndex(c => new { c.ProductionOrderId, c.RecordedAtUtc });

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionOrder>()
            .WithMany()
            .HasForeignKey(c => c.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_production_cycle_entries_duration",
                "cycle_duration_ms > 0");
            table.HasCheckConstraint(
                "ck_production_cycle_entries_source",
                "source IN ('manual')");
            table.HasCheckConstraint(
                "ck_production_cycle_entries_cycle_number",
                "cycle_number > 0");
            table.HasCheckConstraint(
                "ck_production_cycle_entries_quantity",
                "quantity_produced IS NULL OR quantity_produced >= 0");
        });
    }
}
