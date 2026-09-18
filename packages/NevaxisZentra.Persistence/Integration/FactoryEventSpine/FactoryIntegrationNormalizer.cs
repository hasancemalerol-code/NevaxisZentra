using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisZentra.Persistence.FactoryOrchestration;

namespace NevaxisZentra.Persistence.Integration.FactoryEventSpine;

public static class FactoryIntegrationNormalizer
{
    public static string ComputePayloadHash(string rawPayloadJson)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPayloadJson));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string BuildIdempotencyKey(Guid tenantId, Guid connectorId, string sourceEventId) =>
        $"{tenantId:N}:{connectorId:N}:{sourceEventId.Trim().ToLowerInvariant()}";

    public static FactoryCanonicalEventContract Normalize(
        string rawPayloadJson,
        string sourceSystem,
        string defaultEventType = "production.signal")
    {
        using var doc = JsonDocument.Parse(rawPayloadJson);
        var root = doc.RootElement;

        var eventType = ReadString(root, "eventType", "event_type") ?? defaultEventType;
        var eventVersion = ReadString(root, "eventVersion", "event_version") ?? "1";
        var sourceEventId = ReadString(root, "sourceEventId", "source_event_id", "id")
            ?? ComputePayloadHash(rawPayloadJson)[..16];
        var occurredAt = ReadDateTime(root, "occurredAt", "occurred_at", "timestamp") ?? DateTimeOffset.UtcNow;

        var machineId = MapResource(ReadString(root, "machineCode", "machine_code", "machineId", "machine_id", "resourceId"));
        var orderId = ReadString(root, "orderReference", "order_reference", "orderId", "order_id");
        var materialId = ReadString(root, "materialCode", "material_code", "materialId", "material_id");
        var moldId = ReadString(root, "moldCode", "mold_code", "moldId", "mold_id");
        var productionLineId = ReadString(root, "productionLineId", "production_line_id", "lineId");
        var shiftId = ReadString(root, "shiftId", "shift_id");

        var severity = ReadString(root, "severity") ?? InferSeverity(eventType);
        var title = ReadString(root, "title") ?? $"Fabrika olayı: {eventType}";
        var description = ReadString(root, "description", "message") ?? title;

        var measurements = ParseMeasurements(root);
        var businessAttributes = ParseBusinessAttributes(root);

        return new FactoryCanonicalEventContract(
            eventType,
            eventVersion,
            sourceEventId,
            occurredAt,
            machineId,
            orderId,
            materialId,
            moldId,
            productionLineId,
            shiftId,
            severity,
            title,
            description,
            measurements,
            businessAttributes);
    }

    public static FactoryDomainEvent ToFactoryDomainEvent(
        FactoryCanonicalEventContract contract,
        Guid facilityId,
        string sourceType,
        string sourceClassification)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["occurredAtUtc"] = contract.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture),
            ["measurements"] = contract.Measurements,
            ["businessAttributes"] = contract.BusinessAttributes,
            ["schemaVersion"] = FactoryEventSpineSchemaVersions.V1,
        };

        foreach (var (key, value) in contract.BusinessAttributes)
        {
            metadata[key] = value;
        }

        return new FactoryDomainEvent(
            contract.EventType,
            contract.Title,
            contract.Description,
            contract.Severity,
            sourceClassification,
            facilityId,
            sourceType,
            contract.SourceEventId,
            contract.MachineId,
            contract.OrderId,
            contract.MoldId,
            contract.MaterialId,
            JsonSerializer.Serialize(metadata));
    }

    public static IReadOnlyList<string> BuildCorrelationKeys(FactoryCanonicalEventContract contract, Guid facilityId)
    {
        var keys = new List<string> { facilityId.ToString("N") };
        if (!string.IsNullOrWhiteSpace(contract.OrderId))
        {
            keys.Add($"outcome:order:{contract.OrderId.ToLowerInvariant()}");
        }
        else if (!string.IsNullOrWhiteSpace(contract.MachineId))
        {
            keys.Add($"machine:{contract.MachineId.ToLowerInvariant()}");
        }

        return keys;
    }

    private static string? MapResource(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Equals("173", StringComparison.OrdinalIgnoreCase))
        {
            return "INJ-004";
        }

        if (value.Equals("IMM-04", StringComparison.OrdinalIgnoreCase))
        {
            return "INJ-004";
        }

        return value.Trim();
    }

    private static string InferSeverity(string eventType) => eventType switch
    {
        var e when e.Contains("critical", StringComparison.OrdinalIgnoreCase) => "critical",
        var e when e.Contains("degradation", StringComparison.OrdinalIgnoreCase) => "critical",
        var e when e.Contains("shortage", StringComparison.OrdinalIgnoreCase) => "high",
        var e when e.Contains("downtime", StringComparison.OrdinalIgnoreCase) => "high",
        _ => "medium",
    };

    private static Dictionary<string, FactoryNormalizedMeasurement> ParseMeasurements(JsonElement root)
    {
        var result = new Dictionary<string, FactoryNormalizedMeasurement>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("measurements", out var measurements) || measurements.ValueKind != JsonValueKind.Object)
        {
            if (root.TryGetProperty("cycleTimeSec", out var cycle) && cycle.TryGetDecimal(out var cycleVal))
            {
                result["cycleTimeSec"] = FactoryIntegrationUnitNormalizer.NormalizeMeasurement("cycleTimeSec", cycleVal, null, "sec");
            }

            if (root.TryGetProperty("cycleReferenceSec", out var refCycle) && refCycle.TryGetDecimal(out var refVal))
            {
                result["cycleReferenceSec"] = FactoryIntegrationUnitNormalizer.NormalizeMeasurement("cycleReferenceSec", refVal, null, "sec");
            }

            return result;
        }

        foreach (var property in measurements.EnumerateObject())
        {
            decimal? numeric = null;
            string? text = null;
            string? unit = null;
            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDecimal(out var num))
            {
                numeric = num;
            }
            else if (property.Value.ValueKind == JsonValueKind.Object)
            {
                if (property.Value.TryGetProperty("value", out var valueEl) && valueEl.TryGetDecimal(out var nested))
                {
                    numeric = nested;
                }

                unit = property.Value.TryGetProperty("unit", out var unitEl) ? unitEl.GetString() : null;
            }
            else
            {
                text = property.Value.GetString();
            }

            result[property.Name] = FactoryIntegrationUnitNormalizer.NormalizeMeasurement(property.Name, numeric, text, unit);
        }

        return result;
    }

    private static Dictionary<string, string> ParseBusinessAttributes(JsonElement root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("businessAttributes", out var attrs) && attrs.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in attrs.EnumerateObject())
            {
                result[property.Name] = property.Value.ToString();
            }
        }

        foreach (var key in new[] { "orderReference", "requiredQuantity", "projectedQuantity", "materialCoverageHours", "microStopCount" })
        {
            if (root.TryGetProperty(key, out var value))
            {
                result[key] = value.ToString();
            }
        }

        return result;
    }

    private static string? ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static DateTimeOffset? ReadDateTime(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
