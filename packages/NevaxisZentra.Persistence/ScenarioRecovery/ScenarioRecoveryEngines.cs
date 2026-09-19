using NevaxisZentra.Persistence.ThroughputIntelligence;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.ScenarioRecovery;

public static class ScenarioRecoveryActionRegistry
{
    private static readonly HashSet<string> RegisteredActionTypes =
    [
        ScenarioRecoveryActionTypes.AddOperator,
        ScenarioRecoveryActionTypes.MoveOperator,
        ScenarioRecoveryActionTypes.ChangeBufferCapacity,
        ScenarioRecoveryActionTypes.ResequenceOrder,
        ScenarioRecoveryActionTypes.ReassignResource,
        ScenarioRecoveryActionTypes.ExpediteMaterial,
        ScenarioRecoveryActionTypes.CombinedIntervention,
    ];

    public static bool IsRegistered(string actionType) =>
        RegisteredActionTypes.Contains(actionType);

    public static ScenarioRecoveryActionDefinition ValidateOrReject(
        string actionType,
        string? targetEntityCode,
        IReadOnlyDictionary<string, string>? parameters)
    {
        if (!IsRegistered(actionType))
        {
            throw new ScenarioRecoveryException(
                ScenarioRecoveryFailureCodes.UnsupportedAction,
                $"Unknown action type: {actionType}");
        }

        parameters ??= new Dictionary<string, string>();
        var riskClass = actionType switch
        {
            ScenarioRecoveryActionTypes.AddOperator => "LOW",
            ScenarioRecoveryActionTypes.MoveOperator => "MEDIUM",
            ScenarioRecoveryActionTypes.ChangeBufferCapacity => "LOW",
            ScenarioRecoveryActionTypes.ResequenceOrder => "MEDIUM",
            ScenarioRecoveryActionTypes.ReassignResource => "HIGH",
            ScenarioRecoveryActionTypes.ExpediteMaterial => "MEDIUM",
            ScenarioRecoveryActionTypes.CombinedIntervention => "HIGH",
            _ => "MEDIUM",
        };

        var requiresApproval = actionType is ScenarioRecoveryActionTypes.ReassignResource
            or ScenarioRecoveryActionTypes.ResequenceOrder
            or ScenarioRecoveryActionTypes.CombinedIntervention;

        return new ScenarioRecoveryActionDefinition(
            actionType,
            ResolveTargetEntityType(actionType),
            targetEntityCode,
            parameters,
            riskClass,
            requiresApproval,
            actionType != ScenarioRecoveryActionTypes.ExpediteMaterial);
    }

    public static ScenarioIntervention ToScenarioIntervention(ScenarioRecoveryActionDefinition action) =>
        action.ActionType switch
        {
            ScenarioRecoveryActionTypes.AddOperator => new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.AddOperator,
                null,
                action.TargetEntityCode,
                action.Parameters.GetValueOrDefault("operatorCode"),
                null,
                null,
                null,
                null,
                null),
            ScenarioRecoveryActionTypes.MoveOperator => new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.MoveOperator,
                null,
                action.TargetEntityCode,
                action.Parameters.GetValueOrDefault("operatorCode"),
                action.Parameters.GetValueOrDefault("targetStationCode"),
                null,
                null,
                null,
                null),
            ScenarioRecoveryActionTypes.ChangeBufferCapacity => new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.ChangeBufferCapacity,
                null,
                action.TargetEntityCode ?? ThroughputIntelligenceFixtureCodes.BufferCode,
                null,
                null,
                int.TryParse(action.Parameters.GetValueOrDefault("delta"), out var delta) ? delta : 20,
                null,
                null,
                null),
            ScenarioRecoveryActionTypes.ReassignResource => new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.ReassignResource,
                null,
                action.TargetEntityCode,
                null,
                null,
                null,
                null,
                null,
                action.Parameters.GetValueOrDefault("resourceCode")),
            _ => new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.AddOperator,
                null,
                action.TargetEntityCode ?? ThroughputIntelligenceFixtureCodes.AssemblyStationCode,
                action.Parameters.GetValueOrDefault("operatorCode"),
                null,
                null,
                null,
                null,
                null),
        };

    private static string ResolveTargetEntityType(string actionType) =>
        actionType switch
        {
            ScenarioRecoveryActionTypes.AddOperator or ScenarioRecoveryActionTypes.MoveOperator
                => "station",
            ScenarioRecoveryActionTypes.ChangeBufferCapacity => "buffer",
            ScenarioRecoveryActionTypes.ResequenceOrder => "order",
            ScenarioRecoveryActionTypes.ReassignResource => "resource",
            ScenarioRecoveryActionTypes.ExpediteMaterial => "material",
            ScenarioRecoveryActionTypes.CombinedIntervention => "line",
            _ => "entity",
        };
}

