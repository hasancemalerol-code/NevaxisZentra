using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.ThroughputIntelligence;

public static class ThroughputIntelligenceFixtureSeeder
{
    public static async Task<ThroughputIntelligenceSeedResult> SeedMixLine01Mp2ExtensionAsync(
        NevaxisFactOSDbContext db,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        var line = await db.ProductionLines.AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.TenantId == tenantId && l.FacilityId == facilityId && l.Code == MixedProductionFixtureCodes.LineCode,
                cancellationToken)
            ?? throw new ThroughputIntelligenceException(
                ThroughputIntelligenceFailureCodes.NotFound, "MIX-LINE-01 must be seeded before MP2 extension.");

        var stations = await db.ProductionStations
            .Where(s => s.LineId == line.Id)
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync(cancellationToken);

        var resources = await db.ManufacturingResources
            .Where(r => stations.Select(s => s.Id).Contains(r.StationId))
            .ToListAsync(cancellationToken);

        var stationPairs = stations
            .Select(s => (s, resources.First(r => r.StationId == s.Id)))
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var eventsSeeded = 0;

        if (!await db.LineThroughputTargets.AnyAsync(
            t => t.LineId == line.Id && t.TenantId == tenantId, cancellationToken))
        {
            db.LineThroughputTargets.Add(new LineThroughputTarget
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                LineId = line.Id,
                TargetGoodUnitsPerHour = 670m,
                RequiredGoodQuantity = 5360,
                AvailableProductionHours = 8m,
                TargetCycleSeconds = 5.37m,
                OrderReference = "ORD-MIX-001",
                IsActive = true,
                EffectiveFromUtc = now.AddDays(-1),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        var assemblyStation = stations.First(s => s.Code == ThroughputIntelligenceFixtureCodes.AssemblyStationCode);
        var weldingStation = stations.First(s => s.Code == ThroughputIntelligenceFixtureCodes.WeldingStationCode);

        var skillsSeeded = await SeedOperatorSkillsAsync(
            db, tenantId, facilityId, assemblyStation, weldingStation, stations, now, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        var existingMp2Marker = await db.CanonicalProductionEvents.AnyAsync(
            e => e.LineId == line.Id && e.Source == "mp2-throughput-fixture", cancellationToken);

        if (!existingMp2Marker)
        {
            eventsSeeded = await SeedThroughputEventHistoryAsync(
                db, tenantId, facilityId, line.Id, stationPairs, cancellationToken);
        }

        return new ThroughputIntelligenceSeedResult(
            line.Id,
            eventsSeeded,
            skillsSeeded.Skills,
            skillsSeeded.Qualifications,
            existingMp2Marker
                ? "MP2 extension already seeded."
                : $"MP2 throughput fixture seeded with {eventsSeeded} events.");
    }

    public static async Task<int> EnsureMixLine01Mp2EventHistoryAsync(
        NevaxisFactOSDbContext db,
        Guid tenantId,
        Guid facilityId,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        var existingCount = await db.CanonicalProductionEvents.CountAsync(
            e => e.LineId == lineId && e.Source == "mp2-throughput-fixture",
            cancellationToken);

        if (existingCount == 0)
        {
            var stations = await db.ProductionStations
                .Where(s => s.LineId == lineId)
                .OrderBy(s => s.SequenceOrder)
                .ToListAsync(cancellationToken);

            var resources = await db.ManufacturingResources
                .Where(r => stations.Select(s => s.Id).Contains(r.StationId))
                .ToListAsync(cancellationToken);

            var stationPairs = stations
                .Select(s => (s, resources.First(r => r.StationId == s.Id)))
                .ToList();

            await SeedThroughputEventHistoryAsync(
                db, tenantId, facilityId, lineId, stationPairs, cancellationToken);
        }

        return await RefreshMixLine01Mp2EventTimestampsAsync(db, lineId, cancellationToken);
    }

    public static async Task<int> RefreshMixLine01Mp2EventTimestampsAsync(
        NevaxisFactOSDbContext db,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        var events = await db.CanonicalProductionEvents
            .Where(e => e.LineId == lineId && e.Source == "mp2-throughput-fixture")
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            return 0;
        }

        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-55);
        for (var i = 0; i < events.Count; i++)
        {
            var timestamp = baseTime.AddSeconds(i * 5.5);
            events[i].OccurredAtUtc = timestamp;
            events[i].ReceivedAtUtc = timestamp;
        }

        await db.SaveChangesAsync(cancellationToken);
        return events.Count;
    }

