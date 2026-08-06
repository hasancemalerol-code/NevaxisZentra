using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ProductionPlanEntityConfiguration
    : IEntityTypeConfiguration<ProductionPlan>
{
    public void Configure(EntityTypeBuilder<ProductionPlan> entity)
    {
        entity.ToTable("production_plans");
        entity.HasKey(plan => plan.Id);
        entity.Property(plan => plan.Id).HasColumnName("id");
        entity.Property(plan => plan.TenantId).HasColumnName("tenant_id");
        entity.Property(plan => plan.FacilityId).HasColumnName("facility_id");
        entity.Property(plan => plan.ReferenceCode)
            .HasColumnName("reference_code")
            .HasMaxLength(100);
        entity.Property(plan => plan.Title)
            .HasColumnName("title")
            .HasMaxLength(300);
        entity.Property(plan => plan.Status)
            .HasColumnName("status")
            .HasConversion(
                value => ProductionPlanValueConversions.ToStorage(value),
                value => ProductionPlanValueConversions.StatusFromStorage(value))
            .HasMaxLength(30);
        entity.Property(plan => plan.Origin)
            .HasColumnName("origin")
            .HasConversion(
                value => ProductionPlanValueConversions.ToStorage(value),
                value => ProductionPlanValueConversions.OriginFromStorage(value))
            .HasMaxLength(40);
        entity.Property(plan => plan.AiGenerated).HasColumnName("ai_generated");
        entity.Property(plan => plan.Confidence)
            .HasColumnName("confidence")
            .HasConversion(
                value => ProductionPlanValueConversions.ToStorage(value),
                value => ProductionPlanValueConversions.ConfidenceFromStorage(value))
            .HasMaxLength(20);
        entity.Property(plan => plan.PlannedStartAtUtc)
            .HasColumnName("planned_start_at_utc");
        entity.Property(plan => plan.PlannedEndAtUtc)
            .HasColumnName("planned_end_at_utc");
        entity.Property(plan => plan.Notes)
            .HasColumnName("notes")
            .HasMaxLength(4000);
        entity.Property(plan => plan.ApprovalStatus)
            .HasColumnName("approval_status")
            .HasConversion(
                value => ProductionPlanValueConversions.ToStorage(value),
                value => ProductionPlanValueConversions.ApprovalStatusFromStorage(value))
            .HasMaxLength(30);
        entity.Property(plan => plan.TransferStatus)
            .HasColumnName("transfer_status")
            .HasConversion(
                value => ProductionPlanValueConversions.ToStorage(value),
                value => ProductionPlanValueConversions.TransferStatusFromStorage(value))
            .HasMaxLength(30);
        entity.Property(plan => plan.TransferTargetSystem)
            .HasColumnName("transfer_target_system")
            .HasConversion(
                value => value.HasValue
                    ? ProductionPlanValueConversions.ToStorage(value.Value)
                    : null,
                value => string.IsNullOrWhiteSpace(value)
                    ? null
                    : ProductionPlanValueConversions.TargetSystemFromStorage(value))
            .HasMaxLength(30);
        entity.Property(plan => plan.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(200);
        entity.Property(plan => plan.AiExplanation)
            .HasColumnName("ai_explanation")
            .HasMaxLength(4000);
        entity.Property(plan => plan.CreatedAtUtc)
            .HasColumnName("created_at_utc");
        entity.Property(plan => plan.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");
        entity.Property(plan => plan.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();
        entity.Property(plan => plan.DemandsJson)
            .HasColumnName("demands_json")
            .HasColumnType("jsonb");
        entity.Property(plan => plan.RisksJson)
            .HasColumnName("risks_json")
            .HasColumnType("jsonb");
        entity.Property(plan => plan.ResourceAssignmentJson)
            .HasColumnName("resource_assignment_json")
            .HasColumnType("jsonb");
        entity.Property(plan => plan.TimeWindowJson)
            .HasColumnName("time_window_json")
            .HasColumnType("jsonb");
        entity.Property(plan => plan.ApprovalJson)
            .HasColumnName("approval_json")
            .HasColumnType("jsonb");
        entity.Property(plan => plan.TransferJson)
            .HasColumnName("transfer_json")
            .HasColumnType("jsonb");
        entity.Property(plan => plan.SourceReferencesJson)
            .HasColumnName("source_references_json")
            .HasColumnType("jsonb");
        entity.Property(plan => plan.HistoryJson)
            .HasColumnName("history_json")
            .HasColumnType("jsonb");
        entity.HasIndex(plan => new { plan.TenantId, plan.FacilityId, plan.UpdatedAtUtc });
        entity.HasIndex(plan => new { plan.TenantId, plan.FacilityId, plan.ReferenceCode })
            .IsUnique();
        entity.HasIndex(plan => new { plan.TenantId, plan.FacilityId, plan.Status });
        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(plan => plan.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(plan => new { plan.TenantId, plan.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_status",
            "status IN ('draft', 'needs-review', 'ready-for-approval', 'approved', 'rejected', 'scheduled', 'in-progress', 'completed', 'cancelled')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_origin",
            "origin IN ('approved-order', 'erp-production-request', 'manual', 'ai-recommendation')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_confidence",
            "confidence IN ('low', 'medium', 'high')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_approval_status",
            "approval_status IN ('not-required', 'pending', 'approved', 'rejected', 'expired')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_transfer_status",
            "transfer_status IN ('not-started', 'pending', 'in-progress', 'completed', 'failed')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_transfer_target_system",
            "transfer_target_system IS NULL OR transfer_target_system IN ('erp', 'mes', 'crevafactos')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_reference_code",
            "reference_code <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_title",
            "title <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_created_by",
            "created_by <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_planned_window",
            "planned_end_at_utc >= planned_start_at_utc"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_demands_json",
            "jsonb_typeof(demands_json) = 'array'"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_risks_json",
            "jsonb_typeof(risks_json) = 'array'"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_resource_assignment_json",
            "jsonb_typeof(resource_assignment_json) = 'object'"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_time_window_json",
            "jsonb_typeof(time_window_json) = 'object'"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_approval_json",
            "jsonb_typeof(approval_json) = 'object'"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_transfer_json",
            "jsonb_typeof(transfer_json) = 'object'"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_source_references_json",
            "jsonb_typeof(source_references_json) = 'array'"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_production_plans_history_json",
            "jsonb_typeof(history_json) = 'array'"));
    }
}
