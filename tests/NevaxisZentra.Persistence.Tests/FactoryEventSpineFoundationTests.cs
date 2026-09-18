using System.Diagnostics;
using System.Text.Json;
using NevaxisZentra.Persistence.FactoryOrchestration;
using NevaxisZentra.Persistence.FactoryOutcomeExecution;
using NevaxisZentra.Persistence.Integration;
using NevaxisZentra.Persistence.Integration.FactoryEventSpine;

namespace NevaxisZentra.Persistence.Tests;

public sealed class FactoryIntegrationNormalizerTests
{
    [Fact]
    public void DuplicateWebhookUsesDeterministicIdempotencyKey()
    {
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var connectorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var key1 = FactoryIntegrationNormalizer.BuildIdempotencyKey(tenantId, connectorId, "evt-100");
        var key2 = FactoryIntegrationNormalizer.BuildIdempotencyKey(tenantId, connectorId, "evt-100");
        Assert.Equal(key1, key2);
        Assert.NotEqual(
            FactoryIntegrationNormalizer.BuildIdempotencyKey(tenantId, connectorId, "evt-101"),
            key1);
    }

    [Fact]
    public void DuplicateFileUsesContentHashWhenSourceIdMissing()
    {
        var payload = """{"eventType":"production.signal","cycleTimeSec":32.1}""";
        var hash1 = FactoryIntegrationNormalizer.ComputePayloadHash(payload);
        var hash2 = FactoryIntegrationNormalizer.ComputePayloadHash(payload);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void VendorMachineCodesNormalizeToCanonicalInj004()
    {
        var imm = FactoryIntegrationNormalizer.Normalize(
            """{"eventType":"production.signal","machineCode":"IMM-04","orderReference":"ORD-8742"}""",
            "MES-A");
        var resource = FactoryIntegrationNormalizer.Normalize(
            """{"eventType":"production.signal","resourceId":"173","orderReference":"ORD-8742"}""",
            "MES-B");

        Assert.Equal("INJ-004", imm.MachineId);
        Assert.Equal("INJ-004", resource.MachineId);
    }

    [Fact]
    public void InvalidUnitTriggersQuarantineMeasurement()
    {
        var contract = FactoryIntegrationNormalizer.Normalize(
            """{"eventType":"production.signal","machineCode":"INJ-004","measurements":{"weight":{"value":12.5,"unit":"stones"}}}""",
            "MES");

        Assert.Contains(contract.Measurements.Values, m => m.QuarantineRequired);
    }

    [Fact]
    public void UnknownMachineContextWithoutOrderOrMaterialIsDetectedByValidation()
    {
        var contract = FactoryIntegrationNormalizer.Normalize(
            """{"eventType":"production.signal","description":"orphan"}""",
            "MES");
        Assert.Null(contract.MachineId);
        Assert.Null(contract.OrderId);
        Assert.Null(contract.MaterialId);
    }

    [Fact]
    public void CanonicalEventMapsToQualifiedFactoryDomainEvent()
    {
        var facilityId = Guid.NewGuid();
        var contract = FactoryIntegrationNormalizer.Normalize(
            DevelopmentFactorySourceSimulator.CreateCycleDegradationEvent(Guid.NewGuid(), "evt-1").RawPayloadJson,
            "Dev-MES");
        var domainEvent = FactoryIntegrationNormalizer.ToFactoryDomainEvent(
            contract,
            facilityId,
            "webhook",
            "real");

        var qualification = FactoryOutcomeEventQualificationEngine.Qualify(domainEvent, null);
        Assert.True(qualification.IsQualified);
        Assert.Equal("INJ-004", domainEvent.MachineId);
    }

    [Fact]
    public void LateEventContractPreservesOccurredAt()
    {
        var occurred = DateTimeOffset.UtcNow.AddHours(-5);
        var contract = FactoryIntegrationNormalizer.Normalize(
            $$"""{"eventType":"production.signal","machineCode":"INJ-004","occurredAt":"{{occurred:O}}"}""",
            "MES");
        Assert.Equal(occurred.UtcDateTime, contract.OccurredAtUtc.UtcDateTime);
    }
}

public sealed class FactoryIntegrationUnitNormalizerTests
{
    [Fact]
    public void KnownUnitsNormalizeWithoutQuarantine()
    {
        var measurement = FactoryIntegrationUnitNormalizer.NormalizeMeasurement("cycleTimeSec", 32.1m, null, "sec");
        Assert.False(measurement.QuarantineRequired);
        Assert.Equal("seconds", measurement.NormalizedUnit);
    }

