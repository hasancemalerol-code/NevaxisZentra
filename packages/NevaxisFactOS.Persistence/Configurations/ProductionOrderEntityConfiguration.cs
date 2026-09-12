using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Production;
using NevaxisFactOS.Persistence.ProductionPlanning;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class ProductionOrderEntityConfiguration
    : IEntityTypeConfiguration<ProductionOrder>
{
    private static readonly Dictionary<ProductionOrderStatus, string> StatusValues =
        new()
        {
            [ProductionOrderStatus.Ready] = "ready",
            [ProductionOrderStatus.InProgress] = "in-progress",
            [ProductionOrderStatus.Paused] = "paused",
            [ProductionOrderStatus.Completed] = "completed",
            [ProductionOrderStatus.Cancelled] = "cancelled",
        };

    public void Configure(EntityTypeBuilder<ProductionOrder> entity)
    {
        entity.ToTable("production_orders");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.ProductionPlanHandoffId).HasColumnName("production_plan_handoff_id");
        entity.Property(o => o.SourceProductionPlanId).HasColumnName("source_production_plan_id");
        entity.Property(o => o.SourceProductionPlanVersion).HasColumnName("source_production_plan_version");
        entity.Property(o => o.ReferenceCode)
            .HasColumnName("reference_code")
            .HasMaxLength(200);
        entity.Property(o => o.Title)
            .HasColumnName("title")
            .HasMaxLength(300);
        entity.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(o => o.PlannedStartAtUtc).HasColumnName("planned_start_at_utc");
        entity.Property(o => o.PlannedEndAtUtc).HasColumnName("planned_end_at_utc");
        entity.Property(o => o.ActualStartAtUtc).HasColumnName("actual_start_at_utc");
        entity.Property(o => o.ActualEndAtUtc).HasColumnName("actual_end_at_utc");
        entity.Property(o => o.ShiftName)
            .HasColumnName("shift_name")
            .HasMaxLength(100);
        entity.Property(o => o.MachineReference)
            .HasColumnName("machine_reference")
            .HasMaxLength(200);
        entity.Property(o => o.MoldReference)
            .HasColumnName("mold_reference")
            .HasMaxLength(200);
        entity.Property(o => o.ProductReference)
            .HasColumnName("product_reference")
            .HasMaxLength(200);
        entity.Property(o => o.PlannedQuantity).HasColumnName("planned_quantity");
        entity.Property(o => o.PlannedQuantityUnit)
            .HasColumnName("planned_quantity_unit")
            .HasMaxLength(50);
        entity.Property(o => o.GoodQuantity).HasColumnName("good_quantity");
        entity.Property(o => o.ScrapQuantity).HasColumnName("scrap_quantity");
        entity.Property(o => o.LastProductionEntryAtUtc).HasColumnName("last_production_entry_at_utc");
        entity.Property(o => o.CreatedBySubjectId)
            .HasColumnName("created_by_subject_id")
            .HasMaxLength(200);
        entity.Property(o => o.LastModifiedBySubjectId)
            .HasColumnName("last_modified_by_subject_id")
            .HasMaxLength(200);
        entity.Property(o => o.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(o => o.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(o => o.HistoryJson)
            .HasColumnName("history_json")
            .HasColumnType("jsonb");

        // 3L — operational summary fields
        entity.Property(o => o.TotalDowntimeSeconds).HasColumnName("total_downtime_seconds");
        entity.Property(o => o.TotalSetupSeconds).HasColumnName("total_setup_seconds");
        entity.Property(o => o.ManualCycleCount).HasColumnName("manual_cycle_count");
        entity.Property(o => o.TotalManualCycleDurationMilliseconds).HasColumnName("total_manual_cycle_duration_ms");
        entity.Property(o => o.LastCycleAtUtc).HasColumnName("last_cycle_at_utc");

        // 5L — resource summary fields (denormalized)
        entity.Property(o => o.CurrentShiftId).HasColumnName("current_shift_id");
        entity.Property(o => o.CurrentShiftCode).HasColumnName("current_shift_code").HasMaxLength(20);
        entity.Property(o => o.CurrentShiftStartAtUtc).HasColumnName("current_shift_start_at_utc");
        entity.Property(o => o.CurrentShiftEndAtUtc).HasColumnName("current_shift_end_at_utc");
        entity.Property(o => o.PrimaryOperatorSubjectId).HasColumnName("primary_operator_subject_id").HasMaxLength(200);
        entity.Property(o => o.ActiveOperatorCount).HasColumnName("active_operator_count");
        entity.Property(o => o.LastAssignmentAtUtc).HasColumnName("last_assignment_at_utc");

        entity.HasIndex(o => o.ProductionPlanHandoffId).IsUnique();
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.Status });
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.PlannedStartAtUtc });
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.ReferenceCode });
        entity.HasIndex(o => o.SourceProductionPlanId);

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(o => o.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionPlanHandoff>()
            .WithMany()
            .HasForeignKey(o => o.ProductionPlanHandoffId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_orders_status",
            "status IN ('ready', 'in-progress', 'paused', 'completed', 'cancelled')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_orders_reference_code",
            "reference_code <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_orders_title",
            "title <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_orders_version",
            "version >= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_orders_actual_times",
            "(actual_start_at_utc IS NULL OR actual_end_at_utc IS NULL OR actual_end_at_utc >= actual_start_at_utc)"));
    }
}
