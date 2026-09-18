using System.Text.Json;
using NevaxisZentra.Persistence.DigitalWorkforce;

namespace NevaxisZentra.Persistence.FactoryOrchestration;

public static class FactoryCaseCorrelationEngine
{
    public static string BuildCorrelationKey(FactoryDomainEvent eventData)
    {
        var parts = new List<string> { eventData.FacilityId.ToString("N") };

        if (!string.IsNullOrWhiteSpace(eventData.OrderId))
        {
            parts.Add($"outcome:order:{eventData.OrderId.ToLowerInvariant()}");
            return string.Join('|', parts).ToLowerInvariant();
        }

        var hasEntitySignal = false;

        if (!string.IsNullOrWhiteSpace(eventData.MachineId))
        {
            parts.Add($"machine:{eventData.MachineId.ToLowerInvariant()}");
            hasEntitySignal = true;
        }

        if (!string.IsNullOrWhiteSpace(eventData.MoldId))
        {
            parts.Add($"mold:{eventData.MoldId}");
            hasEntitySignal = true;
        }

        if (!string.IsNullOrWhiteSpace(eventData.MaterialId))
        {
            parts.Add($"material:{eventData.MaterialId}");
            hasEntitySignal = true;
        }

        if (!hasEntitySignal)
        {
            parts.Add(eventData.EventType.Split('.')[0]);
            parts.Add($"source:{eventData.SourceType}:{eventData.SourceId}");
        }

        return string.Join('|', parts).ToLowerInvariant();
    }

    public static string ResolvePrimaryDomain(string eventType) => eventType switch
    {
        var e when e.StartsWith("production.", StringComparison.OrdinalIgnoreCase) => FactoryOrchestrationDomains.Production,
        var e when e.StartsWith("maintenance.", StringComparison.OrdinalIgnoreCase) => FactoryOrchestrationDomains.Maintenance,
        var e when e.StartsWith("material.", StringComparison.OrdinalIgnoreCase) => FactoryOrchestrationDomains.Material,
        var e when e.StartsWith("quality.", StringComparison.OrdinalIgnoreCase) => FactoryOrchestrationDomains.Quality,
        var e when e.StartsWith("supplier.", StringComparison.OrdinalIgnoreCase) => FactoryOrchestrationDomains.Supplier,
        var e when e.StartsWith("mold.", StringComparison.OrdinalIgnoreCase) => FactoryOrchestrationDomains.Mold,
        var e when e.StartsWith("iot.", StringComparison.OrdinalIgnoreCase) => FactoryOrchestrationDomains.Iot,
        _ => FactoryOrchestrationDomains.Production,
    };

    public static IReadOnlyList<string> ResolveParticipatingDomains(string eventType, string? metadataJson)
    {
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ResolvePrimaryDomain(eventType),
        };

        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return domains.ToList();
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.TryGetProperty("impactedDomains", out var impacted) &&
                impacted.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in impacted.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        domains.Add(item.GetString() ?? string.Empty);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // ignore malformed metadata
        }

        domains.Remove(string.Empty);
        return domains.ToList();
    }

    public static bool ShouldCorrelate(DigitalWorkCase existing, FactoryDomainEvent incoming)
    {
        if (existing.Status == DigitalWorkCaseStatuses.Closed || existing.AdministrativelyClosed)
        {
            return false;
        }

        var incomingKey = BuildCorrelationKey(incoming);
        if (string.Equals(existing.CorrelationKey, incomingKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(incoming.OrderId) &&
            existing.CorrelationKey?.Contains($"outcome:order:{incoming.OrderId}", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(incoming.MachineId) &&
               existing.MetadataJson.Contains(incoming.MachineId, StringComparison.OrdinalIgnoreCase);
    }
}
