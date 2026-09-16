using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.MixedProduction;

public static class MixedProductionFixtureSeeder
{
    public static async Task<MixedProductionSeedResult> SeedMixLine01Async(
        NevaxisFactOSDbContext db,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.ProductionLines.AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.TenantId == tenantId && l.FacilityId == facilityId && l.Code == MixedProductionFixtureCodes.LineCode,
                cancellationToken);

        if (existing is not null)
        {
            return new MixedProductionSeedResult(
                existing.Id,
                existing.Code,
                await db.ProductionStations.CountAsync(s => s.LineId == existing.Id, cancellationToken),
                await db.ManufacturingResources.CountAsync(
                    r => r.TenantId == tenantId && r.FacilityId == facilityId,
                    cancellationToken),
                await db.CanonicalProductionEvents.CountAsync(
                    e => e.TenantId == tenantId && e.FacilityId == facilityId && e.LineId == existing.Id,
                    cancellationToken),
                "MIX-LINE-01 already seeded.");
        }

        var now = DateTimeOffset.UtcNow;
        var areaId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var routingId = Guid.NewGuid();
        var correlation = $"mix-line-01-{facilityId:N}";

        db.ManufacturingAreas.Add(new ManufacturingArea
        {
            Id = areaId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = MixedProductionFixtureCodes.AreaCode,
            Name = "Mixed Production Area",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        db.ProductionLines.Add(new ProductionLine
        {
            Id = lineId,
            TenantId = tenantId,
            FacilityId = facilityId,
            AreaId = areaId,
            Code = MixedProductionFixtureCodes.LineCode,
            Name = "Mixed Production Line 01",
            CurrentOrderReference = "ORD-MIX-001",
            CurrentProductCode = MixedProductionFixtureCodes.ProductCode,
            CurrentShiftCode = "SHIFT-A",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        var stationDefs = new (string Code, string Name, string Kind, int Seq, string ResourceType, string ResourceCode, int? Capacity)[]
        {
            ("ST-10", "Injection", MixedProductionStationKinds.Process, 10, MixedProductionResourceTypes.Machine, "INJ-001", null),
            ("BUF-20", "Intermediate Buffer", MixedProductionStationKinds.Buffer, 20, MixedProductionResourceTypes.Buffer, "BUF-20", 40),
            ("ST-30", "Human Assembly", MixedProductionStationKinds.Process, 30, MixedProductionResourceTypes.Human, "OP-ASM-01", null),
            ("ST-40", "Welding/Punta", MixedProductionStationKinds.Process, 40, MixedProductionResourceTypes.Robot, "WELD-01", null),
            ("ST-50", "Quality/Test", MixedProductionStationKinds.Quality, 50, MixedProductionResourceTypes.QualityStation, "QC-01", null),
            ("ST-60", "Packaging", MixedProductionStationKinds.Packaging, 60, MixedProductionResourceTypes.Human, "OP-PKG-01", null),
        };

        var stations = new List<(ProductionStation Station, ManufacturingResource Resource)>();
        foreach (var (code, name, kind, seq, resourceType, resourceCode, capacity) in stationDefs)
        {
            var stationId = Guid.NewGuid();
            var resourceId = Guid.NewGuid();
            var station = new ProductionStation
            {
                Id = stationId,
                TenantId = tenantId,
                FacilityId = facilityId,
                LineId = lineId,
                Code = code,
                Name = name,
                StationKind = kind,
                SequenceOrder = seq,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            var resource = new ManufacturingResource
            {
                Id = resourceId,
                TenantId = tenantId,
                FacilityId = facilityId,
                StationId = stationId,
                ResourceType = resourceType,
                Code = resourceCode,
                Name = name,
                MachineRegistryCode = resourceType == MixedProductionResourceTypes.Machine ? resourceCode : null,
                BufferCapacity = capacity,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            db.ProductionStations.Add(station);
            db.ManufacturingResources.Add(resource);
            stations.Add((station, resource));

            if (resourceType == MixedProductionResourceTypes.Buffer && capacity.HasValue)
            {
                db.BufferResourceStates.Add(new BufferResourceState
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    ResourceId = resourceId,
                    Capacity = capacity.Value,
                    CurrentQuantity = 0,
                    UpdatedAtUtc = now,
                });
            }
        }

        db.ProductRoutings.Add(new ProductRouting
        {
            Id = routingId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductCode = MixedProductionFixtureCodes.ProductCode,
            ProductName = "Mixed Assembly Product",
            Revision = MixedProductionFixtureCodes.RoutingRevision,
            EffectiveFromUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        var ops = new[]
        {
            (10, "OP-10", "Injection"),
            (20, "OP-20", "Buffer Transfer"),
            (30, "OP-30", "Manual Assembly"),
            (40, "OP-40", "Welding"),
            (50, "OP-50", "Quality Test"),
            (60, "OP-60", "Packaging"),
        };

        for (var i = 0; i < ops.Length; i++)
        {
            var (seq, opCode, opName) = ops[i];
            db.RoutingOperations.Add(new RoutingOperation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                RoutingId = routingId,
                StationId = stations[i].Station.Id,
                SequenceOrder = seq,
                OperationCode = opCode,
                OperationName = opName,
                CreatedAtUtc = now,
            });

            db.DigitalWorkInstructions.Add(new DigitalWorkInstruction
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                OperationCode = opCode,
                StationCode = stations[i].Station.Code,
                ProductCode = MixedProductionFixtureCodes.ProductCode,
                Revision = MixedProductionFixtureCodes.RoutingRevision,
                Title = $"{opName} Work Instruction",
                StepsJson = $"[\"Prepare {opName}\",\"Execute {opName}\",\"Confirm quality\"]",
                SafetyNotes = "Follow station safety protocol.",
                EffectiveFromUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        db.ConnectorResourceMappings.Add(new ConnectorResourceMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ConnectorCode = "OPC-INJ-001",
            MappingKind = "opc-ua",
            StationId = stations[0].Station.Id,
            ResourceId = stations[0].Resource.Id,
            OperationCode = "OP-10",
            ExternalReference = "ns=2;s=INJ-001/Cycle",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        await db.SaveChangesAsync(cancellationToken);

        var acceptanceEvents = await RunAcceptanceSequenceAsync(
            db, tenantId, facilityId, lineId, stations, correlation, cancellationToken);

        return new MixedProductionSeedResult(
            lineId,
            MixedProductionFixtureCodes.LineCode,
            stations.Count,
            stations.Count,
            acceptanceEvents,
            "MIX-LINE-01 seeded with acceptance sequence.");
    }

    public static async Task<int> RunAcceptanceSequenceAsync(
        NevaxisFactOSDbContext db,
        Guid tenantId,
        Guid facilityId,
        Guid lineId,
        IReadOnlyList<(ProductionStation Station, ManufacturingResource Resource)> stations,
        string correlation,
        CancellationToken cancellationToken)
    {
        var service = new MixedProductionService(db);
        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-30);
        var eventCount = 0;

        async Task RecordAsync(
            int stationIndex,
            string eventType,
            decimal? qty = null,
            string? operatorCode = null,
            string suffix = "")
        {
            var (station, resource) = stations[stationIndex];
            await service.RecordEventAsync(new RecordProductionEventRequest(
                facilityId,
                lineId,
                station.Id,
                resource.Id,
                eventType,
                $"OP-{stations[stationIndex].Station.SequenceOrder}",
                "ORD-MIX-001",
                MixedProductionFixtureCodes.ProductCode,
                operatorCode,
                "SHIFT-A",
                qty,
                "pcs",
                null,
                correlation,
                "mixed-line-fixture",
                $"{correlation}-{eventCount}{suffix}",
                $"{correlation}-{eventCount}{suffix}",
                baseTime.AddSeconds(eventCount * 15)),
                tenantId,
                cancellationToken);
            eventCount++;
        }

        await RecordAsync(0, MixedProductionEventTypes.CycleStart, suffix: "-inj-start");
        await RecordAsync(0, MixedProductionEventTypes.CycleEnd, suffix: "-inj-end");
        await RecordAsync(0, MixedProductionEventTypes.Good, 1, suffix: "-inj-good");
        await RecordAsync(1, MixedProductionEventTypes.BufferChange, 1, suffix: "-buf-inc");

        await service.PerformOperatorActionAsync(new OperatorActionRequest(
            facilityId, stations[2].Station.Id, "OP-ASM-01", "OP-30", "start",
            "ORD-MIX-001", "SHIFT-A", null, null, null), tenantId, cancellationToken);
        eventCount++;
        await service.PerformOperatorActionAsync(new OperatorActionRequest(
            facilityId, stations[2].Station.Id, "OP-ASM-01", "OP-30", "complete",
            "ORD-MIX-001", "SHIFT-A", "good", null, null), tenantId, cancellationToken);
        eventCount++;
        await RecordAsync(1, MixedProductionEventTypes.BufferChange, 0, suffix: "-buf-dec");

        await RecordAsync(3, MixedProductionEventTypes.Run, suffix: "-weld-run");
        await RecordAsync(3, MixedProductionEventTypes.Good, 1, suffix: "-weld-good");

        await RecordAsync(4, MixedProductionEventTypes.QualityResult, 1, suffix: "-qc-good");
        await RecordAsync(4, MixedProductionEventTypes.Good, 1, suffix: "-qc-pass");
        await RecordAsync(4, MixedProductionEventTypes.Reject, 1, suffix: "-qc-reject");

        await service.PerformOperatorActionAsync(new OperatorActionRequest(
            facilityId, stations[5].Station.Id, "OP-PKG-01", "OP-60", "start",
            "ORD-MIX-001", "SHIFT-A", null, null, null), tenantId, cancellationToken);
        eventCount++;
        await service.PerformOperatorActionAsync(new OperatorActionRequest(
            facilityId, stations[5].Station.Id, "OP-PKG-01", "OP-60", "complete",
            "ORD-MIX-001", "SHIFT-A", "good", null, null), tenantId, cancellationToken);
        eventCount++;

        return eventCount;
    }
}
