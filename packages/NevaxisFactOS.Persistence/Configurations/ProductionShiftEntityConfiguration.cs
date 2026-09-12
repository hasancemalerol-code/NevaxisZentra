using NevaxisFactOS.Persistence.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

public sealed class ProductionShiftEntityConfiguration
    : IEntityTypeConfiguration<ProductionShift>
{
    public void Configure(EntityTypeBuilder<ProductionShift> builder)
    {
        builder.ToTable("production_shifts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.FacilityId).HasColumnName("facility_id");
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(20);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
        builder.Property(e => e.StartLocalTime).HasColumnName("start_local_time");
        builder.Property(e => e.EndLocalTime).HasColumnName("end_local_time");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.Version).HasColumnName("version");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => new { e.TenantId, e.FacilityId, e.IsActive })
            .HasDatabaseName("ix_production_shifts_facility_active");
    }
}
