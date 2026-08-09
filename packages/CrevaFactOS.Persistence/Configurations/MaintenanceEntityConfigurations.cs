using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class MaintenanceRequestEntityConfiguration : IEntityTypeConfiguration<Maintenance.MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenanceRequest> entity)
    {
        entity.ToTable("maintenance_requests");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.RequestNumber).HasColumnName("request_number").HasMaxLength(50);
        entity.Property(r => r.AssetType).HasColumnName("asset_type").HasMaxLength(50);
        entity.Property(r => r.AssetId).HasColumnName("asset_id").HasMaxLength(200);
        entity.Property(r => r.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(r => r.AssetName).HasColumnName("asset_name").HasMaxLength(200);
        entity.Property(r => r.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(r => r.Type).HasColumnName("type").HasMaxLength(30)
            .HasConversion(v => Maintenance.MaintenanceValueConversions.ToStorage(v), v => Maintenance.MaintenanceValueConversions.RequestTypeFromStorage(v));
        entity.Property(r => r.Priority).HasColumnName("priority").HasMaxLength(20)
            .HasConversion(v => Maintenance.MaintenanceValueConversions.ToStorage(v), v => Maintenance.MaintenanceValueConversions.PriorityFromStorage(v));
        entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(30)
            .HasConversion(v => Maintenance.MaintenanceValueConversions.ToStorage(v), v => Maintenance.MaintenanceValueConversions.RequestStatusFromStorage(v));
        entity.Property(r => r.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(r => r.Description).HasColumnName("description").HasMaxLength(4000);
        entity.Property(r => r.ReportedAtUtc).HasColumnName("reported_at_utc");
        entity.Property(r => r.ReportedBySubjectId).HasColumnName("reported_by_subject_id").HasMaxLength(200);
        entity.Property(r => r.Source).HasColumnName("source").HasMaxLength(50);
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(r => r.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.RequestNumber }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenanceWorkOrderEntityConfiguration : IEntityTypeConfiguration<Maintenance.MaintenanceWorkOrder>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenanceWorkOrder> entity)
    {
        entity.ToTable("maintenance_work_orders");
        entity.HasKey(w => w.Id);
        entity.Property(w => w.Id).HasColumnName("id");
        entity.Property(w => w.TenantId).HasColumnName("tenant_id");
        entity.Property(w => w.FacilityId).HasColumnName("facility_id");
        entity.Property(w => w.WorkOrderNumber).HasColumnName("work_order_number").HasMaxLength(50);
        entity.Property(w => w.MaintenanceRequestId).HasColumnName("maintenance_request_id");
        entity.Property(w => w.AssetType).HasColumnName("asset_type").HasMaxLength(50);
        entity.Property(w => w.AssetId).HasColumnName("asset_id").HasMaxLength(200);
        entity.Property(w => w.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(w => w.AssetName).HasColumnName("asset_name").HasMaxLength(200);
        entity.Property(w => w.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(w => w.Type).HasColumnName("type").HasMaxLength(30)
            .HasConversion(v => Maintenance.MaintenanceValueConversions.ToStorage(v), v => Maintenance.MaintenanceValueConversions.WorkOrderTypeFromStorage(v));
        entity.Property(w => w.Priority).HasColumnName("priority").HasMaxLength(20)
            .HasConversion(v => Maintenance.MaintenanceValueConversions.ToStorage(v), v => Maintenance.MaintenanceValueConversions.PriorityFromStorage(v));
        entity.Property(w => w.Status).HasColumnName("status").HasMaxLength(30)
            .HasConversion(v => Maintenance.MaintenanceValueConversions.ToStorage(v), v => Maintenance.MaintenanceValueConversions.WorkOrderStatusFromStorage(v));
        entity.Property(w => w.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(w => w.Description).HasColumnName("description").HasMaxLength(4000);
        entity.Property(w => w.AssignedTechnicianSubjectId).HasColumnName("assigned_technician_subject_id").HasMaxLength(200);
        entity.Property(w => w.PlannedStartUtc).HasColumnName("planned_start_utc");
        entity.Property(w => w.PlannedEndUtc).HasColumnName("planned_end_utc");
        entity.Property(w => w.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(w => w.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(w => w.CompletionNote).HasColumnName("completion_note").HasMaxLength(2000);
        entity.Property(w => w.ResolutionCode).HasColumnName("resolution_code").HasMaxLength(30)
            .HasConversion(
                v => v.HasValue ? Maintenance.MaintenanceValueConversions.ToStorage(v.Value) : null,
                v => v == null ? null : Maintenance.MaintenanceValueConversions.ResolutionCodeFromStorage(v));
        entity.Property(w => w.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(w => w.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(w => w.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(w => w.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(w => new { w.TenantId, w.FacilityId, w.WorkOrderNumber }).IsUnique();
        entity.HasOne(w => w.MaintenanceRequest).WithMany()
            .HasForeignKey(w => w.MaintenanceRequestId)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(w => w.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(w => new { w.TenantId, w.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PreventiveMaintenancePlanEntityConfiguration : IEntityTypeConfiguration<Maintenance.PreventiveMaintenancePlan>
{
    public void Configure(EntityTypeBuilder<Maintenance.PreventiveMaintenancePlan> entity)
    {
        entity.ToTable("preventive_maintenance_plans");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.Code).HasColumnName("code").HasMaxLength(50);
        entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(p => p.AssetType).HasColumnName("asset_type").HasMaxLength(50);
        entity.Property(p => p.AssetId).HasColumnName("asset_id").HasMaxLength(200);
        entity.Property(p => p.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(p => p.IntervalType).HasColumnName("interval_type").HasMaxLength(20)
            .HasConversion(v => Maintenance.MaintenanceValueConversions.ToStorage(v), v => Maintenance.MaintenanceValueConversions.IntervalTypeFromStorage(v));
        entity.Property(p => p.IntervalValue).HasColumnName("interval_value");
        entity.Property(p => p.NextDueAtUtc).HasColumnName("next_due_at_utc");
        entity.Property(p => p.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(v => Maintenance.MaintenanceValueConversions.ToStorage(v), v => Maintenance.MaintenanceValueConversions.PlanStatusFromStorage(v));
        entity.Property(p => p.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Code }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
