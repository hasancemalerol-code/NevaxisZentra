using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.IntelligenceRuntime;

internal static class IntelligenceRuntimeEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IIntelligenceRuntimeTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class IntelligenceRuntimeRouteRecordConfiguration : IEntityTypeConfiguration<IntelligenceRuntimeRouteRecord>
{
    public void Configure(EntityTypeBuilder<IntelligenceRuntimeRouteRecord> builder)
    {
        builder.ToTable("intelligence_runtime_routes");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.WorkerId).HasColumnName("worker_id");
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.OptimizationRunId).HasColumnName("optimization_run_id");
        builder.Property(x => x.ScenarioRunId).HasColumnName("scenario_recovery_run_id");
        builder.Property(x => x.ContextSnapshotId).HasColumnName("context_snapshot_id");
        builder.Property(x => x.ModelGatewayAuditId).HasColumnName("model_gateway_audit_id");
        builder.Property(x => x.TaskType).HasColumnName("task_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.RequestClassification).HasColumnName("request_classification").HasMaxLength(64).IsRequired();
        builder.Property(x => x.RouteType).HasColumnName("route_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.IntelligencePath).HasColumnName("intelligence_path").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProviderId).HasColumnName("provider_id").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.RoutingReason).HasColumnName("routing_reason").HasMaxLength(512).IsRequired();
        builder.Property(x => x.WhyModelCalledOrSkipped).HasColumnName("why_model_called_or_skipped").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ModelInvoked).HasColumnName("model_invoked");
        builder.Property(x => x.UsedFallback).HasColumnName("used_fallback");
        builder.Property(x => x.InputTokens).HasColumnName("input_tokens");
        builder.Property(x => x.OutputTokens).HasColumnName("output_tokens");
        builder.Property(x => x.EstimatedCost).HasColumnName("estimated_cost");
        builder.Property(x => x.PriceVersion).HasColumnName("price_version").HasMaxLength(32);
        builder.Property(x => x.LatencyMs).HasColumnName("latency_ms");
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DigitalWorkCaseId });
    }
}

public sealed class ProviderPriceCatalogRecordConfiguration : IEntityTypeConfiguration<ProviderPriceCatalogRecord>
{
    public void Configure(EntityTypeBuilder<ProviderPriceCatalogRecord> builder)
    {
        builder.ToTable("provider_price_catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProviderId).HasColumnName("provider_id").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.InputPricePer1K).HasColumnName("input_price_per_1k");
        builder.Property(x => x.OutputPricePer1K).HasColumnName("output_price_per_1k");
        builder.Property(x => x.CachedInputPricePer1K).HasColumnName("cached_input_price_per_1k");
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(8).IsRequired();
        builder.Property(x => x.PriceVersion).HasColumnName("price_version").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasMaxLength(128).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.EffectiveFromUtc).HasColumnName("effective_from_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.ProviderId, x.ModelName, x.PriceVersion }).IsUnique();
    }
}

public sealed class TenantIntelligenceLearningPolicyRecordConfiguration
    : IEntityTypeConfiguration<TenantIntelligenceLearningPolicyRecord>
{
    public void Configure(EntityTypeBuilder<TenantIntelligenceLearningPolicyRecord> builder)
    {
        builder.ToTable("tenant_intelligence_learning_policies");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LearningPolicy).HasColumnName("learning_policy").HasMaxLength(64).IsRequired();
        builder.Property(x => x.AllowExport).HasColumnName("allow_export");
        builder.Property(x => x.AllowTraining).HasColumnName("allow_training");
        builder.Property(x => x.AllowShadow).HasColumnName("allow_shadow");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId }).IsUnique();
    }
}

public sealed class NevaxisVerifiedManufacturingDatasetRecordConfiguration
    : IEntityTypeConfiguration<NevaxisVerifiedManufacturingDatasetRecord>
{
    public void Configure(EntityTypeBuilder<NevaxisVerifiedManufacturingDatasetRecord> builder)
    {
        builder.ToTable("nevaxis_verified_manufacturing_datasets");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.VersionCode).HasColumnName("version_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").HasMaxLength(16).IsRequired();
        builder.Property(x => x.ReadinessState).HasColumnName("readiness_state").HasMaxLength(32).IsRequired();
        builder.Property(x => x.TenantPolicy).HasColumnName("tenant_policy").HasMaxLength(64).IsRequired();
        builder.Property(x => x.SanitizationVersion).HasColumnName("sanitization_version").HasMaxLength(16).IsRequired();
        builder.Property(x => x.PolicyVersion).HasColumnName("policy_version").HasMaxLength(16).IsRequired();
        builder.Property(x => x.IncludedCaseCount).HasColumnName("included_case_count");
        builder.Property(x => x.ExcludedCaseCount).HasColumnName("excluded_case_count");
        builder.Property(x => x.ManifestJson).HasColumnName("manifest_json");
        builder.Property(x => x.QualityMetricsJson).HasColumnName("quality_metrics_json");
        builder.Property(x => x.TaskDistributionJson).HasColumnName("task_distribution_json");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.VersionCode }).IsUnique();
    }
}

public sealed class NevaxisVerifiedManufacturingDatasetEntryRecordConfiguration
    : IEntityTypeConfiguration<NevaxisVerifiedManufacturingDatasetEntryRecord>
{
    public void Configure(EntityTypeBuilder<NevaxisVerifiedManufacturingDatasetEntryRecord> builder)
    {
        builder.ToTable("nevaxis_verified_manufacturing_dataset_entries");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.DatasetVersionId).HasColumnName("dataset_version_id");
        builder.Property(x => x.SourceCaseId).HasColumnName("source_case_id");
        builder.Property(x => x.VerifiedImprovementId).HasColumnName("verified_improvement_id");
        builder.Property(x => x.IsNegativeLearning).HasColumnName("is_negative_learning");
        builder.Property(x => x.RecordJson).HasColumnName("record_json");
        builder.Property(x => x.ProvenanceJson).HasColumnName("provenance_json");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DatasetVersionId, x.SourceCaseId }).IsUnique();
    }
}

