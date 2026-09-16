using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public sealed class FactoryPredictiveSignalConfiguration : IEntityTypeConfiguration<FactoryPredictiveSignal>
{
    public void Configure(EntityTypeBuilder<FactoryPredictiveSignal> builder)
    {
        builder.ToTable("factory_predictive_signals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResourceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResourceId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SignalType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ObservationWindow).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Trend).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DataQuality).HasMaxLength(32).IsRequired();
        builder.Property(x => x.EngineVersion).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ResourceId, x.SignalType, x.ObservedAtUtc });
    }
}

public sealed class FactoryOperationalBaselineConfiguration : IEntityTypeConfiguration<FactoryOperationalBaseline>
{
    public void Configure(EntityTypeBuilder<FactoryOperationalBaseline> builder)
    {
        builder.ToTable("factory_operational_baselines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResourceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResourceId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SignalType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Trend).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DataQuality).HasMaxLength(32).IsRequired();
        builder.Property(x => x.BaselineSource).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EngineVersion).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ResourceId, x.SignalType }).IsUnique();
    }
}

public sealed class FactoryPredictedRiskConfiguration : IEntityTypeConfiguration<FactoryPredictedRisk>
{
    public void Configure(EntityTypeBuilder<FactoryPredictedRisk> builder)
    {
        builder.ToTable("factory_predicted_risks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RiskType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RiskState).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AffectedOutcome).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ConfidenceBand).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Horizon).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(32).IsRequired();
        builder.Property(x => x.TimeToImpactLabel).HasMaxLength(64);
        builder.Property(x => x.OrderReference).HasMaxLength(64);
        builder.Property(x => x.MachineCode).HasMaxLength(64);
        builder.Property(x => x.MaterialCode).HasMaxLength(64);
        builder.Property(x => x.DataQuality).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DataClassification).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CorrelationKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EngineVersion).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.IsActive, x.RiskState });
    }
}

public sealed class FactoryPredictionEvaluationConfiguration : IEntityTypeConfiguration<FactoryPredictionEvaluation>
{
    public void Configure(EntityTypeBuilder<FactoryPredictionEvaluation> builder)
    {
        builder.ToTable("factory_prediction_evaluations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EvaluationOutcome).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Horizon).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ConfidenceBand).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.PredictedRiskId });
    }
}
