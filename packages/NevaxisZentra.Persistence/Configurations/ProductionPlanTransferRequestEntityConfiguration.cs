using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.ProductionPlanning;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ProductionPlanTransferRequestEntityConfiguration
    : IEntityTypeConfiguration<ProductionPlanTransferRequest>
{
    private static readonly Dictionary<ProductionPlanTransferRequestStatus, string> StatusValues =
        new()
        {
            [ProductionPlanTransferRequestStatus.Requested] = "requested",
            [ProductionPlanTransferRequestStatus.Processing] = "processing",
            [ProductionPlanTransferRequestStatus.Succeeded] = "succeeded",
            [ProductionPlanTransferRequestStatus.Failed] = "failed",
            [ProductionPlanTransferRequestStatus.Uncertain] = "uncertain",
        };

    public void Configure(EntityTypeBuilder<ProductionPlanTransferRequest> entity)
    {
        entity.ToTable("production_plan_transfer_requests");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ProductionPlanId).HasColumnName("production_plan_id");
        entity.Property(r => r.TargetSystem)
            .HasColumnName("target_system")
            .HasMaxLength(50);
        entity.Property(r => r.OperationType)
            .HasColumnName("operation_type")
            .HasMaxLength(100);
        entity.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(r => r.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(500);
        entity.Property(r => r.AttemptNumber).HasColumnName("attempt_number");
        entity.Property(r => r.RequestedBySubjectId)
            .HasColumnName("requested_by_subject_id")
            .HasMaxLength(200);
        entity.Property(r => r.RequestedAtUtc).HasColumnName("requested_at_utc");
        entity.Property(r => r.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(r => r.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(r => r.ExternalReference)
            .HasColumnName("external_reference")
            .HasMaxLength(500);
        entity.Property(r => r.FailureCode)
            .HasColumnName("failure_code")
            .HasMaxLength(100);
        entity.Property(r => r.SafeFailureMessage)
            .HasColumnName("safe_failure_message")
            .HasMaxLength(2000);
        entity.Property(r => r.RetryableFailure).HasColumnName("retryable_failure");
        entity.Property(r => r.ResponseFingerprint)
            .HasColumnName("response_fingerprint")
            .HasMaxLength(200);
        entity.Property(r => r.CorrelationId).HasColumnName("correlation_id");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.ProductionPlanId });
        entity.HasIndex(r => new { r.TenantId, r.Status });
        entity.HasIndex(r => new { r.TenantId, r.TargetSystem, r.Status });
        entity.HasIndex(r => r.IdempotencyKey).IsUnique();

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionPlan>()
            .WithMany()
            .HasForeignKey(r => r.ProductionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_transfer_requests_status",
            "status IN ('requested', 'processing', 'succeeded', 'failed', 'uncertain')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_transfer_requests_target_system",
            "target_system IN ('erp', 'mes')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_transfer_requests_operation_type",
            "operation_type <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_transfer_requests_idempotency_key",
            "idempotency_key <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_transfer_requests_attempt_number",
            "attempt_number >= 1"));
    }
}
