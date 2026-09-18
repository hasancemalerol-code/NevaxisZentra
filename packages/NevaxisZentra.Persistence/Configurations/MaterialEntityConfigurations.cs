using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class MaterialRegistryEntryEntityConfiguration : IEntityTypeConfiguration<Material.MaterialRegistryEntry>
{
    public void Configure(EntityTypeBuilder<Material.MaterialRegistryEntry> entity)
    {
        entity.ToTable("materials");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.FacilityId).HasColumnName("facility_id");
        entity.Property(m => m.Code).HasColumnName("code").HasMaxLength(100);
        entity.Property(m => m.CodeNormalized).HasColumnName("code_normalized").HasMaxLength(100);
        entity.Property(m => m.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(m => m.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(m => m.MaterialCategory).HasColumnName("material_category").HasMaxLength(30)
            .HasConversion(v => Material.MaterialValueConversions.ToStorage(v), v => Material.MaterialValueConversions.CategoryFromStorage(v));
        entity.Property(m => m.PolymerFamily).HasColumnName("polymer_family").HasMaxLength(20)
            .HasConversion(v => Material.MaterialValueConversions.ToStorage(v), v => Material.MaterialValueConversions.PolymerFromStorage(v));
        entity.Property(m => m.Manufacturer).HasColumnName("manufacturer").HasMaxLength(200);
        entity.Property(m => m.TradeNameOrGrade).HasColumnName("trade_name_or_grade").HasMaxLength(200);
        entity.Property(m => m.ColorName).HasColumnName("color_name").HasMaxLength(100);
        entity.Property(m => m.ColorCode).HasColumnName("color_code").HasMaxLength(50);
        entity.Property(m => m.PhysicalForm).HasColumnName("physical_form").HasMaxLength(20)
            .HasConversion(v => Material.MaterialValueConversions.ToStorage(v), v => Material.MaterialValueConversions.PhysicalFormFromStorage(v));
        entity.Property(m => m.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(20)
            .HasConversion(v => Material.MaterialValueConversions.ToStorage(v), v => Material.MaterialValueConversions.LifecycleFromStorage(v));
        entity.Property(m => m.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20);
        entity.Property(m => m.IsActive).HasColumnName("is_active");
        entity.Property(m => m.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(v => Material.MaterialValueConversions.ToStorage(v), v => Material.MaterialValueConversions.SourceFromStorage(v));
        entity.Property(m => m.SourceSystem).HasColumnName("source_system").HasMaxLength(100);
        entity.Property(m => m.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(m => m.ExternalMaterialCode).HasColumnName("external_material_code").HasMaxLength(200);
        entity.Property(m => m.DataRecordedAtUtc).HasColumnName("data_recorded_at_utc");
        entity.Property(m => m.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(m => m.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(m => m.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(m => m.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(m => m.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(m => m.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(m => m.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(m => new { m.TenantId, m.CodeNormalized }).IsUnique();
        entity.HasIndex(m => new { m.TenantId, m.CreateIdempotencyKey }).IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.LifecycleStatus });
        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.MaterialCategory });
        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.PolymerFamily });

        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(m => new { m.TenantId, m.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(m => m.TechnicalSpecification)
            .WithOne(s => s.Material)
            .HasForeignKey<Material.MaterialTechnicalSpecification>(s => s.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaterialTechnicalSpecificationEntityConfiguration
    : IEntityTypeConfiguration<Material.MaterialTechnicalSpecification>
{
    public void Configure(EntityTypeBuilder<Material.MaterialTechnicalSpecification> entity)
    {
        entity.ToTable("material_technical_specifications");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.TenantId).HasColumnName("tenant_id");
        entity.Property(s => s.MaterialId).HasColumnName("material_id");
        entity.Property(s => s.DensityGPerCm3).HasColumnName("density_g_per_cm3").HasPrecision(18, 4);
        entity.Property(s => s.MeltFlowRate).HasColumnName("melt_flow_rate").HasPrecision(18, 4);
        entity.Property(s => s.MeltFlowUnit).HasColumnName("melt_flow_unit").HasMaxLength(20);
        entity.Property(s => s.MeltFlowTestTemperatureC).HasColumnName("melt_flow_test_temperature_c").HasPrecision(18, 2);
        entity.Property(s => s.MeltFlowTestLoadKg).HasColumnName("melt_flow_test_load_kg").HasPrecision(18, 3);
        entity.Property(s => s.MoistureLimitPercent).HasColumnName("moisture_limit_percent").HasPrecision(18, 3);
        entity.Property(s => s.DryingRequired).HasColumnName("drying_required");
        entity.Property(s => s.DryingTemperatureMinC).HasColumnName("drying_temperature_min_c").HasPrecision(18, 2);
        entity.Property(s => s.DryingTemperatureMaxC).HasColumnName("drying_temperature_max_c").HasPrecision(18, 2);
        entity.Property(s => s.DryingTimeMinHours).HasColumnName("drying_time_min_hours").HasPrecision(18, 2);
        entity.Property(s => s.DryingTimeMaxHours).HasColumnName("drying_time_max_hours").HasPrecision(18, 2);
        entity.Property(s => s.MeltTemperatureMinC).HasColumnName("melt_temperature_min_c").HasPrecision(18, 2);
        entity.Property(s => s.MeltTemperatureMaxC).HasColumnName("melt_temperature_max_c").HasPrecision(18, 2);
        entity.Property(s => s.MoldTemperatureMinC).HasColumnName("mold_temperature_min_c").HasPrecision(18, 2);
        entity.Property(s => s.MoldTemperatureMaxC).HasColumnName("mold_temperature_max_c").HasPrecision(18, 2);
        entity.Property(s => s.StorageTemperatureMinC).HasColumnName("storage_temperature_min_c").HasPrecision(18, 2);
        entity.Property(s => s.StorageTemperatureMaxC).HasColumnName("storage_temperature_max_c").HasPrecision(18, 2);
        entity.Property(s => s.MaximumRegrindPercent).HasColumnName("maximum_regrind_percent").HasPrecision(18, 3);
        entity.Property(s => s.TechnicalNotes).HasColumnName("technical_notes").HasMaxLength(2000);
        entity.Property(s => s.SafetyDataSheetReference).HasColumnName("safety_data_sheet_reference").HasMaxLength(500);
        entity.Property(s => s.TechnicalDataSheetReference).HasColumnName("technical_data_sheet_reference").HasMaxLength(500);
        entity.Property(s => s.SpecificationSource).HasColumnName("specification_source").HasMaxLength(20)
            .HasConversion(
                v => Material.MaterialValueConversions.ToStorage(v),
                v => Material.MaterialValueConversions.SpecificationSourceFromStorage(v));
        entity.Property(s => s.SpecificationSourceReference).HasColumnName("specification_source_reference").HasMaxLength(500);
        entity.Property(s => s.SpecificationRecordedAtUtc).HasColumnName("specification_recorded_at_utc");
        entity.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(s => s.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(s => s.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(s => s.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(s => s.MaterialId).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaterialLifecycleEventEntityConfiguration : IEntityTypeConfiguration<Material.MaterialLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<Material.MaterialLifecycleEvent> entity)
    {
        entity.ToTable("material_lifecycle_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.MaterialId).HasColumnName("material_id");
        entity.Property(e => e.PreviousStatus).HasColumnName("previous_status").HasMaxLength(20)
            .HasConversion(v => Material.MaterialValueConversions.ToStorage(v), v => Material.MaterialValueConversions.LifecycleFromStorage(v));
        entity.Property(e => e.NewStatus).HasColumnName("new_status").HasMaxLength(20)
            .HasConversion(v => Material.MaterialValueConversions.ToStorage(v), v => Material.MaterialValueConversions.LifecycleFromStorage(v));
        entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1000);
        entity.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.MaterialId, e.IdempotencyKey }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MaterialId, e.OccurredAtUtc });

        entity.HasOne<Material.MaterialRegistryEntry>().WithMany()
            .HasForeignKey(e => e.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
