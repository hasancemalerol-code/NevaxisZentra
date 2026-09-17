using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.ThroughputIntelligence;

namespace NevaxisFactOS.Persistence.ThroughputOptimizer;

public static class ThroughputOptimizerFixtureSeeder
{
    public static async Task RetirePostInterventionEventsAsync(
        NevaxisFactOSDbContext db,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.CanonicalProductionEvents
            .Where(e => e.LineId == lineId && e.Source == "mp3-post-intervention")
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
        NevaxisFactOSDbContext db,
        Guid tenantId,
        Guid facilityId,
        Guid lineId,
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
        var events = new List<CanonicalProductionEvent>();
        for (var i = 0; i < 180; i++)
        {
            var station = i % 4 == 0 ? welding : assembly;
            events.Add(new CanonicalProductionEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                LineId = lineId,
                StationId = station.Id,
                EventType = MixedProductionEventTypes.Good,
                Source = "mp3-post-intervention",
                IdempotencyKey = $"mp3-post:{lineId:N}:{now:yyyyMMddHHmmss}:{i}",
                OccurredAtUtc = now.AddMinutes(-25 + (i * 0.12)),
                ReceivedAtUtc = now.AddMinutes(-25 + (i * 0.12)),
            });
        }

        db.CanonicalProductionEvents.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
        return events.Count;
    }
}
