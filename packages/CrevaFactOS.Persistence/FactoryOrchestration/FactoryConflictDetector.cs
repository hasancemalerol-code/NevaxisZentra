namespace CrevaFactOS.Persistence.FactoryOrchestration;

public sealed record FactoryDataConflict(
    string Code,
    string Message,
    string SourceA,
    string SourceB);

public static class FactoryConflictDetector
{
    public static IReadOnlyList<FactoryDataConflict> Detect(FactoryDomainEvent eventData, string? existingMetadataJson)
    {
        var conflicts = new List<FactoryDataConflict>();

        if (string.IsNullOrWhiteSpace(existingMetadataJson))
        {
            return conflicts;
        }

        if (existingMetadataJson.Contains("running", StringComparison.OrdinalIgnoreCase) &&
            eventData.EventType.Contains("stopped", StringComparison.OrdinalIgnoreCase))
        {
            conflicts.Add(new FactoryDataConflict(
                "machine-state-mismatch",
                "IoT/üretim durumu çakışıyor: bir kaynak çalışıyor, diğeri durdu diyor.",
                "production",
                "iot"));
        }

        if (existingMetadataJson.Contains("available", StringComparison.OrdinalIgnoreCase) &&
            eventData.EventType.Contains("material", StringComparison.OrdinalIgnoreCase))
        {
            conflicts.Add(new FactoryDataConflict(
                "material-state-mismatch",
                "Malzeme durumu çakışıyor: ERP/operatör ile risk sinyali uyuşmuyor.",
                "erp-projection",
                "material-risk"));
        }

        return conflicts;
    }
}
