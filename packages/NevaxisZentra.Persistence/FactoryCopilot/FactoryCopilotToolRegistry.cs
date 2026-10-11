namespace NevaxisZentra.Persistence.FactoryCopilot;

public sealed record FactoryCopilotToolDefinition(
    string ToolId,
    string AccessClass,
    string Description,
    IReadOnlyList<string> AllowedIntents);

public static class FactoryCopilotToolRegistry
{
    private static readonly IReadOnlyDictionary<string, FactoryCopilotToolDefinition> Tools =
        new Dictionary<string, FactoryCopilotToolDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [FactoryCopilotToolIds.GetMachinePerformance] = new(
                FactoryCopilotToolIds.GetMachinePerformance,
                FactoryCopilotToolAccessClasses.Analytic,
                "Per-machine produced units, stops and top stop reasons over the available period (read-only aggregate)",
                [FactoryCopilotIntents.GeneralQuestion]),
            [FactoryCopilotToolIds.GetLineState] = new(
                FactoryCopilotToolIds.GetLineState,
                FactoryCopilotToolAccessClasses.Read,
                "Canonical line operational state",
                [FactoryCopilotIntents.CurrentState, FactoryCopilotIntents.CauseAnalysis]),
            [FactoryCopilotToolIds.GetThroughputOverview] = new(
                FactoryCopilotToolIds.GetThroughputOverview,
                FactoryCopilotToolAccessClasses.Analytic,
                "MP2 throughput overview, constraint and loss tree",
                [
                    FactoryCopilotIntents.CurrentState,
                    FactoryCopilotIntents.CauseAnalysis,
                    FactoryCopilotIntents.TrendComparison,
                    FactoryCopilotIntents.ConstraintAnalysis,
                    FactoryCopilotIntents.ExecutiveSummary,
                ]),
            [FactoryCopilotToolIds.GetCurrentConstraint] = new(
                FactoryCopilotToolIds.GetCurrentConstraint,
                FactoryCopilotToolAccessClasses.Analytic,
                "Current throughput constraint",
                [FactoryCopilotIntents.CauseAnalysis, FactoryCopilotIntents.ConstraintAnalysis]),
            [FactoryCopilotToolIds.GetLossTree] = new(
                FactoryCopilotToolIds.GetLossTree,
                FactoryCopilotToolAccessClasses.Analytic,
                "Throughput loss tree",
                [FactoryCopilotIntents.CauseAnalysis]),
            [FactoryCopilotToolIds.GetScenarioComparison] = new(
                FactoryCopilotToolIds.GetScenarioComparison,
                FactoryCopilotToolAccessClasses.Read,
                "Scenario recovery comparison facts",
                [
                    FactoryCopilotIntents.ScenarioExplanation,
                    FactoryCopilotIntents.RecoveryOptions,
                ]),
            [FactoryCopilotToolIds.GetWorkDetail] = new(
                FactoryCopilotToolIds.GetWorkDetail,
                FactoryCopilotToolAccessClasses.Read,
                "ULTRA E work projection",
                [FactoryCopilotIntents.WorkStatus, FactoryCopilotIntents.VerificationStatus]),
            [FactoryCopilotToolIds.GetVerifiedKnowledge] = new(
                FactoryCopilotToolIds.GetVerifiedKnowledge,
                FactoryCopilotToolAccessClasses.Read,
                "Verified factory knowledge retrieval",
                [
                    FactoryCopilotIntents.SimilarCases,
                    FactoryCopilotIntents.KnowledgeLookup,
                    FactoryCopilotIntents.MaintenanceRisk,
                    FactoryCopilotIntents.QualityRisk,
                    FactoryCopilotIntents.MaterialRisk,
                ]),
            [FactoryCopilotToolIds.GetRecoveryOptions] = new(
                FactoryCopilotToolIds.GetRecoveryOptions,
                FactoryCopilotToolAccessClasses.Read,
                "Registered recovery/scenario path",
                [
                    FactoryCopilotIntents.RecoveryOptions,
                    FactoryCopilotIntents.OrderRisk,
                    FactoryCopilotIntents.ScheduleExplanation,
                ]),
            [FactoryCopilotToolIds.RunScenarioSimulation] = new(
                FactoryCopilotToolIds.RunScenarioSimulation,
                FactoryCopilotToolAccessClasses.Simulation,
                "Throughput scenario simulation",
                [FactoryCopilotIntents.RecoveryOptions]),
            [FactoryCopilotToolIds.GetVerificationResult] = new(
                FactoryCopilotToolIds.GetVerificationResult,
                FactoryCopilotToolAccessClasses.Read,
                "Verified intervention outcome",
                [FactoryCopilotIntents.VerificationStatus]),
            [FactoryCopilotToolIds.GetShiftContext] = new(
                FactoryCopilotToolIds.GetShiftContext,
                FactoryCopilotToolAccessClasses.Read,
                "Shift handover context",
                [FactoryCopilotIntents.ShiftSummary, FactoryCopilotIntents.TrendComparison]),
            [FactoryCopilotToolIds.GetExecutiveSignals] = new(
                FactoryCopilotToolIds.GetExecutiveSignals,
                FactoryCopilotToolAccessClasses.Analytic,
                "High-value operational signals",
                [FactoryCopilotIntents.ExecutiveSummary]),
            [FactoryCopilotToolIds.GetOptimizationRun] = new(
                FactoryCopilotToolIds.GetOptimizationRun,
                FactoryCopilotToolAccessClasses.Read,
                "Continuous optimization run explainability",
                [FactoryCopilotIntents.OptimizationExplanation]),
        };

    public static FactoryCopilotToolDefinition Require(string toolId, string intent)
    {
        if (!Tools.TryGetValue(toolId, out var definition))
        {
            throw new FactoryCopilotException(
                FactoryCopilotFailureCodes.ToolUnauthorized,
                $"Unknown copilot tool: {toolId}");
        }

        // Free-form factory questions may use any read-only or analytic tool, never a simulation tool.
        var generalQuestionAllowed = intent == FactoryCopilotIntents.GeneralQuestion
            && definition.AccessClass != FactoryCopilotToolAccessClasses.Simulation;
        if (!generalQuestionAllowed
            && !definition.AllowedIntents.Contains(intent, StringComparer.OrdinalIgnoreCase))
        {
            throw new FactoryCopilotException(
                FactoryCopilotFailureCodes.ToolUnauthorized,
                $"Tool {toolId} is not authorized for intent {intent}.");
        }

        return definition;
    }

    public static IReadOnlyList<FactoryCopilotToolDefinition> List() => Tools.Values.ToList();
}

public sealed class FactoryCopilotException : Exception
{
    public FactoryCopilotException(string failureCode, string message) : base(message)
    {
        FailureCode = failureCode;
    }

    public string FailureCode { get; }
}
