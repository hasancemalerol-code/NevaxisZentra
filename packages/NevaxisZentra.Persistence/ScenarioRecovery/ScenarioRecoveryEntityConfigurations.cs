using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.ScenarioRecovery;

internal static class ScenarioRecoveryEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IScenarioRecoveryTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class ScenarioRecoveryRunRecordConfiguration : IEntityTypeConfiguration<ScenarioRecoveryRunRecord>
{
    public void Configure(EntityTypeBuilder<ScenarioRecoveryRunRecord> builder)
    {
        builder.ToTable("scenario_recovery_runs");
        builder.HasKey(x => x.Id);
        ScenarioRecoveryEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.ContextSnapshotId).HasColumnName("context_snapshot_id");
        builder.Property(x => x.CorrelationKey).HasColumnName("correlation_key").HasMaxLength(240).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntryPoint).HasColumnName("entry_point").HasMaxLength(32).IsRequired();
        builder.Property(x => x.CurrentPhase).HasColumnName("current_phase").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExecutionState).HasColumnName("execution_state").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProblemJson).HasColumnName("problem_json");
        builder.Property(x => x.ObjectivesJson).HasColumnName("objectives_json");
        builder.Property(x => x.SnapshotSummaryJson).HasColumnName("snapshot_summary_json");
        builder.Property(x => x.ComparisonJson).HasColumnName("comparison_json");
        builder.Property(x => x.SelectedOptionLabel).HasColumnName("selected_option_label").HasMaxLength(32);
        builder.Property(x => x.RecoveryPlanJson).HasColumnName("recovery_plan_json");
        builder.Property(x => x.VerificationJson).HasColumnName("verification_json");
        builder.Property(x => x.SourceEntityType).HasColumnName("source_entity_type").HasMaxLength(64);
        builder.Property(x => x.SourceEntityId).HasColumnName("source_entity_id").HasMaxLength(128);
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").HasMaxLength(16).IsRequired();
        builder.Property(x => x.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.CorrelationKey });
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId, x.UpdatedAtUtc });
    }
}

public sealed class ScenarioRecoveryOptionRecordConfiguration : IEntityTypeConfiguration<ScenarioRecoveryOptionRecord>
{
    public void Configure(EntityTypeBuilder<ScenarioRecoveryOptionRecord> builder)
    {
        builder.ToTable("scenario_recovery_options");
        builder.HasKey(x => x.Id);
        ScenarioRecoveryEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.RunId).HasColumnName("run_id");
        builder.Property(x => x.OptionLabel).HasColumnName("option_label").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(64).IsRequired();
        builder.Property(x => x.FeasibilityState).HasColumnName("feasibility_state").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ActionsJson).HasColumnName("actions_json");
        builder.Property(x => x.SimulationJson).HasColumnName("simulation_json");
        builder.Property(x => x.MetricsJson).HasColumnName("metrics_json");
        builder.Property(x => x.AssumptionsJson).HasColumnName("assumptions_json");
        builder.Property(x => x.RequiresApproval).HasColumnName("requires_approval");
        builder.Property(x => x.IsDominated).HasColumnName("is_dominated");
        builder.Property(x => x.RecommendationScore).HasColumnName("recommendation_score");
        builder.Property(x => x.FeasibilityReasonsJson).HasColumnName("feasibility_reasons_json");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.RunId, x.OptionLabel }).IsUnique();
    }
}

public sealed class ScenarioRecoveryPredictionAccuracyRecordConfiguration
    : IEntityTypeConfiguration<ScenarioRecoveryPredictionAccuracyRecord>
{
    public void Configure(EntityTypeBuilder<ScenarioRecoveryPredictionAccuracyRecord> builder)
    {
        builder.ToTable("scenario_recovery_prediction_accuracy");
        builder.HasKey(x => x.Id);
        ScenarioRecoveryEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.RunId).HasColumnName("run_id");
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.MetricCode).HasColumnName("metric_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.PredictedValue).HasColumnName("predicted_value");
        builder.Property(x => x.ActualValue).HasColumnName("actual_value");
        builder.Property(x => x.PredictionError).HasColumnName("prediction_error");
        builder.Property(x => x.VerificationOutcome).HasColumnName("verification_outcome").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.RunId, x.MetricCode });
    }
}
