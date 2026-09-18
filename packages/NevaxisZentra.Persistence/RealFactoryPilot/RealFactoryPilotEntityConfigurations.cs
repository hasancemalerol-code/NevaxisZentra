using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.RealFactoryPilot;

internal static class RealFactoryPilotEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property("Id").HasColumnName("id");
        builder.Property("TenantId").HasColumnName("tenant_id");
        builder.Property("FacilityId").HasColumnName("facility_id");
    }
}

internal sealed class RealFactoryPilotRunRecordConfiguration : IEntityTypeConfiguration<RealFactoryPilotRunRecord>
{
    public void Configure(EntityTypeBuilder<RealFactoryPilotRunRecord> builder)
    {
        builder.ToTable("real_factory_pilot_runs");
        builder.HasKey(x => x.Id);
        RealFactoryPilotEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.ThroughputOptimizerRunId).HasColumnName("throughput_optimizer_run_id");
        builder.Property(x => x.LineCode).HasColumnName("line_code").HasMaxLength(64);
        builder.Property(x => x.CurrentPhase).HasColumnName("current_phase").HasMaxLength(32);
        builder.Property(x => x.AcceptanceKind).HasColumnName("acceptance_kind").HasMaxLength(64);
        builder.Property(x => x.ConnectorStateJson).HasColumnName("connector_state_json");
        builder.Property(x => x.ShadowStateJson).HasColumnName("shadow_state_json");
        builder.Property(x => x.CalibrationJson).HasColumnName("calibration_json");
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId }).IsUnique();
    }
}

internal sealed class RealFactoryFieldMappingRecordConfiguration : IEntityTypeConfiguration<RealFactoryFieldMappingRecord>
{
    public void Configure(EntityTypeBuilder<RealFactoryFieldMappingRecord> builder)
    {
        builder.ToTable("real_factory_field_mappings");
        builder.HasKey(x => x.Id);
        RealFactoryPilotEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ConnectorId).HasColumnName("connector_id");
        builder.Property(x => x.MappingVersionId).HasColumnName("mapping_version_id");
        builder.Property(x => x.MappingVersionNumber).HasColumnName("mapping_version_number");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
        builder.Property(x => x.SourceField).HasColumnName("source_field").HasMaxLength(256);
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(64);
        builder.Property(x => x.TargetEntity).HasColumnName("target_entity").HasMaxLength(64);
        builder.Property(x => x.TargetEntityId).HasColumnName("target_entity_id").HasMaxLength(128);
        builder.Property(x => x.CanonicalEventType).HasColumnName("canonical_event_type").HasMaxLength(64);
        builder.Property(x => x.Transformation).HasColumnName("transformation").HasMaxLength(128);
        builder.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(32);
        builder.Property(x => x.Scale).HasColumnName("scale");
        builder.Property(x => x.QualityRule).HasColumnName("quality_rule").HasMaxLength(128);
        builder.Property(x => x.TimestampRule).HasColumnName("timestamp_rule").HasMaxLength(32);
        builder.Property(x => x.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.ActivatedAtUtc).HasColumnName("activated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.ConnectorId, x.SourceField, x.MappingVersionNumber });
    }
}

internal sealed class RealFactoryDiscoveryRecordConfiguration : IEntityTypeConfiguration<RealFactoryDiscoveryRecord>
{
    public void Configure(EntityTypeBuilder<RealFactoryDiscoveryRecord> builder)
    {
        builder.ToTable("real_factory_discovery_records");
        builder.HasKey(x => x.Id);
        RealFactoryPilotEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ConnectorId).HasColumnName("connector_id");
        builder.Property(x => x.DiscoveryJson).HasColumnName("discovery_json");
        builder.Property(x => x.DiscoveredAtUtc).HasColumnName("discovered_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.ConnectorId, x.DiscoveredAtUtc });
    }
}

internal sealed class RealFactoryShadowSessionRecordConfiguration : IEntityTypeConfiguration<RealFactoryShadowSessionRecord>
{
    public void Configure(EntityTypeBuilder<RealFactoryShadowSessionRecord> builder)
    {
        builder.ToTable("real_factory_shadow_sessions");
        builder.HasKey(x => x.Id);
        RealFactoryPilotEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.PilotRunId).HasColumnName("pilot_run_id");
        builder.Property(x => x.Mode).HasColumnName("mode").HasMaxLength(32);
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.BlocksPhysicalActions).HasColumnName("blocks_physical_actions");
        builder.Property(x => x.RecommendationsJson).HasColumnName("recommendations_json");
        builder.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(x => x.EndedAtUtc).HasColumnName("ended_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId, x.IsActive });
    }
}

