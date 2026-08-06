using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class FacilityEntityConfiguration
    : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> entity)
    {
        entity.ToTable("facilities");
        entity.HasKey(facility => facility.Id);
        entity.Property(facility => facility.Id).HasColumnName("id");
        entity.Property(facility => facility.TenantId).HasColumnName("tenant_id");
        entity.Property(facility => facility.Code)
            .HasColumnName("code")
            .HasMaxLength(50);
        entity.Property(facility => facility.Name)
            .HasColumnName("name")
            .HasMaxLength(200);
        entity.Property(facility => facility.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);
        entity.Property(facility => facility.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(100);
        entity.Property(facility => facility.CreatedAtUtc)
            .HasColumnName("created_at_utc");
        entity.Property(facility => facility.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");
        entity.Property(facility => facility.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();
        entity.HasIndex(facility => new { facility.TenantId, facility.Code })
            .IsUnique();
        entity.HasIndex(facility => new { facility.TenantId, facility.Id })
            .IsUnique();
        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(facility => facility.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_facilities_status",
            "status IN ('Active', 'Inactive')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_facilities_code",
            "code <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_facilities_name",
            "name <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_facilities_time_zone_id",
            "time_zone_id <> ''"));
    }
}
