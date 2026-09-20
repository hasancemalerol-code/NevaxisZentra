using NevaxisZentra.Persistence.ContextKnowledge;
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
}
