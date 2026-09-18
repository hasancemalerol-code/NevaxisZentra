using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.Integration;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.Tenancy;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.RealFactoryPilot;

public static class RealFactoryPilotFixtureSeeder
{
    public static async Task<RealFactoryPilotSeedResult> EnsurePilotFactoryAsync(
        NevaxisZentraDbContext db,
        Guid tenantId,
        Guid facilityId,
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        var line = await db.ProductionLines
            .FirstOrDefaultAsync(
                l => l.TenantId == tenantId && l.FacilityId == facilityId && l.Code == RealFactoryPilotFixtureCodes.LineCode,
                cancellationToken);

        if (line is null)
        {
            line = await SeedPilotLineAsync(db, tenantId, facilityId, cancellationToken);
        }

        await EnsurePilotThroughputTargetAsync(db, tenantId, facilityId, line.Id, cancellationToken);

        var plcConnector = await EnsureConnectorAsync(
            db, tenantId, facilityId, DevelopmentPilotRealFactoryPilotIds.PlcConnectorId,
            RealFactoryPilotFixtureCodes.PlcConnectorName, IntegrationConnectorKinds.IotPlcBridge,
            "opc-ua", "PILOT-PLC", subjectId, cancellationToken);

        var mesConnector = await EnsureConnectorAsync(
            db, tenantId, facilityId, DevelopmentPilotRealFactoryPilotIds.MesConnectorId,
            RealFactoryPilotFixtureCodes.MesConnectorName, IntegrationConnectorKinds.RestApi,
            "mes", "PILOT-MES", subjectId, cancellationToken);

        await EnsureMappingsAsync(db, tenantId, facilityId, plcConnector.Id, mesConnector.Id, subjectId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new RealFactoryPilotSeedResult(
            line.Id,
            line.Code,
            plcConnector.Id,
            mesConnector.Id,
            "FACTORY-PILOT-01 synthetic fixture ready.");
    }

    public static async Task<int> IngestBaselineEventsAsync(
        NevaxisZentraDbContext db,
        IMixedProductionService mixedProduction,
        Guid tenantId,
        Guid facilityId,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        var stations = await db.ProductionStations.AsNoTracking()
            .Where(s => s.LineId == lineId)
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync(cancellationToken);
        var resources = await db.ManufacturingResources.AsNoTracking()
            .Where(r => stations.Select(s => s.Id).Contains(r.StationId))
            .ToListAsync(cancellationToken);

        var assembly = stations.First(s => s.Code == "ST-30");
        var welding = stations.First(s => s.Code == "ST-40");
        var assemblyResource = resources.First(r => r.StationId == assembly.Id);
        var weldingResource = resources.First(r => r.StationId == welding.Id);

        var markerExists = await db.CanonicalProductionEvents.AnyAsync(
            e => e.LineId == lineId && e.Source == "real-factory-pilot-baseline", cancellationToken);
        if (markerExists)
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        var count = 0;
        for (var i = 0; i < 50; i++)
        {
            var station = i % 4 == 0 ? welding : assembly;
            var resource = station.Id == welding.Id ? weldingResource : assemblyResource;
            await mixedProduction.RecordEventAsync(
                new RecordProductionEventRequest(
                    facilityId, lineId, station.Id, resource.Id,
                    MixedProductionEventTypes.Good, $"OP-{station.SequenceOrder}",
                    "ORD-PILOT-001", RealFactoryPilotFixtureCodes.ProductCode,
                    "OP-ASM-01", "SHIFT-A", 1, "pcs", null,
                    $"pilot-baseline-{lineId:N}", "real-factory-pilot-baseline",
                    $"pilot-baseline:{lineId:N}:{i}", $"pilot-baseline:{lineId:N}:{i}",
                    now.AddMinutes(-45 + i)),
                tenantId,
                cancellationToken);
            count++;
        }

        return count;
    }

    public static async Task<int> IngestPostInterventionEventsAsync(
        NevaxisZentraDbContext db,
        IMixedProductionService mixedProduction,
        Guid tenantId,
        Guid facilityId,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        var markerExists = await db.CanonicalProductionEvents.AnyAsync(
            e => e.LineId == lineId && e.Source == "real-factory-pilot-post", cancellationToken);
        if (markerExists)
        {
            return 0;
        }

        var stations = await db.ProductionStations.AsNoTracking()
            .Where(s => s.LineId == lineId)
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync(cancellationToken);
        var assembly = stations.First(s => s.Code == "ST-30");
        var resource = await db.ManufacturingResources.AsNoTracking()
            .FirstAsync(r => r.StationId == assembly.Id, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var count = 0;
        for (var i = 0; i < 45; i++)
        {
            await mixedProduction.RecordEventAsync(
                new RecordProductionEventRequest(
                    facilityId, lineId, assembly.Id, resource.Id,
                    MixedProductionEventTypes.Good, "OP-30",
                    "ORD-PILOT-001", RealFactoryPilotFixtureCodes.ProductCode,
                    "OP-ASM-02", "SHIFT-A", 1, "pcs", null,
                    $"pilot-post-{lineId:N}", "real-factory-pilot-post",
                    $"pilot-post:{lineId:N}:{i}", $"pilot-post:{lineId:N}:{i}",
                    now.AddMinutes(-20 + i)),
                tenantId,
                cancellationToken);
            count++;
        }

        return count;
    }

    private static async Task<ProductionLine> SeedPilotLineAsync(
        NevaxisZentraDbContext db, Guid tenantId, Guid facilityId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var areaId = Guid.NewGuid();
        var lineId = DevelopmentPilotRealFactoryPilotIds.PilotLineId;

        db.ManufacturingAreas.Add(new ManufacturingArea
        {
            Id = areaId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Code = RealFactoryPilotFixtureCodes.AreaCode,
            Name = "Pilot Production Area",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        var line = new ProductionLine
        {
            Id = lineId,
            TenantId = tenantId,
            FacilityId = facilityId,
            AreaId = areaId,
            Code = RealFactoryPilotFixtureCodes.LineCode,
            Name = "Pilot Production Line 01",
            CurrentOrderReference = "ORD-PILOT-001",
            CurrentProductCode = RealFactoryPilotFixtureCodes.ProductCode,
            CurrentShiftCode = "SHIFT-A",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.ProductionLines.Add(line);

        var stationDefs = new (string Code, string Name, string Kind, int Seq, string ResourceType, string ResourceCode, int? BufferCapacity)[]
        {
            ("ST-10", "Injection", MixedProductionStationKinds.Process, 10, MixedProductionResourceTypes.Machine, "INJ-PILOT-01", null),
            ("BUF-20", "Intermediate Buffer", MixedProductionStationKinds.Buffer, 20, MixedProductionResourceTypes.Buffer, "BUF-20", 40),
            ("ST-30", "Human Assembly", MixedProductionStationKinds.Process, 30, MixedProductionResourceTypes.Human, "OP-ASM-01", null),
            ("ST-40", "Welding/Punta", MixedProductionStationKinds.Process, 40, MixedProductionResourceTypes.Robot, "WELD-01", null),
            ("ST-50", "Quality/Test", MixedProductionStationKinds.Quality, 50, MixedProductionResourceTypes.QualityStation, "QC-01", null),
            ("ST-60", "Packaging", MixedProductionStationKinds.Packaging, 60, MixedProductionResourceTypes.Human, "OP-PKG-01", null),
        };

        foreach (var (code, name, kind, seq, resourceType, resourceCode, bufferCapacity) in stationDefs)
        {
            var stationId = Guid.NewGuid();
            db.ProductionStations.Add(new ProductionStation
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
            });
            db.ManufacturingResources.Add(new ManufacturingResource
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                StationId = stationId,
                Code = resourceCode,
                Name = name,
                ResourceType = resourceType,
                BufferCapacity = bufferCapacity,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return line;
    }

    private static async Task EnsurePilotThroughputTargetAsync(
        NevaxisZentraDbContext db, Guid tenantId, Guid facilityId, Guid lineId, CancellationToken cancellationToken)
    {
        if (await db.LineThroughputTargets.AnyAsync(t => t.LineId == lineId, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        db.LineThroughputTargets.Add(new LineThroughputTarget
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            LineId = lineId,
            TargetGoodUnitsPerHour = 670m,
            RequiredGoodQuantity = 5360,
            AvailableProductionHours = 8m,
            TargetCycleSeconds = 5.37m,
            OrderReference = "ORD-PILOT-001",
            IsActive = true,
            EffectiveFromUtc = now.AddDays(-1),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }

    private static async Task<IntegrationConnector> EnsureConnectorAsync(
        NevaxisZentraDbContext db, Guid tenantId, Guid facilityId, Guid connectorId,
        string name, string kind, string sourceType, string sourceName, string subjectId,
        CancellationToken cancellationToken)
    {
        var existing = await db.IntegrationConnectors
            .FirstOrDefaultAsync(c => c.Id == connectorId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var connector = new IntegrationConnector
        {
            Id = connectorId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Name = name,
            ConnectorKind = kind,
            SourceSystemType = sourceType,
            SourceSystemName = sourceName,
            Environment = IntegrationEnvironments.Simulated,
            Status = IntegrationConnectorStatuses.Configured,
            Enabled = false,
            AuthenticationMode = IntegrationAuthenticationModes.None,
            EndpointOrLocation = $"simulated://{name.ToLowerInvariant()}",
            HealthStatus = IntegrationHealthStatuses.Unknown,
            OwnerSubjectId = subjectId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.IntegrationConnectors.Add(connector);
        return connector;
    }

    private static async Task EnsureMappingsAsync(
        NevaxisZentraDbContext db, Guid tenantId, Guid facilityId,
        Guid plcConnectorId, Guid mesConnectorId, string subjectId,
        CancellationToken cancellationToken)
    {
        await EnsureMappingSetAsync(db, tenantId, facilityId, plcConnectorId, subjectId,
            RealFactoryFieldMappingEngine.DefaultPlcMappings(), cancellationToken);
        await EnsureMappingSetAsync(db, tenantId, facilityId, mesConnectorId, subjectId,
            RealFactoryFieldMappingEngine.DefaultMesMappings(), cancellationToken);
    }

    private static async Task EnsureMappingSetAsync(
        NevaxisZentraDbContext db, Guid tenantId, Guid facilityId, Guid connectorId,
        string subjectId, IReadOnlyList<FactoryFieldMappingRule> rules,
        CancellationToken cancellationToken)
    {
        if (await db.RealFactoryFieldMappingRecords.AnyAsync(m => m.ConnectorId == connectorId, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var rule in rules)
        {
            db.RealFactoryFieldMappingRecords.Add(new RealFactoryFieldMappingRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ConnectorId = connectorId,
                MappingVersionNumber = 1,
                Status = RealFactoryMappingStatuses.Draft,
                SourceField = rule.SourceField,
                SourceType = rule.SourceType,
                TargetEntity = rule.TargetEntity,
                TargetEntityId = rule.TargetEntityId,
                CanonicalEventType = rule.CanonicalEventType,
                Transformation = rule.Transformation,
                Unit = rule.Unit,
                Scale = rule.Scale,
                QualityRule = rule.QualityRule,
                TimestampRule = rule.TimestampRule,
                CreatedBySubjectId = subjectId,
                CreatedAtUtc = now,
            });
        }
    }
}

public sealed record RealFactoryPilotSeedResult(
    Guid LineId,
    string LineCode,
    Guid PlcConnectorId,
    Guid MesConnectorId,
    string Message);
