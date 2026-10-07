using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.FactoryIntelligence;

/// <summary>
/// The Factory Intelligence tables were created by a hand-written migration with snake_case columns, but the
/// entity configurations never mapped the property names, so EF queried PascalCase columns that do not exist
/// (e.g. <c>column f.Id does not exist</c>). Every property of these entities maps to its snake_case column.
/// </summary>
public static class FactoryIntelligenceColumnNames
{
    public static string ToSnakeCase(string propertyName)
    {
        var builder = new System.Text.StringBuilder(propertyName.Length + 6);
        for (var index = 0; index < propertyName.Length; index++)
        {
            var current = propertyName[index];
            if (char.IsUpper(current) && index > 0
                && (char.IsLower(propertyName[index - 1]) || char.IsDigit(propertyName[index - 1])))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    public static void MapSnakeCaseColumns<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetColumnName(ToSnakeCase(property.Name));
        }
    }
}

public sealed class FactoryPredictiveSignalConfiguration : IEntityTypeConfiguration<FactoryPredictiveSignal>
{
    public void Configure(EntityTypeBuilder<FactoryPredictiveSignal> builder)
    {
        builder.ToTable("factory_predictive_signals");
        FactoryIntelligenceColumnNames.MapSnakeCaseColumns(builder);
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
        FactoryIntelligenceColumnNames.MapSnakeCaseColumns(builder);
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
        FactoryIntelligenceColumnNames.MapSnakeCaseColumns(builder);
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
        FactoryIntelligenceColumnNames.MapSnakeCaseColumns(builder);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EvaluationOutcome).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Horizon).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ConfidenceBand).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.PredictedRiskId });
    }
}
