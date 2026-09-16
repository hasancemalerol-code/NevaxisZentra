using NevaxisFactOS.Persistence.FactoryOutcomeExecution;

namespace NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;

public static class FactoryRecoveryStrategyIntelligenceEngine
{
    public static IReadOnlyList<FactoryRecoveryStrategyPerformance> BuildPerformance(
        IReadOnlyList<FactoryOutcomeLearningRecord> records,
        IReadOnlyList<FactoryOutcomeLearningRecord> failureRecords)
    {
        var successGroups = records
            .Where(r => r.LearningDisposition == FactoryRecoveryLearningDisposition.VerifiedSuccess)
            .GroupBy(r => r.SelectedStrategyKey, StringComparer.OrdinalIgnoreCase);

        var performances = new List<FactoryRecoveryStrategyPerformance>();
        foreach (var group in successGroups)
        {
            var successes = group.Count();
            var recoveryTimes = group
                .Select(r => r.TimeToRecoveryMinutes)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .OrderBy(v => v)
                .ToList();
            var deliveryRelevant = group.Count(r => r.DeliveryProtected.HasValue);
            var deliveryProtected = group.Count(r => r.DeliveryProtected == true);

            var blockers = group
                .SelectMany(r => DeserializeList(r.BlockersJson))
                .GroupBy(b => b, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            var failureCount = failureRecords.Count(f =>
                string.Equals(f.SelectedStrategyKey, group.Key, StringComparison.OrdinalIgnoreCase));

            performances.Add(new FactoryRecoveryStrategyPerformance(
                group.Key,
                successes,
                successes,
                Median(recoveryTimes),
                deliveryProtected,
                deliveryRelevant > 0 ? deliveryRelevant : null,
                FactoryRecoverySimilarityEngine.ResolveSampleConfidence(successes),
                blockers,
                failureCount));
        }

        return performances.OrderByDescending(p => p.VerifiedSuccesses).ToList();
    }

    public static FactoryRecoveryPlanHistoricalEvidence BuildPlanEvidence(
        string planKey,
        string strategyLabel,
        IReadOnlyList<FactoryOutcomeLearningRecord> successRecords,
        IReadOnlyList<FactoryOutcomeLearningRecord> failureRecords)
    {
        var related = successRecords
            .Where(r => StrategyMatchesPlan(planKey, r.SelectedStrategyKey))
            .ToList();
        var failures = failureRecords
            .Where(r => StrategyMatchesPlan(planKey, r.SelectedStrategyKey))
            .ToList();

        var recoveryTimes = related
            .Select(r => r.TimeToRecoveryMinutes)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .OrderBy(v => v)
            .ToList();

        var knownRisks = related
            .SelectMany(r => DeserializeList(r.BlockersJson))
            .GroupBy(b => b, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        var failureModes = failures
            .Select(r => r.FailureSummary ?? r.CausePattern)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        return new FactoryRecoveryPlanHistoricalEvidence(
            planKey,
            related.Count,
            related.Count,
            Median(recoveryTimes),
            FactoryRecoverySimilarityEngine.ResolveSampleConfidence(related.Count),
            knownRisks,
            failureModes);
    }

    private static bool StrategyMatchesPlan(string planKey, string strategyKey) =>
        planKey switch
        {
            FactoryOutcomeExecutionPlanKeys.Primary =>
                strategyKey.Contains("kurtarma", StringComparison.OrdinalIgnoreCase) ||
                strategyKey.Contains("recovery", StringComparison.OrdinalIgnoreCase) ||
                strategyKey.Contains("kapasite koruma", StringComparison.OrdinalIgnoreCase),
            FactoryOutcomeExecutionPlanKeys.Alternative =>
                strategyKey.Contains("alternatif", StringComparison.OrdinalIgnoreCase) ||
                strategyKey.Contains("kapasiteye", StringComparison.OrdinalIgnoreCase) ||
                strategyKey.Contains("transfer", StringComparison.OrdinalIgnoreCase),
            _ => string.Equals(planKey, strategyKey, StringComparison.OrdinalIgnoreCase),
        };

    private static int? Median(List<int> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        return values.Count % 2 == 1
            ? values[values.Count / 2]
            : (values[(values.Count - 1) / 2] + values[values.Count / 2]) / 2;
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }
}
