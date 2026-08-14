namespace CrevaFactOS.Persistence.ShiftWorkforce;

/// <summary>
/// Deterministic local workforce source for development pilot scenarios.
/// Does not claim to represent a real ERP integration.
/// </summary>
public sealed class LocalDevelopmentWorkforceSourceProvider : IWorkforceSourceProvider
{
    public string SourceSystemIdentifier => ShiftWorkforceSourceSystems.LocalDevelopmentPilot;

    public Task<IReadOnlyList<WorkforceSourceRecord>> FetchPersonsAsync(
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        _ = tenantId;
        _ = facilityId;
        _ = cancellationToken;

        var now = DateTimeOffset.UtcNow;
        var records = new List<WorkforceSourceRecord>
        {
            new(
                "pilot-swf-001",
                "PILOT-SWF-001",
                "Pilot Vardiya Sorumlusu 01",
                "Üretim",
                "Vardiya Sorumlusu",
                true,
                ShiftWorkforceAvailabilityStates.Available,
                "SHIFT-A",
                "CAL-2026-W33",
                now.AddHours(-2),
                "pilot-swf-001-v1",
                ShiftWorkforceFreshnessStates.Fresh,
                ["Pilot veri — gerçek ERP kaydı değildir."]),
            new(
                "pilot-swf-002",
                "PILOT-SWF-002",
                "Pilot Enjeksiyon Operatörü 01",
                "Enjeksiyon",
                "Operatör",
                true,
                ShiftWorkforceAvailabilityStates.Available,
                "SHIFT-B",
                "CAL-2026-W33",
                now.AddMinutes(-30),
                "pilot-swf-002-v1",
                ShiftWorkforceFreshnessStates.Fresh,
                []),
            new(
                "pilot-swf-003",
                "PILOT-SWF-003",
                "Pilot Enjeksiyon Operatörü 02",
                "Enjeksiyon",
                "Operatör",
                true,
                ShiftWorkforceAvailabilityStates.Leave,
                "SHIFT-B",
                "CAL-2026-W33",
                now.AddHours(-1),
                "pilot-swf-003-v2",
                ShiftWorkforceFreshnessStates.Stale,
                []),
            new(
                "pilot-swf-004",
                "PILOT-SWF-004",
                "Pilot Kalıp Teknisyeni 01",
                "Kalıphane",
                "Kalıp Teknisyeni",
                true,
                ShiftWorkforceAvailabilityStates.Available,
                "SHIFT-A",
                "CAL-2026-W33",
                now.AddHours(-4),
                "pilot-swf-004-v1",
                ShiftWorkforceFreshnessStates.Fresh,
                []),
            new(
                "pilot-swf-005",
                "PILOT-SWF-005",
                "Pilot Kalite Operatörü 01",
                "Kalite",
                "Kalite Operatörü",
                true,
                ShiftWorkforceAvailabilityStates.Available,
                "SHIFT-C",
                "CAL-2026-W33",
                now.AddHours(-3),
                "pilot-swf-005-v1",
                ShiftWorkforceFreshnessStates.Fresh,
                ["Bazı yetkinlik doğrulamaları eksik."]),
            new(
                "pilot-swf-006",
                "PILOT-SWF-006",
                "Pilot Bakım Teknisyeni 01",
                "Bakım",
                "Bakım Teknisyeni",
                false,
                ShiftWorkforceAvailabilityStates.Unknown,
                null,
                null,
                now.AddDays(-14),
                "pilot-swf-006-v1",
                ShiftWorkforceFreshnessStates.Fresh,
                []),
        };

        return Task.FromResult<IReadOnlyList<WorkforceSourceRecord>>(records);
    }
}
