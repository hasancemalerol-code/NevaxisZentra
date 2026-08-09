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
