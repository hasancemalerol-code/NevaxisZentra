using System.Text.Json;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;
using NevaxisFactOS.Persistence.FactoryOrchestration;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;

namespace NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;

public static class FactoryRecoveryPatternEngine
{
    public static FactoryRecoveryPattern Extract(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        FactoryOutcomeImpactSnapshot? impact = null)
    {
        var metadata = FactoryDecisionCenterProjectionEngine.ParseMetadata(rootCase.MetadataJson);
        var orderRef = GetMetadataString(metadata, FactoryDecisionCenterMetadataKeys.OrderReference);
        var machineCode = GetMetadataString(metadata, FactoryDecisionCenterMetadataKeys.MachineCode)
            ?? impact?.MachineCode;
        var materialCode = GetMetadataString(metadata, FactoryDecisionCenterMetadataKeys.MaterialCode)
            ?? impact?.MaterialCode;
        var moldCode = GetMetadataString(metadata, FactoryDecisionCenterMetadataKeys.MoldCode);

        var eventCategory = ResolveEventCategory(rootCase);
        var outcomeType = ResolveOutcomeType(rootCase, orderRef);
        var workerCodes = childCases
            .Select(c => c.PrimaryWorker?.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        var plan = FactoryOutcomeRecoveryPlanEngine.FromJson(rootCase.RecoveryPlanJson);
        var selected = FactoryOutcomeRecoveryPlanEngine.ResolveSelectedPlan(plan);
        var strategyFamily = selected?.Strategy ?? plan.SelectedPlanKey;

        var blockers = new List<string>();
        if (!string.IsNullOrWhiteSpace(rootCase.CurrentBlockerCode) &&
            !string.Equals(rootCase.CurrentBlockerCode, FactoryCaseBlockerCodes.None, StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(rootCase.CurrentBlockerCode);
        }

        return new FactoryRecoveryPattern(
            outcomeType,
            eventCategory,
            machineCode,
            orderRef,
            materialCode,
            moldCode,
            rootCase.Severity,
            BuildPatternKey(outcomeType, eventCategory, machineCode, materialCode),
            strategyFamily,
            workerCodes,
            blockers);
    }

    public static string BuildPatternKey(
        string outcomeType,
        string eventCategory,
        string? machineCode,
        string? materialCode)
    {
        var parts = new List<string> { $"outcome:{Normalize(outcomeType)}" };
        if (!string.IsNullOrWhiteSpace(eventCategory))
        {
            parts.Add($"event:{Normalize(eventCategory)}");
        }

        if (!string.IsNullOrWhiteSpace(machineCode))
        {
            parts.Add($"machine:{Normalize(machineCode)}");
        }

        if (!string.IsNullOrWhiteSpace(materialCode))
        {
            parts.Add($"material:{Normalize(materialCode)}");
        }

        return string.Join('|', parts);
    }

    public static string Serialize(FactoryRecoveryPattern pattern) =>
        JsonSerializer.Serialize(pattern);

    public static FactoryRecoveryPattern Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FactoryRecoveryPattern(
                string.Empty, string.Empty, null, null, null, null,
                string.Empty, string.Empty, string.Empty, [], []);
        }

        try
        {
            return JsonSerializer.Deserialize<FactoryRecoveryPattern>(json)
                ?? new FactoryRecoveryPattern(
                    string.Empty, string.Empty, null, null, null, null,
                    string.Empty, string.Empty, string.Empty, [], []);
        }
        catch (JsonException)
        {
            return new FactoryRecoveryPattern(
                string.Empty, string.Empty, null, null, null, null,
                string.Empty, string.Empty, string.Empty, [], []);
        }
    }

    private static string ResolveOutcomeType(DigitalWorkCase rootCase, string? orderRef) =>
        !string.IsNullOrWhiteSpace(orderRef)
            ? "order-delivery-risk"
            : $"outcome:{rootCase.PrimaryDomain}";

    private static string ResolveEventCategory(DigitalWorkCase rootCase)
    {
        if (!string.IsNullOrWhiteSpace(rootCase.SourceClassification))
        {
            return rootCase.SourceClassification;
        }

        return rootCase.Source switch
        {
            var s when s.Contains("outcome", StringComparison.OrdinalIgnoreCase) => "factory.outcome",
            _ => rootCase.PrimaryDomain,
        };
    }

    private static string? GetMetadataString(
        Dictionary<string, JsonElement> metadata,
        string key) =>
        metadata.TryGetValue(key, out var value) ? value.GetString() : null;

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
