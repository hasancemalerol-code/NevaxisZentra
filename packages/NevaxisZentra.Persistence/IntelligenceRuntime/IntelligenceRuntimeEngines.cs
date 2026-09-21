using NevaxisZentra.Persistence.ModelGateway;

namespace NevaxisZentra.Persistence.IntelligenceRuntime;

public static class IntelligenceRoutingPolicyEngine
{
    public static IntelligenceRoutingDecision Decide(
        IntelligenceRouteRequest request,
        IntelligenceTaskDefinition task,
        ModelGatewayRoutingDecision gatewayRouting,
        NevaxisModelRegistryRecord? productionNevaxisModel,
        IntelligenceBudgetPolicy? budget,
        int modelCallsToday)
    {
        if (budget is not null && modelCallsToday >= budget.MaxModelCalls)
        {
            return BuildDecision(
                IntelligenceRouteTypes.Deterministic,
                ModelGatewayIntelligencePaths.DeterministicEngine,
                ModelGatewayProviderIds.DeterministicEngine,
                "deterministic-engine-v1",
                "budget-exceeded",
                "WHY NO MODEL: intelligence budget max model calls reached.",
                false,
                false);
        }

        if (request.CostBudgetRemaining is < 0.01m)
        {
            return BuildDecision(
                IntelligenceRouteTypes.Deterministic,
                ModelGatewayIntelligencePaths.DeterministicEngine,
                ModelGatewayProviderIds.DeterministicEngine,
                "deterministic-engine-v1",
                "cost-budget-remaining",
                "WHY NO MODEL: remaining cost budget below threshold.",
                false,
                false);
        }

        var preferredRoute = ResolvePreferredRoute(task, request, productionNevaxisModel);
        if (preferredRoute == IntelligenceRouteTypes.Deterministic
            || preferredRoute == IntelligenceRouteTypes.Analytics
            || preferredRoute == IntelligenceRouteTypes.Solver
            || preferredRoute == IntelligenceRouteTypes.Retrieval
            || preferredRoute == IntelligenceRouteTypes.Simulation)
        {
            return BuildDecision(
                preferredRoute,
                gatewayRouting.IntelligencePath,
                gatewayRouting.ProviderId,
                gatewayRouting.ModelName,
                gatewayRouting.RoutingReason,
                BuildWhyNoModel(preferredRoute, task, request),
                false,
                gatewayRouting.UsedFallback);
        }

        if (preferredRoute == IntelligenceRouteTypes.NevaxisModel
            && productionNevaxisModel is not null
            && productionNevaxisModel.Status is NevaxisModelStatuses.Production or NevaxisModelStatuses.Limited)
        {
            return BuildDecision(
                IntelligenceRouteTypes.NevaxisModel,
                ModelGatewayIntelligencePaths.LocalModel,
                productionNevaxisModel.ModelCode,
                productionNevaxisModel.Version,
                $"nevaxis-production:{task.TaskType}",
                $"WHY MODEL: Nevaxis production model eligible for {task.TaskType}.",
                true,
                false);
        }

        if (preferredRoute == IntelligenceRouteTypes.FrontierModel || task.FrontierRequired)
        {
            return BuildDecision(
                IntelligenceRouteTypes.FrontierModel,
                gatewayRouting.IntelligencePath,
                gatewayRouting.ProviderId,
                gatewayRouting.ModelName,
                gatewayRouting.RoutingReason,
                $"WHY MODEL: frontier required for {task.TaskType}.",
                gatewayRouting.IntelligencePath != ModelGatewayIntelligencePaths.DeterministicEngine,
                gatewayRouting.UsedFallback);
        }

        return BuildDecision(
            IntelligenceRouteTypes.Deterministic,
            gatewayRouting.IntelligencePath,
            gatewayRouting.ProviderId,
            gatewayRouting.ModelName,
            gatewayRouting.RoutingReason,
            BuildWhyNoModel(IntelligenceRouteTypes.Deterministic, task, request),
            false,
            gatewayRouting.UsedFallback);
    }

    public static string MapGatewayRequestClassification(string taskType) =>
        taskType switch
        {
            IntelligenceTaskTypes.CrossDomainDiagnosis or IntelligenceTaskTypes.FactoryCopilotSynthesis
                => ModelGatewayRequestClassifications.CrossModuleDiagnosis,
            IntelligenceTaskTypes.RootCauseCandidate
                => ModelGatewayRequestClassifications.HypothesisGeneration,
            IntelligenceTaskTypes.RecoveryPlanDraft
                => ModelGatewayRequestClassifications.RecoveryPlanReasoning,
            IntelligenceTaskTypes.ScenarioExplanation
                => ModelGatewayRequestClassifications.ScenarioSimulation,
            IntelligenceTaskTypes.TrendAnalysis or IntelligenceTaskTypes.AnomalyAnalysis
                => ModelGatewayRequestClassifications.OperationalExplanation,
            IntelligenceTaskTypes.StateLookup or IntelligenceTaskTypes.ThresholdEvaluation
                => ModelGatewayRequestClassifications.ThroughputCalculation,
            _ => ModelGatewayRequestClassifications.OperationalExplanation,
        };

