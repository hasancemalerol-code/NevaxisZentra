using System.Text.Json;
using CrevaFactOS.Persistence.DigitalWorkforce;

namespace CrevaFactOS.Persistence.FactoryOrchestration;

public static class FactoryRecoveryPlanEngine
{
    public static FactoryRecoveryPlan Build(
        DigitalWorkCase rootCase,
        IReadOnlyList<FactoryImpactNode> impacts,
        IReadOnlyList<FactoryCaseDependency> dependencies)
    {
        var actions = impacts.Select(i => $"{i.Domain}: {i.Summary}").ToList();
        var dependencyKeys = dependencies
            .Where(d => d.EdgeType is FactoryDependencyEdgeTypes.DependsOn or FactoryDependencyEdgeTypes.Blocks)
            .Select(d => $"{d.FromNodeKey} → {d.ToNodeKey}")
            .ToList();

        return new FactoryRecoveryPlan(
            $"Fabrika vakası çözümü: {rootCase.Title}",
            actions,
            dependencyKeys,
            [
                "Makine operasyonel",
                "Bakım tamamlandı",
                "Kalite uygun",
                "Üretim devam ediyor",
            ],
            "Alternatif makine/kalıp veya sipariş yeniden planlama");
    }

    public static string ToJson(FactoryRecoveryPlan plan) =>
        JsonSerializer.Serialize(new
        {
            objective = plan.Objective,
            actions = plan.Actions,
            dependencies = plan.Dependencies,
            verificationCriteria = plan.VerificationCriteria,
            fallback = plan.Fallback,
        });

    public static FactoryRecoveryPlan FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FactoryRecoveryPlan(string.Empty, [], [], [], null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return new FactoryRecoveryPlan(
                doc.RootElement.GetProperty("objective").GetString() ?? string.Empty,
                ReadArray(doc.RootElement, "actions"),
                ReadArray(doc.RootElement, "dependencies"),
                ReadArray(doc.RootElement, "verificationCriteria"),
                doc.RootElement.TryGetProperty("fallback", out var fallback) ? fallback.GetString() : null);
        }
        catch (JsonException)
        {
            return new FactoryRecoveryPlan(string.Empty, [], [], [], null);
        }
    }

    private static List<string> ReadArray(JsonElement root, string property) =>
        root.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(s => s.Length > 0).ToList()
            : [];
}
