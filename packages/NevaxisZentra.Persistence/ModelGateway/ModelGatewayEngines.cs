using System.Diagnostics;
using System.Text.Json;

namespace NevaxisZentra.Persistence.ModelGateway;

public interface IModelProvider
{
    string ProviderId { get; }
    string IntelligencePath { get; }
    bool IsAvailable { get; }
    Task<ModelProviderInvokeResult> InvokeAsync(ModelProviderInvokeRequest request, CancellationToken cancellationToken);
}

public sealed record ModelProviderInvokeRequest(
    ModelGatewayRequest GatewayRequest,
    ModelGatewayRoutingDecision Routing);

public sealed record ModelProviderInvokeResult(
    bool Succeeded,
    string? FailureState,
    OperationalProposal? Proposal,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost,
    string ResponseClassification);

public static class ModelRouterEngine
{
    public static ModelGatewayRoutingDecision Route(ModelGatewayRequest request, ModelCostBudgetState? budget)
    {
        if (budget?.IsLimitReached == true)
        {
            return DeterministicRoute("cost-budget-limit-reached");
        }

        if (!string.IsNullOrWhiteSpace(request.PreferredPath) &&
            string.Equals(request.PreferredPath, ModelGatewayIntelligencePaths.DeterministicEngine, StringComparison.OrdinalIgnoreCase))
        {
            return DeterministicRoute("explicit-deterministic-request");
        }

        if (IsDeterministicOnly(request.RequestClassification))
        {
            return DeterministicRoute($"deterministic-capability:{request.RequestClassification}");
        }

        if (request.Context.DataQualityState is "CONFLICTED_STATE" or "INSUFFICIENT_CONTEXT")
        {
            return DeterministicRoute("insufficient-trusted-context");
        }

        if (request.ActionRiskLevel >= 4)
        {
            return DeterministicRoute("high-risk-action-requires-deterministic-path");
        }

        if (request.RequiresStructuredOutput && IsComplexReasoning(request.RequestClassification))
        {
            if (request.CostBudgetRemaining is < 0.05m)
            {
                return DeterministicRoute("cost-budget-fallback-to-deterministic");
            }

            return new ModelGatewayRoutingDecision(
                ModelGatewayIntelligencePaths.FrontierModel,
                ModelGatewayProviderIds.FrontierModel,
                "frontier-reasoning-v1",
                "1.0",
                $"complex-reasoning:{request.RequestClassification}",
                false);
        }

        if (IsComplexReasoning(request.RequestClassification))
        {
            return new ModelGatewayRoutingDecision(
                ModelGatewayIntelligencePaths.LocalModel,
                ModelGatewayProviderIds.LocalModel,
                "local-reasoning-v1",
                "1.0",
                $"local-eligible:{request.RequestClassification}",
                false);
        }

        return DeterministicRoute($"default-deterministic:{request.RequestClassification}");
    }

    public static ModelGatewayRoutingDecision Fallback(
        ModelGatewayRoutingDecision original,
        string reason) =>
        original.IntelligencePath switch
        {
            ModelGatewayIntelligencePaths.FrontierModel => new ModelGatewayRoutingDecision(
                ModelGatewayIntelligencePaths.LocalModel,
                ModelGatewayProviderIds.LocalModel,
                "local-reasoning-v1",
                "1.0",
                reason,
                true),
            _ => DeterministicRoute(reason),
        };

    private static bool IsDeterministicOnly(string classification) =>
        classification is ModelGatewayRequestClassifications.ThroughputCalculation
            or ModelGatewayRequestClassifications.TaktCalculation
            or ModelGatewayRequestClassifications.ConstraintDetection
            or ModelGatewayRequestClassifications.LossTree
            or ModelGatewayRequestClassifications.ScenarioSimulation
            or ModelGatewayRequestClassifications.PolicyEvaluation
            or ModelGatewayRequestClassifications.SafetyGuard
            or ModelGatewayRequestClassifications.VerificationMath;

    private static bool IsComplexReasoning(string classification) =>
        classification is ModelGatewayRequestClassifications.OperationalExplanation
            or ModelGatewayRequestClassifications.HypothesisGeneration
            or ModelGatewayRequestClassifications.RecoveryPlanReasoning
            or ModelGatewayRequestClassifications.CrossModuleDiagnosis;

    private static ModelGatewayRoutingDecision DeterministicRoute(string reason) =>
        new(
            ModelGatewayIntelligencePaths.DeterministicEngine,
            ModelGatewayProviderIds.DeterministicEngine,
            "deterministic-engine-v1",
            "1.0",
            reason,
            false);
}

public static class OperationalProposalValidator
{
    private static readonly string[] InjectionMarkers =
    [
        "ignore previous instructions",
        "bypass approval",
        "plc.write",
        "machine.command",
        "safety.override",
        "execute arbitrary",
    ];

