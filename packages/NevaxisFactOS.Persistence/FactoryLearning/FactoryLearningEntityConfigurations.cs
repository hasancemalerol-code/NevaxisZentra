using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.FactoryLearning;

internal sealed class FactoryLossEntityConfiguration : IEntityTypeConfiguration<FactoryLoss>
{
    public void Configure(EntityTypeBuilder<FactoryLoss> entity)
    {
        entity.ToTable("factory_losses");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(40);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000);
        entity.Property(e => e.LineId).HasColumnName("line_id").HasMaxLength(60);
        entity.Property(e => e.MachineId).HasColumnName("machine_id").HasMaxLength(60);
        entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(60);
        entity.Property(e => e.MoldId).HasColumnName("mold_id").HasMaxLength(60);
        entity.Property(e => e.MaterialId).HasColumnName("material_id").HasMaxLength(60);
        entity.Property(e => e.ShiftId).HasColumnName("shift_id").HasMaxLength(60);
        entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
        entity.Property(e => e.QuantityImpact).HasColumnName("quantity_impact");
        entity.Property(e => e.CostEstimate).HasColumnName("cost_estimate");
        entity.Property(e => e.SourceModule).HasColumnName("source_module").HasMaxLength(80);
        entity.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
        entity.Property(e => e.DataClassification).HasColumnName("data_classification").HasMaxLength(20);
        entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
        entity.Property(e => e.CorrelationKey).HasColumnName("correlation_key").HasMaxLength(200);
        entity.Property(e => e.RecurrenceCount).HasColumnName("recurrence_count");
        entity.Property(e => e.FactoryCaseId).HasColumnName("factory_case_id");
        entity.Property(e => e.FactoryDecisionId).HasColumnName("factory_decision_id");
        entity.Property(e => e.LossTreePathJson).HasColumnName("loss_tree_path_json");
        entity.Property(e => e.FirstSeenAtUtc).HasColumnName("first_seen_at_utc");
        entity.Property(e => e.LastSeenAtUtc).HasColumnName("last_seen_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.CorrelationKey })
            .HasDatabaseName("ix_factory_losses_tenant_facility_correlation");
    }
}

internal sealed class FactoryLossPatternEntityConfiguration : IEntityTypeConfiguration<FactoryLossPattern>
{
    public void Configure(EntityTypeBuilder<FactoryLossPattern> entity)
    {
        entity.ToTable("factory_loss_patterns");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.PatternType).HasColumnName("pattern_type").HasMaxLength(60);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000);
        entity.Property(e => e.PatternKey).HasColumnName("pattern_key").HasMaxLength(200);
        entity.Property(e => e.ContextJson).HasColumnName("context_json");
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
        entity.Property(e => e.OccurrenceCount).HasColumnName("occurrence_count");
        entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
        entity.Property(e => e.DataClassification).HasColumnName("data_classification").HasMaxLength(20);
        entity.Property(e => e.FirstObservedAtUtc).HasColumnName("first_observed_at_utc");
        entity.Property(e => e.LastObservedAtUtc).HasColumnName("last_observed_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.PatternKey })
            .HasDatabaseName("ix_factory_loss_patterns_tenant_facility_key");
    }
}

internal sealed class ImprovementOpportunityEntityConfiguration : IEntityTypeConfiguration<ImprovementOpportunity>
{
    public void Configure(EntityTypeBuilder<ImprovementOpportunity> entity)
    {
        entity.ToTable("improvement_opportunities");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.PatternId).HasColumnName("pattern_id");
        entity.Property(e => e.LossId).HasColumnName("loss_id");
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.ProblemSummary).HasColumnName("problem_summary").HasMaxLength(2000);
        entity.Property(e => e.HypothesisJson).HasColumnName("hypothesis_json");
        entity.Property(e => e.ExpectedMechanism).HasColumnName("expected_mechanism").HasMaxLength(500);
        entity.Property(e => e.ExpectedImpactJson).HasColumnName("expected_impact_json");
        entity.Property(e => e.AffectedScopeJson).HasColumnName("affected_scope_json");
        entity.Property(e => e.RiskJson).HasColumnName("risk_json");
        entity.Property(e => e.VerificationCriteriaJson).HasColumnName("verification_criteria_json");
        entity.Property(e => e.RollbackCriteriaJson).HasColumnName("rollback_criteria_json");
        entity.Property(e => e.PriorityScore).HasColumnName("priority_score");
        entity.Property(e => e.RecoverablePotentialScore).HasColumnName("recoverable_potential_score");
        entity.Property(e => e.ImpactClassification).HasColumnName("impact_classification").HasMaxLength(20);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.OwnerSubjectId).HasColumnName("owner_subject_id").HasMaxLength(120);
        entity.Property(e => e.Domain).HasColumnName("domain").HasMaxLength(40);
        entity.Property(e => e.AssignedWorkerId).HasColumnName("assigned_worker_id");
        entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.Status })
            .HasDatabaseName("ix_improvement_opportunities_tenant_facility_status");
    }
}

