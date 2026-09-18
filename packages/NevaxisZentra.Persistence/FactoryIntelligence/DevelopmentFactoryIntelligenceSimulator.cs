namespace NevaxisZentra.Persistence.FactoryIntelligence;

public static class DevelopmentFactoryIntelligenceSimulator
{
    public const string MachineCode = "INJ-006";
    public const string OrderReference = "ORD-8841";
    public const string MaterialCode = "PP-GRANULE";
    public const decimal CycleReferenceSec = 27.5m;

    public static readonly decimal[] CycleDriftSequence = [28.1m, 28.5m, 29.0m, 29.8m, 30.7m];

    public static IReadOnlyList<FactorySignalWindowInput> BuildWindow(
        int step,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset observedAtUtc,
        string scenario = "gradual-degradation")
    {
        if (scenario == "false-alert" && step >= 6)
        {
            return BuildStableWindow(tenantId, facilityId, observedAtUtc);
        }

        if (step <= 0)
        {
            return BuildStableWindow(tenantId, facilityId, observedAtUtc);
        }

        var index = Math.Min(step - 1, CycleDriftSequence.Length - 1);
        var cycle = scenario == "false-alert" && step >= 5
            ? 28.0m
            : CycleDriftSequence[index];
        var microStops = 1m + step * 1.4m;
        var materialCoverage = Math.Max(2.5m, 12m - step * 1.8m);
        var maintenance = step >= 3 ? 1m + (step - 2) * 0.6m : 0.4m;
        var reject = step >= 4 ? 1.2m + (step - 3) * 0.8m : 0.8m;

        return
        [
            Signal(tenantId, facilityId, FactoryIntelligenceSignalTypes.CycleTimeDrift, cycle, observedAtUtc),
            Signal(tenantId, facilityId, FactoryIntelligenceSignalTypes.MicroStopFrequency, microStops, observedAtUtc),
            Signal(tenantId, facilityId, FactoryIntelligenceSignalTypes.MaterialCoverageHours, materialCoverage, observedAtUtc),
            Signal(tenantId, facilityId, FactoryIntelligenceSignalTypes.MaintenanceWarning, maintenance, observedAtUtc),
            Signal(tenantId, facilityId, FactoryIntelligenceSignalTypes.ScrapRejectTrend, reject, observedAtUtc),
        ];
    }

    public static IReadOnlyList<FactorySignalWindowInput> BuildStaleWindow(
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset observedAtUtc) =>
    [
        new FactorySignalWindowInput(
            tenantId,
            facilityId,
            "machine",
            MachineCode,
            FactoryIntelligenceSignalTypes.CycleTimeDrift,
            30.5m,
            observedAtUtc,
            FactoryIntelligenceDataQualityStates.Stale),
    ];

    public static IReadOnlyList<FactorySignalWindowInput> BuildInsufficientTelemetryWindow(
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset observedAtUtc) =>
    [
        new FactorySignalWindowInput(
            tenantId,
            facilityId,
            "material",
            MaterialCode,
            FactoryIntelligenceSignalTypes.MaterialCoverageHours,
            4.2m,
            observedAtUtc,
            FactoryIntelligenceDataQualityStates.Partial),
    ];

    private static IReadOnlyList<FactorySignalWindowInput> BuildStableWindow(
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset observedAtUtc) =>
    [
        Signal(tenantId, facilityId, FactoryIntelligenceSignalTypes.CycleTimeDrift, 27.8m, observedAtUtc),
        Signal(tenantId, facilityId, FactoryIntelligenceSignalTypes.MicroStopFrequency, 1m, observedAtUtc),
        Signal(tenantId, facilityId, FactoryIntelligenceSignalTypes.MaterialCoverageHours, 12m, observedAtUtc),
    ];

    private static FactorySignalWindowInput Signal(
        Guid tenantId,
        Guid facilityId,
        string signalType,
        decimal value,
        DateTimeOffset observedAtUtc) =>
        new(
            tenantId,
            facilityId,
            signalType == FactoryIntelligenceSignalTypes.MaterialCoverageHours ? "material" : "machine",
            signalType == FactoryIntelligenceSignalTypes.MaterialCoverageHours ? MaterialCode : MachineCode,
            signalType,
            value,
            observedAtUtc,
            FactoryIntelligenceDataQualityStates.Good,
            [$"inj006-sim:{signalType}:{observedAtUtc:O}"]);
}
