using NevaxisFactOS.Persistence.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

public sealed class ProductionOperatorAssignmentEntityConfiguration
    : IEntityTypeConfiguration<ProductionOperatorAssignment>
{
    public void Configure(EntityTypeBuilder<ProductionOperatorAssignment> builder)
    {
        builder.ToTable("production_operator_assignments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.FacilityId).HasColumnName("facility_id");
        builder.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
        builder.Property(e => e.SubjectId).HasColumnName("subject_id").HasMaxLength(200);
        builder.Property(e => e.AssignmentRole).HasColumnName("assignment_role").HasMaxLength(30);
        builder.Property(e => e.AssignedAtUtc).HasColumnName("assigned_at_utc");
        builder.Property(e => e.AssignedBySubjectId).HasColumnName("assigned_by_subject_id").HasMaxLength(200);
        builder.Property(e => e.ReleasedAtUtc).HasColumnName("released_at_utc");
        builder.Property(e => e.ReleasedBySubjectId).HasColumnName("released_by_subject_id").HasMaxLength(200);
        builder.Property(e => e.ReleaseReason).HasColumnName("release_reason").HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        builder.Property(e => e.Version).HasColumnName("version");

        // Active assignments per order
        builder.HasIndex(e => new { e.ProductionOrderId, e.ReleasedAtUtc })
            .HasDatabaseName("ix_production_operator_assignments_order_released");

        // Subject lookup for operator summary
        builder.HasIndex(e => new { e.SubjectId, e.ReleasedAtUtc })
            .HasDatabaseName("ix_production_operator_assignments_subject_released");
    }
}
