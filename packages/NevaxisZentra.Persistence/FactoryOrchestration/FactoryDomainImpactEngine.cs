using System.Text.Json;

namespace NevaxisZentra.Persistence.FactoryOrchestration;

public sealed record FactoryImpactNode(string Domain, string Summary, string Severity);

public static class FactoryDomainImpactEngine
{
    public static IReadOnlyList<FactoryImpactNode> Evaluate(FactoryDomainEvent eventData)
    {
        var impacts = new List<FactoryImpactNode>
        {
            new(FactoryCaseCorrelationEngine.ResolvePrimaryDomain(eventData.EventType), eventData.Title, eventData.Severity),
        };

        if (!string.IsNullOrWhiteSpace(eventData.MachineId))
        {
            impacts.Add(new FactoryImpactNode(
                FactoryOrchestrationDomains.Production,
                $"Makine {eventData.MachineId} üretim kapasitesini etkiliyor",
                eventData.Severity));
            impacts.Add(new FactoryImpactNode(
                FactoryOrchestrationDomains.Maintenance,
                $"Makine {eventData.MachineId} bakım durumu izleniyor",
                eventData.Severity));
        }

        if (!string.IsNullOrWhiteSpace(eventData.OrderId))
        {
            impacts.Add(new FactoryImpactNode(
                FactoryOrchestrationDomains.Planning,
                $"Sipariş {eventData.OrderId} termin riski",
                eventData.Severity));
        }

        if (!string.IsNullOrWhiteSpace(eventData.MaterialId))
        {
            impacts.Add(new FactoryImpactNode(
                FactoryOrchestrationDomains.Material,
                $"Malzeme {eventData.MaterialId} kısıtı",
                eventData.Severity));
        }

        if (!string.IsNullOrWhiteSpace(eventData.MoldId))
        {
            impacts.Add(new FactoryImpactNode(
                FactoryOrchestrationDomains.Mold,
                $"Kalıp {eventData.MoldId} yaşam döngüsü etkisi",
                eventData.Severity));
        }

        if (eventData.EventType.Contains("supplier", StringComparison.OrdinalIgnoreCase))
        {
            impacts.Add(new FactoryImpactNode(
                FactoryOrchestrationDomains.Supplier,
                "Tedarikçi/teslimat riski",
                eventData.Severity));
        }

        if (eventData.EventType.Contains("quality", StringComparison.OrdinalIgnoreCase))
        {
            impacts.Add(new FactoryImpactNode(
                FactoryOrchestrationDomains.Quality,
                "Kalite hold / NCR etkisi",
                eventData.Severity));
        }

        return impacts
            .GroupBy(i => i.Domain, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public static string ToJson(IReadOnlyList<FactoryImpactNode> impacts) =>
        JsonSerializer.Serialize(impacts.Select(i => new { domain = i.Domain, summary = i.Summary, severity = i.Severity }));
}
