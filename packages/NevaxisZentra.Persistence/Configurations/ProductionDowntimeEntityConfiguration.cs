using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Production;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ProductionDowntimeEntityConfiguration
    : IEntityTypeConfiguration<ProductionDowntime>
{
    public void Configure(EntityTypeBuilder<ProductionDowntime> entity)
    {
        entity.ToTable("production_downtimes");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(d => d.Category).HasColumnName("category").HasMaxLength(20);
        entity.Property(d => d.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        entity.Property(d => d.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(d => d.EndedAtUtc).HasColumnName("ended_at_utc");
        entity.Property(d => d.DurationSeconds).HasColumnName("duration_seconds");
        entity.Property(d => d.Comment).HasColumnName("comment").HasMaxLength(1000);
        entity.Property(d => d.StartedBySubjectId).HasColumnName("started_by_subject_id").HasMaxLength(200);
        entity.Property(d => d.EndedBySubjectId).HasColumnName("ended_by_subject_id").HasMaxLength(200);

        // 4L recovery metadata
        entity.Property(d => d.RecoveredAtUtc).HasColumnName("recovered_at_utc");
        entity.Property(d => d.RecoveredBySubjectId).HasColumnName("recovered_by_subject_id").HasMaxLength(200);
        entity.Property(d => d.RecoveryReason).HasColumnName("recovery_reason").HasMaxLength(1000);

        entity.Property(d => d.CorrelationId).HasColumnName("correlation_id");
        entity.Property(d => d.Version).HasColumnName("version");

        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.ProductionOrderId, d.StartedAtUtc });
        entity.HasIndex(d => d.ProductionOrderId);

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionOrder>()
            .WithMany()
            .HasForeignKey(d => d.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_production_downtimes_category",
                "category IN ('planned', 'unplanned')");
            table.HasCheckConstraint(
                "ck_production_downtimes_ended_after_started",
                "(ended_at_utc IS NULL OR ended_at_utc >= started_at_utc)");
            table.HasCheckConstraint(
                "ck_production_downtimes_duration",
                "(duration_seconds IS NULL) = (ended_at_utc IS NULL)");
            table.HasCheckConstraint(
                "ck_production_downtimes_version",
                "version >= 1");
        });
    }
}