internal sealed class FactoryExperimentEntityConfiguration : IEntityTypeConfiguration<FactoryExperiment>
{
    public void Configure(EntityTypeBuilder<FactoryExperiment> entity)
    {
        entity.ToTable("factory_experiments");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.OpportunityId).HasColumnName("opportunity_id");
        entity.Property(e => e.ExperimentType).HasColumnName("experiment_type").HasMaxLength(30);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Hypothesis).HasColumnName("hypothesis").HasMaxLength(2000);
        entity.Property(e => e.ScopeJson).HasColumnName("scope_json");
        entity.Property(e => e.BaselineJson).HasColumnName("baseline_json");
        entity.Property(e => e.TargetJson).HasColumnName("target_json");
        entity.Property(e => e.GuardrailsJson).HasColumnName("guardrails_json");
        entity.Property(e => e.StopConditionsJson).HasColumnName("stop_conditions_json");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.Outcome).HasColumnName("outcome").HasMaxLength(30);
        entity.Property(e => e.OwnerSubjectId).HasColumnName("owner_subject_id").HasMaxLength(120);
        entity.Property(e => e.ApprovalState).HasColumnName("approval_state").HasMaxLength(20);
        entity.Property(e => e.ApprovedBy).HasColumnName("approved_by").HasMaxLength(120);
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.StoppedAtUtc).HasColumnName("stopped_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.Status })
            .HasDatabaseName("ix_factory_experiments_tenant_facility_status");
    }
}

internal sealed class FactoryExperimentMeasurementEntityConfiguration : IEntityTypeConfiguration<FactoryExperimentMeasurement>
{
    public void Configure(EntityTypeBuilder<FactoryExperimentMeasurement> entity)
    {
        entity.ToTable("factory_experiment_measurements");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.ExperimentId).HasColumnName("experiment_id");
        entity.Property(e => e.Phase).HasColumnName("phase").HasMaxLength(20);
        entity.Property(e => e.MetricKey).HasColumnName("metric_key").HasMaxLength(80);
        entity.Property(e => e.MetricValue).HasColumnName("metric_value");
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20);
        entity.Property(e => e.DataClassification).HasColumnName("data_classification").HasMaxLength(20);
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
        entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.ExperimentId, e.Phase })
            .HasDatabaseName("ix_factory_experiment_measurements_tenant_experiment_phase");
    }
}

internal sealed class VerifiedImprovementEntityConfiguration : IEntityTypeConfiguration<VerifiedImprovement>
{
    public void Configure(EntityTypeBuilder<VerifiedImprovement> entity)
    {
        entity.ToTable("verified_improvements");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExperimentId).HasColumnName("experiment_id");
        entity.Property(e => e.OpportunityId).HasColumnName("opportunity_id");
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.OutcomeSummary).HasColumnName("outcome_summary").HasMaxLength(2000);
        entity.Property(e => e.BeforeJson).HasColumnName("before_json");
        entity.Property(e => e.AfterJson).HasColumnName("after_json");
        entity.Property(e => e.DeltaJson).HasColumnName("delta_json");
        entity.Property(e => e.EffectivenessScore).HasColumnName("effectiveness_score");
        entity.Property(e => e.ImpactClassification).HasColumnName("impact_classification").HasMaxLength(20);
        entity.Property(e => e.VerificationEvidenceJson).HasColumnName("verification_evidence_json");
        entity.Property(e => e.StandardizationState).HasColumnName("standardization_state").HasMaxLength(20);
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.StandardizedAtUtc).HasColumnName("standardized_at_utc");
        entity.Property(e => e.SustainabilityCheckedAtUtc).HasColumnName("sustainability_checked_at_utc");
        entity.Property(e => e.SustainabilityStatus).HasColumnName("sustainability_status").HasMaxLength(20);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token");
    }
}

internal sealed class FactoryLearningMemoryEntityConfiguration : IEntityTypeConfiguration<FactoryLearningMemory>
{
    public void Configure(EntityTypeBuilder<FactoryLearningMemory> entity)
    {
        entity.ToTable("factory_learning_memories");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.VerifiedImprovementId).HasColumnName("verified_improvement_id");
        entity.Property(e => e.DecisionMemoryId).HasColumnName("decision_memory_id");
        entity.Property(e => e.ProblemPattern).HasColumnName("problem_pattern").HasMaxLength(2000);
        entity.Property(e => e.ContextJson).HasColumnName("context_json");
        entity.Property(e => e.RootCauseSummary).HasColumnName("root_cause_summary").HasMaxLength(2000);
        entity.Property(e => e.RootCauseConfidence).HasColumnName("root_cause_confidence").HasMaxLength(30);
        entity.Property(e => e.InterventionSummary).HasColumnName("intervention_summary").HasMaxLength(2000);
        entity.Property(e => e.ExperimentSummary).HasColumnName("experiment_summary").HasMaxLength(2000);
        entity.Property(e => e.ResultSummary).HasColumnName("result_summary").HasMaxLength(2000);
        entity.Property(e => e.EffectivenessScore).HasColumnName("effectiveness_score");
        entity.Property(e => e.MemoryQuality).HasColumnName("memory_quality").HasMaxLength(20);
        entity.Property(e => e.ScopeJson).HasColumnName("scope_json");
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
        entity.Property(e => e.RevalidationCriteriaJson).HasColumnName("revalidation_criteria_json");
        entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
    }
}

internal sealed class FactoryRootCauseAnalysisEntityConfiguration : IEntityTypeConfiguration<FactoryRootCauseAnalysis>
{
    public void Configure(EntityTypeBuilder<FactoryRootCauseAnalysis> entity)
    {
        entity.ToTable("factory_root_cause_analyses");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.PatternId).HasColumnName("pattern_id");
        entity.Property(e => e.LossId).HasColumnName("loss_id");
        entity.Property(e => e.Method).HasColumnName("method").HasMaxLength(30);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000);
        entity.Property(e => e.WhyChainJson).HasColumnName("why_chain_json");
        entity.Property(e => e.ContributingFactorsJson).HasColumnName("contributing_factors_json");
        entity.Property(e => e.CandidateCause).HasColumnName("candidate_cause").HasMaxLength(500);
        entity.Property(e => e.ConfidenceLevel).HasColumnName("confidence_level").HasMaxLength(30);
        entity.Property(e => e.IsConfirmedCause).HasColumnName("is_confirmed_cause");
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token");
    }
}
