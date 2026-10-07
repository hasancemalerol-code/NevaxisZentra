using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NevaxisZentra.Persistence.FactoryIntelligence;

namespace NevaxisZentra.Persistence.Tests;

public sealed class FactoryIntelligenceColumnMappingTests
{
    // Column lists of the packaged schema (created by migration 20260916150000_AddFactoryIntelligenceFoundation).
    public static TheoryData<Type, string, string> Tables => new()
    {
        { typeof(FactoryPredictiveSignal), "factory_predictive_signals", "id,tenant_id,facility_id,resource_type,resource_id,signal_type,observation_window,measured_value,baseline_value,deviation_percent,trend,data_quality,source_references_json,engine_version,observed_at_utc,created_at_utc" },
        { typeof(FactoryOperationalBaseline), "factory_operational_baselines", "id,tenant_id,facility_id,resource_type,resource_id,signal_type,reference_value,current_value,deviation_percent,trend,data_quality,baseline_source,engine_version,updated_at_utc" },
        { typeof(FactoryPredictedRisk), "factory_predicted_risks", "id,tenant_id,facility_id,risk_type,risk_state,affected_outcome,confidence_band,horizon,severity,time_to_impact_label,projected_gap_quantity,order_reference,machine_code,material_code,contributing_signals_json,explanation_json,assumptions_json,data_quality,data_classification,correlation_key,idempotency_key,linked_case_id,engine_version,generated_at_utc,updated_at_utc,resolved_at_utc,is_active" },
        { typeof(FactoryPredictionEvaluation), "factory_prediction_evaluations", "id,tenant_id,facility_id,predicted_risk_id,evaluation_outcome,predicted_outcome,actual_outcome,horizon,confidence_band,intervention_occurred,notes,evaluated_at_utc" },
    };

    [Theory]
    [MemberData(nameof(Tables))]
    public void EntityColumnsMatchTheSnakeCaseSchema(Type entity, string table, string expectedColumns)
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(entity)!;
        Assert.Equal(table, entityType.GetTableName());

        var store = StoreObjectIdentifier.Table(table, entityType.GetSchema());
        var actual = entityType.GetProperties().Select(property => property.GetColumnName(store)).OrderBy(name => name, StringComparer.Ordinal);
        Assert.Equal(expectedColumns.Split(',').OrderBy(name => name, StringComparer.Ordinal), actual);
    }

    [Theory]
    [InlineData("Id", "id")]
    [InlineData("TenantId", "tenant_id")]
    [InlineData("IsActive", "is_active")]
    [InlineData("ObservedAtUtc", "observed_at_utc")]
    [InlineData("ContributingSignalsJson", "contributing_signals_json")]
    public void SnakeCaseConversionMatchesTheSchemaConvention(string property, string column)
    {
        Assert.Equal(column, FactoryIntelligenceColumnNames.ToSnakeCase(property));
    }

    private static NevaxisZentraDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisZentraDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only",
                npgsql => npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisZentraDbContext(options);
    }
}
