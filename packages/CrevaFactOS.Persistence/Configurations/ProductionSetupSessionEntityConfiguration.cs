using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ProductionSetupSessionEntityConfiguration
    : IEntityTypeConfiguration<ProductionSetupSession>
{
    public void Configure(EntityTypeBuilder<ProductionSetupSession> entity)
    {
        entity.ToTable("production_setup_sessions");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.TenantId).HasColumnName("tenant_id");
        entity.Property(s => s.FacilityId).HasColumnName("facility_id");
        entity.Property(s => s.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(s => s.SetupType).HasColumnName("setup_type").HasMaxLength(50);
        entity.Property(s => s.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(s => s.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(s => s.DurationSeconds).HasColumnName("duration_seconds");
        entity.Property(s => s.PreviousMachineReference).HasColumnName("previous_machine_reference").HasMaxLength(200);
        entity.Property(s => s.PreviousMoldReference).HasColumnName("previous_mold_reference").HasMaxLength(200);
        entity.Property(s => s.TargetMachineReference).HasColumnName("target_machine_reference").HasMaxLength(200);
        entity.Property(s => s.TargetMoldReference).HasColumnName("target_mold_reference").HasMaxLength(200);
        entity.Property(s => s.Comment).HasColumnName("comment").HasMaxLength(1000);
        entity.Property(s => s.StartedBySubjectId).HasColumnName("started_by_subject_id").HasMaxLength(200);
        entity.Property(s => s.CompletedBySubjectId).HasColumnName("completed_by_subject_id").HasMaxLength(200);

        // 4L recovery metadata
        entity.Property(s => s.RecoveredAtUtc).HasColumnName("recovered_at_utc");
        entity.Property(s => s.RecoveredBySubjectId).HasColumnName("recovered_by_subject_id").HasMaxLength(200);
        entity.Property(s => s.RecoveryReason).HasColumnName("recovery_reason").HasMaxLength(1000);

        entity.Property(s => s.CorrelationId).HasColumnName("correlation_id");
        entity.Property(s => s.Version).HasColumnName("version");

        entity.HasIndex(s => new { s.TenantId, s.FacilityId, s.ProductionOrderId, s.StartedAtUtc });
        entity.HasIndex(s => s.ProductionOrderId);

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(s => new { s.TenantId, s.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionOrder>()
            .WithMany()
            .HasForeignKey(s => s.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_production_setup_sessions_type",
                "setup_type IN ('initial-setup', 'mold-change', 'material-change', 'product-change', 'parameter-adjustment', 'other')");
            table.HasCheckConstraint(
                "ck_production_setup_sessions_completed_after_started",
                "(completed_at_utc IS NULL OR completed_at_utc >= started_at_utc)");
            table.HasCheckConstraint(
                "ck_production_setup_sessions_duration",
                "(duration_seconds IS NULL) = (completed_at_utc IS NULL)");
            table.HasCheckConstraint(
                "ck_production_setup_sessions_version",
                "version >= 1");
        });
    }
}
