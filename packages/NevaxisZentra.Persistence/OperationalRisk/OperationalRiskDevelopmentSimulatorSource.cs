namespace NevaxisZentra.Persistence.OperationalRisk;

/// <summary>
/// Development-only simulator for calculator boundary tests. Output is explicitly simulated
/// and must not be represented as real module or customer evidence.
/// </summary>
public sealed class OperationalRiskDevelopmentSimulatorSource : IOperationalRiskEvidenceSource
{
    public const string ScenarioMissingDenominator = "sim-missing-denominator";
    public const string ScenarioSourceConflict = "sim-source-conflict";

    public string SourceModule => OperationalRiskSourceModules.DevelopmentSimulator;

    public Task<OperationalRiskSourceAdapterResult> CollectSnapshotsAsync(
        OperationalRiskOperationContext context,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default)
    {
        if (windowEndUtc < windowStartUtc)
        {
            return Task.FromResult(OperationalRiskSourceAdapterResult.Empty());
        }

        var snapshots = new List<OperationalRiskSourceSnapshot>
        {
            new(
                context.TenantId,
                context.FacilityId,
                SourceModule,
                "development-simulator-assessment",
                ScenarioMissingDenominator,
                OperationalRiskCategories.ProductionContinuity,
                OperationalRiskScopeTypes.Facility,
                null,
                windowEndUtc,
                OperationalRiskEvidenceStates.Simulated,
                "missing-denominator",
                null,
                null,
                null,
                OperationalRiskDetectionConstants.DevelopmentPilotProvenance,
                OperationalRiskDetectionConstants.RuleSetVersion,
                ScenarioMissingDenominator,
                false,
                true),
            new(
                context.TenantId,
                context.FacilityId,
                SourceModule,
                "development-simulator-assessment",
                ScenarioSourceConflict,
                OperationalRiskCategories.Other,
                OperationalRiskScopeTypes.Facility,
                null,
                windowEndUtc,
                OperationalRiskEvidenceStates.SourceConflict,
                "source-conflict",
                null,
                null,
                null,
                OperationalRiskDetectionConstants.DevelopmentPilotProvenance,
                OperationalRiskDetectionConstants.RuleSetVersion,
                ScenarioSourceConflict,
                false,
                true),
        };

        return Task.FromResult(OperationalRiskSourceAdapterResult.Success(snapshots));
    }
}
