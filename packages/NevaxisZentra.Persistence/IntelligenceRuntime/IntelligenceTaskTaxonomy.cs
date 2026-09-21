using NevaxisZentra.Persistence.ModelGateway;

namespace NevaxisZentra.Persistence.IntelligenceRuntime;

public static class IntelligenceTaskTaxonomy
{
    private static readonly IReadOnlyDictionary<string, IntelligenceTaskDefinition> Tasks =
        new Dictionary<string, IntelligenceTaskDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [IntelligenceTaskTypes.StateLookup] = new(
                IntelligenceTaskTypes.StateLookup,
                IntelligenceRouteTypes.Deterministic,
                IntelligenceRouteTypes.Deterministic,
                "LOW", false, false, false, "LOW"),
            [IntelligenceTaskTypes.ThresholdEvaluation] = new(
                IntelligenceTaskTypes.ThresholdEvaluation,
                IntelligenceRouteTypes.Deterministic,
                IntelligenceRouteTypes.Analytics,
                "LOW", false, false, false, "LOW"),
            [IntelligenceTaskTypes.TrendAnalysis] = new(
                IntelligenceTaskTypes.TrendAnalysis,
                IntelligenceRouteTypes.Analytics,
                IntelligenceRouteTypes.Deterministic,
                "LOW", false, false, false, "LOW"),
            [IntelligenceTaskTypes.AnomalyAnalysis] = new(
                IntelligenceTaskTypes.AnomalyAnalysis,
                IntelligenceRouteTypes.Analytics,
                IntelligenceRouteTypes.Deterministic,
                "MEDIUM", false, true, false, "MEDIUM"),
            [IntelligenceTaskTypes.Classification] = new(
                IntelligenceTaskTypes.Classification,
                IntelligenceRouteTypes.NevaxisModel,
                IntelligenceRouteTypes.FrontierModel,
                "MEDIUM", true, true, false, "MEDIUM"),
            [IntelligenceTaskTypes.Extraction] = new(
                IntelligenceTaskTypes.Extraction,
                IntelligenceRouteTypes.NevaxisModel,
                IntelligenceRouteTypes.FrontierModel,
                "MEDIUM", true, true, false, "MEDIUM"),
            [IntelligenceTaskTypes.Summarization] = new(
                IntelligenceTaskTypes.Summarization,
                IntelligenceRouteTypes.NevaxisModel,
                IntelligenceRouteTypes.FrontierModel,
                "MEDIUM", true, true, false, "MEDIUM"),
            [IntelligenceTaskTypes.CaseSimilarity] = new(
                IntelligenceTaskTypes.CaseSimilarity,
                IntelligenceRouteTypes.Retrieval,
                IntelligenceRouteTypes.NevaxisModel,
                "LOW", false, true, false, "LOW"),
            [IntelligenceTaskTypes.RootCauseCandidate] = new(
                IntelligenceTaskTypes.RootCauseCandidate,
                IntelligenceRouteTypes.NevaxisModel,
                IntelligenceRouteTypes.FrontierModel,
                "MEDIUM", true, true, false, "MEDIUM"),
            [IntelligenceTaskTypes.CrossDomainDiagnosis] = new(
                IntelligenceTaskTypes.CrossDomainDiagnosis,
                IntelligenceRouteTypes.FrontierModel,
                IntelligenceRouteTypes.Deterministic,
                "HIGH", true, false, true, "HIGH"),
            [IntelligenceTaskTypes.RecoveryPlanDraft] = new(
                IntelligenceTaskTypes.RecoveryPlanDraft,
                IntelligenceRouteTypes.Solver,
                IntelligenceRouteTypes.FrontierModel,
                "MEDIUM", true, true, false, "MEDIUM"),
            [IntelligenceTaskTypes.ScheduleExplanation] = new(
                IntelligenceTaskTypes.ScheduleExplanation,
                IntelligenceRouteTypes.Deterministic,
                IntelligenceRouteTypes.Analytics,
                "LOW", false, false, false, "LOW"),
            [IntelligenceTaskTypes.ScenarioExplanation] = new(
                IntelligenceTaskTypes.ScenarioExplanation,
                IntelligenceRouteTypes.Deterministic,
                IntelligenceRouteTypes.Retrieval,
                "LOW", false, false, false, "LOW"),
            [IntelligenceTaskTypes.ExecutiveSynthesis] = new(
                IntelligenceTaskTypes.ExecutiveSynthesis,
                IntelligenceRouteTypes.Analytics,
                IntelligenceRouteTypes.FrontierModel,
                "MEDIUM", true, true, false, "MEDIUM"),
            [IntelligenceTaskTypes.WorkExplanation] = new(
                IntelligenceTaskTypes.WorkExplanation,
                IntelligenceRouteTypes.Deterministic,
                IntelligenceRouteTypes.Retrieval,
                "LOW", false, false, false, "LOW"),
            [IntelligenceTaskTypes.VerificationExplanation] = new(
                IntelligenceTaskTypes.VerificationExplanation,
                IntelligenceRouteTypes.Deterministic,
                IntelligenceRouteTypes.Analytics,
                "LOW", false, false, false, "LOW"),
            [IntelligenceTaskTypes.FactoryCopilotSynthesis] = new(
                IntelligenceTaskTypes.FactoryCopilotSynthesis,
                IntelligenceRouteTypes.FrontierModel,
                IntelligenceRouteTypes.Deterministic,
                "MEDIUM", true, true, true, "MEDIUM"),
        };

    public static IntelligenceTaskDefinition Require(string taskType) =>
        Tasks.TryGetValue(taskType, out var definition)
            ? definition
            : throw new IntelligenceRuntimeException(
                IntelligenceRuntimeFailureStates.EvaluationFailed,
                $"Unknown intelligence task type: {taskType}");

    public static IntelligenceTaskDefinition Resolve(string taskType, string requestClassification)
    {
        if (Tasks.TryGetValue(taskType, out var definition))
        {
            return definition;
        }

        return requestClassification switch
        {
            ModelGatewayRequestClassifications.ThroughputCalculation
                or ModelGatewayRequestClassifications.TaktCalculation
                or ModelGatewayRequestClassifications.ConstraintDetection
                or ModelGatewayRequestClassifications.LossTree
                or ModelGatewayRequestClassifications.VerificationMath => Require(IntelligenceTaskTypes.StateLookup),
            ModelGatewayRequestClassifications.ScenarioSimulation => Require(IntelligenceTaskTypes.RecoveryPlanDraft),
            ModelGatewayRequestClassifications.OperationalExplanation => Require(IntelligenceTaskTypes.WorkExplanation),
            ModelGatewayRequestClassifications.HypothesisGeneration => Require(IntelligenceTaskTypes.RootCauseCandidate),
            ModelGatewayRequestClassifications.RecoveryPlanReasoning => Require(IntelligenceTaskTypes.RecoveryPlanDraft),
            ModelGatewayRequestClassifications.CrossModuleDiagnosis => Require(IntelligenceTaskTypes.CrossDomainDiagnosis),
            _ => Require(IntelligenceTaskTypes.StateLookup),
        };
    }

    public static IReadOnlyList<IntelligenceTaskDefinition> List() => Tasks.Values.ToList();
}

public sealed class IntelligenceRuntimeException : Exception
{
    public IntelligenceRuntimeException(string failureState, string message) : base(message) =>
        FailureState = failureState;

    public string FailureState { get; }
}
