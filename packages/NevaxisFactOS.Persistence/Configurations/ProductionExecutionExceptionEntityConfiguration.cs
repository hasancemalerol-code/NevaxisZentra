using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Production;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

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

        // 5L: exception ownership columns
        builder.Property(e => e.AssignedToSubjectId).HasColumnName("assigned_to_subject_id").HasMaxLength(200);
        builder.Property(e => e.AssignedAtUtc).HasColumnName("assigned_at_utc");
        builder.Property(e => e.AssignedBySubjectId).HasColumnName("assigned_by_subject_id").HasMaxLength(200);

        builder.Property(e => e.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        builder.Property(e => e.FollowUpStatus).HasColumnName("follow_up_status").HasMaxLength(30);
        builder.Property(e => e.FollowUpUpdatedBy).HasColumnName("follow_up_updated_by").HasMaxLength(200);

        // Primary composite index for filtered exception queries
        builder.HasIndex(e => new { e.TenantId, e.FacilityId, e.ProductionOrderId, e.Status })
            .HasDatabaseName("ix_production_exceptions_order_status");

        // Timeline index for history queries
        builder.HasIndex(e => new { e.ProductionOrderId, e.RaisedAtUtc })
            .HasDatabaseName("ix_production_exceptions_order_raised_at");
    }
}

public sealed class ProductionFollowUpAuditEventEntityConfiguration
    : IEntityTypeConfiguration<ProductionFollowUpAuditEvent>
{
    public void Configure(EntityTypeBuilder<ProductionFollowUpAuditEvent> builder)
    {
        builder.ToTable("production_follow_up_audit_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.FacilityId).HasColumnName("facility_id");
        builder.Property(e => e.ExceptionId).HasColumnName("exception_id");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50);
        builder.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
        builder.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(200);
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");

        builder.HasIndex(e => e.ExceptionId);
        builder.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExceptionId, e.OccurredAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Exception).WithMany()
            .HasForeignKey(e => e.ExceptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
