using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.Integration.FactoryEventSpine;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class MixedProductionPermissionTests
{
    [Fact]
    public void PermissionKeysAreStable()
    {
        Assert.Equal("mixed-production.read", MixedProductionPermissionKeys.Read);
        Assert.Equal("mixed-production.manage", MixedProductionPermissionKeys.Manage);
        Assert.Equal("mixed-production.operate", MixedProductionPermissionKeys.Operate);
    }
}

public sealed class MixedProductionResourceTypeTests
{
    [Fact]
    public void AllRequiredResourceTypesExist()
    {
        Assert.Equal("machine", MixedProductionResourceTypes.Machine);
        Assert.Equal("human", MixedProductionResourceTypes.Human);
        Assert.Equal("robot", MixedProductionResourceTypes.Robot);
        Assert.Equal("tool", MixedProductionResourceTypes.Tool);
        Assert.Equal("conveyor", MixedProductionResourceTypes.Conveyor);
        Assert.Equal("buffer", MixedProductionResourceTypes.Buffer);
        Assert.Equal("quality_station", MixedProductionResourceTypes.QualityStation);
    }
}

public sealed class MixedProductionEventTypeTests
{
    [Fact]
    public void CanonicalEventTypesIncludeRequiredSet()
    {
        var required = new[]
        {
            MixedProductionEventTypes.Run,
            MixedProductionEventTypes.Idle,
            MixedProductionEventTypes.Stop,
            MixedProductionEventTypes.Fault,
            MixedProductionEventTypes.Starved,
            MixedProductionEventTypes.Blocked,
            MixedProductionEventTypes.CycleStart,
            MixedProductionEventTypes.CycleEnd,
            MixedProductionEventTypes.Good,
            MixedProductionEventTypes.Reject,
            MixedProductionEventTypes.Rework,
            MixedProductionEventTypes.MaterialWait,
            MixedProductionEventTypes.OperatorWait,
        };

        foreach (var eventType in required)
        {
            Assert.False(string.IsNullOrWhiteSpace(eventType));
        }
    }
}

public sealed class MixedProductionEntityMetadataTests
{
    [Fact]
    public void ProductionLineCodeUniqueIndexIsTenantAndFacilityScoped()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductionLine));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes()
            .Single(index =>
                index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(["Code", "FacilityId", "TenantId"], StringComparer.Ordinal));

        Assert.True(uniqueIndex.IsUnique);
    }

    [Fact]
    public void CanonicalProductionEventRequiresIdempotencyKey()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(CanonicalProductionEvent));
        Assert.NotNull(entityType);

        Assert.False(entityType.FindProperty(nameof(CanonicalProductionEvent.IdempotencyKey))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(CanonicalProductionEvent.TenantId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(CanonicalProductionEvent.FacilityId))!.IsNullable);
    }

    [Fact]
    public void RoutingOperationSequenceUniquePerRouting()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(RoutingOperation));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes()
            .Single(index =>
                index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(["FacilityId", "RoutingId", "SequenceOrder", "TenantId"], StringComparer.Ordinal));

        Assert.True(uniqueIndex.IsUnique);
    }

    [Fact]
    public void ManufacturingResourcePreservesMachineRegistryCode()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ManufacturingResource));
        Assert.NotNull(entityType);

        Assert.NotNull(entityType.FindProperty(nameof(ManufacturingResource.MachineRegistryCode)));
    }

    private static NevaxisFactOSDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisFactOSDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxisfactos_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisFactOSDbContext(options);
    }
}

public sealed class MixedProductionLineStateTests
{
    [Fact]
    public void FaultEventProducesFaultedLineState()
    {
        var events = new[]
        {
            CreateEvent(MixedProductionEventTypes.Run),
            CreateEvent(MixedProductionEventTypes.Fault),
        };

        Assert.Equal(MixedProductionLineStates.Faulted, MixedProductionLineStateEngine.DeriveLineState(events, []));
    }

    [Fact]
    public void BlockedBufferProducesBlockedLineState()
    {
        var buffers = new[] { new BufferResourceState { IsBlocked = true, IsStarved = false } };
        Assert.Equal(
            MixedProductionLineStates.Blocked,
            MixedProductionLineStateEngine.DeriveLineState([], buffers));
    }

    [Fact]
    public void RunningEventsProduceRunningLineState()
    {
        var events = new[] { CreateEvent(MixedProductionEventTypes.Good) };
        Assert.Equal(
            MixedProductionLineStates.Running,
            MixedProductionLineStateEngine.DeriveLineState(events, []));
    }

    private static CanonicalProductionEvent CreateEvent(string eventType) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            OccurredAtUtc = DateTimeOffset.UtcNow,
        };
}