    public static ModelGatewayProposalValidationResult Validate(OperationalProposal? proposal)
    {
        if (proposal is null)
        {
            return Reject("proposal-null");
        }

        var rejections = new List<string>();
        if (string.IsNullOrWhiteSpace(proposal.Hypothesis))
        {
            rejections.Add("hypothesis-required");
        }

        if (proposal.EvidenceRefs.Count == 0)
        {
            rejections.Add("evidence-refs-required");
        }

        if (proposal.ProposedActions.Count == 0)
        {
            rejections.Add("proposed-actions-required");
        }

        foreach (var action in proposal.ProposedActions)
        {
            if (!ModelGatewayAllowedActionTypes.All.Contains(action.ActionType))
            {
                rejections.Add($"unauthorized-action-type:{action.ActionType}");
            }

            if (string.IsNullOrWhiteSpace(action.TargetEntityId))
            {
                rejections.Add("target-entity-required");
            }

            if (ContainsInjection(action.Summary))
            {
                rejections.Add("injection-detected");
            }
        }

        if (ContainsInjection(proposal.Hypothesis) || proposal.Assumptions.Any(ContainsInjection))
        {
            rejections.Add("injection-detected");
        }

        if (rejections.Count > 0)
        {
            return new ModelGatewayProposalValidationResult(false, "REJECTED", rejections, null);
        }

        return new ModelGatewayProposalValidationResult(true, "ACCEPTED", [], proposal);
    }

    private static bool ContainsInjection(string value) =>
        InjectionMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static ModelGatewayProposalValidationResult Reject(string reason) =>
        new(false, "REJECTED", [reason], null);
}

public static class ModelCostGovernanceEngine
{
    public static ModelCostBudgetState Evaluate(
        decimal dailySpend,
        decimal monthlySpend,
        decimal caseSpend,
        ModelGatewayBudgetPolicyRecord? policy)
    {
        var dailyBudget = policy?.DailyBudget ?? 50m;
        var monthlyBudget = policy?.MonthlyBudget ?? 500m;
        var caseBudget = policy?.CaseBudget ?? 5m;

        return new ModelCostBudgetState(
            Math.Max(0, dailyBudget - dailySpend),
            Math.Max(0, monthlyBudget - monthlySpend),
            Math.Max(0, caseBudget - caseSpend),
            dailySpend >= dailyBudget || monthlySpend >= monthlyBudget || caseSpend >= caseBudget);
    }

    public static bool IsRequestClassRestricted(string requestClass, ModelGatewayBudgetPolicyRecord? policy)
    {
        if (policy is null || string.IsNullOrWhiteSpace(policy.RestrictedRequestClassesJson))
        {
            return false;
        }

        var restricted = JsonSerializer.Deserialize<List<string>>(policy.RestrictedRequestClassesJson) ?? [];
        return restricted.Contains(requestClass, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class DeterministicEngineProvider : IModelProvider
{
    public string ProviderId => ModelGatewayProviderIds.DeterministicEngine;
    public string IntelligencePath => ModelGatewayIntelligencePaths.DeterministicEngine;
    public bool IsAvailable => true;

    public Task<ModelProviderInvokeResult> InvokeAsync(
        ModelProviderInvokeRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ctx = request.GatewayRequest.Context;
        var proposal = new OperationalProposal(
            Hypothesis: $"Deterministic analysis for {request.GatewayRequest.RequestClassification}",
            EvidenceRefs: ["deterministic-engine", ctx.ConstraintSummary ?? "constraint-unknown"],
            ProposedActions:
            [
                new OperationalProposalAction(
                    "READ_ONLY_ANALYSIS",
                    "line",
                    "throughput",
                    "Deterministic throughput diagnosis completed.",
                    ["mp2-constraint", "canonical-state"]),
            ],
            ExpectedOutcome: "Structured deterministic recommendation without model inference.",
            Assumptions: ["canonical-state-trusted", "mp2-data-quality-acceptable"],
            Confidence: 0.95m,
            Risks: [],
            RequiredApprovals: []);

        return Task.FromResult(new ModelProviderInvokeResult(
            true,
            null,
            proposal,
            0,
            0,
            0m,
            "deterministic-proposal"));
    }
}

public sealed class LocalModelProvider : IModelProvider
{
    public string ProviderId => ModelGatewayProviderIds.LocalModel;
    public string IntelligencePath => ModelGatewayIntelligencePaths.LocalModel;
    public bool IsAvailable => false;

    public Task<ModelProviderInvokeResult> InvokeAsync(
        ModelProviderInvokeRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ModelProviderInvokeResult(
            false,
            ModelGatewayFailureStates.ProviderUnavailable,
            null,
            0,
            0,
            0m,
            "unavailable"));
}

public sealed class FrontierModelProvider : IModelProvider
{
    private readonly DeterministicEngineProvider deterministicFallback = new();

    public string ProviderId => ModelGatewayProviderIds.FrontierModel;
    public string IntelligencePath => ModelGatewayIntelligencePaths.FrontierModel;
    public bool IsAvailable => true;

    public async Task<ModelProviderInvokeResult> InvokeAsync(
        ModelProviderInvokeRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.GatewayRequest.Context.DataQualityState == "INSUFFICIENT_CONTEXT")
        {
            return new ModelProviderInvokeResult(
                false,
                ModelGatewayFailureStates.InsufficientContext,
                null,
                0,
                0,
                0m,
                "insufficient-context");
        }

        var deterministic = await deterministicFallback.InvokeAsync(request, cancellationToken);
        if (!deterministic.Succeeded || deterministic.Proposal is null)
        {
            return deterministic;
        }

        var enriched = deterministic.Proposal with
        {
            Hypothesis = $"Frontier-assisted reasoning: {deterministic.Proposal.Hypothesis}",
            Confidence = Math.Min(0.85m, deterministic.Proposal.Confidence),
        };

        return new ModelProviderInvokeResult(
            true,
            null,
            enriched,
            120,
            80,
            0.04m,
            "frontier-structured-proposal");
    }
}