internal sealed class RealFactoryDataQualityRecordConfiguration : IEntityTypeConfiguration<RealFactoryDataQualityRecord>
{
    public void Configure(EntityTypeBuilder<RealFactoryDataQualityRecord> builder)
    {
        builder.ToTable("real_factory_data_quality_records");
        builder.HasKey(x => x.Id);
        RealFactoryPilotEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ConnectorId).HasColumnName("connector_id");
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.OverallState).HasColumnName("overall_state").HasMaxLength(32);
        builder.Property(x => x.AssessmentJson).HasColumnName("assessment_json");
        builder.Property(x => x.BlocksAutonomousAction).HasColumnName("blocks_autonomous_action");
        builder.Property(x => x.AssessedAtUtc).HasColumnName("assessed_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.LineId, x.AssessedAtUtc });
    }
}

internal sealed class RealFactoryShadowAccuracyRecordConfiguration : IEntityTypeConfiguration<RealFactoryShadowAccuracyRecord>
{
    public void Configure(EntityTypeBuilder<RealFactoryShadowAccuracyRecord> builder)
    {
        builder.ToTable("real_factory_shadow_accuracy_records");
        builder.HasKey(x => x.Id);
        RealFactoryPilotEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.PilotRunId).HasColumnName("pilot_run_id");
        builder.Property(x => x.PredictedConstraintStation).HasColumnName("predicted_constraint_station").HasMaxLength(64);
        builder.Property(x => x.ObservedConstraintStation).HasColumnName("observed_constraint_station").HasMaxLength(64);
        builder.Property(x => x.PredictedGoodThroughputPerHour).HasColumnName("predicted_good_throughput_per_hour");
        builder.Property(x => x.ActualGoodThroughputPerHour).HasColumnName("actual_good_throughput_per_hour");
        builder.Property(x => x.PredictedDeltaPercent).HasColumnName("predicted_delta_percent");
        builder.Property(x => x.ActualDeltaPercent).HasColumnName("actual_delta_percent");
        builder.Property(x => x.ConstraintMatched).HasColumnName("constraint_matched");
        builder.Property(x => x.ThroughputWithinTolerance).HasColumnName("throughput_within_tolerance");
        builder.Property(x => x.RecordedAtUtc).HasColumnName("recorded_at_utc");
    }
}

internal sealed class Mp2CalibrationVersionRecordConfiguration : IEntityTypeConfiguration<Mp2CalibrationVersionRecord>
{
    public void Configure(EntityTypeBuilder<Mp2CalibrationVersionRecord> builder)
    {
        builder.ToTable("mp2_calibration_versions");
        builder.HasKey(x => x.Id);
        RealFactoryPilotEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.VersionNumber).HasColumnName("version_number");
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(512);
        builder.Property(x => x.EvidenceJson).HasColumnName("evidence_json");
        builder.Property(x => x.ParametersJson).HasColumnName("parameters_json");
        builder.Property(x => x.EffectiveFromUtc).HasColumnName("effective_from_utc");
        builder.Property(x => x.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.LineId, x.VersionNumber }).IsUnique();
    }
}

internal sealed class RealFactoryPilotEvidencePackRecordConfiguration : IEntityTypeConfiguration<RealFactoryPilotEvidencePackRecord>
{
    public void Configure(EntityTypeBuilder<RealFactoryPilotEvidencePackRecord> builder)
    {
        builder.ToTable("real_factory_pilot_evidence_packs");
        builder.HasKey(x => x.Id);
        RealFactoryPilotEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.PilotRunId).HasColumnName("pilot_run_id");
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.FacilityCode).HasColumnName("facility_code").HasMaxLength(64);
        builder.Property(x => x.LineCode).HasColumnName("line_code").HasMaxLength(64);
        builder.Property(x => x.PilotPeriodStartUtc).HasColumnName("pilot_period_start_utc");
        builder.Property(x => x.PilotPeriodEndUtc).HasColumnName("pilot_period_end_utc");
        builder.Property(x => x.PackJson).HasColumnName("pack_json");
        builder.Property(x => x.VerificationOutcome).HasColumnName("verification_outcome").HasMaxLength(64);
        builder.Property(x => x.KnowledgePromoted).HasColumnName("knowledge_promoted");
        builder.Property(x => x.GeneratedAtUtc).HasColumnName("generated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.PilotRunId }).IsUnique();
    }
}
