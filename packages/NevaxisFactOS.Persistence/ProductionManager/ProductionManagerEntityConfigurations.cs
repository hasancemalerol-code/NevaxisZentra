using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.ProductionManager;

internal sealed class FactoryStateSnapshotEntityConfiguration : IEntityTypeConfiguration<FactoryStateSnapshot>
{
    public void Configure(EntityTypeBuilder<FactoryStateSnapshot> entity)
    {
        entity.ToTable("factory_state_snapshots");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.CapturedAtUtc).HasColumnName("captured_at_utc");
        entity.Property(e => e.ProductionStateJson).HasColumnName("production_state_json");
        entity.Property(e => e.CapacityStateJson).HasColumnName("capacity_state_json");
        entity.Property(e => e.MachineStateJson).HasColumnName("machine_state_json");
        entity.Property(e => e.MaterialStateJson).HasColumnName("material_state_json");
        entity.Property(e => e.QualityStateJson).HasColumnName("quality_state_json");
        entity.Property(e => e.MaintenanceStateJson).HasColumnName("maintenance_state_json");
        entity.Property(e => e.MoldStateJson).HasColumnName("mold_state_json");
        entity.Property(e => e.SupplierStateJson).HasColumnName("supplier_state_json");
        entity.Property(e => e.ShiftStateJson).HasColumnName("shift_state_json");
        entity.Property(e => e.ActiveCasesJson).HasColumnName("active_cases_json");
        entity.Property(e => e.ConstraintsJson).HasColumnName("constraints_json");
        entity.Property(e => e.FreshnessJson).HasColumnName("freshness_json");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.CapturedAtUtc })
            .HasDatabaseName("ix_factory_state_snapshots_tenant_facility_captured");
    }
}

internal sealed class FactoryPredictionEntityConfiguration : IEntityTypeConfiguration<FactoryPrediction>
{
    public void Configure(EntityTypeBuilder<FactoryPrediction> entity)
    {
        entity.ToTable("factory_predictions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.StateSnapshotId).HasColumnName("state_snapshot_id");
        entity.Property(e => e.PredictionType).HasColumnName("prediction_type").HasMaxLength(40);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000);
        entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(e => e.ConfidenceBand).HasColumnName("confidence_band").HasMaxLength(30);
        entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
        entity.Property(e => e.Horizon).HasColumnName("horizon").HasMaxLength(60);
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
        entity.Property(e => e.DataClassification).HasColumnName("data_classification").HasMaxLength(20);
        entity.Property(e => e.CorrelationKey).HasColumnName("correlation_key").HasMaxLength(200);
        entity.Property(e => e.PredictedAtUtc).HasColumnName("predicted_at_utc");
        entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.CorrelationKey })
            .HasDatabaseName("ix_factory_predictions_tenant_facility_correlation");
    }
}

internal sealed class FactoryDecisionEntityConfiguration : IEntityTypeConfiguration<FactoryDecision>
{
    public void Configure(EntityTypeBuilder<FactoryDecision> entity)
    {
        entity.ToTable("factory_decisions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.FactoryCaseId).HasColumnName("factory_case_id");
        entity.Property(e => e.StateSnapshotId).HasColumnName("state_snapshot_id");
        entity.Property(e => e.DecisionType).HasColumnName("decision_type").HasMaxLength(40);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.ProblemSummary).HasColumnName("problem_summary").HasMaxLength(2000);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.AutonomyMode).HasColumnName("autonomy_mode").HasMaxLength(20);
        entity.Property(e => e.ConfidenceBand).HasColumnName("confidence_band").HasMaxLength(30);
        entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
        entity.Property(e => e.SelectedOptionId).HasColumnName("selected_option_id");
        entity.Property(e => e.SelectedBy).HasColumnName("selected_by").HasMaxLength(120);
        entity.Property(e => e.ExpectedOutcomeJson).HasColumnName("expected_outcome_json");
        entity.Property(e => e.VerificationCriteriaJson).HasColumnName("verification_criteria_json");
        entity.Property(e => e.ApprovalState).HasColumnName("approval_state").HasMaxLength(30);
        entity.Property(e => e.AiDecisionLedgerId).HasColumnName("ai_decision_ledger_id");
        entity.Property(e => e.ExecutionIdempotencyKey).HasColumnName("execution_idempotency_key").HasMaxLength(120);
        entity.Property(e => e.ExecutedAtUtc).HasColumnName("executed_at_utc");
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.ActualOutcomeJson).HasColumnName("actual_outcome_json");
        entity.Property(e => e.DecisionResult).HasColumnName("decision_result").HasMaxLength(500);
        entity.Property(e => e.EffectivenessScore).HasColumnName("effectiveness_score");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasOne(e => e.StateSnapshot).WithMany().HasForeignKey(e => e.StateSnapshotId);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.Status })
            .HasDatabaseName("ix_factory_decisions_tenant_facility_status");
        entity.HasIndex(e => new { e.TenantId, e.ExecutionIdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_factory_decisions_tenant_idempotency")
            .HasFilter("execution_idempotency_key IS NOT NULL");
    }
}

