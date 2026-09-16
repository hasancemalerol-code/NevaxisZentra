using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.ContextKnowledge;

internal static class ContextKnowledgeEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IContextKnowledgeTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class CanonicalOperationalStateRecordConfiguration : IEntityTypeConfiguration<CanonicalOperationalStateRecord>
{
    public void Configure(EntityTypeBuilder<CanonicalOperationalStateRecord> builder)
    {
        builder.ToTable("context_knowledge_canonical_states");
        builder.HasKey(x => x.Id);
        ContextKnowledgeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResolutionStatus).HasColumnName("resolution_status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResolvedStateValue).HasColumnName("resolved_state_value").HasMaxLength(256);
        builder.Property(x => x.Confidence).HasColumnName("confidence");
        builder.Property(x => x.SourceClaimsJson).HasColumnName("source_claims_json");
        builder.Property(x => x.ConflictReasonsJson).HasColumnName("conflict_reasons_json");
        builder.Property(x => x.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.EntityType, x.EntityId }).IsUnique();
    }
}

public sealed class FactoryKnowledgeRecordConfiguration : IEntityTypeConfiguration<FactoryKnowledgeRecord>
{
    public void Configure(EntityTypeBuilder<FactoryKnowledgeRecord> builder)
    {
        builder.ToTable("context_knowledge_factory_records");
        builder.HasKey(x => x.Id);
        ContextKnowledgeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.SourceCaseId).HasColumnName("source_case_id");
        builder.Property(x => x.KnowledgeKey).HasColumnName("knowledge_key").HasMaxLength(240).IsRequired();
        builder.Property(x => x.ProblemSymptom).HasColumnName("problem_symptom").HasMaxLength(512).IsRequired();
        builder.Property(x => x.RelevantConditionsJson).HasColumnName("relevant_conditions_json");
        builder.Property(x => x.RootCause).HasColumnName("root_cause").HasMaxLength(512).IsRequired();
        builder.Property(x => x.DecisionSummary).HasColumnName("decision_summary").HasMaxLength(512).IsRequired();
        builder.Property(x => x.ActionsJson).HasColumnName("actions_json");
        builder.Property(x => x.EvidenceRefsJson).HasColumnName("evidence_refs_json");
        builder.Property(x => x.BeforeStateJson).HasColumnName("before_state_json");
        builder.Property(x => x.AfterStateJson).HasColumnName("after_state_json");
        builder.Property(x => x.VerifiedOutcome).HasColumnName("verified_outcome").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Confidence).HasColumnName("confidence");
        builder.Property(x => x.ApplicabilityConstraintsJson).HasColumnName("applicability_constraints_json");
        builder.Property(x => x.PromotionStatus).HasColumnName("promotion_status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ObservationWindowStartUtc).HasColumnName("observation_window_start_utc");
        builder.Property(x => x.ObservationWindowEndUtc).HasColumnName("observation_window_end_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.PromotedAtUtc).HasColumnName("promoted_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.KnowledgeKey }).IsUnique();
    }
}

public sealed class ContextSnapshotRecordConfiguration : IEntityTypeConfiguration<ContextSnapshotRecord>
{
    public void Configure(EntityTypeBuilder<ContextSnapshotRecord> builder)
    {
        builder.ToTable("context_knowledge_snapshots");
        builder.HasKey(x => x.Id);
        ContextKnowledgeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.SnapshotCode).HasColumnName("snapshot_code").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ScopeJson).HasColumnName("scope_json");
        builder.Property(x => x.BudgetJson).HasColumnName("budget_json");
        builder.Property(x => x.MaterializedContextJson).HasColumnName("materialized_context_json");
        builder.Property(x => x.ProvenanceJson).HasColumnName("provenance_json");
        builder.Property(x => x.MetricsJson).HasColumnName("metrics_json");
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.SnapshotCode }).IsUnique();
    }
}

public sealed class DataLifecyclePolicyRecordConfiguration : IEntityTypeConfiguration<DataLifecyclePolicyRecord>
{
    public void Configure(EntityTypeBuilder<DataLifecyclePolicyRecord> builder)
    {
        builder.ToTable("context_knowledge_lifecycle_policies");
        builder.HasKey(x => x.Id);
        ContextKnowledgeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.DataType).HasColumnName("data_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.HotRetentionHours).HasColumnName("hot_retention_hours");
        builder.Property(x => x.WarmRetentionDays).HasColumnName("warm_retention_days");
        builder.Property(x => x.ColdArchiveDays).HasColumnName("cold_archive_days");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DataType }).IsUnique();
    }
}

public sealed class TelemetryAggregateRecordConfiguration : IEntityTypeConfiguration<TelemetryAggregateRecord>
{
    public void Configure(EntityTypeBuilder<TelemetryAggregateRecord> builder)
    {
        builder.ToTable("context_knowledge_telemetry_aggregates");
        builder.HasKey(x => x.Id);
        ContextKnowledgeEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.SignalType).HasColumnName("signal_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Granularity).HasColumnName("granularity").HasMaxLength(16).IsRequired();
        builder.Property(x => x.WindowStartUtc).HasColumnName("window_start_utc");
        builder.Property(x => x.WindowEndUtc).HasColumnName("window_end_utc");
        builder.Property(x => x.AggregateValue).HasColumnName("aggregate_value");
        builder.Property(x => x.SampleCount).HasColumnName("sample_count");
        builder.Property(x => x.PreserveRawWindow).HasColumnName("preserve_raw_window");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ResourceId, x.SignalType, x.Granularity, x.WindowStartUtc });
    }
}
