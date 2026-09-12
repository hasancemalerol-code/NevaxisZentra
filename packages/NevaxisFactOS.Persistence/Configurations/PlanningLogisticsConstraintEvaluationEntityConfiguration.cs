using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.ProductionPlanning;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class PlanningLogisticsConstraintEvaluationEntityConfiguration
    : IEntityTypeConfiguration<PlanningLogisticsConstraintEvaluation>
{
    public void Configure(EntityTypeBuilder<PlanningLogisticsConstraintEvaluation> entity)
    {
        entity.ToTable("planning_logistics_constraint_evaluations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
        entity.Property(e => e.SourceExportCaseId).HasColumnName("source_export_case_id");
        entity.Property(e => e.SourceExportCaseNumber)
            .HasColumnName("source_export_case_number")
            .HasMaxLength(100);
        entity.Property(e => e.SourceShipmentId).HasColumnName("source_shipment_id");
        entity.Property(e => e.SourceShipmentReference)
            .HasColumnName("source_shipment_reference")
            .HasMaxLength(100);
        entity.Property(e => e.SourceOrderReference)
            .HasColumnName("source_order_reference")
            .HasMaxLength(100);
        entity.Property(e => e.SourceVersion).HasColumnName("source_version");
        entity.Property(e => e.PickupDeadlineUtc).HasColumnName("pickup_deadline_utc");
        entity.Property(e => e.DepartureDeadlineUtc).HasColumnName("departure_deadline_utc");
        entity.Property(e => e.LatestProductionCompletionUtc).HasColumnName("latest_production_completion_utc");
        entity.Property(e => e.PlannedProductionCompletionUtc).HasColumnName("planned_production_completion_utc");
        entity.Property(e => e.SlackMinutes).HasColumnName("slack_minutes");
        entity.Property(e => e.RiskLevel)
            .HasColumnName("risk_level")
            .HasMaxLength(20);
        entity.Property(e => e.Blocking).HasColumnName("blocking");
        entity.Property(e => e.BlockingReason).HasColumnName("blocking_reason");
        entity.Property(e => e.LinkageStatus)
            .HasColumnName("linkage_status")
            .HasMaxLength(50);
        entity.Property(e => e.ConstraintsJson)
            .HasColumnName("constraints_json")
            .HasColumnType("jsonb");
        entity.Property(e => e.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ProductionPlanId, e.EvaluatedAtUtc });
        entity.HasIndex(e => new { e.TenantId, e.ProductionPlanId });

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionPlan>()
            .WithMany()
            .HasForeignKey(e => e.ProductionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_planning_logistics_evaluations_version",
            "version >= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_planning_logistics_evaluations_risk_level",
            "risk_level IN ('none', 'low', 'medium', 'high', 'critical')"));
    }
}
