using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class MaintenanceAssetMachineMappingEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenanceAssetMachineMapping>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenanceAssetMachineMapping> entity)
    {
        entity.ToTable("maintenance_asset_machine_mappings");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.FacilityId).HasColumnName("facility_id");
        entity.Property(m => m.AssetType).HasColumnName("asset_type").HasMaxLength(50);
        entity.Property(m => m.AssetId).HasColumnName("asset_id").HasMaxLength(200);
        entity.Property(m => m.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(m => m.AssetName).HasColumnName("asset_name").HasMaxLength(200);
        entity.Property(m => m.MachineId).HasColumnName("machine_id");
        entity.Property(m => m.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(m => m.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(m => m.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.AssetReference }).IsUnique();
        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.MachineId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(m => new { m.TenantId, m.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenanceConditionMetricDefinitionEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenanceConditionMetricDefinition>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenanceConditionMetricDefinition> entity)
    {
        entity.ToTable("maintenance_condition_metric_definitions");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.MetricKey).HasColumnName("metric_key").HasMaxLength(100);
        entity.Property(d => d.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        entity.Property(d => d.ExpectedUnit).HasColumnName("expected_unit").HasMaxLength(30);
        entity.Property(d => d.EvaluationWindowHours).HasColumnName("evaluation_window_hours");
        entity.Property(d => d.MinimumSamples).HasColumnName("minimum_samples");
        entity.Property(d => d.BaselineMethod).HasColumnName("baseline_method").HasMaxLength(20);
        entity.Property(d => d.DeviationThresholdPercent).HasColumnName("deviation_threshold_percent").HasPrecision(9, 2);
        entity.Property(d => d.TrendThresholdPercent).HasColumnName("trend_threshold_percent").HasPrecision(9, 2);
        entity.Property(d => d.VolatilityThresholdPercent).HasColumnName("volatility_threshold_percent").HasPrecision(9, 2);
        entity.Property(d => d.Enabled).HasColumnName("enabled");
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(d => d.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(d => d.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.MetricKey }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenanceConditionProfileEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenanceConditionProfile>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenanceConditionProfile> entity)
    {
        entity.ToTable("maintenance_condition_profiles");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(p => p.MachineId).HasColumnName("machine_id");
        entity.Property(p => p.ConditionStatus).HasColumnName("condition_status").HasMaxLength(20);
        entity.Property(p => p.OverallConditionScore).HasColumnName("overall_condition_score").HasPrecision(5, 2);
        entity.Property(p => p.LastEvaluatedAtUtc).HasColumnName("last_evaluated_at_utc");
        entity.Property(p => p.DataFreshness).HasColumnName("data_freshness").HasMaxLength(20);
        entity.Property(p => p.DataSufficiency).HasColumnName("data_sufficiency").HasMaxLength(20);
        entity.Property(p => p.ActiveAnomalyCount).HasColumnName("active_anomaly_count");
        entity.Property(p => p.HighestAnomalySeverity).HasColumnName("highest_anomaly_severity").HasMaxLength(20);
        entity.Property(p => p.TrendState).HasColumnName("trend_state").HasMaxLength(20);
        entity.Property(p => p.EvaluationIdentity).HasColumnName("evaluation_identity").HasMaxLength(128);
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.AssetReference }).IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.MachineId });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenanceConditionAnomalyEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenanceConditionAnomaly>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenanceConditionAnomaly> entity)
    {
        entity.ToTable("maintenance_condition_anomalies");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(a => a.MachineId).HasColumnName("machine_id");
        entity.Property(a => a.MetricKey).HasColumnName("metric_key").HasMaxLength(100);
        entity.Property(a => a.DetectedAtUtc).HasColumnName("detected_at_utc");
        entity.Property(a => a.WindowStartUtc).HasColumnName("window_start_utc");
        entity.Property(a => a.WindowEndUtc).HasColumnName("window_end_utc");
        entity.Property(a => a.ObservedValue).HasColumnName("observed_value").HasPrecision(18, 4);
        entity.Property(a => a.BaselineValue).HasColumnName("baseline_value").HasPrecision(18, 4);
        entity.Property(a => a.DeviationPercent).HasColumnName("deviation_percent").HasPrecision(9, 2);
        entity.Property(a => a.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(a => a.Direction).HasColumnName("direction").HasMaxLength(20);
        entity.Property(a => a.EvidenceSummary).HasColumnName("evidence_summary").HasMaxLength(2000);
        entity.Property(a => a.SourceFreshness).HasColumnName("source_freshness").HasMaxLength(20);
        entity.Property(a => a.DataQuality).HasColumnName("data_quality").HasMaxLength(20);
        entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(a => a.EvaluationIdentity).HasColumnName("evaluation_identity").HasMaxLength(128);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");

        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.EvaluationIdentity }).IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.AssetReference, a.DetectedAtUtc });
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.MachineId, a.MetricKey });
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.Status });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