internal sealed class FactoryDecisionOptionEntityConfiguration : IEntityTypeConfiguration<FactoryDecisionOption>
{
    public void Configure(EntityTypeBuilder<FactoryDecisionOption> entity)
    {
        entity.ToTable("factory_decision_options");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.DecisionId).HasColumnName("decision_id");
        entity.Property(e => e.OptionCode).HasColumnName("option_code").HasMaxLength(20);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.ExpectedOutcomeJson).HasColumnName("expected_outcome_json");
        entity.Property(e => e.RisksJson).HasColumnName("risks_json");
        entity.Property(e => e.Score).HasColumnName("score");
        entity.Property(e => e.RequiresApproval).HasColumnName("requires_approval");
        entity.Property(e => e.IsRejected).HasColumnName("is_rejected");
        entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
        entity.Property(e => e.Rank).HasColumnName("rank");
        entity.Property(e => e.DataClassification).HasColumnName("data_classification").HasMaxLength(20);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne(e => e.Decision).WithMany(d => d.Options).HasForeignKey(e => e.DecisionId);
        entity.HasIndex(e => new { e.TenantId, e.DecisionId, e.OptionCode })
            .IsUnique()
            .HasDatabaseName("ux_factory_decision_options_tenant_decision_code");
    }
}

internal sealed class FactorySimulationEntityConfiguration : IEntityTypeConfiguration<FactorySimulation>
{
    public void Configure(EntityTypeBuilder<FactorySimulation> entity)
    {
        entity.ToTable("factory_simulations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.DecisionId).HasColumnName("decision_id");
        entity.Property(e => e.StateSnapshotId).HasColumnName("state_snapshot_id");
        entity.Property(e => e.ScenarioType).HasColumnName("scenario_type").HasMaxLength(40);
        entity.Property(e => e.InputJson).HasColumnName("input_json");
        entity.Property(e => e.OutputJson).HasColumnName("output_json");
        entity.Property(e => e.ComparisonJson).HasColumnName("comparison_json");
        entity.Property(e => e.DataClassification).HasColumnName("data_classification").HasMaxLength(20);
        entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(120);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}

internal sealed class FactoryDecisionOutcomeEntityConfiguration : IEntityTypeConfiguration<FactoryDecisionOutcome>
{
    public void Configure(EntityTypeBuilder<FactoryDecisionOutcome> entity)
    {
        entity.ToTable("factory_decision_outcomes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.DecisionId).HasColumnName("decision_id");
        entity.Property(e => e.ExpectedJson).HasColumnName("expected_json");
        entity.Property(e => e.ActualJson).HasColumnName("actual_json");
        entity.Property(e => e.VarianceJson).HasColumnName("variance_json");
        entity.Property(e => e.EffectivenessScore).HasColumnName("effectiveness_score");
        entity.Property(e => e.Result).HasColumnName("result").HasMaxLength(500);
        entity.Property(e => e.DataClassification).HasColumnName("data_classification").HasMaxLength(20);
        entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.DecisionId })
            .HasDatabaseName("ix_factory_decision_outcomes_tenant_decision");
    }
}

internal sealed class FactoryDecisionMemoryEntityConfiguration : IEntityTypeConfiguration<FactoryDecisionMemory>
{
    public void Configure(EntityTypeBuilder<FactoryDecisionMemory> entity)
    {
        entity.ToTable("factory_decision_memories");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.DecisionId).HasColumnName("decision_id");
        entity.Property(e => e.DecisionType).HasColumnName("decision_type").HasMaxLength(40);
        entity.Property(e => e.ContextJson).HasColumnName("context_json");
        entity.Property(e => e.SelectedOptionCode).HasColumnName("selected_option_code").HasMaxLength(20);
        entity.Property(e => e.OutcomeSummary).HasColumnName("outcome_summary").HasMaxLength(2000);
        entity.Property(e => e.EffectivenessScore).HasColumnName("effectiveness_score");
        entity.Property(e => e.ConditionsJson).HasColumnName("conditions_json");
        entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.DecisionType })
            .HasDatabaseName("ix_factory_decision_memories_tenant_facility_type");
    }
}

internal sealed class ProductionManagerPolicyEntityConfiguration : IEntityTypeConfiguration<ProductionManagerPolicy>
{
    public void Configure(EntityTypeBuilder<ProductionManagerPolicy> entity)
    {
        entity.ToTable("production_manager_policies");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.AutonomyMode).HasColumnName("autonomy_mode").HasMaxLength(20);
        entity.Property(e => e.AllowedDecisionTypesJson).HasColumnName("allowed_decision_types_json");
        entity.Property(e => e.MaxScheduleChangeHours).HasColumnName("max_schedule_change_hours");
        entity.Property(e => e.MaxFinancialImpact).HasColumnName("max_financial_impact");
        entity.Property(e => e.ApprovalThresholdScore).HasColumnName("approval_threshold_score");
        entity.Property(e => e.LowConfidenceThreshold).HasColumnName("low_confidence_threshold");
        entity.Property(e => e.StaleDataMaxMinutes).HasColumnName("stale_data_max_minutes");
        entity.Property(e => e.ProhibitedDecisionsJson).HasColumnName("prohibited_decisions_json");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId })
            .IsUnique()
            .HasDatabaseName("ux_production_manager_policies_tenant_facility");
    }
}