public static class ScenarioRecoveryMultiActionConflictDetector
{
    public static IReadOnlyList<string> DetectConflicts(IReadOnlyList<ScenarioRecoveryActionDefinition> actions)
    {
        var conflicts = new List<string>();
        var moveOperators = actions.Where(a => a.ActionType == ScenarioRecoveryActionTypes.MoveOperator).ToList();
        if (moveOperators.Count > 1
            && moveOperators.Select(a => a.Parameters.GetValueOrDefault("operatorCode")).Distinct().Count() == 1)
        {
            conflicts.Add("duplicate-operator-move");
        }

        var addOperators = actions.Count(a => a.ActionType == ScenarioRecoveryActionTypes.AddOperator);
        if (addOperators > 2)
        {
            conflicts.Add("excessive-operator-additions");
        }

        if (actions.Any(a => a.ActionType == ScenarioRecoveryActionTypes.ResequenceOrder)
            && actions.Any(a => a.ActionType == ScenarioRecoveryActionTypes.ReassignResource))
        {
            conflicts.Add("schedule-resource-sequence-conflict");
        }

        return conflicts;
    }
}

public static class ScenarioRecoveryFeasibilityEngine
{
    public static (string State, IReadOnlyList<string> Reasons) Evaluate(
        ScenarioMetricSnapshot simulation,
        IReadOnlyList<ScenarioRecoveryActionDefinition> actions,
        string dataQuality)
    {
        var reasons = new List<string>();

        if (dataQuality is ThroughputIntelligenceDataQualityStates.Insufficient
            or ThroughputIntelligenceDataQualityStates.Conflicting)
        {
            return (ScenarioRecoveryFeasibilityStates.Infeasible, ["insufficient-or-conflicted-context"]);
        }

        var conflicts = ScenarioRecoveryMultiActionConflictDetector.DetectConflicts(actions);
        if (conflicts.Count > 0)
        {
            return (ScenarioRecoveryFeasibilityStates.Infeasible, conflicts);
        }

        if (simulation.Eligibility == ThroughputIntelligenceScenarioEligibility.NotEligible)
        {
            reasons.AddRange(simulation.LimitingFactors);
            return (ScenarioRecoveryFeasibilityStates.Infeasible, reasons);
        }

        if (simulation.OperationalRiskScore >= 0.7m)
        {
            reasons.Add("elevated-operational-risk");
            return (ScenarioRecoveryFeasibilityStates.FeasibleWithRisk, reasons);
        }

        if (simulation.Eligibility == ThroughputIntelligenceScenarioEligibility.RequiresControlledApproval)
        {
            reasons.Add("requires-controlled-approval");
            return (ScenarioRecoveryFeasibilityStates.FeasibleWithRisk, reasons);
        }

        return (ScenarioRecoveryFeasibilityStates.Feasible, reasons);
    }
}