public sealed class MixedProductionTimelineTests
{
    [Fact]
    public void TimelineOrdersEventsChronologically()
    {
        var stationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var stations = new Dictionary<Guid, ProductionStation>
        {
            [stationId] = new ProductionStation { Id = stationId, Code = "ST-10" },
        };
        var resources = new Dictionary<Guid, ManufacturingResource>
        {
            [resourceId] = new ManufacturingResource { Id = resourceId, Code = "INJ-001" },
        };

        var events = new[]
        {
            CreateTimelineEvent(stationId, resourceId, MixedProductionEventTypes.CycleStart, DateTimeOffset.UtcNow.AddMinutes(-2)),
            CreateTimelineEvent(stationId, resourceId, MixedProductionEventTypes.CycleEnd, DateTimeOffset.UtcNow.AddMinutes(-1)),
            CreateTimelineEvent(stationId, resourceId, MixedProductionEventTypes.Good, DateTimeOffset.UtcNow),
        };

        var timeline = MixedProductionTimelineEngine.Build(events, stations, resources);

        Assert.Equal(3, timeline.Count);
        Assert.True(timeline[0].OccurredAtUtc <= timeline[1].OccurredAtUtc);
        Assert.True(timeline[1].OccurredAtUtc <= timeline[2].OccurredAtUtc);
        Assert.Contains(timeline, entry => entry.SourceCategory == "production");
    }

    [Fact]
    public void TimelineCategorizesOperatorAndBufferEvents()
    {
        var stationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var stations = new Dictionary<Guid, ProductionStation>
        {
            [stationId] = new ProductionStation { Id = stationId, Code = "ST-30" },
        };
        var resources = new Dictionary<Guid, ManufacturingResource>
        {
            [resourceId] = new ManufacturingResource { Id = resourceId, Code = "OP-ASM-01" },
        };

        var events = new[]
        {
            CreateTimelineEvent(stationId, resourceId, MixedProductionEventTypes.OperatorStart, DateTimeOffset.UtcNow.AddMinutes(-1)),
            CreateTimelineEvent(stationId, resourceId, MixedProductionEventTypes.BufferChange, DateTimeOffset.UtcNow),
        };

        var timeline = MixedProductionTimelineEngine.Build(events, stations, resources);

        Assert.Contains(timeline, entry => entry.SourceCategory == "operator");
        Assert.Contains(timeline, entry => entry.SourceCategory == "buffer");
    }

    private static CanonicalProductionEvent CreateTimelineEvent(
        Guid stationId,
        Guid resourceId,
        string eventType,
        DateTimeOffset occurredAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            StationId = stationId,
            ResourceId = resourceId,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
        };
}

public sealed class MixedProductionPlaybackTests
{
    [Fact]
    public void PlaybackReconstructsFramesAcrossWindow()
    {
        var lineId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var windowStart = DateTimeOffset.UtcNow.AddMinutes(-5);
        var windowEnd = windowStart.AddMinutes(2);

        var line = new ProductionLine { Id = lineId, Code = MixedProductionFixtureCodes.LineCode };
        var station = new ProductionStation
        {
            Id = stationId,
            Code = "ST-10",
            SequenceOrder = 10,
        };
        var resource = new ManufacturingResource { Id = resourceId, StationId = stationId, Code = "INJ-001" };
        var events = new[]
        {
            new CanonicalProductionEvent
            {
                Id = Guid.NewGuid(),
                StationId = stationId,
                ResourceId = resourceId,
                EventType = MixedProductionEventTypes.CycleStart,
                OccurredAtUtc = windowStart.AddMinutes(1),
            },
            new CanonicalProductionEvent
            {
                Id = Guid.NewGuid(),
                StationId = stationId,
                ResourceId = resourceId,
                EventType = MixedProductionEventTypes.Good,
                OccurredAtUtc = windowStart.AddMinutes(1.5),
            },
        };

        var playback = MixedProductionPlaybackEngine.Build(
            line,
            windowStart,
            windowEnd,
            events,
            [station],
            new Dictionary<Guid, BufferResourceState>(),
            new Dictionary<Guid, ManufacturingResource> { [stationId] = resource });

        Assert.True(playback.Frames.Count > 0);
        Assert.Contains(playback.Frames, frame => frame.LineState == MixedProductionLineStates.Running);
        Assert.Contains(playback.Frames, frame => frame.Stations.Any(s => s.StationCode == "ST-10"));
    }
}

public sealed class MixedProductionAcceptanceSequenceTests
{
    [Fact]
    public void MixLine01AcceptanceSequenceProducesCorrelatedTimeline()
    {
        var correlation = "mix-line-acceptance";
        var stationIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        var resourceIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        var stationCodes = new[] { "ST-10", "BUF-20", "ST-30", "ST-40", "ST-50", "ST-60" };
        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-30);
        var eventCount = 0;

        var stations = stationIds
            .Select((id, index) => new ProductionStation
            {
                Id = id,
                Code = stationCodes[index],
                SequenceOrder = (index + 1) * 10,
            })
            .ToDictionary(s => s.Id);

        var resources = resourceIds
            .Select((id, index) => new ManufacturingResource
            {
                Id = id,
                StationId = stationIds[index],
                Code = stationCodes[index],
            })
            .ToDictionary(r => r.Id);

        var events = new List<CanonicalProductionEvent>();

