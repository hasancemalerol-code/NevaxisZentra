using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class QualityControlPlanEntityConfiguration : IEntityTypeConfiguration<Quality.QualityControlPlan>
{
    public void Configure(EntityTypeBuilder<Quality.QualityControlPlan> entity)
    {
        entity.ToTable("quality_control_plans");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.Code).HasColumnName("code").HasMaxLength(100);
        entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(p => p.ProductReference).HasColumnName("product_reference").HasMaxLength(200);
        entity.Property(p => p.ProductGroup).HasColumnName("product_group").HasMaxLength(200);
        entity.Property(p => p.OperationReference).HasColumnName("operation_reference").HasMaxLength(200);
        entity.Property(p => p.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.PlanStatusFromStorage(v));
        entity.Property(p => p.Revision).HasColumnName("revision").HasMaxLength(20);
        entity.Property(p => p.EffectiveFromUtc).HasColumnName("effective_from_utc");
        entity.Property(p => p.EffectiveToUtc).HasColumnName("effective_to_utc");
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Code }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QualityInspectionDefinitionEntityConfiguration
    : IEntityTypeConfiguration<Quality.QualityInspectionDefinition>
{
    public void Configure(EntityTypeBuilder<Quality.QualityInspectionDefinition> entity)
    {
        entity.ToTable("quality_inspection_definitions");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.QualityControlPlanId).HasColumnName("quality_control_plan_id");
        entity.Property(d => d.Code).HasColumnName("code").HasMaxLength(100);
        entity.Property(d => d.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(d => d.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(d => d.InspectionType).HasColumnName("inspection_type").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.InspectionTypeFromStorage(v));
        entity.Property(d => d.Sequence).HasColumnName("sequence");
        entity.Property(d => d.Unit).HasColumnName("unit").HasMaxLength(50);
        entity.Property(d => d.NominalValue).HasColumnName("nominal_value");
        entity.Property(d => d.LowerLimit).HasColumnName("lower_limit");
        entity.Property(d => d.UpperLimit).HasColumnName("upper_limit");
        entity.Property(d => d.Required).HasColumnName("required");
        entity.Property(d => d.Critical).HasColumnName("critical");
        entity.Property(d => d.Instruction).HasColumnName("instruction").HasMaxLength(2000);
        entity.Property(d => d.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(d => new { d.QualityControlPlanId, d.Code }).IsUnique();
        entity.HasOne(d => d.QualityControlPlan).WithMany(p => p.InspectionDefinitions)
            .HasForeignKey(d => d.QualityControlPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QualityInspectionEntityConfiguration : IEntityTypeConfiguration<Quality.QualityInspection>
{
    public void Configure(EntityTypeBuilder<Quality.QualityInspection> entity)
    {
        entity.ToTable("quality_inspections");
        entity.HasKey(i => i.Id);
        entity.Property(i => i.Id).HasColumnName("id");
        entity.Property(i => i.TenantId).HasColumnName("tenant_id");
        entity.Property(i => i.FacilityId).HasColumnName("facility_id");
        entity.Property(i => i.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(i => i.QualityControlPlanId).HasColumnName("quality_control_plan_id");
        entity.Property(i => i.PlanCodeSnapshot).HasColumnName("plan_code_snapshot").HasMaxLength(100);
        entity.Property(i => i.PlanNameSnapshot).HasColumnName("plan_name_snapshot").HasMaxLength(200);
        entity.Property(i => i.PlanRevisionSnapshot).HasColumnName("plan_revision_snapshot").HasMaxLength(50);
        entity.Property(i => i.InspectionNumber).HasColumnName("inspection_number").HasMaxLength(50);
        entity.Property(i => i.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.InspectionStatusFromStorage(v));
        entity.Property(i => i.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(i => i.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(i => i.InspectorSubjectId).HasColumnName("inspector_subject_id").HasMaxLength(200);
        entity.Property(i => i.OverallResult).HasColumnName("overall_result").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.OverallResultFromStorage(v));
        entity.Property(i => i.ReleaseStatus).HasColumnName("release_status").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.ReleaseStatusFromStorage(v));
        entity.Property(i => i.Notes).HasColumnName("notes").HasMaxLength(2000);
        entity.Property(i => i.ReleaseNote).HasColumnName("release_note").HasMaxLength(2000);
        entity.Property(i => i.ReleasedBySubjectId).HasColumnName("released_by_subject_id").HasMaxLength(200);
        entity.Property(i => i.ReleasedAtUtc).HasColumnName("released_at_utc");
        entity.Property(i => i.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(i => i.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(i => i.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(i => i.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(i => new { i.TenantId, i.FacilityId, i.InspectionNumber }).IsUnique();
        entity.HasIndex(i => new { i.TenantId, i.FacilityId, i.ProductionOrderId });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(i => new { i.TenantId, i.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionOrder>().WithMany()
            .HasForeignKey(i => i.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Quality.QualityControlPlan>().WithMany()
            .HasForeignKey(i => i.QualityControlPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QualityInspectionResultEntityConfiguration : IEntityTypeConfiguration<Quality.QualityInspectionResult>
{
    public void Configure(EntityTypeBuilder<Quality.QualityInspectionResult> entity)
    {
        entity.ToTable("quality_inspection_results");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.QualityInspectionId).HasColumnName("quality_inspection_id");
        entity.Property(r => r.InspectionDefinitionId).HasColumnName("inspection_definition_id");
        entity.Property(r => r.DefinitionCodeSnapshot).HasColumnName("definition_code_snapshot").HasMaxLength(100);
        entity.Property(r => r.DefinitionNameSnapshot).HasColumnName("definition_name_snapshot").HasMaxLength(200);
        entity.Property(r => r.InspectionTypeSnapshot).HasColumnName("inspection_type_snapshot").HasMaxLength(20);
        entity.Property(r => r.UnitSnapshot).HasColumnName("unit_snapshot").HasMaxLength(50);
        entity.Property(r => r.NominalValueSnapshot).HasColumnName("nominal_value_snapshot");
        entity.Property(r => r.LowerLimitSnapshot).HasColumnName("lower_limit_snapshot");
        entity.Property(r => r.UpperLimitSnapshot).HasColumnName("upper_limit_snapshot");
        entity.Property(r => r.RequiredSnapshot).HasColumnName("required_snapshot");
        entity.Property(r => r.CriticalSnapshot).HasColumnName("critical_snapshot");
        entity.Property(r => r.MeasuredNumericValue).HasColumnName("measured_numeric_value");
        entity.Property(r => r.MeasuredTextValue).HasColumnName("measured_text_value").HasMaxLength(500);
        entity.Property(r => r.BooleanResult).HasColumnName("boolean_result");
        entity.Property(r => r.Result).HasColumnName("result").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.ItemResultFromStorage(v));
        entity.Property(r => r.Comment).HasColumnName("comment").HasMaxLength(1000);
        entity.Property(r => r.RecordedAtUtc).HasColumnName("recorded_at_utc");
        entity.Property(r => r.RecordedBySubjectId).HasColumnName("recorded_by_subject_id").HasMaxLength(200);
        entity.Property(r => r.Source).HasColumnName("source").HasMaxLength(50);
        entity.Property(r => r.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(r => new { r.QualityInspectionId, r.InspectionDefinitionId }).IsUnique();
        entity.HasOne<Quality.QualityInspection>().WithMany(i => i.Results)
            .HasForeignKey(r => r.QualityInspectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QualityNonconformanceEntityConfiguration : IEntityTypeConfiguration<Quality.QualityNonconformance>
{
    public void Configure(EntityTypeBuilder<Quality.QualityNonconformance> entity)
    {
        entity.ToTable("quality_nonconformances");
        entity.HasKey(n => n.Id);
        entity.Property(n => n.Id).HasColumnName("id");
        entity.Property(n => n.TenantId).HasColumnName("tenant_id");
        entity.Property(n => n.FacilityId).HasColumnName("facility_id");
        entity.Property(n => n.NcrNumber).HasColumnName("ncr_number").HasMaxLength(50);
        entity.Property(n => n.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(n => n.QualityInspectionId).HasColumnName("quality_inspection_id");
        entity.Property(n => n.QualityInspectionResultId).HasColumnName("quality_inspection_result_id");
        entity.Property(n => n.ProductReference).HasColumnName("product_reference").HasMaxLength(200);
        entity.Property(n => n.OperationReference).HasColumnName("operation_reference").HasMaxLength(200);
        entity.Property(n => n.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(n => n.Description).HasColumnName("description").HasMaxLength(4000);
        entity.Property(n => n.Category).HasColumnName("category").HasMaxLength(100);
        entity.Property(n => n.Severity).HasColumnName("severity").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.NcrSeverityFromStorage(v));
        entity.Property(n => n.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.NcrStatusFromStorage(v));
        entity.Property(n => n.DetectedAtUtc).HasColumnName("detected_at_utc");
        entity.Property(n => n.DetectedBySubjectId).HasColumnName("detected_by_subject_id").HasMaxLength(200);
        entity.Property(n => n.OwnerSubjectId).HasColumnName("owner_subject_id").HasMaxLength(200);
        entity.Property(n => n.DispositionStatus).HasColumnName("disposition_status").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.DispositionStatusFromStorage(v));
        entity.Property(n => n.DispositionType).HasColumnName("disposition_type").HasMaxLength(30)
            .HasConversion(
                v => v.HasValue ? Quality.QualityValueConversions.ToStorage(v.Value) : null,
                v => v == null ? null : Quality.QualityValueConversions.DispositionTypeFromStorage(v));
        entity.Property(n => n.DispositionNote).HasColumnName("disposition_note").HasMaxLength(2000);
        entity.Property(n => n.DispositionDecidedBySubjectId).HasColumnName("disposition_decided_by_subject_id").HasMaxLength(200);
        entity.Property(n => n.DispositionDecidedAtUtc).HasColumnName("disposition_decided_at_utc");
        entity.Property(n => n.HoldStatus).HasColumnName("hold_status").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.NcrHoldStatusFromStorage(v));
        entity.Property(n => n.ClosedAtUtc).HasColumnName("closed_at_utc");
        entity.Property(n => n.ClosedBySubjectId).HasColumnName("closed_by_subject_id").HasMaxLength(200);
        entity.Property(n => n.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(n => n.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(n => n.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(n => new { n.TenantId, n.FacilityId, n.NcrNumber }).IsUnique();
        entity.HasIndex(n => new { n.TenantId, n.FacilityId, n.QualityInspectionResultId })
            .IsUnique()
            .HasFilter("quality_inspection_result_id IS NOT NULL");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(n => n.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(n => new { n.TenantId, n.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QualityHoldEntityConfiguration : IEntityTypeConfiguration<Quality.QualityHold>
{
    public void Configure(EntityTypeBuilder<Quality.QualityHold> entity)
    {
        entity.ToTable("quality_holds");
        entity.HasKey(h => h.Id);
        entity.Property(h => h.Id).HasColumnName("id");
        entity.Property(h => h.TenantId).HasColumnName("tenant_id");
        entity.Property(h => h.FacilityId).HasColumnName("facility_id");
        entity.Property(h => h.NcrId).HasColumnName("ncr_id");
        entity.Property(h => h.ProductionOrderId).HasColumnName("production_order_id");
        entity.Property(h => h.Scope).HasColumnName("scope").HasMaxLength(50);
        entity.Property(h => h.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.HoldRecordStatusFromStorage(v));
        entity.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(2000);
        entity.Property(h => h.PlacedAtUtc).HasColumnName("placed_at_utc");
        entity.Property(h => h.PlacedBySubjectId).HasColumnName("placed_by_subject_id").HasMaxLength(200);
        entity.Property(h => h.ReleasedAtUtc).HasColumnName("released_at_utc");
        entity.Property(h => h.ReleasedBySubjectId).HasColumnName("released_by_subject_id").HasMaxLength(200);
        entity.Property(h => h.ReleaseReason).HasColumnName("release_reason").HasMaxLength(2000);
        entity.Property(h => h.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasOne(h => h.Nonconformance).WithMany(n => n.Holds)
            .HasForeignKey(h => h.NcrId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QualityCorrectiveActionEntityConfiguration : IEntityTypeConfiguration<Quality.QualityCorrectiveAction>
{
    public void Configure(EntityTypeBuilder<Quality.QualityCorrectiveAction> entity)
    {
        entity.ToTable("quality_corrective_actions");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.NcrId).HasColumnName("ncr_id");
        entity.Property(a => a.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(a => a.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(a => a.OwnerSubjectId).HasColumnName("owner_subject_id").HasMaxLength(200);
        entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(v => Quality.QualityValueConversions.ToStorage(v), v => Quality.QualityValueConversions.CorrectiveActionStatusFromStorage(v));
        entity.Property(a => a.DueAtUtc).HasColumnName("due_at_utc");
        entity.Property(a => a.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(a => a.CompletedBySubjectId).HasColumnName("completed_by_subject_id").HasMaxLength(200);
        entity.Property(a => a.CompletionNote).HasColumnName("completion_note").HasMaxLength(2000);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasOne(a => a.Nonconformance).WithMany(n => n.CorrectiveActions)
            .HasForeignKey(a => a.NcrId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