public static class ScenarioRecoveryComparisonEngine
{
    public static IReadOnlyList<ScenarioRecoveryComparisonMetric> BuildMetrics(
        ScenarioMetricSnapshot baseline,
        ScenarioMetricSnapshot option)
    {
        return
        [
            Metric("GOOD_THROUGHPUT", "Good throughput", baseline.PredictedGoodThroughputPerHour,
                option.PredictedGoodThroughputPerHour, option.DeltaGoodThroughputPerHour, "pcs/h", "higher"),
            Metric("SCRAP", "Scrap rate", baseline.PredictedScrapRatePercent,
                option.PredictedScrapRatePercent, option.PredictedScrapRatePercent - baseline.PredictedScrapRatePercent, "%", "lower"),
            Metric("WIP", "WIP", baseline.PredictedWipAverage, option.PredictedWipAverage,
                option.PredictedWipAverage - baseline.PredictedWipAverage, "units", "lower"),
            Metric("UTILIZATION", "Utilization", baseline.UtilizationPercent, option.UtilizationPercent,
                option.UtilizationPercent - baseline.UtilizationPercent, "%", "higher"),
            Metric("RISK", "Operational risk", 0m, option.OperationalRiskScore, option.OperationalRiskScore, "score", "lower"),
            Metric("QUALITY", "Quality impact", 0m, option.QualityImpactScore, option.QualityImpactScore, "score", "lower"),
        ];
    }

    private static ScenarioRecoveryComparisonMetric Metric(
        string code, string label, decimal baseline, decimal option, decimal delta, string unit, string direction) =>
        new(code, label, baseline, option, delta, unit, direction);
}

public static class ScenarioRecoveryDominanceEngine
{
    public static IReadOnlyList<string> MarkDominatedOptions(IReadOnlyList<ScenarioRecoveryOptionSnapshot> options)
    {
        var dominated = new HashSet<string>();
        var candidates = options.Where(o => o.OptionLabel != ScenarioRecoveryOptionLabels.Baseline).ToList();

        foreach (var a in candidates)
        {
            foreach (var b in candidates)
            {
                if (a.OptionLabel == b.OptionLabel) continue;
                if (IsDominatedBy(a, b))
                {
                    dominated.Add(a.OptionLabel);
                }
            }
        }

        return dominated.ToList();
    }

    private static bool IsDominatedBy(ScenarioRecoveryOptionSnapshot candidate, ScenarioRecoveryOptionSnapshot other)
    {
        if (other.Simulation.PredictedGoodThroughputPerHour < candidate.Simulation.PredictedGoodThroughputPerHour)
        {
            return false;
        }

        if (other.Simulation.OperationalRiskScore > candidate.Simulation.OperationalRiskScore)
        {
            return false;
        }

        if (other.Simulation.PredictedWipAverage > candidate.Simulation.PredictedWipAverage)
        {
            return false;
        }

        return other.Simulation.PredictedGoodThroughputPerHour > candidate.Simulation.PredictedGoodThroughputPerHour
            || other.Simulation.OperationalRiskScore < candidate.Simulation.OperationalRiskScore
            || other.Simulation.PredictedWipAverage < candidate.Simulation.PredictedWipAverage;
    }
}

public static class ScenarioRecoveryRecommendationEngine
{
    public static ScenarioRecoveryRecommendationRationale Recommend(
        IReadOnlyList<ScenarioRecoveryOptionSnapshot> options,
        IReadOnlyList<ScenarioRecoveryObjective> objectives)
    {
        var eligible = options
            .Where(o => o.OptionLabel != ScenarioRecoveryOptionLabels.Baseline
                && o.FeasibilityState != ScenarioRecoveryFeasibilityStates.Infeasible
                && !o.IsDominated)
            .OrderByDescending(o => Score(o, objectives))
            .ToList();

        if (eligible.Count == 0)
        {
            return new ScenarioRecoveryRecommendationRationale(
                ScenarioRecoveryOptionLabels.Baseline,
                ["no-feasible-non-dominated-option"],
                ["maintain-current-plan"],
                true);
        }

        var selected = eligible[0];
        var scoreComponents = new List<string>
        {
            $"throughput-delta={selected.Simulation.DeltaGoodThroughputPerHour:F1}",
            $"risk={selected.Simulation.OperationalRiskScore:F2}",
            $"quality-impact={selected.Simulation.QualityImpactScore:F2}",
            $"feasibility={selected.FeasibilityState}",
        };

        var guardrails = objectives
            .Where(o => o.IsHardConstraint)
            .Select(o => $"hard:{o.ObjectiveType}")
            .ToList();

        if (selected.RequiresApproval)
        {
            guardrails.Add("approval-required-before-execute");
        }

        return new ScenarioRecoveryRecommendationRationale(
            selected.OptionLabel,
            scoreComponents,
            guardrails,
            true);
    }

