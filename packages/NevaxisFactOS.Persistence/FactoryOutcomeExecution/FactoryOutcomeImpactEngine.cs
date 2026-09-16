using System.Globalization;
using System.Text.Json;
using NevaxisFactOS.Persistence.FactoryOrchestration;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public static class FactoryOutcomeImpactEngine
{
    public static FactoryOutcomeImpactSnapshot Evaluate(FactoryDomainEvent eventData)
    {
        var domainImpacts = FactoryDomainImpactEngine.Evaluate(eventData);
        var metadata = ParseMetadata(eventData.MetadataJson);

        var requiredQty = ReadInt(metadata, "requiredQuantity");
        var projectedQty = ReadInt(metadata, "projectedQuantity");
        var gap = requiredQty.HasValue && projectedQty.HasValue
            ? Math.Max(0, requiredQty.Value - projectedQty.Value)
            : ReadInt(metadata, "gapQuantity");

        var orderRef = eventData.OrderId ?? ReadString(metadata, "orderReference");
        var machineCode = eventData.MachineId ?? ReadString(metadata, "machineCode");
        var materialCode = eventData.MaterialId ?? ReadString(metadata, "materialCode");
        var confidence = ReadDecimal(metadata, "confidence") ?? ResolveConfidence(eventData.Severity);
        var financialImpact = ReadString(metadata, "financialImpactTry");

        var outcomeAtRisk = !string.IsNullOrWhiteSpace(orderRef)
            ? $"Sipariş {orderRef} teslimat taahhüdü"
            : !string.IsNullOrWhiteSpace(machineCode)
                ? $"Makine {machineCode} üretim çıktısı"
                : eventData.Title;

        var timeToBreach = ReadString(metadata, "deliveryCommitmentUtc") is not null
            ? FormatTimeToBreach(ReadString(metadata, "deliveryCommitmentUtc"))
            : ReadString(metadata, "timeToBreachLabel");

        return new FactoryOutcomeImpactSnapshot(
            outcomeAtRisk,
            requiredQty,
            gap,
            orderRef,
            machineCode,
            materialCode,
            timeToBreach,
            confidence,
            financialImpact,
            domainImpacts);
    }

    public static FactoryOutcomeImpactSnapshot? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new FactoryOutcomeImpactSnapshot(
                root.TryGetProperty("outcomeAtRisk", out var outcome) ? outcome.GetString() ?? string.Empty : string.Empty,
                root.TryGetProperty("quantityAtRisk", out var qty) && qty.TryGetInt32(out var qtyVal) ? qtyVal : null,
                root.TryGetProperty("projectedGap", out var gap) && gap.TryGetInt32(out var gapVal) ? gapVal : null,
                root.TryGetProperty("orderReference", out var order) ? order.GetString() : null,
                root.TryGetProperty("machineCode", out var machine) ? machine.GetString() : null,
                root.TryGetProperty("materialCode", out var material) ? material.GetString() : null,
                root.TryGetProperty("timeToBreachLabel", out var breach) ? breach.GetString() : null,
                root.TryGetProperty("confidence", out var confidence) && confidence.TryGetDecimal(out var confVal) ? confVal : 0.7m,
                root.TryGetProperty("financialImpactTry", out var financial) ? financial.GetString() : null,
                []);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string ToJson(FactoryOutcomeImpactSnapshot snapshot) =>
        JsonSerializer.Serialize(new
        {
            outcomeAtRisk = snapshot.OutcomeAtRisk,
            quantityAtRisk = snapshot.QuantityAtRisk,
            projectedGap = snapshot.ProjectedGap,
            orderReference = snapshot.OrderReference,
            machineCode = snapshot.MachineCode,
            materialCode = snapshot.MaterialCode,
            timeToBreachLabel = snapshot.TimeToBreachLabel,
            confidence = snapshot.Confidence,
            financialImpactTry = snapshot.FinancialImpactTry,
            domainImpacts = snapshot.DomainImpacts.Select(i => new { domain = i.Domain, summary = i.Summary, severity = i.Severity }),
        });

    private static Dictionary<string, JsonElement> ParseMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static int? ReadInt(Dictionary<string, JsonElement> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && value.TryGetInt32(out var parsed) ? parsed : null;

    private static string? ReadString(Dictionary<string, JsonElement> metadata, string key) =>
        metadata.TryGetValue(key, out var value) ? value.GetString() : null;

    private static decimal? ReadDecimal(Dictionary<string, JsonElement> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && value.TryGetDecimal(out var parsed) ? parsed : null;

    private static decimal ResolveConfidence(string severity) => severity switch
    {
        "critical" => 0.92m,
        "high" => 0.85m,
        "medium" => 0.72m,
        _ => 0.6m,
    };

    private static string? FormatTimeToBreach(string? commitmentUtc)
    {
        if (!DateTimeOffset.TryParse(commitmentUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var target))
        {
            return null;
        }

        var remaining = target - DateTimeOffset.UtcNow;
        if (remaining.TotalHours <= 0)
        {
            return "Taahhüt süresi aşıldı";
        }

        return remaining.TotalHours < 24
            ? $"{Math.Ceiling(remaining.TotalHours)} saat içinde"
            : $"{Math.Ceiling(remaining.TotalDays)} gün içinde";
    }
}