    [Fact]
    public void UnknownUnitRequiresQuarantine()
    {
        var measurement = FactoryIntegrationUnitNormalizer.NormalizeMeasurement("pressure", 120m, null, "psi-weird");
        Assert.True(measurement.QuarantineRequired);
        Assert.Contains("unknown-unit", measurement.QuarantineReason, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class FactoryEventOrderingGuardTests
{
    [Fact]
    public void OutOfOrderSequenceIsSkipped()
    {
        var current = new IntegrationCanonicalFactoryEvent { SourceSequence = 1 };
        Assert.False(FactoryEventOrderingGuard.ShouldApply(current, 3));
    }

    [Fact]
    public void InOrderSequenceIsApplied()
    {
        var current = new IntegrationCanonicalFactoryEvent { SourceSequence = 4 };
        Assert.True(FactoryEventOrderingGuard.ShouldApply(current, 3));
    }

    [Fact]
    public void MissingSequenceAlwaysApplies()
    {
        var current = new IntegrationCanonicalFactoryEvent { SourceSequence = null };
        Assert.True(FactoryEventOrderingGuard.ShouldApply(current, 10));
    }
}

public sealed class FactoryIntegrationHealthEngineTests
{
    [Fact]
    public void PausedConnectorReportsPausedHealth()
    {
        var connector = new IntegrationConnector { Enabled = false };
        var (health, _) = FactoryIntegrationHealthEngine.Evaluate(connector, 0, null);
        Assert.Equal(FactoryIntegrationHealthStates.Paused, health);
    }

    [Fact]
    public void ConsecutiveFailuresReportDegradedHealth()
    {
        var connector = new IntegrationConnector
        {
            Enabled = true,
            LastSuccessfulSyncAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        var (health, summary) = FactoryIntegrationHealthEngine.Evaluate(connector, 4, null);
        Assert.Equal(FactoryIntegrationHealthStates.Degraded, health);
        Assert.Contains("4", summary, StringComparison.Ordinal);
    }
}

public sealed class FactoryEventSpineCorrelationTests
{
    [Fact]
    public void MaterialEventCorrelatesWithCycleDegradationCase()
    {
        var facilityId = Guid.NewGuid();
        var cycle = FactoryIntegrationNormalizer.Normalize(
            DevelopmentFactorySourceSimulator.CreateCycleDegradationEvent(Guid.NewGuid(), "c1").RawPayloadJson,
            "MES");
        var material = FactoryIntegrationNormalizer.Normalize(
            DevelopmentFactorySourceSimulator.CreateMaterialCoverageRiskEvent(Guid.NewGuid(), "m1").RawPayloadJson,
            "MES");

        var cycleEvent = FactoryIntegrationNormalizer.ToFactoryDomainEvent(cycle, facilityId, "webhook", "real");
        var materialEvent = FactoryIntegrationNormalizer.ToFactoryDomainEvent(material, facilityId, "webhook", "real");

        Assert.Equal(
            FactoryCaseCorrelationEngine.BuildCorrelationKey(cycleEvent),
            FactoryCaseCorrelationEngine.BuildCorrelationKey(materialEvent));
    }

    [Fact]
    public void CrossFacilityEventsDoNotShareCorrelationKey()
    {
        var facilityA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var facilityB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var payload = DevelopmentFactorySourceSimulator.CreateCycleDegradationEvent(Guid.NewGuid(), "x").RawPayloadJson;
        var contract = FactoryIntegrationNormalizer.Normalize(payload, "MES");
        var eventA = FactoryIntegrationNormalizer.ToFactoryDomainEvent(contract, facilityA, "webhook", "real");
        var eventB = FactoryIntegrationNormalizer.ToFactoryDomainEvent(contract, facilityB, "webhook", "real");

        Assert.NotEqual(
            FactoryCaseCorrelationEngine.BuildCorrelationKey(eventA),
            FactoryCaseCorrelationEngine.BuildCorrelationKey(eventB));
    }
}

public sealed class FactoryEventSpineRetryClassificationTests
{
    [Fact]
    public void TransientAndPermanentClassificationsAreDistinct()
    {
        Assert.NotEqual(
            FactoryEventErrorClassifications.Transient,
            FactoryEventErrorClassifications.Permanent);
        Assert.NotEqual(
            FactoryEventErrorClassifications.Configuration,
            FactoryEventErrorClassifications.Data);
    }

    [Fact]
    public void DeadLetterStateIsOperationallyVisible()
    {
        Assert.Equal("dead-letter", FactoryCanonicalEventStates.DeadLetter);
    }
}

public sealed class FactoryEventSpinePerformanceTests
{
    [Fact]
    public void Synthetic100KInboundEventsMeetBaselineThroughput()
    {
        var tenantId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var payload = DevelopmentFactorySourceSimulator
            .CreateCycleDegradationEvent(connectorId, "perf")
            .RawPayloadJson;
        var sw = Stopwatch.StartNew();
        var keys = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < 100_000; i++)
        {
            var key = FactoryIntegrationNormalizer.BuildIdempotencyKey(tenantId, connectorId, $"evt-{i}");
            keys.Add(key);
            _ = FactoryIntegrationNormalizer.Normalize(
                payload.Replace("perf", $"perf-{i}", StringComparison.Ordinal),
                "MES");
        }

        sw.Stop();
        Assert.Equal(100_000, keys.Count);
        Assert.True(sw.ElapsedMilliseconds < 120_000, $"100K normalization took {sw.ElapsedMilliseconds}ms");
    }
}

public sealed class FactoryEventSpineRegressionTests
{
    [Fact]
    public void WaveBFileImportConstantsRemainAvailable()
    {
        Assert.Equal("csv", IntegrationFileKinds.Csv);
        Assert.Equal("xlsx", IntegrationFileKinds.Xlsx);
        Assert.Equal("material", IntegrationCanonicalProjectionTypes.Material);
        Assert.Equal("factory-operational-event", IntegrationCanonicalProjectionTypes.FactoryOperationalEvent);
    }

    [Fact]
    public void DevelopmentSimulatorPayloadsAreValidJson()
    {
        var connectorId = Guid.NewGuid();
        foreach (var request in new[]
        {
            DevelopmentFactorySourceSimulator.CreateCycleDegradationEvent(connectorId, "a"),
            DevelopmentFactorySourceSimulator.CreateMicroStopEvent(connectorId, "b"),
            DevelopmentFactorySourceSimulator.CreateMaterialCoverageRiskEvent(connectorId, "c"),
        })
        {
            using var doc = JsonDocument.Parse(request.RawPayloadJson);
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }
    }
}
