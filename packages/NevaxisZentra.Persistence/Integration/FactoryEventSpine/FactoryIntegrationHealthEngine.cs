using NevaxisZentra.Persistence.Integration;

namespace NevaxisZentra.Persistence.Integration.FactoryEventSpine;

public static class FactoryIntegrationHealthEngine
{
    public static (string HealthState, string Summary) Evaluate(
        IntegrationConnector connector,
        int consecutiveFailures,
        TimeSpan? ingestionDelay)
    {
        if (!connector.Enabled)
        {
            return (FactoryIntegrationHealthStates.Paused, "Bağlantı duraklatıldı.");
        }

        if (string.Equals(connector.HealthStatus, IntegrationHealthStatuses.Misconfigured, StringComparison.OrdinalIgnoreCase))
        {
            return (FactoryIntegrationHealthStates.Misconfigured, connector.FailureStatus ?? "Yapılandırma hatası.");
        }

        if (connector.LastSuccessfulSyncAtUtc is null && connector.LastAttemptedSyncAtUtc is not null)
        {
            return (FactoryIntegrationHealthStates.Offline, "Henüz başarılı veri alınamadı.");
        }

        if (consecutiveFailures >= 3)
        {
            return (FactoryIntegrationHealthStates.Degraded, $"Ardışık {consecutiveFailures} başarısız deneme.");
        }

        if (ingestionDelay.HasValue && ingestionDelay.Value > TimeSpan.FromMinutes(30))
        {
            return (FactoryIntegrationHealthStates.Degraded, $"Veri gecikmesi: {Math.Round(ingestionDelay.Value.TotalMinutes)} dk");
        }

        if (connector.LastFailureAtUtc.HasValue &&
            connector.LastSuccessfulSyncAtUtc.HasValue &&
            connector.LastFailureAtUtc > connector.LastSuccessfulSyncAtUtc)
        {
            return (FactoryIntegrationHealthStates.Degraded, connector.FailureStatus ?? "Son deneme başarısız.");
        }

        return (FactoryIntegrationHealthStates.Healthy, "Veri akışı sağlıklı.");
    }
}