    public static decimal ScoreOption(
        ScenarioRecoveryOptionSnapshot option,
        IReadOnlyList<ScenarioRecoveryObjective> objectives)
    {
        if (option.FeasibilityState == ScenarioRecoveryFeasibilityStates.Infeasible || option.IsDominated)
        {
            return decimal.MinValue;
        }

        return Score(option, objectives);
    }

    private static decimal Score(
        ScenarioRecoveryOptionSnapshot option,
        IReadOnlyList<ScenarioRecoveryObjective> objectives)
    {
        var throughputWeight = objectives.FirstOrDefault(o =>
            o.ObjectiveType == ScenarioRecoveryObjectiveTypes.MaximizeVerifiedThroughput)?.Priority ?? 3;
        var otifWeight = objectives.FirstOrDefault(o =>
            o.ObjectiveType == ScenarioRecoveryObjectiveTypes.ProtectOtif)?.Priority ?? 2;

        return option.Simulation.DeltaGoodThroughputPerHour * throughputWeight
            - option.Simulation.OperationalRiskScore * 20m
            - option.Simulation.QualityImpactScore * 10m
            - (option.RequiresApproval ? 3m : 0m)
            + otifWeight;
    }
}

public static class ScenarioRecoveryTradeOffEngine
{
    public static IReadOnlyList<ScenarioRecoveryTradeOff> Explain(
        IReadOnlyList<ScenarioRecoveryOptionSnapshot> options,
        ScenarioRecoveryOptionSnapshot baseline)
    {
        var tradeOffs = new List<ScenarioRecoveryTradeOff>();
        foreach (var option in options.Where(o => o.OptionLabel != ScenarioRecoveryOptionLabels.Baseline))
        {
            var advantages = new List<string>();
            var disadvantages = new List<string>();
            var facts = new List<string>();

            if (option.Simulation.DeltaGoodThroughputPerHour > 0)
            {
                advantages.Add($"Higher throughput (+{option.Simulation.DeltaGoodThroughputPerHour:F0}/h)");
                facts.Add($"throughput-delta={option.Simulation.DeltaGoodThroughputPerHour:F1}");
            }
            else if (option.Simulation.DeltaGoodThroughputPerHour < 0)
            {
                disadvantages.Add($"Lower throughput ({option.Simulation.DeltaGoodThroughputPerHour:F0}/h)");
            }

            if (option.Simulation.PredictedWipAverage > baseline.Simulation.PredictedWipAverage)
            {
                disadvantages.Add("Higher WIP");
                facts.Add($"wip-delta={option.Simulation.PredictedWipAverage - baseline.Simulation.PredictedWipAverage:F1}");
            }
            else if (option.Simulation.PredictedWipAverage < baseline.Simulation.PredictedWipAverage)
            {
                advantages.Add("Lower WIP");
            }

            if (option.RequiresApproval)
            {
                disadvantages.Add("Requires approval");
            }

            if (option.Simulation.ConstraintAfter?.StationCode != baseline.Simulation.ConstraintBefore?.StationCode)
            {
                facts.Add(
                    $"constraint-movement={baseline.Simulation.ConstraintBefore?.StationCode}->{option.Simulation.ConstraintAfter?.StationCode}");
            }

            tradeOffs.Add(new ScenarioRecoveryTradeOff(option.OptionLabel, advantages, disadvantages, facts));
        }

        return tradeOffs;
    }
}