public sealed class NevaxisModelRegistryRecordConfiguration : IEntityTypeConfiguration<NevaxisModelRegistryRecord>
{
    public void Configure(EntityTypeBuilder<NevaxisModelRegistryRecord> builder)
    {
        builder.ToTable("nevaxis_model_registry");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ModelCode).HasColumnName("model_code").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(x => x.BaseModel).HasColumnName("base_model").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.EvaluationStatus).HasColumnName("evaluation_status").HasMaxLength(32);
        builder.Property(x => x.SafetyStatus).HasColumnName("safety_status").HasMaxLength(32);
        builder.Property(x => x.DeploymentType).HasColumnName("deployment_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ContextLimit).HasColumnName("context_limit");
        builder.Property(x => x.TaskCapabilitiesJson).HasColumnName("task_capabilities_json");
        builder.Property(x => x.TrainingDatasetVersionId).HasColumnName("training_dataset_version_id");
        builder.Property(x => x.EvaluationDatasetVersionId).HasColumnName("evaluation_dataset_version_id");
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(x => x.PromotedAtUtc).HasColumnName("promoted_at_utc");
        builder.Property(x => x.RetiredAtUtc).HasColumnName("retired_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ModelCode, x.Version }).IsUnique();
    }
}

public sealed class NevaxisModelTrainingJobRecordConfiguration : IEntityTypeConfiguration<NevaxisModelTrainingJobRecord>
{
    public void Configure(EntityTypeBuilder<NevaxisModelTrainingJobRecord> builder)
    {
        builder.ToTable("nevaxis_model_training_jobs");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ModelId).HasColumnName("model_id");
        builder.Property(x => x.DatasetVersionId).HasColumnName("dataset_version_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Method).HasColumnName("method").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ArtifactMetadataJson).HasColumnName("artifact_metadata_json");
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(512);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public sealed class NevaxisModelEvaluationRunRecordConfiguration : IEntityTypeConfiguration<NevaxisModelEvaluationRunRecord>
{
    public void Configure(EntityTypeBuilder<NevaxisModelEvaluationRunRecord> builder)
    {
        builder.ToTable("nevaxis_model_evaluation_runs");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ModelId).HasColumnName("model_id");
        builder.Property(x => x.EvaluationDatasetVersionId).HasColumnName("evaluation_dataset_version_id");
        builder.Property(x => x.EvaluationMode).HasColumnName("evaluation_mode").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(64).IsRequired();
        builder.Property(x => x.StructuredOutputValidityRate).HasColumnName("structured_output_validity_rate");
        builder.Property(x => x.SafetyPassRate).HasColumnName("safety_pass_rate");
        builder.Property(x => x.HallucinatedEntityRate).HasColumnName("hallucinated_entity_rate");
        builder.Property(x => x.UnsupportedClaimRate).HasColumnName("unsupported_claim_rate");
        builder.Property(x => x.EstimatedCost).HasColumnName("estimated_cost");
        builder.Property(x => x.LatencyMs).HasColumnName("latency_ms");
        builder.Property(x => x.MetricsJson).HasColumnName("metrics_json");
        builder.Property(x => x.SafetyBenchmarkJson).HasColumnName("safety_benchmark_json");
        builder.Property(x => x.IsShadowMode).HasColumnName("is_shadow_mode");
        builder.Property(x => x.ExecutedOperationally).HasColumnName("executed_operationally");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}

public sealed class VerifiedOutcomeIntelligenceCostRecordConfiguration
    : IEntityTypeConfiguration<VerifiedOutcomeIntelligenceCostRecord>
{
    public void Configure(EntityTypeBuilder<VerifiedOutcomeIntelligenceCostRecord> builder)
    {
        builder.ToTable("verified_outcome_intelligence_costs");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.OptimizationRunId).HasColumnName("optimization_run_id");
        builder.Property(x => x.ScenarioRunId).HasColumnName("scenario_recovery_run_id");
        builder.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TotalIntelligenceCost).HasColumnName("total_intelligence_cost");
        builder.Property(x => x.ModelCallCount).HasColumnName("model_call_count");
        builder.Property(x => x.DeterministicRouteCount).HasColumnName("deterministic_route_count");
        builder.Property(x => x.CostPerVerifiedOutcome).HasColumnName("cost_per_verified_outcome");
        builder.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DigitalWorkCaseId });
    }
}

public sealed class IntelligenceRuntimeBudgetPolicyRecordConfiguration
    : IEntityTypeConfiguration<IntelligenceRuntimeBudgetPolicyRecord>
{
    public void Configure(EntityTypeBuilder<IntelligenceRuntimeBudgetPolicyRecord> builder)
    {
        builder.ToTable("intelligence_runtime_budget_policies");
        builder.HasKey(x => x.Id);
        IntelligenceRuntimeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ScopeId).HasColumnName("scope_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.MaxModelCalls).HasColumnName("max_model_calls");
        builder.Property(x => x.MaxContextTokens).HasColumnName("max_context_tokens");
        builder.Property(x => x.MaxOutputTokens).HasColumnName("max_output_tokens");
        builder.Property(x => x.MaxEstimatedCost).HasColumnName("max_estimated_cost");
        builder.Property(x => x.MaxRetries).HasColumnName("max_retries");
        builder.Property(x => x.MaxSimulationCount).HasColumnName("max_simulation_count");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ScopeType, x.ScopeId }).IsUnique();
    }
}
