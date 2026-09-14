using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.GoldenDemoP1;

internal sealed class GoldenDemoProductionEventEntityConfiguration : IEntityTypeConfiguration<GoldenDemoProductionEvent>
{
    public void Configure(EntityTypeBuilder<GoldenDemoProductionEvent> entity)
    {
        entity.ToTable("golden_demo_production_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(e => e.MachineCode).HasColumnName("machine_code").HasMaxLength(50);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.MoldReference).HasColumnName("mold_reference").HasMaxLength(100);
        entity.Property(e => e.ProductReference).HasColumnName("product_reference").HasMaxLength(100);
        entity.Property(e => e.ShiftTargetGoodParts).HasColumnName("shift_target_good_parts");
        entity.Property(e => e.ShiftStartUtc).HasColumnName("shift_start_utc");
        entity.Property(e => e.ShiftEndUtc).HasColumnName("shift_end_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MachineAssetId, e.Status });
    }
}

internal sealed class GoldenDemoCycleBaselineEntityConfiguration : IEntityTypeConfiguration<GoldenDemoCycleBaseline>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCycleBaseline> entity)
    {
        entity.ToTable("golden_demo_cycle_baselines");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionEventId).HasColumnName("production_event_id");
        entity.Property(e => e.ReferenceSource).HasColumnName("reference_source").HasMaxLength(40);
        entity.Property(e => e.ReferenceSourceLabel).HasColumnName("reference_source_label").HasMaxLength(200);
        entity.Property(e => e.TotalCycleSeconds).HasColumnName("total_cycle_seconds");
        entity.Property(e => e.PhaseDurationsJson).HasColumnName("phase_durations_json");
        entity.Property(e => e.ProcessParametersJson).HasColumnName("process_parameters_json");
        entity.Property(e => e.ReferenceKwhPerGoodPart).HasColumnName("reference_kwh_per_good_part");
        entity.Property(e => e.ValidFromUtc).HasColumnName("valid_from_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(e => e.ProductionEventId).IsUnique();
    }
}

internal sealed class GoldenDemoCycleObservationEntityConfiguration : IEntityTypeConfiguration<GoldenDemoCycleObservation>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCycleObservation> entity)
    {
        entity.ToTable("golden_demo_cycle_observations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionEventId).HasColumnName("production_event_id");
        entity.Property(e => e.ObservationKind).HasColumnName("observation_kind").HasMaxLength(40);
        entity.Property(e => e.TotalCycleSeconds).HasColumnName("total_cycle_seconds");
        entity.Property(e => e.PhaseDurationsJson).HasColumnName("phase_durations_json");
        entity.Property(e => e.ProcessParametersJson).HasColumnName("process_parameters_json");
        entity.Property(e => e.MicroStopCount).HasColumnName("micro_stop_count");
        entity.Property(e => e.MicroStopDurationSeconds).HasColumnName("micro_stop_duration_seconds");
        entity.Property(e => e.GoodParts).HasColumnName("good_parts");
        entity.Property(e => e.RejectParts).HasColumnName("reject_parts");
        entity.Property(e => e.AvailabilityPercent).HasColumnName("availability_percent");
        entity.Property(e => e.KwhPerGoodPart).HasColumnName("kwh_per_good_part");
        entity.Property(e => e.TotalEnergyKwh).HasColumnName("total_energy_kwh");
        entity.Property(e => e.MachineState).HasColumnName("machine_state").HasMaxLength(40);
        entity.Property(e => e.ObservedAtUtc).HasColumnName("observed_at_utc");
        entity.Property(e => e.WindowStartUtc).HasColumnName("window_start_utc");
        entity.Property(e => e.WindowEndUtc).HasColumnName("window_end_utc");
        entity.HasIndex(e => new { e.ProductionEventId, e.ObservationKind });
    }
}