    private static string ResolvePreferredRoute(
        IntelligenceTaskDefinition task,
        IntelligenceRouteRequest request,
        NevaxisModelRegistryRecord? productionNevaxisModel)
    {
        if (string.Equals(request.PreferredPath, ModelGatewayIntelligencePaths.DeterministicEngine, StringComparison.OrdinalIgnoreCase))
        {
            return IntelligenceRouteTypes.Deterministic;
        }

        if (request.ContextQualityState is "CONFLICTED_STATE" or "INSUFFICIENT_CONTEXT")
        {
            return IntelligenceRouteTypes.Deterministic;
        }

        if (request.ActionRiskLevel >= 4)
        {
            return IntelligenceRouteTypes.Deterministic;
        }

        if (productionNevaxisModel is not null
            && task.NevaxisModelEligible
            && SupportsTask(productionNevaxisModel, task.TaskType))
        {
            return IntelligenceRouteTypes.NevaxisModel;
        }

        return task.PreferredRoute;
    }

    private static bool SupportsTask(NevaxisModelRegistryRecord model, string taskType)
    {
        if (string.IsNullOrWhiteSpace(model.TaskCapabilitiesJson) || model.TaskCapabilitiesJson == "[]")
        {
            return false;
        }

        try
        {
            var capabilities = System.Text.Json.JsonSerializer.Deserialize<List<string>>(model.TaskCapabilitiesJson) ?? [];
            return capabilities.Contains(taskType, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string BuildWhyNoModel(string routeType, IntelligenceTaskDefinition task, IntelligenceRouteRequest request) =>
        routeType switch
        {
            IntelligenceRouteTypes.Deterministic =>
                $"WHY NO MODEL: {task.TaskType} answered from canonical operational state ({request.RequestClassification}).",
            IntelligenceRouteTypes.Analytics =>
                "WHY NO MODEL: MP2 analytics and trend comparison available without model inference.",
            IntelligenceRouteTypes.Solver =>
                "WHY NO MODEL: registered solver/scenario path can evaluate options deterministically.",
            IntelligenceRouteTypes.Retrieval =>
                "WHY NO MODEL: verified factory knowledge retrieval is sufficient.",
            IntelligenceRouteTypes.Simulation =>
                "WHY NO MODEL: scenario simulation engine handles what-if evaluation.",
            _ => "WHY NO MODEL: preferred non-model route selected.",
        };

    private static IntelligenceRoutingDecision BuildDecision(
        string routeType,
        string intelligencePath,
        string providerId,
        string modelName,
        string routingReason,
        string whyModelCalledOrSkipped,
        bool modelInvoked,
        bool usedFallback) =>
        new(
            routeType,
            intelligencePath,
            providerId,
            modelName,
            "1.0",
            routingReason,
            whyModelCalledOrSkipped,
            modelInvoked,
            usedFallback);
}

public static class ProviderPriceCatalogEngine
{
    public static ProviderPriceSnapshot Resolve(
        IReadOnlyList<ProviderPriceCatalogRecord> catalog,
        string providerId,
        string modelName)
    {
        var match = catalog
            .Where(x => x.IsActive
                && string.Equals(x.ProviderId, providerId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ModelName, modelName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.EffectiveFromUtc)
            .FirstOrDefault();

        if (match is null)
        {
            return new ProviderPriceSnapshot(
                providerId,
                modelName,
                0m,
                0m,
                "USD",
                "unpriced",
                DateTimeOffset.UtcNow);
        }

        return new ProviderPriceSnapshot(
            match.ProviderId,
            match.ModelName,
            match.InputPricePer1K,
            match.OutputPricePer1K,
            match.Currency,
            match.PriceVersion,
            match.EffectiveFromUtc);
    }

    public static decimal EstimateCost(ProviderPriceSnapshot price, int inputTokens, int outputTokens)
    {
        if (price.PriceVersion == "unpriced")
        {
            return 0m;
        }

        return Math.Round(
            inputTokens / 1000m * price.InputPricePer1K + outputTokens / 1000m * price.OutputPricePer1K,
            6);
    }

    public static List<ProviderPriceCatalogRecord> SeedDefaults(DateTimeOffset now) =>
    [
        new()
        {
            Id = Guid.Parse("10000000-0000-4000-8000-000000000fd1"),
            ProviderId = "openai",
            ModelName = "gpt-4.1-mini",
            InputPricePer1K = 0.0004m,
            OutputPricePer1K = 0.0016m,
            Currency = "USD",
            PriceVersion = "2026-09-21",
            Source = "seed-default",
            IsActive = true,
            EffectiveFromUtc = now,
            CreatedAtUtc = now,
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-4000-8000-000000000fd2"),
            ProviderId = ModelGatewayProviderIds.FrontierModel,
            ModelName = "frontier-reasoning-v1",
            InputPricePer1K = 0.003m,
            OutputPricePer1K = 0.012m,
            Currency = "USD",
            PriceVersion = "2026-09-21",
            Source = "seed-default",
            IsActive = true,
            EffectiveFromUtc = now,
            CreatedAtUtc = now,
        },
    ];
}
