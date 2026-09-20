using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.ScenarioRecovery;

public static class ScenarioRecoveryFixtureSeeder
{
    public static async Task RetirePostInterventionEventsAsync(
        NevaxisZentraDbContext db,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.CanonicalProductionEvents
            .Where(e => e.LineId == lineId && e.Source == "ultra-d-post-intervention")
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        var retireTime = DateTimeOffset.UtcNow.AddDays(-2);
        foreach (var productionEvent in existing)
        {
            productionEvent.OccurredAtUtc = retireTime;
            productionEvent.ReceivedAtUtc = retireTime;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<int> SeedPostInterventionEventsAsync(
        NevaxisZentraDbContext db,
        Guid tenantId,
        Guid facilityId,
        Guid lineId,
        decimal? targetGoodThroughputPerHour = null,
        CancellationToken cancellationToken = default)
    {
        var stations = await db.ProductionStations.AsNoTracking()
            .Where(s => s.LineId == lineId)
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync(cancellationToken);

        var assembly = stations.FirstOrDefault(s => s.Code == ThroughputIntelligenceFixtureCodes.AssemblyStationCode)
            ?? stations.First();
        var welding = stations.FirstOrDefault(s => s.Code == ThroughputIntelligenceFixtureCodes.WeldingStationCode)
            ?? stations.Last();

        var now = DateTimeOffset.UtcNow;
        var eventCount = targetGoodThroughputPerHour.HasValue
            ? (int)Math.Ceiling(Math.Max(0m, targetGoodThroughputPerHour.Value - 190m) + 50m)
            : 400;
        eventCount = Math.Max(eventCount, 400);
        var events = new List<CanonicalProductionEvent>();
        for (var i = 0; i < eventCount; i++)
        {
            var station = i % 3 == 0 ? welding : assembly;
            events.Add(new CanonicalProductionEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                LineId = lineId,
                StationId = station.Id,
                EventType = MixedProductionEventTypes.Good,
                Source = "ultra-d-post-intervention",
                IdempotencyKey = $"ultra-d-post:{lineId:N}:{now:yyyyMMddHHmmss}:{i}",
                OccurredAtUtc = now.AddMinutes(-20 + (i * 0.1)),
                ReceivedAtUtc = now.AddMinutes(-20 + (i * 0.1)),
            });
        }

        db.CanonicalProductionEvents.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
        return events.Count;
    }
}
