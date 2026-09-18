using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.ThroughputOptimizer;

internal static class ThroughputOptimizerEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IThroughputOptimizerTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class ThroughputOptimizerRunRecordConfiguration : IEntityTypeConfiguration<ThroughputOptimizerRunRecord>
{
    public void Configure(EntityTypeBuilder<ThroughputOptimizerRunRecord> builder)
    {
        builder.ToTable("throughput_optimizer_runs");
        builder.HasKey(x => x.Id);
        ThroughputOptimizerEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.ContextSnapshotId).HasColumnName("context_snapshot_id");
        builder.Property(x => x.CorrelationKey).HasColumnName("correlation_key").HasMaxLength(240).IsRequired();
        builder.Property(x => x.CurrentPhase).HasColumnName("current_phase").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ObservationJson).HasColumnName("observation_json");
        builder.Property(x => x.DiagnosisJson).HasColumnName("diagnosis_json");
        builder.Property(x => x.SimulationJson).HasColumnName("simulation_json");
        builder.Property(x => x.SelectionJson).HasColumnName("selection_json");
        builder.Property(x => x.ActionPlanJson).HasColumnName("action_plan_json");
        builder.Property(x => x.ModelIntelligencePath).HasColumnName("model_intelligence_path").HasMaxLength(32);
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.CorrelationKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DigitalWorkCaseId }).IsUnique();
    }
}

public sealed class VerifiedImprovementRecordConfiguration : IEntityTypeConfiguration<VerifiedImprovementRecord>
{
    public void Configure(EntityTypeBuilder<VerifiedImprovementRecord> builder)
    {
        builder.ToTable("verified_improvement_records");
        builder.HasKey(x => x.Id);
        ThroughputOptimizerEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.ThroughputOptimizerRunId).HasColumnName("throughput_optimizer_run_id");
        builder.Property(x => x.VerificationOutcome).HasColumnName("verification_outcome").HasMaxLength(64).IsRequired();
        builder.Property(x => x.BaselineJson).HasColumnName("baseline_json");
        builder.Property(x => x.TargetImprovementPerHour).HasColumnName("target_improvement_per_hour");
        builder.Property(x => x.InterventionSummary).HasColumnName("intervention_summary").HasMaxLength(512).IsRequired();
        builder.Property(x => x.ActualJson).HasColumnName("actual_json");
        builder.Property(x => x.QualityGuardPassed).HasColumnName("quality_guard_passed");
        builder.Property(x => x.SafetyGuardPassed).HasColumnName("safety_guard_passed");
        builder.Property(x => x.ObservationStartUtc).HasColumnName("observation_start_utc");
        builder.Property(x => x.ObservationEndUtc).HasColumnName("observation_end_utc");
        builder.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at_utc");
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DigitalWorkCaseId }).IsUnique();
    }
}

public sealed class VerifiedTrainingDatasetCandidateRecordConfiguration : IEntityTypeConfiguration<VerifiedTrainingDatasetCandidateRecord>
{
    public void Configure(EntityTypeBuilder<VerifiedTrainingDatasetCandidateRecord> builder)
    {
        builder.ToTable("verified_training_dataset_candidates");
        builder.HasKey(x => x.Id);
        ThroughputOptimizerEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.VerifiedImprovementId).HasColumnName("verified_improvement_id");
        builder.Property(x => x.EligibilityStatus).HasColumnName("eligibility_status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.IneligibilityReason).HasColumnName("ineligibility_reason").HasMaxLength(512);
        builder.Property(x => x.RecordJson).HasColumnName("record_json");
        builder.Property(x => x.TenantPolicyJson).HasColumnName("tenant_policy_json");
        builder.Property(x => x.VerificationLevel).HasColumnName("verification_level").HasMaxLength(32).IsRequired();
        builder.Property(x => x.DataQualityScore).HasColumnName("data_quality_score").HasMaxLength(16).IsRequired();
        builder.Property(x => x.AnonymizationEligible).HasColumnName("anonymization_eligible");
        builder.Property(x => x.ExportEligible).HasColumnName("export_eligible");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DigitalWorkCaseId }).IsUnique();
    }
}
