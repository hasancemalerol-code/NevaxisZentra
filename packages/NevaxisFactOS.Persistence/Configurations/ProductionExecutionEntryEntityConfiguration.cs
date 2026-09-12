using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Production;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class ProductionExecutionEntryEntityConfiguration
    : IEntityTypeConfiguration<ProductionExecutionEntry>
{
    private static readonly Dictionary<ProductionExecutionEntryType, string> EntryTypeValues =
        new()
        {
            [ProductionExecutionEntryType.GoodProduction] = "good-production",
            [ProductionExecutionEntryType.Scrap] = "scrap",
            [ProductionExecutionEntryType.Reversal] = "reversal",
        };

    public void Configure(EntityTypeBuilder<ProductionExecutionEntry> entity)
    {
        entity.ToTable("production_execution_entries");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(e => e.EntryType)
            .HasColumnName("entry_type")
            .HasConversion(
                value => EntryTypeValues[value],
                value => EntryTypeValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(50);
        entity.Property(e => e.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        entity.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(1000);
        entity.Property(e => e.RecordedBySubjectId)
            .HasColumnName("recorded_by_subject_id")
            .HasMaxLength(200);
        entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
        entity.Property(e => e.ReversesEntryId).HasColumnName("reverses_entry_id");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.Version).HasColumnName("version");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ProductionOrderId, e.RecordedAtUtc });
        entity.HasIndex(e => e.ProductionOrderId);
        entity.HasIndex(e => e.ReversesEntryId)
            .HasFilter("reverses_entry_id IS NOT NULL");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionOrder>()
            .WithMany()
            .HasForeignKey(e => e.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_execution_entries_entry_type",
            "entry_type IN ('good-production', 'scrap', 'reversal')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_execution_entries_unit",
            "unit IN ('piece')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_execution_entries_quantity_positive",
            "quantity > 0"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_execution_entries_version",
            "version >= 1"));
    }
}
