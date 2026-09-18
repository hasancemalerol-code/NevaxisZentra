using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.AdaptiveProductionPlanning;

internal sealed class ProductionPlanVersionEntityConfiguration : IEntityTypeConfiguration<ProductionPlanVersion>
{
    public void Configure(EntityTypeBuilder<ProductionPlanVersion> entity)
    {
        entity.ToTable("production_plan_versions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
        entity.Property(e => e.PreviousVersionId).HasColumnName("previous_version_id");
        entity.Property(e => e.VersionNumber).HasColumnName("version_number");
        entity.Property(e => e.PlanningHorizon).HasColumnName("planning_horizon").HasMaxLength(30);
        entity.Property(e => e.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(30);
        entity.Property(e => e.ObjectiveProfileJson).HasColumnName("objective_profile_json");
        entity.Property(e => e.ConstraintSnapshotId).HasColumnName("constraint_snapshot_id");
        entity.Property(e => e.DecisionId).HasColumnName("decision_id");
        entity.Property(e => e.Score).HasColumnName("score");
        entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(120);
        entity.Property(e => e.PublishedAtUtc).HasColumnName("published_at_utc");
        entity.Property(e => e.SupersededAtUtc).HasColumnName("superseded_at_utc");
        entity.Property(e => e.ChangeSummaryJson).HasColumnName("change_summary_json");
        entity.Property(e => e.ExpectedImpactJson).HasColumnName("expected_impact_json");
        entity.Property(e => e.ActualImpactJson).HasColumnName("actual_impact_json");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ProductionPlanId, e.VersionNumber })
            .IsUnique()
            .HasDatabaseName("ix_production_plan_versions_plan_version");
    }
}

internal sealed class ProductionPlanOperationEntityConfiguration : IEntityTypeConfiguration<ProductionPlanOperation>
{
    public void Configure(EntityTypeBuilder<ProductionPlanOperation> entity)
    {
        entity.ToTable("production_plan_operations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
        entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(60);
        entity.Property(e => e.OperationCode).HasColumnName("operation_code").HasMaxLength(40);
        entity.Property(e => e.MachineId).HasColumnName("machine_id").HasMaxLength(60);
        entity.Property(e => e.LineId).HasColumnName("line_id").HasMaxLength(60);
        entity.Property(e => e.MoldId).HasColumnName("mold_id").HasMaxLength(60);
        entity.Property(e => e.ShiftId).HasColumnName("shift_id").HasMaxLength(60);
        entity.Property(e => e.PlannedStartUtc).HasColumnName("planned_start_utc");
        entity.Property(e => e.PlannedEndUtc).HasColumnName("planned_end_utc");
        entity.Property(e => e.SetupMinutes).HasColumnName("setup_minutes");
        entity.Property(e => e.MaterialReadinessState).HasColumnName("material_readiness_state").HasMaxLength(30);
        entity.Property(e => e.SequenceRank).HasColumnName("sequence_rank");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
        entity.HasIndex(e => new { e.TenantId, e.PlanVersionId, e.SequenceRank })
            .HasDatabaseName("ix_production_plan_operations_version_sequence");
    }
}

internal sealed class PlanningConstraintSnapshotEntityConfiguration : IEntityTypeConfiguration<PlanningConstraintSnapshot>
{
    public void Configure(EntityTypeBuilder<PlanningConstraintSnapshot> entity)
    {
        entity.ToTable("planning_constraint_snapshots");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.PlanningHorizon).HasColumnName("planning_horizon").HasMaxLength(30);
        entity.Property(e => e.CapturedAtUtc).HasColumnName("captured_at_utc");
        entity.Property(e => e.OrdersJson).HasColumnName("orders_json");
        entity.Property(e => e.MachinesJson).HasColumnName("machines_json");
        entity.Property(e => e.MaterialsJson).HasColumnName("materials_json");
        entity.Property(e => e.MoldsJson).HasColumnName("molds_json");
        entity.Property(e => e.MaintenanceJson).HasColumnName("maintenance_json");
        entity.Property(e => e.QualityJson).HasColumnName("quality_json");
        entity.Property(e => e.ShiftsJson).HasColumnName("shifts_json");
        entity.Property(e => e.CapacityJson).HasColumnName("capacity_json");
        entity.Property(e => e.IncidentsJson).HasColumnName("incidents_json");
        entity.Property(e => e.FreshnessJson).HasColumnName("freshness_json");
        entity.Property(e => e.HardConstraintsJson).HasColumnName("hard_constraints_json");
        entity.Property(e => e.SoftConstraintsJson).HasColumnName("soft_constraints_json");
    }
}