internal sealed class GoldenDemoRecoveryPlanEntityConfiguration : IEntityTypeConfiguration<GoldenDemoRecoveryPlan>
{
    public void Configure(EntityTypeBuilder<GoldenDemoRecoveryPlan> entity)
    {
        entity.ToTable("golden_demo_recovery_plans");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionEventId).HasColumnName("production_event_id");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.RecommendationSummary).HasColumnName("recommendation_summary");
        entity.Property(e => e.RecommendedScenarioCode).HasColumnName("recommended_scenario_code").HasMaxLength(60);
        entity.Property(e => e.RequiresApproval).HasColumnName("requires_approval");
        entity.Property(e => e.ApprovedBySubjectId).HasColumnName("approved_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.ApprovedAtUtc).HasColumnName("approved_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => e.ProductionEventId).IsUnique();
    }
}

internal sealed class GoldenDemoRecoveryPlanActionEntityConfiguration : IEntityTypeConfiguration<GoldenDemoRecoveryPlanAction>
{
    public void Configure(EntityTypeBuilder<GoldenDemoRecoveryPlanAction> entity)
    {
        entity.ToTable("golden_demo_recovery_plan_actions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.RecoveryPlanId).HasColumnName("recovery_plan_id");
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.Property(e => e.ActionCode).HasColumnName("action_code").HasMaxLength(60);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.ResponsibleRole).HasColumnName("responsible_role").HasMaxLength(80);
        entity.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(20);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(e => e.EvidenceRequired).HasColumnName("evidence_required");
        entity.Property(e => e.EstimatedDowntimeMinutes).HasColumnName("estimated_downtime_minutes");
        entity.Property(e => e.RequiresApproval).HasColumnName("requires_approval");
        entity.Property(e => e.ValidationCriteria).HasColumnName("validation_criteria");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.HasIndex(e => new { e.RecoveryPlanId, e.SortOrder });
    }
}

internal sealed class GoldenDemoAgentFindingEntityConfiguration : IEntityTypeConfiguration<GoldenDemoAgentFinding>
{
    public void Configure(EntityTypeBuilder<GoldenDemoAgentFinding> entity)
    {
        entity.ToTable("golden_demo_agent_findings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionEventId).HasColumnName("production_event_id");
        entity.Property(e => e.AgentCode).HasColumnName("agent_code").HasMaxLength(60);
        entity.Property(e => e.WorkerCode).HasColumnName("worker_code").HasMaxLength(60);
        entity.Property(e => e.Summary).HasColumnName("summary");
        entity.Property(e => e.DetailJson).HasColumnName("detail_json");
        entity.Property(e => e.EvidenceClassification).HasColumnName("evidence_classification").HasMaxLength(30);
        entity.Property(e => e.GeneratedAtUtc).HasColumnName("generated_at_utc");
        entity.HasIndex(e => new { e.ProductionEventId, e.AgentCode });
    }
}

internal sealed class GoldenDemoVerificationRunEntityConfiguration : IEntityTypeConfiguration<GoldenDemoVerificationRun>
{
    public void Configure(EntityTypeBuilder<GoldenDemoVerificationRun> entity)
    {
        entity.ToTable("golden_demo_verification_runs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionEventId).HasColumnName("production_event_id");
        entity.Property(e => e.Result).HasColumnName("result").HasMaxLength(30);
        entity.Property(e => e.CriteriaJson).HasColumnName("criteria_json");
        entity.Property(e => e.Summary).HasColumnName("summary");
        entity.Property(e => e.RecoveredGoodPartsPerShift).HasColumnName("recovered_good_parts_per_shift");
        entity.Property(e => e.TriggeredBySubjectId).HasColumnName("triggered_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.HasIndex(e => new { e.ProductionEventId, e.VerifiedAtUtc });
    }
}

internal sealed class GoldenDemoDigitalWorkCaseLinkEntityConfiguration : IEntityTypeConfiguration<GoldenDemoDigitalWorkCaseLink>
{
    public void Configure(EntityTypeBuilder<GoldenDemoDigitalWorkCaseLink> entity)
    {
        entity.ToTable("golden_demo_digital_work_case_links");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionEventId).HasColumnName("production_event_id");
        entity.Property(e => e.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        entity.Property(e => e.WorkerCode).HasColumnName("worker_code").HasMaxLength(60);
        entity.Property(e => e.LinkedAtUtc).HasColumnName("linked_at_utc");
        entity.HasIndex(e => new { e.ProductionEventId, e.WorkerCode }).IsUnique();
    }
}
