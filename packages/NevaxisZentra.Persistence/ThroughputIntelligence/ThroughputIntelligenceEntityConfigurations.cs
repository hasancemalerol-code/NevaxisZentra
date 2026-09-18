using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.ThroughputIntelligence;

internal static class ThroughputIntelligenceEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IThroughputIntelligenceTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class LineThroughputTargetConfiguration : IEntityTypeConfiguration<LineThroughputTarget>
{
    public void Configure(EntityTypeBuilder<LineThroughputTarget> builder)
    {
        builder.ToTable("throughput_intelligence_line_targets");
        builder.HasKey(x => x.Id);
        ThroughputIntelligenceEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.TargetGoodUnitsPerHour).HasColumnName("target_good_units_per_hour");
        builder.Property(x => x.RequiredGoodQuantity).HasColumnName("required_good_quantity");
        builder.Property(x => x.AvailableProductionHours).HasColumnName("available_production_hours");
        builder.Property(x => x.TargetCycleSeconds).HasColumnName("target_cycle_seconds");
        builder.Property(x => x.OrderReference).HasColumnName("order_reference").HasMaxLength(64);
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.EffectiveFromUtc).HasColumnName("effective_from_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId, x.IsActive });
    }
}

public sealed class OperatorSkillProfileConfiguration : IEntityTypeConfiguration<OperatorSkillProfile>
{
    public void Configure(EntityTypeBuilder<OperatorSkillProfile> builder)
    {
        builder.ToTable("throughput_intelligence_operator_skills");
        builder.HasKey(x => x.Id);
        ThroughputIntelligenceEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.OperatorCode).HasColumnName("operator_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.SkillLevel).HasColumnName("skill_level");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.OperatorCode }).IsUnique();
    }
}

public sealed class OperatorStationQualificationConfiguration : IEntityTypeConfiguration<OperatorStationQualification>
{
    public void Configure(EntityTypeBuilder<OperatorStationQualification> builder)
    {
        builder.ToTable("throughput_intelligence_operator_qualifications");
        builder.HasKey(x => x.Id);
        ThroughputIntelligenceEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.OperatorCode).HasColumnName("operator_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.StationId).HasColumnName("station_id");
        builder.Property(x => x.OperationCode).HasColumnName("operation_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.QualificationStatus).HasColumnName("qualification_status").HasMaxLength(32);
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.OperatorCode, x.StationId, x.OperationCode }).IsUnique();
    }
}

public sealed class ThroughputScenarioDefinitionConfiguration : IEntityTypeConfiguration<ThroughputScenarioDefinition>
{
    public void Configure(EntityTypeBuilder<ThroughputScenarioDefinition> builder)
    {
        builder.ToTable("throughput_intelligence_scenarios");
        builder.HasKey(x => x.Id);
        ThroughputIntelligenceEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.ScenarioCode).HasColumnName("scenario_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ScenarioType).HasColumnName("scenario_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(256).IsRequired();
        builder.Property(x => x.AnalysisWindow).HasColumnName("analysis_window").HasMaxLength(32);
        builder.Property(x => x.WindowStartUtc).HasColumnName("window_start_utc");
        builder.Property(x => x.WindowEndUtc).HasColumnName("window_end_utc");
        builder.Property(x => x.InterventionsJson).HasColumnName("interventions_json");
        builder.Property(x => x.AssumptionsJson).HasColumnName("assumptions_json");
        builder.Property(x => x.GuardrailsJson).HasColumnName("guardrails_json");
        builder.Property(x => x.RequestedOutputsJson).HasColumnName("requested_outputs_json");
        builder.Property(x => x.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId, x.ScenarioCode }).IsUnique();
    }
}

public sealed class ThroughputScenarioResultConfiguration : IEntityTypeConfiguration<ThroughputScenarioResult>
{
    public void Configure(EntityTypeBuilder<ThroughputScenarioResult> builder)
    {
        builder.ToTable("throughput_intelligence_scenario_results");
        builder.HasKey(x => x.Id);
        ThroughputIntelligenceEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ScenarioDefinitionId).HasColumnName("scenario_definition_id");
        builder.Property(x => x.Eligibility).HasColumnName("eligibility").HasMaxLength(64);
        builder.Property(x => x.DataQuality).HasColumnName("data_quality").HasMaxLength(32);
        builder.Property(x => x.ResultJson).HasColumnName("result_json");
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64);
        builder.Property(x => x.EventsConsidered).HasColumnName("events_considered");
        builder.Property(x => x.StationsEvaluated).HasColumnName("stations_evaluated");
        builder.Property(x => x.AnalysisDurationMs).HasColumnName("analysis_duration_ms");
        builder.Property(x => x.ComputedAtUtc).HasColumnName("computed_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ScenarioDefinitionId });
    }
}
