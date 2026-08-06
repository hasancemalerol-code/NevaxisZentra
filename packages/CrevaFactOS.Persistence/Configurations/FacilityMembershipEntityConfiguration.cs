using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class FacilityMembershipEntityConfiguration
    : IEntityTypeConfiguration<FacilityMembership>
{
    public void Configure(EntityTypeBuilder<FacilityMembership> entity)
    {
        entity.ToTable("facility_memberships");
        entity.HasKey(membership => membership.Id);
        entity.Property(membership => membership.Id).HasColumnName("id");
        entity.Property(membership => membership.TenantId).HasColumnName("tenant_id");
        entity.Property(membership => membership.FacilityId).HasColumnName("facility_id");
        entity.Property(membership => membership.TenantMembershipId)
            .HasColumnName("tenant_membership_id");
        entity.Property(membership => membership.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);
        entity.Property(membership => membership.CreatedAtUtc)
            .HasColumnName("created_at_utc");
        entity.Property(membership => membership.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");
        entity.Property(membership => membership.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();
        entity.HasIndex(membership => new
        {
            membership.TenantId,
            membership.FacilityId,
            membership.TenantMembershipId,
        }).IsUnique();
        entity.HasIndex(membership => new
        {
            membership.TenantId,
            membership.TenantMembershipId,
            membership.Status,
        });
        entity.HasIndex(membership => new
        {
            membership.TenantId,
            membership.FacilityId,
            membership.Status,
        });
        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(membership => new
            {
                membership.TenantId,
                membership.FacilityId,
            })
            .HasPrincipalKey(facility => new
            {
                facility.TenantId,
                facility.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(membership => new
            {
                membership.TenantId,
                membership.TenantMembershipId,
            })
            .HasPrincipalKey(tenantMembership => new
            {
                tenantMembership.TenantId,
                tenantMembership.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_facility_memberships_status",
            "status IN ('Active', 'Inactive')"));
    }
}