    private static async Task<(int Skills, int Qualifications)> SeedOperatorSkillsAsync(
        NevaxisFactOSDbContext db,
        Guid tenantId,
        Guid facilityId,
        ProductionStation assemblyStation,
        ProductionStation weldingStation,
        IReadOnlyList<ProductionStation> stations,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var skillDefs = new[]
        {
            (ThroughputIntelligenceFixtureCodes.OperatorAssembly, "Assembly Operator 1", 3),
            (ThroughputIntelligenceFixtureCodes.OperatorAssembly2, "Assembly Operator 2", 3),
            (ThroughputIntelligenceFixtureCodes.OperatorPackaging, "Packaging Operator", 2),
        };

        var skills = 0;
        foreach (var (code, name, level) in skillDefs)
        {
            if (await db.OperatorSkillProfiles.AnyAsync(
                s => s.TenantId == tenantId && s.FacilityId == facilityId && s.OperatorCode == code,
                cancellationToken))
            {
                continue;
            }

            db.OperatorSkillProfiles.Add(new OperatorSkillProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                OperatorCode = code,
                DisplayName = name,
                SkillLevel = level,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            skills++;
        }

        var qualDefs = new List<(string Op, Guid StationId, string OpCode, DateTimeOffset? Expires)>
        {
            (ThroughputIntelligenceFixtureCodes.OperatorAssembly, assemblyStation.Id, "OP-30", null),
            (ThroughputIntelligenceFixtureCodes.OperatorAssembly2, assemblyStation.Id, "OP-30", null),
            (ThroughputIntelligenceFixtureCodes.OperatorAssembly2, weldingStation.Id, "OP-40", null),
            (ThroughputIntelligenceFixtureCodes.OperatorPackaging, stations.First(s => s.Code == "ST-60").Id, "OP-60", null),
            ("OP-EXPIRED", assemblyStation.Id, "OP-30", now.AddDays(-1)),
        };

        var qualifications = 0;
        foreach (var (op, stationId, opCode, expires) in qualDefs)
        {
            if (await db.OperatorStationQualifications.AnyAsync(
                q => q.TenantId == tenantId && q.FacilityId == facilityId
                    && q.OperatorCode == op && q.StationId == stationId,
                cancellationToken))
            {
                continue;
            }

            db.OperatorStationQualifications.Add(new OperatorStationQualification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                OperatorCode = op,
                StationId = stationId,
                OperationCode = opCode,
                QualificationStatus = expires.HasValue && expires <= now ? "expired" : "qualified",
                ExpiresAtUtc = expires,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            qualifications++;
        }

        return (skills, qualifications);
    }

    private static async Task<int> SeedThroughputEventHistoryAsync(
        NevaxisFactOSDbContext db,
        Guid tenantId,
        Guid facilityId,
        Guid lineId,
        List<(ProductionStation Station, ManufacturingResource Resource)> stations,
        CancellationToken cancellationToken)
    {
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(tenantId, "fixture", Guid.Empty));
        var service = new MixedProductionService(db, new TenantDatabaseSession(db, accessor));
        var baseTime = DateTimeOffset.UtcNow.AddHours(-1);
        var correlation = $"mp2-throughput-{facilityId:N}";
        var eventCount = 0;

        var injection = stations[0];
        var buffer = stations[1];
        var assembly = stations[2];
        var welding = stations[3];
        var quality = stations[4];
        var packaging = stations[5];

        async Task RecordAsync(
            (ProductionStation Station, ManufacturingResource Resource) pair,
            string eventType,
            decimal? qty = null,
            string? operatorCode = null,
            string suffix = "")
        {
            await service.RecordEventAsync(new RecordProductionEventRequest(
                facilityId,
                lineId,
                pair.Station.Id,
                pair.Resource.Id,
                eventType,
                $"OP-{pair.Station.SequenceOrder}",
                "ORD-MIX-001",
                MixedProductionFixtureCodes.ProductCode,
                operatorCode,
                "SHIFT-A",
                qty,
                "pcs",
                null,
                correlation,
                "mp2-throughput-fixture",
                $"{correlation}-{eventCount}{suffix}",
                $"{correlation}-{eventCount}{suffix}",
                baseTime.AddSeconds(eventCount * 5.5)),
                tenantId,
                cancellationToken);
            eventCount++;
        }

        for (var cycle = 0; cycle < 60; cycle++)
        {
            await RecordAsync(injection, MixedProductionEventTypes.CycleStart, suffix: "-inj-s");
            await RecordAsync(injection, MixedProductionEventTypes.CycleEnd, suffix: "-inj-e");
            await RecordAsync(injection, MixedProductionEventTypes.Good, 1, suffix: "-inj-g");
            await RecordAsync(buffer, MixedProductionEventTypes.BufferChange, 1, suffix: "-buf+");

            if (cycle % 8 == 0)
            {
                await RecordAsync(assembly, MixedProductionEventTypes.OperatorWait, operatorCode: ThroughputIntelligenceFixtureCodes.OperatorAssembly, suffix: "-asm-wait");
            }

            await RecordAsync(
                assembly,
                MixedProductionEventTypes.OperatorStart,
                operatorCode: ThroughputIntelligenceFixtureCodes.OperatorAssembly,
                suffix: "-asm-start");
            await RecordAsync(
                assembly,
                MixedProductionEventTypes.OperatorComplete,
                operatorCode: ThroughputIntelligenceFixtureCodes.OperatorAssembly,
                suffix: "-asm-complete");
            await RecordAsync(
                assembly,
                MixedProductionEventTypes.Good,
                1,
                operatorCode: ThroughputIntelligenceFixtureCodes.OperatorAssembly,
                suffix: "-asm-good");
            await RecordAsync(buffer, MixedProductionEventTypes.BufferChange, 0, suffix: "-buf-");

            if (cycle % 5 == 0)
            {
                await RecordAsync(welding, MixedProductionEventTypes.Stop, suffix: "-weld-stop");
                await RecordAsync(welding, MixedProductionEventTypes.Run, suffix: "-weld-run");
            }

            await RecordAsync(welding, MixedProductionEventTypes.CycleStart, suffix: "-weld-s");
            await RecordAsync(welding, MixedProductionEventTypes.CycleEnd, suffix: "-weld-e");
            await RecordAsync(welding, MixedProductionEventTypes.Good, 1, suffix: "-weld-g");

            if (cycle % 12 == 0)
            {
                await RecordAsync(quality, MixedProductionEventTypes.Reject, 1, suffix: "-qc-rej");
            }
            else
            {
                await RecordAsync(quality, MixedProductionEventTypes.Good, 1, suffix: "-qc-g");
            }

            if (cycle % 20 == 0)
            {
                await RecordAsync(assembly, MixedProductionEventTypes.MaterialWait, suffix: "-mat-wait");
            }

            if (cycle % 15 == 0)
            {
                await RecordAsync(welding, MixedProductionEventTypes.Starved, suffix: "-weld-starved");
            }
        }

        return eventCount;
    }
}
