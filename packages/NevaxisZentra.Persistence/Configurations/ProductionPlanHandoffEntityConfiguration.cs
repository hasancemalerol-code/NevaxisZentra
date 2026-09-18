using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.ProductionPlanning;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ProductionPlanHandoffEntityConfiguration
    : IEntityTypeConfiguration<ProductionPlanHandoff>
{
    private static readonly Dictionary<ProductionPlanHandoffStatus, string> StatusValues =
        new()
        {
            [ProductionPlanHandoffStatus.Prepared] = "prepared",
            [ProductionPlanHandoffStatus.Consumed] = "consumed",
            [ProductionPlanHandoffStatus.Failed] = "failed",
        };

    public void Configure(EntityTypeBuilder<ProductionPlanHandoff> entity)
    {
        entity.ToTable("production_plan_handoffs");
        entity.HasKey(h => h.Id);
        entity.Property(h => h.Id).HasColumnName("id");
        entity.Property(h => h.TenantId).HasColumnName("tenant_id");
        entity.Property(h => h.FacilityId).HasColumnName("facility_id");
        entity.Property(h => h.ProductionPlanId).HasColumnName("production_plan_id");
        entity.Property(h => h.ProductionPlanVersion).HasColumnName("production_plan_version");
        entity.Property(h => h.ReferenceCode)
            .HasColumnName("reference_code")
            .HasMaxLength(100);
        entity.Property(h => h.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(h => h.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(500);
        entity.Property(h => h.PreparedAtUtc).HasColumnName("prepared_at_utc");
        entity.Property(h => h.PreparedBySubjectId)
            .HasColumnName("prepared_by_subject_id")
            .HasMaxLength(200);
        entity.Property(h => h.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        entity.Property(h => h.ProductionReference)
            .HasColumnName("production_reference")
            .HasMaxLength(500);
        entity.Property(h => h.FailureCode)
            .HasColumnName("failure_code")
            .HasMaxLength(100);
        entity.Property(h => h.CorrelationId).HasColumnName("correlation_id");
        entity.Property(h => h.Version).HasColumnName("version");
        entity.Property(h => h.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb");
        entity.Property(h => h.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(h => h.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(h => h.IdempotencyKey).IsUnique();
        entity.HasIndex(h => new { h.TenantId, h.FacilityId, h.ProductionPlanId });
        entity.HasIndex(h => new { h.TenantId, h.Status });

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(h => new { h.TenantId, h.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionPlan>()
            .WithMany()
            .HasForeignKey(h => h.ProductionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_handoffs_status",
            "status IN ('prepared', 'consumed', 'failed')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_handoffs_idempotency_key",
            "idempotency_key <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_handoffs_reference_code",
            "reference_code <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_handoffs_correlation",
            "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_handoffs_version",
            "version >= 1"));
    }
}
