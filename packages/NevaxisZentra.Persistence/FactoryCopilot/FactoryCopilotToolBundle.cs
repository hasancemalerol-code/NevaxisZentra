using NevaxisZentra.Persistence.ContextKnowledge;
using NevaxisZentra.Persistence.ContinuousOptimization;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.ScenarioRecovery;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.FactoryCopilot;

internal sealed record FactoryCopilotToolBundle
{
    public CanonicalOperationalStateResult? LineState { get; init; }
    public ThroughputIntelligenceOverview? ThroughputOverview { get; init; }
    public ThroughputIntelligenceOverview? PriorShiftOverview { get; init; }
    public IReadOnlyList<ScenarioComparisonRow>? ScenarioComparison { get; init; }
    public ScenarioRecoveryWorkspace? ScenarioWorkspace { get; init; }
    public ScenarioMetricSnapshot? SimulationSnapshot { get; init; }
    public WorksCaseDetailSnapshot? WorkDetail { get; init; }
    public IReadOnlyList<FactoryKnowledgeRecordSummary>? VerifiedKnowledge { get; init; }
    public IReadOnlyList<string> ExecutiveSignals { get; init; } = [];
    public IReadOnlyList<string> ShiftSignals { get; init; } = [];
    public IReadOnlyList<string> RecoveryOptions { get; init; } = [];
    public IReadOnlyList<string> KnownEntityTokens { get; init; } = [];
    public FactoryCopilotMachinePerformance? MachinePerformance { get; init; }
    public ContinuousOptimizationRunSummary? OptimizationRun { get; init; }
    public ContinuousOptimizationExplainability? OptimizationExplainability { get; init; }
}

internal sealed record FactoryCopilotMachineStopReason(string Reason, int Count, decimal Minutes);

/// <summary>
/// One machine over the available period, split by the factory calendar: scheduled production time, planned breaks
/// (not a loss), outside-shift time (not counted) and unplanned downtime (stops inside scheduled production time).
/// </summary>
internal sealed record FactoryCopilotMachineSummary(
    string ResourceCode,
    string ResourceName,
    decimal ScheduledUnits,
    decimal PlannedBreakUnits,
    decimal OutsideShiftUnits,
    decimal UnclassifiedUnits,
    int UnplannedStops,
    decimal UnplannedStopMinutes,
    decimal PlannedBreakStopMinutes,
    decimal OutsideShiftStopMinutes,
    int UnclassifiedStopCount,
    decimal UnclassifiedStopMinutes,
    IReadOnlyList<FactoryCopilotMachineStopReason> TopUnplannedReasons,
    decimal? AvailabilityPercent,
    decimal? UnitsPerScheduledHour);

/// <summary>Read-only per-machine aggregate over the whole available event period of one line.</summary>
internal sealed record FactoryCopilotMachinePerformance(
    DateTimeOffset PeriodFromUtc,
    DateTimeOffset PeriodToUtc,
    int EventCount,
    bool CalendarClassified,
    int WorkingDays,
    int ScheduledMinutesPerMachine,
    IReadOnlyList<FactoryCopilotMachineSummary> Machines);