public static class ScenarioRecoveryOptionGenerator
{
    public static IReadOnlyList<(string Label, string Title, string Category, IReadOnlyList<ScenarioRecoveryActionDefinition> Actions)>
        GenerateGoldenRecoveryOptions(string assemblyStationCode)
    {
        return
        [
            (
                ScenarioRecoveryOptionLabels.Baseline,
                "Current plan / do nothing",
                ScenarioRecoveryCategories.ProductionRecovery,
                []),
            (
                ScenarioRecoveryOptionLabels.OptionA,
                "Add qualified operator to Assembly",
                ScenarioRecoveryCategories.OperatorReallocation,
                [
                    ScenarioRecoveryActionRegistry.ValidateOrReject(
                        ScenarioRecoveryActionTypes.AddOperator,
                        assemblyStationCode,
                        new Dictionary<string, string>
                        {
                            ["operatorCode"] = ThroughputIntelligenceFixtureCodes.OperatorAssembly2,
                        }),
                ]),
            (
                ScenarioRecoveryOptionLabels.OptionB,
                "Increase buffer and resequence schedule",
                ScenarioRecoveryCategories.CombinedIntervention,
                [
                    ScenarioRecoveryActionRegistry.ValidateOrReject(
                        ScenarioRecoveryActionTypes.ChangeBufferCapacity,
                        ThroughputIntelligenceFixtureCodes.BufferCode,
                        new Dictionary<string, string> { ["delta"] = "20" }),
                    ScenarioRecoveryActionRegistry.ValidateOrReject(
                        ScenarioRecoveryActionTypes.ResequenceOrder,
                        "ORD-418",
                        new Dictionary<string, string> { ["position"] = "earlier" }),
                ]),
            (
                ScenarioRecoveryOptionLabels.OptionC,
                "Combined operator + schedule/resource intervention",
                ScenarioRecoveryCategories.CombinedIntervention,
                [
                    ScenarioRecoveryActionRegistry.ValidateOrReject(
                        ScenarioRecoveryActionTypes.AddOperator,
                        assemblyStationCode,
                        new Dictionary<string, string>
                        {
                            ["operatorCode"] = ThroughputIntelligenceFixtureCodes.OperatorAssembly2,
                        }),
                    ScenarioRecoveryActionRegistry.ValidateOrReject(
                        ScenarioRecoveryActionTypes.ReassignResource,
                        assemblyStationCode,
                        new Dictionary<string, string> { ["resourceCode"] = "RES-ASM-02" }),
                ]),
        ];
    }
}

public static class ScenarioRecoveryCorrelationEngine
{
    public static string BuildCorrelationKey(Guid lineId, string? suffix = null)
    {
        var key = $"{ScenarioRecoveryCorrelationPrefixes.LineRecovery}:{lineId:N}";
        return suffix is null ? key : $"{key}:{suffix}";
    }
}

public static class ScenarioRecoveryVerificationMapper
{
    public static string MapOutcome(string throughputOutcome) =>
        throughputOutcome switch
        {
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement => ScenarioRecoveryVerificationOutcomes.VerifiedSuccess,
            ThroughputOptimizerVerificationOutcomes.PartialImprovement => ScenarioRecoveryVerificationOutcomes.PartiallyVerified,
            ThroughputOptimizerVerificationOutcomes.NoImprovement => ScenarioRecoveryVerificationOutcomes.NoImprovement,
            ThroughputOptimizerVerificationOutcomes.Regression => ScenarioRecoveryVerificationOutcomes.Regression,
            _ => ScenarioRecoveryVerificationOutcomes.Inconclusive,
        };
}