        void Add(int stationIndex, string eventType, string? operatorCode = null)
        {
            events.Add(new CanonicalProductionEvent
            {
                Id = Guid.NewGuid(),
                StationId = stationIds[stationIndex],
                ResourceId = resourceIds[stationIndex],
                EventType = eventType,
                OperatorCode = operatorCode,
                CorrelationKey = correlation,
                OccurredAtUtc = baseTime.AddSeconds(eventCount * 15),
            });
            eventCount++;
        }

        Add(0, MixedProductionEventTypes.CycleStart);
        Add(0, MixedProductionEventTypes.CycleEnd);
        Add(0, MixedProductionEventTypes.Good);
        Add(1, MixedProductionEventTypes.BufferChange);
        Add(2, MixedProductionEventTypes.OperatorStart, "OP-ASM-01");
        Add(2, MixedProductionEventTypes.OperatorComplete, "OP-ASM-01");
        Add(1, MixedProductionEventTypes.BufferChange);
        Add(3, MixedProductionEventTypes.Run);
        Add(3, MixedProductionEventTypes.Good);
        Add(4, MixedProductionEventTypes.QualityResult);
        Add(4, MixedProductionEventTypes.Good);
        Add(4, MixedProductionEventTypes.Reject);
        Add(5, MixedProductionEventTypes.OperatorStart, "OP-PKG-01");
        Add(5, MixedProductionEventTypes.OperatorComplete, "OP-PKG-01");
        Add(5, MixedProductionEventTypes.Good);

        var timeline = MixedProductionTimelineEngine.Build(events, stations, resources);

        Assert.True(timeline.Count >= 14);
        Assert.All(timeline, entry => Assert.Equal(correlation, entry.CorrelationKey));
        Assert.Contains(timeline, entry => entry.EventType == MixedProductionEventTypes.CycleStart);
        Assert.Contains(timeline, entry => entry.EventType == MixedProductionEventTypes.BufferChange);
        Assert.Contains(timeline, entry => entry.EventType == MixedProductionEventTypes.OperatorComplete);
        Assert.Contains(timeline, entry => entry.EventType == MixedProductionEventTypes.Reject);
        Assert.Contains(timeline, entry => entry.EventType == MixedProductionEventTypes.Good);
    }
}

public sealed class MixedProductionTenantIsolationTests
{
    [Fact]
    public void CanonicalProductionEventHasTenantScopedIdempotencyIndex()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(CanonicalProductionEvent));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes()
            .Single(index =>
                index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(["FacilityId", "IdempotencyKey", "TenantId"], StringComparer.Ordinal));

        Assert.True(uniqueIndex.IsUnique);
    }

    [Fact]
    public void QueryFilterRequiresTenantContextForMixedProductionEntities()
    {
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(Guid.NewGuid(), "mixed-production-test", Guid.NewGuid()));
        using var context = CreateContext(accessor);
        var lineEntity = context.Model.FindEntityType(typeof(ProductionLine));
        Assert.NotEmpty(lineEntity!.GetDeclaredQueryFilters());

        var eventEntity = context.Model.FindEntityType(typeof(CanonicalProductionEvent));
        Assert.NotEmpty(eventEntity!.GetDeclaredQueryFilters());
    }

    private static NevaxisFactOSDbContext CreateContext(ITenantContextAccessor? tenantContextAccessor = null)
    {
        var options = new DbContextOptionsBuilder<NevaxisFactOSDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxisfactos_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisFactOSDbContext(options, tenantContextAccessor);
    }
}

public sealed class MixedProductionInjectionCompatibilityTests
{
    [Fact]
    public void MachineResourceSupportsInjectionRegistryCode()
    {
        var resource = new ManufacturingResource
        {
            ResourceType = MixedProductionResourceTypes.Machine,
            MachineRegistryCode = "INJ-001",
            Code = "INJ-001",
        };

        Assert.Equal("INJ-001", resource.MachineRegistryCode);
        Assert.Equal(MixedProductionResourceTypes.Machine, resource.ResourceType);
    }

    [Fact]
    public void FixtureCodesReferenceInjectionMachine()
    {
        Assert.Equal("MIX-LINE-01", MixedProductionFixtureCodes.LineCode);
        Assert.Equal("MIX-PROD-01", MixedProductionFixtureCodes.ProductCode);
    }
}

public sealed class MixedProductionRegressionTests
{
    [Fact]
    public void EventSpineConstantsRemainAvailableForRegression()
    {
        Assert.Equal("dispatch-pending", FactoryCanonicalEventStates.DispatchPending);
        Assert.Equal("dead-letter", FactoryCanonicalEventStates.DeadLetter);
    }

    [Fact]
    public void DigitalWorkCaseAggregateRemainsCanonical()
    {
        Assert.NotNull(typeof(DigitalWorkCase).GetProperty(nameof(DigitalWorkCase.Id)));
        Assert.NotNull(typeof(DigitalWorkCase).GetProperty(nameof(DigitalWorkCase.TenantId)));
    }
}
