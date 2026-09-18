using NevaxisZentra.Persistence.FactoryOrchestration;

namespace NevaxisZentra.Persistence.FactoryOutcomeExecution;

public static class FactoryOutcomeEventQualificationEngine
{
    private static readonly HashSet<string> SignificantEventPrefixes =
    [
        "production.",
        "maintenance.",
        "material.",
        "quality.",
        "mold.",
        "supplier.",
        "iot.",
        "coordinator.",
    ];

    public static FactoryEventQualificationResult Qualify(
        FactoryDomainEvent eventData,
        DigitalWorkforce.DigitalWorkCase? existingCase)
    {
        if (string.Equals(eventData.Severity, "info", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(eventData.MachineId) &&
            string.IsNullOrWhiteSpace(eventData.OrderId) &&
            string.IsNullOrWhiteSpace(eventData.MaterialId))
        {
            return new FactoryEventQualificationResult(false, false, "Operasyonel önem taşımayan bilgi olayı.");
        }

        if (!SignificantEventPrefixes.Any(prefix =>
                eventData.EventType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return new FactoryEventQualificationResult(false, false, "Desteklenmeyen olay kategorisi.");
        }

        if (existingCase is not null &&
            existingCase.Status != DigitalWorkforce.DigitalWorkCaseStatuses.Closed &&
            !existingCase.AdministrativelyClosed)
        {
            return new FactoryEventQualificationResult(true, true, null);
        }

        var hasOutcomeSignal =
            !string.IsNullOrWhiteSpace(eventData.MachineId) ||
            !string.IsNullOrWhiteSpace(eventData.OrderId) ||
            !string.IsNullOrWhiteSpace(eventData.MaterialId) ||
            !string.IsNullOrWhiteSpace(eventData.MoldId) ||
            string.Equals(eventData.Severity, "critical", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(eventData.Severity, "high", StringComparison.OrdinalIgnoreCase);

        if (!hasOutcomeSignal)
        {
            return new FactoryEventQualificationResult(false, false, "Sonuç riski sinyali yok.");
        }

        return new FactoryEventQualificationResult(true, false, null);
    }
}
