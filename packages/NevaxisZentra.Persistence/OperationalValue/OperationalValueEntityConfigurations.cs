using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.OperationalValue;

internal static class OperationalValueEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IOperationalValueTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class VerifiedOperationalValueRecordConfiguration : IEntityTypeConfiguration<VerifiedOperationalValueRecord>
{
    public void Configure(EntityTypeBuilder<VerifiedOperationalValueRecord> builder)
    {
        builder.ToTable("verified_operational_value_records");
        builder.HasKey(x => x.Id);
        OperationalValueEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.OptimizationRunId).HasColumnName("optimization_run_id");
        builder.Property(x => x.ScenarioRunId).HasColumnName("scenario_recovery_run_id");
        builder.Property(x => x.VerifiedImprovementId).HasColumnName("verified_improvement_id");
        builder.Property(x => x.ValueCategory).HasColumnName("value_category").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.AttributionClass).HasColumnName("attribution_class").HasMaxLength(32).IsRequired();
        builder.Property(x => x.BaselineValue).HasColumnName("baseline_value");
        builder.Property(x => x.ActualValue).HasColumnName("actual_value");
        builder.Property(x => x.DeltaValue).HasColumnName("delta_value");
        builder.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(32).IsRequired();
        builder.Property(x => x.BaselineSource).HasColumnName("baseline_source").HasMaxLength(128).IsRequired();
        builder.Property(x => x.BaselineWindow).HasColumnName("baseline_window").HasMaxLength(128).IsRequired();
        builder.Property(x => x.BaselineMethod).HasColumnName("baseline_method").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ObservationWindowHours).HasColumnName("observation_window_hours");
        builder.Property(x => x.ConfidenceScore).HasColumnName("confidence_score");
        builder.Property(x => x.CalculationMethod).HasColumnName("calculation_method").HasMaxLength(128).IsRequired();
        builder.Property(x => x.FormulaType).HasColumnName("formula_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.FormulaVersion).HasColumnName("formula_version").HasMaxLength(16).IsRequired();
        builder.Property(x => x.InputVersion).HasColumnName("input_version").HasMaxLength(32).IsRequired();
        builder.Property(x => x.EconomicConversionStatus).HasColumnName("economic_conversion_status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.EconomicValue).HasColumnName("economic_value");
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(8);
        builder.Property(x => x.IntelligenceCost).HasColumnName("intelligence_cost");
        builder.Property(x => x.NetValue).HasColumnName("net_value");
        builder.Property(x => x.RoiPercent).HasColumnName("roi_percent");
        builder.Property(x => x.PaybackDays).HasColumnName("payback_days");
        builder.Property(x => x.WorkflowType).HasColumnName("workflow_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.LineCode).HasColumnName("line_code").HasMaxLength(64);
        builder.Property(x => x.ScopeEntityType).HasColumnName("scope_entity_type").HasMaxLength(64);
        builder.Property(x => x.ScopeEntityId).HasColumnName("scope_entity_id").HasMaxLength(128);
        builder.Property(x => x.OverlapGroupKey).HasColumnName("overlap_group_key").HasMaxLength(64);
        builder.Property(x => x.HumanTouchesBefore).HasColumnName("human_touches_before");
        builder.Property(x => x.HumanTouchesAfter).HasColumnName("human_touches_after");
        builder.Property(x => x.EvidenceJson).HasColumnName("evidence_json");
        builder.Property(x => x.ProvenanceJson).HasColumnName("provenance_json");
        builder.Property(x => x.IsInvalidated).HasColumnName("is_invalidated");
        builder.Property(x => x.InvalidationReason).HasColumnName("invalidation_reason").HasMaxLength(256);
        builder.Property(x => x.InvalidatedAtUtc).HasColumnName("invalidated_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DigitalWorkCaseId });
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.Status, x.ValueCategory });
    }
}

public sealed class TenantEconomicInputConfigurationRecordConfiguration
    : IEntityTypeConfiguration<TenantEconomicInputConfigurationRecord>
{
    public void Configure(EntityTypeBuilder<TenantEconomicInputConfigurationRecord> builder)
    {
        builder.ToTable("tenant_economic_input_configurations");
        builder.HasKey(x => x.Id);
        OperationalValueEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.VersionCode).HasColumnName("version_code").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(8).IsRequired();
        builder.Property(x => x.UnitContributionValue).HasColumnName("unit_contribution_value");
        builder.Property(x => x.ScrapUnitCost).HasColumnName("scrap_unit_cost");
        builder.Property(x => x.DowntimeCostPerHour).HasColumnName("downtime_cost_per_hour");
        builder.Property(x => x.LaborCostPerHour).HasColumnName("labor_cost_per_hour");
        builder.Property(x => x.ChangeoverCostPerMinute).HasColumnName("changeover_cost_per_minute");
        builder.Property(x => x.LateDeliveryPenalty).HasColumnName("late_delivery_penalty");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.EffectiveFromUtc).HasColumnName("effective_from_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.VersionCode }).IsUnique();
    }
}

public sealed class OperationalValuePeriodSummaryRecordConfiguration
    : IEntityTypeConfiguration<OperationalValuePeriodSummaryRecord>
{
    public void Configure(EntityTypeBuilder<OperationalValuePeriodSummaryRecord> builder)
    {
        builder.ToTable("operational_value_period_summaries");
        builder.HasKey(x => x.Id);
        OperationalValueEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.PeriodType).HasColumnName("period_type").HasMaxLength(16).IsRequired();
        builder.Property(x => x.PeriodStartUtc).HasColumnName("period_start_utc");
        builder.Property(x => x.PeriodEndUtc).HasColumnName("period_end_utc");
        builder.Property(x => x.VerifiedOutcomeCount).HasColumnName("verified_outcome_count");
        builder.Property(x => x.GoodOutputGainUnits).HasColumnName("good_output_gain_units");
        builder.Property(x => x.DowntimeAvoidedMinutes).HasColumnName("downtime_avoided_minutes");
        builder.Property(x => x.LateOrdersProtected).HasColumnName("late_orders_protected");
        builder.Property(x => x.HumanTouchesReduced).HasColumnName("human_touches_reduced");
        builder.Property(x => x.IntelligenceCost).HasColumnName("intelligence_cost");
        builder.Property(x => x.VerifiedEconomicValue).HasColumnName("verified_economic_value");
        builder.Property(x => x.NetValue).HasColumnName("net_value");
        builder.Property(x => x.SummaryJson).HasColumnName("summary_json");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.PeriodType, x.PeriodStartUtc }).IsUnique();
    }
}
