using System.Text.Json;
using NevaxisFactOS.Persistence.Integration.FactoryEventSpine;

namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public static class FactoryIntelligenceSignalExtractor
{
    public static IReadOnlyList<FactorySignalWindowInput> FromCanonicalEvent(
        Guid tenantId,
        Guid facilityId,
        IntegrationCanonicalFactoryEvent canonicalEvent,
        string dataQuality = FactoryIntelligenceDataQualityStates.Good)
    {
        if (string.IsNullOrWhiteSpace(canonicalEvent.NormalizedPayloadJson))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(canonicalEvent.NormalizedPayloadJson);
        var root = doc.RootElement;
        var machine = ReadString(root, "machineCode") ?? ReadString(root, "machineId") ?? "unknown";
        var observedAt = canonicalEvent.OccurredAtUtc;
        var references = new List<string> { canonicalEvent.Id.ToString("N") };
        var signals = new List<FactorySignalWindowInput>();

        if (TryReadMeasurement(root, "cycleTimeSec", out var cycle))
        {
            signals.Add(new FactorySignalWindowInput(
                tenantId, facilityId, "machine", machine,
                FactoryIntelligenceSignalTypes.CycleTimeDrift, cycle, observedAt, dataQuality, references));
        }

        if (root.TryGetProperty("microStopCount", out var microStops) && microStops.TryGetInt32(out var microCount))
        {
            signals.Add(new FactorySignalWindowInput(
                tenantId, facilityId, "machine", machine,
                FactoryIntelligenceSignalTypes.MicroStopFrequency, microCount, observedAt, dataQuality, references));
        }

        if (root.TryGetProperty("materialCoverageHours", out var coverage) && coverage.TryGetDecimal(out var hours))
        {
            signals.Add(new FactorySignalWindowInput(
                tenantId, facilityId, "material", ReadString(root, "materialCode") ?? machine,
                FactoryIntelligenceSignalTypes.MaterialCoverageHours, hours, observedAt, dataQuality, references));
        }

        if (root.TryGetProperty("rejectTrendIndex", out var reject) && reject.TryGetDecimal(out var rejectVal))
        {
            signals.Add(new FactorySignalWindowInput(
                tenantId, facilityId, "machine", machine,
                FactoryIntelligenceSignalTypes.ScrapRejectTrend, rejectVal, observedAt, dataQuality, references));
        }

        if (root.TryGetProperty("maintenanceWarningIndex", out var maintenance) && maintenance.TryGetDecimal(out var maintenanceVal))
        {
            signals.Add(new FactorySignalWindowInput(
                tenantId, facilityId, "machine", machine,
                FactoryIntelligenceSignalTypes.MaintenanceWarning, maintenanceVal, observedAt, dataQuality, references));
        }

        return signals;
    }

    private static bool TryReadMeasurement(JsonElement root, string key, out decimal value)
    {
        value = 0;
        if (!root.TryGetProperty("measurements", out var measurements)
            || !measurements.TryGetProperty(key, out var measurement))
        {
            return false;
        }

        if (measurement.ValueKind == JsonValueKind.Number && measurement.TryGetDecimal(out value))
        {
            return true;
        }

        if (measurement.TryGetProperty("value", out var nested) && nested.TryGetDecimal(out value))
        {
            return true;
        }

        return false;
    }

    private static string? ReadString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) ? value.GetString() : null;
}
