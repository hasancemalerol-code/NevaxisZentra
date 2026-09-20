using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.ContinuousOptimization;

internal static class ContinuousOptimizationEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IContinuousOptimizationTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class ContinuousOptimizationRunRecordConfiguration : IEntityTypeConfiguration<ContinuousOptimizationRunRecord>
{
    public void Configure(EntityTypeBuilder<ContinuousOptimizationRunRecord> builder)
    {
        builder.ToTable("continuous_optimization_runs");
        builder.HasKey(x => x.Id);
        ContinuousOptimizationEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.OptimizationType).HasColumnName("optimization_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.TriggerType).HasColumnName("trigger_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CurrentPhase).HasColumnName("current_phase").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(64);
        builder.Property(x => x.FailureState).HasColumnName("failure_state").HasMaxLength(64);
        builder.Property(x => x.CorrelationKey).HasColumnName("correlation_key").HasMaxLength(240).IsRequired();
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.ThroughputOptimizerRunId).HasColumnName("throughput_optimizer_run_id");
        builder.Property(x => x.ScenarioRecoveryRunId).HasColumnName("scenario_recovery_run_id");
        builder.Property(x => x.QualificationJson).HasColumnName("qualification_json");
        builder.Property(x => x.OpportunityJson).HasColumnName("opportunity_json");
        builder.Property(x => x.ExplainabilityJson).HasColumnName("explainability_json");
        builder.Property(x => x.MetricsJson).HasColumnName("metrics_json");
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.CorrelationKey });
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId, x.CurrentPhase });
    }
}

public sealed class ContinuousOptimizationOpportunityRecordConfiguration : IEntityTypeConfiguration<ContinuousOptimizationOpportunityRecord>
{
    public void Configure(EntityTypeBuilder<ContinuousOptimizationOpportunityRecord> builder)
    {
        builder.ToTable("continuous_optimization_opportunities");
        builder.HasKey(x => x.Id);
        ContinuousOptimizationEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.RunId).HasColumnName("run_id");
        builder.Property(x => x.OpportunityType).HasColumnName("opportunity_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ScopeEntityType).HasColumnName("scope_entity_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ScopeEntityId).HasColumnName("scope_entity_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(240).IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.EstimatedBenefit).HasColumnName("estimated_benefit");
        builder.Property(x => x.Confidence).HasColumnName("confidence");
        builder.Property(x => x.RiskClass).HasColumnName("risk_class").HasMaxLength(32).IsRequired();
        builder.Property(x => x.EvidenceJson).HasColumnName("evidence_json");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.RunId }).IsUnique();
    }
}

public sealed class ContinuousOptimizationPolicyRecordConfiguration : IEntityTypeConfiguration<ContinuousOptimizationPolicyRecord>
{
    public void Configure(EntityTypeBuilder<ContinuousOptimizationPolicyRecord> builder)
    {
        builder.ToTable("continuous_optimization_policies");
        builder.HasKey(x => x.Id);
        ContinuousOptimizationEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.OptimizationType).HasColumnName("optimization_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ScopeId).HasColumnName("scope_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        builder.Property(x => x.IsPaused).HasColumnName("is_paused");
        builder.Property(x => x.ThresholdsJson).HasColumnName("thresholds_json");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.OptimizationType, x.ScopeType, x.ScopeId }).IsUnique();
    }
}
