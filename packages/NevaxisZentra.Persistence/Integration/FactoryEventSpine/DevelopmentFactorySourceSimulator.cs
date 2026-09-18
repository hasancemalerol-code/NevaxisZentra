using System.Text.Json;

namespace NevaxisZentra.Persistence.Integration.FactoryEventSpine;

public static class DevelopmentFactorySourceSimulator
{
    public static FactoryEventIngestionRequest CreateCycleDegradationEvent(Guid connectorId, string sourceEventId) =>
        new(
            connectorId,
            sourceEventId,
            JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["eventType"] = "production.performance-degradation",
                ["sourceEventId"] = sourceEventId,
                ["occurredAt"] = DateTimeOffset.UtcNow.ToString("O"),
                ["machineCode"] = "INJ-004",
                ["orderReference"] = "ORD-8742",
                ["severity"] = "critical",
                ["title"] = "INJ-004 çevrim performansı bozuldu",
                ["description"] = "Referans 27.5 sn, gerçek 32.1 sn",
                ["measurements"] = new Dictionary<string, object>
                {
                    ["cycleTimeSec"] = new { value = 32.1m, unit = "sec" },
                    ["cycleReferenceSec"] = new { value = 27.5m, unit = "sec" },
                },
                ["microStopCount"] = 14,
                ["requiredQuantity"] = 1200,
                ["projectedQuantity"] = 980,
            }));

    public static FactoryEventIngestionRequest CreateMicroStopEvent(Guid connectorId, string sourceEventId) =>
        new(
            connectorId,
            sourceEventId,
            JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["eventType"] = "production.micro-stop",
                ["sourceEventId"] = sourceEventId,
                ["occurredAt"] = DateTimeOffset.UtcNow.ToString("O"),
                ["machineCode"] = "INJ-004",
                ["orderReference"] = "ORD-8742",
                ["severity"] = "high",
                ["title"] = "INJ-004 mikro duruş sinyali",
                ["description"] = "14 mikro duruş tespit edildi",
                ["sequence"] = 2,
            }));

    public static FactoryEventIngestionRequest CreateMaterialCoverageRiskEvent(Guid connectorId, string sourceEventId) =>
        new(
            connectorId,
            sourceEventId,
            JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["eventType"] = "material.shortage",
                ["sourceEventId"] = sourceEventId,
                ["occurredAt"] = DateTimeOffset.UtcNow.ToString("O"),
                ["machineCode"] = "INJ-004",
                ["orderReference"] = "ORD-8742",
                ["materialCode"] = "PP-GRANULE",
                ["severity"] = "high",
                ["title"] = "ORD-8742 malzeme kapsaması daraldı",
                ["description"] = "Malzeme kapsaması 5.8 saat",
                ["materialCoverageHours"] = 5.8,
                ["sequence"] = 3,
            }));
}
