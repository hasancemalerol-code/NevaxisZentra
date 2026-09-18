using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class MoldCompatibilityRequirementEntityConfiguration : IEntityTypeConfiguration<Mold.MoldCompatibilityRequirement>
{
    public void Configure(EntityTypeBuilder<Mold.MoldCompatibilityRequirement> entity)
    {
        entity.ToTable("mold_compatibility_requirements");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.MoldId).HasColumnName("mold_id");
        entity.Property(r => r.RequiredClampForceTon).HasColumnName("required_clamp_force_ton").HasPrecision(18, 3);
        entity.Property(r => r.MoldWidthMm).HasColumnName("mold_width_mm").HasPrecision(18, 3);
        entity.Property(r => r.MoldHeightMm).HasColumnName("mold_height_mm").HasPrecision(18, 3);
        entity.Property(r => r.MoldDepthMm).HasColumnName("mold_depth_mm").HasPrecision(18, 3);
        entity.Property(r => r.RequiredTieBarWidthMm).HasColumnName("required_tie_bar_width_mm").HasPrecision(18, 3);
        entity.Property(r => r.RequiredTieBarHeightMm).HasColumnName("required_tie_bar_height_mm").HasPrecision(18, 3);
        entity.Property(r => r.MinimumShotCapacityGrams).HasColumnName("minimum_shot_capacity_grams").HasPrecision(18, 3);
        entity.Property(r => r.RequiredInjectionUnit).HasColumnName("required_injection_unit").HasMaxLength(100);
        entity.Property(r => r.SupportedMaterialGroup).HasColumnName("supported_material_group").HasMaxLength(100);
        entity.Property(r => r.NominalCycleTimeSeconds).HasColumnName("nominal_cycle_time_seconds").HasPrecision(18, 3);
        entity.Property(r => r.EffectiveCavityCount).HasColumnName("effective_cavity_count");
        entity.Property(r => r.SourceType).HasColumnName("source_type").HasMaxLength(20);
        entity.Property(r => r.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(r => r.DataRecordedAtUtc).HasColumnName("data_recorded_at_utc");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(r => r.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(r => r.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.MoldId }).IsUnique();
        entity.HasOne<Mold.MoldRegistryEntry>().WithMany().HasForeignKey(r => r.MoldId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineCompatibilityCapabilityProfileEntityConfiguration
    : IEntityTypeConfiguration<Mold.MachineCompatibilityCapabilityProfile>
{
    public void Configure(EntityTypeBuilder<Mold.MachineCompatibilityCapabilityProfile> entity)
    {
        entity.ToTable("machine_compatibility_capability_profiles");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(p => p.MachineCode).HasColumnName("machine_code").HasMaxLength(100);
        entity.Property(p => p.MachineDisplayName).HasColumnName("machine_display_name").HasMaxLength(200);
        entity.Property(p => p.ClampForceTon).HasColumnName("clamp_force_ton").HasPrecision(18, 3);
        entity.Property(p => p.MaximumMoldWidthMm).HasColumnName("maximum_mold_width_mm").HasPrecision(18, 3);
        entity.Property(p => p.MaximumMoldHeightMm).HasColumnName("maximum_mold_height_mm").HasPrecision(18, 3);
        entity.Property(p => p.MinimumMoldDepthMm).HasColumnName("minimum_mold_depth_mm").HasPrecision(18, 3);
        entity.Property(p => p.MaximumMoldDepthMm).HasColumnName("maximum_mold_depth_mm").HasPrecision(18, 3);
        entity.Property(p => p.TieBarWidthMm).HasColumnName("tie_bar_width_mm").HasPrecision(18, 3);
        entity.Property(p => p.TieBarHeightMm).HasColumnName("tie_bar_height_mm").HasPrecision(18, 3);
        entity.Property(p => p.MaximumShotCapacityGrams).HasColumnName("maximum_shot_capacity_grams").HasPrecision(18, 3);
        entity.Property(p => p.InjectionUnit).HasColumnName("injection_unit").HasMaxLength(100);
        entity.Property(p => p.SupportedMaterialGroups).HasColumnName("supported_material_groups").HasMaxLength(500);
        entity.Property(p => p.SourceType).HasColumnName("source_type").HasMaxLength(20);
        entity.Property(p => p.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(p => p.DataRecordedAtUtc).HasColumnName("data_recorded_at_utc");
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.MachineAssetId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MoldMachineCompatibilityAssessmentEntityConfiguration
    : IEntityTypeConfiguration<Mold.MoldMachineCompatibilityAssessment>
{
    public void Configure(EntityTypeBuilder<Mold.MoldMachineCompatibilityAssessment> entity)
    {
        entity.ToTable("mold_machine_compatibility_assessments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.MoldId).HasColumnName("mold_id");
        entity.Property(a => a.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(50);
        entity.Property(a => a.RequirementVersion).HasColumnName("requirement_version");
        entity.Property(a => a.MachineProfileVersion).HasColumnName("machine_profile_version");
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.StructuredReasonsJson).HasColumnName("structured_reasons_json").HasColumnType("jsonb");
        entity.Property(a => a.StructuredEvidenceJson).HasColumnName("structured_evidence_json").HasColumnType("jsonb");
        entity.Property(a => a.TheoreticalPartsPerHour).HasColumnName("theoretical_parts_per_hour").HasPrecision(18, 3);
        entity.Property(a => a.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(64);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);

        entity.HasIndex(a => new { a.TenantId, a.MoldId, a.MachineAssetId, a.InputFingerprint }).IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.MoldId, a.EvaluatedAtUtc });
        entity.HasOne<Mold.MoldRegistryEntry>().WithMany().HasForeignKey(a => a.MoldId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
