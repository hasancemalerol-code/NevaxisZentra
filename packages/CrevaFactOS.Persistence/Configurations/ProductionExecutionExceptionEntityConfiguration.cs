using CrevaFactOS.Persistence.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

public sealed class ProductionExecutionExceptionEntityConfiguration
    : IEntityTypeConfiguration<ProductionExecutionExceptionRecord>
{
    public void Configure(EntityTypeBuilder<ProductionExecutionExceptionRecord> builder)
    {
        builder.ToTable("production_execution_exceptions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.FacilityId).HasColumnName("facility_id");
        builder.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
        builder.Property(e => e.ExceptionType).HasColumnName("exception_type").HasMaxLength(50);
        builder.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(e => e.Message).HasColumnName("message").HasMaxLength(500);
        builder.Property(e => e.RaisedAtUtc).HasColumnName("raised_at_utc");
        builder.Property(e => e.RaisedBySubjectId).HasColumnName("raised_by_subject_id").HasMaxLength(200);
        builder.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        builder.Property(e => e.ResolvedBySubjectId).HasColumnName("resolved_by_subject_id").HasMaxLength(200);
        builder.Property(e => e.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(1000);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        builder.Property(e => e.Version).HasColumnName("version");

        // Primary composite index for filtered exception queries
        builder.HasIndex(e => new { e.TenantId, e.FacilityId, e.ProductionOrderId, e.Status })
            .HasDatabaseName("ix_production_exceptions_order_status");

        // Timeline index for history queries
        builder.HasIndex(e => new { e.ProductionOrderId, e.RaisedAtUtc })
            .HasDatabaseName("ix_production_exceptions_order_raised_at");
    }
}
