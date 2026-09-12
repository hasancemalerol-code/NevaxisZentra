using NevaxisFactOS.Persistence.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

public sealed class ProductionOrderShiftAssignmentEntityConfiguration
    : IEntityTypeConfiguration<ProductionOrderShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ProductionOrderShiftAssignment> builder)
    {
        builder.ToTable("production_order_shift_assignments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.FacilityId).HasColumnName("facility_id");
        builder.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
        builder.Property(e => e.ProductionShiftId).HasColumnName("production_shift_id");
        builder.Property(e => e.ShiftCode).HasColumnName("shift_code").HasMaxLength(20);
        builder.Property(e => e.ShiftDate).HasColumnName("shift_date").HasColumnType("date");
        builder.Property(e => e.ShiftStartAtUtc).HasColumnName("shift_start_at_utc");
        builder.Property(e => e.ShiftEndAtUtc).HasColumnName("shift_end_at_utc");
        builder.Property(e => e.AssignedAtUtc).HasColumnName("assigned_at_utc");
        builder.Property(e => e.AssignedBySubjectId).HasColumnName("assigned_by_subject_id").HasMaxLength(200);
        builder.Property(e => e.ReleasedAtUtc).HasColumnName("released_at_utc");
        builder.Property(e => e.ReleasedBySubjectId).HasColumnName("released_by_subject_id").HasMaxLength(200);
        builder.Property(e => e.ReleaseReason).HasColumnName("release_reason").HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        builder.Property(e => e.Version).HasColumnName("version");

        // Active assignment per order — partial uniqueness enforced in DB
        builder.HasIndex(e => new { e.ProductionOrderId, e.ReleasedAtUtc })
            .HasDatabaseName("ix_production_shift_assignments_order_released");

        builder.HasIndex(e => new { e.TenantId, e.FacilityId, e.ShiftStartAtUtc })
            .HasDatabaseName("ix_production_shift_assignments_facility_start");
    }
}