internal sealed class PlanningScenarioEntityConfiguration : IEntityTypeConfiguration<PlanningScenario>
{
    public void Configure(EntityTypeBuilder<PlanningScenario> entity)
    {
        entity.ToTable("planning_scenarios");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
        entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
        entity.Property(e => e.ConstraintSnapshotId).HasColumnName("constraint_snapshot_id");
        entity.Property(e => e.ScenarioCode).HasColumnName("scenario_code").HasMaxLength(10);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.TriggerType).HasColumnName("trigger_type").HasMaxLength(60);
        entity.Property(e => e.Score).HasColumnName("score");
        entity.Property(e => e.Rank).HasColumnName("rank");
        entity.Property(e => e.ComparisonJson).HasColumnName("comparison_json");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}

internal sealed class PlanningScenarioMetricEntityConfiguration : IEntityTypeConfiguration<PlanningScenarioMetric>
{
    public void Configure(EntityTypeBuilder<PlanningScenarioMetric> entity)
    {
        entity.ToTable("planning_scenario_metrics");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.ScenarioId).HasColumnName("scenario_id");
        entity.Property(e => e.MetricKey).HasColumnName("metric_key").HasMaxLength(80);
        entity.Property(e => e.MetricValue).HasColumnName("metric_value");
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20);
        entity.Property(e => e.DataClassification).HasColumnName("data_classification").HasMaxLength(20);
    }
}

internal sealed class PlanDeviationEntityConfiguration : IEntityTypeConfiguration<PlanDeviation>
{
    public void Configure(EntityTypeBuilder<PlanDeviation> entity)
    {
        entity.ToTable("plan_deviations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
        entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
        entity.Property(e => e.DeviationType).HasColumnName("deviation_type").HasMaxLength(60);
        entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(e => e.DetectedAtUtc).HasColumnName("detected_at_utc");
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000);
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
    }
}

internal sealed class PlanChangeEntityConfiguration : IEntityTypeConfiguration<PlanChange>
{
    public void Configure(EntityTypeBuilder<PlanChange> entity)
    {
        entity.ToTable("plan_changes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
        entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
        entity.Property(e => e.FromVersionId).HasColumnName("from_version_id");
        entity.Property(e => e.ChangeType).HasColumnName("change_type").HasMaxLength(60);
        entity.Property(e => e.TriggerType).HasColumnName("trigger_type").HasMaxLength(60);
        entity.Property(e => e.InitiatedBySubjectId).HasColumnName("initiated_by_subject_id").HasMaxLength(120);
        entity.Property(e => e.DiffJson).HasColumnName("diff_json");
        entity.Property(e => e.ReasonJson).HasColumnName("reason_json");
        entity.Property(e => e.ExpectedBenefitJson).HasColumnName("expected_benefit_json");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}

internal sealed class AdaptivePlanningPolicyEntityConfiguration : IEntityTypeConfiguration<AdaptivePlanningPolicy>
{
    public void Configure(EntityTypeBuilder<AdaptivePlanningPolicy> entity)
    {
        entity.ToTable("adaptive_planning_policies");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.FrozenWindowMinutes).HasColumnName("frozen_window_minutes");
        entity.Property(e => e.NervousnessCooldownMinutes).HasColumnName("nervousness_cooldown_minutes");
        entity.Property(e => e.MinBenefitThreshold).HasColumnName("min_benefit_threshold");
        entity.Property(e => e.AutonomyMode).HasColumnName("autonomy_mode").HasMaxLength(20);
        entity.Property(e => e.PolicyEnvelopeJson).HasColumnName("policy_envelope_json");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId }).IsUnique();
    }
}

internal sealed class PlanActivityEventEntityConfiguration : IEntityTypeConfiguration<PlanActivityEvent>
{
    public void Configure(EntityTypeBuilder<PlanActivityEvent> entity)
    {
        entity.ToTable("plan_activity_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionPlanId).HasColumnName("production_plan_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(60);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(500);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.ProductionPlanId, e.OccurredAtUtc });
    }
}
