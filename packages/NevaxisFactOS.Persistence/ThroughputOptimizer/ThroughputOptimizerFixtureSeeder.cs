using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.ThroughputIntelligence;

namespace NevaxisFactOS.Persistence.ThroughputOptimizer;

public static class ThroughputOptimizerFixtureSeeder
{
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
        for (var i = 0; i < 40; i++)
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
                Source = "mp3-post-intervention",
                IdempotencyKey = $"mp3-post:{lineId:N}:{i}:{now:yyyyMMdd}",
                OccurredAtUtc = now.AddMinutes(-30 + i),
                ReceivedAtUtc = now.AddMinutes(-30 + i),
            });
        }

        var markerExists = await db.CanonicalProductionEvents.AnyAsync(
            e => e.LineId == lineId && e.Source == "mp3-post-intervention", cancellationToken);
        if (markerExists)
        {
            return 0;
        }

        db.CanonicalProductionEvents.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
        return events.Count;
    }
}
