using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.ContextKnowledge;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.MixedProduction;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ContextKnowledgePermissionTests
{
    [Fact]
    public void PermissionKeysAreStable()
    {
        Assert.Equal("context-knowledge.read", ContextKnowledgePermissionKeys.Read);
        Assert.Equal("context-knowledge.manage", ContextKnowledgePermissionKeys.Manage);
        Assert.Equal("context-knowledge.promote", ContextKnowledgePermissionKeys.Promote);
    }
}

public sealed class ContextKnowledgeEntityMetadataTests
{
    [Fact]
    public void CanonicalOperationalStateRequiresTenantAndFacility()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(CanonicalOperationalStateRecord));
        Assert.NotNull(entityType);
        Assert.False(entityType.FindProperty(nameof(CanonicalOperationalStateRecord.TenantId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(CanonicalOperationalStateRecord.FacilityId))!.IsNullable);
    }

    [Fact]
    public void FactoryKnowledgeRecordHasUniqueKnowledgeKeyPerFacility()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FactoryKnowledgeRecord));
        Assert.NotNull(entityType);
        Assert.Contains(entityType.GetIndexes(), i => i.IsUnique);
    }

    [Fact]
    public void AllContextKnowledgeEntitiesHaveTenantQueryFilters()
    {
        using var context = CreateContext();
        Assert.NotEmpty(context.Model.FindEntityType(typeof(CanonicalOperationalStateRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(FactoryKnowledgeRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ContextSnapshotRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(DataLifecyclePolicyRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(TelemetryAggregateRecord))!.GetDeclaredQueryFilters());
    }

    private static NevaxisZentraDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisZentraDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxiszentra_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisZentraDbContext(options);
    }
}

public sealed class ContextKnowledgeEngineTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _facilityId = Guid.NewGuid();
    private readonly string _entityId = "machine-01";
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void AcceptanceAThreeSourcesSameEventProducesOneOperationalTruth()
    {
        var dedupKey = ContextDeduplicationEngine.BuildEventSpineAlignedKey(_tenantId, Guid.NewGuid(), "evt-001");
        var claims = new[]
        {
            Claim(ContextKnowledgeSourceTypes.Plc, dedupKey, "RUNNING", _now.AddMinutes(-1)),
            Claim(ContextKnowledgeSourceTypes.Mes, dedupKey, "RUNNING", _now.AddMinutes(-2)),
            Claim(ContextKnowledgeSourceTypes.Erp, dedupKey, "RUNNING", _now.AddMinutes(-3)),
        };

        var resolved = CanonicalOperationalStateResolver.Resolve(
            ContextKnowledgeScopeEntityTypes.Machine,
            _entityId,
            claims,
            _now,
            TimeSpan.FromHours(4));

        Assert.Equal(ContextKnowledgeResolutionStatuses.Resolved, resolved.ResolutionStatus);
        Assert.Equal("RUNNING", resolved.ResolvedStateValue);

        var connectorId = Guid.NewGuid();
        var deduped = ContextDeduplicationEngine.DeduplicateByKey(
            claims,
            c => ContextDeduplicationEngine.BuildEventSpineAlignedKey(_tenantId, connectorId, "evt-001"));
        Assert.Single(deduped);
    }

    [Fact]
    public void AcceptanceBOldStateConflictsWithNewStateFreshnessAndSourcePolicyWins()
    {
        var claims = new[]
        {
            Claim(ContextKnowledgeSourceTypes.Mes, "mes-1", "PAUSED", _now.AddHours(-5)),
            Claim(ContextKnowledgeSourceTypes.Plc, "plc-1", "RUNNING", _now.AddMinutes(-1)),
        };

        var resolved = CanonicalOperationalStateResolver.Resolve(
            ContextKnowledgeScopeEntityTypes.Machine,
            _entityId,
            claims,
            _now,
            TimeSpan.FromHours(4));

        Assert.Equal(ContextKnowledgeResolutionStatuses.Resolved, resolved.ResolutionStatus);
        Assert.Equal("RUNNING", resolved.ResolvedStateValue);
    }

    [Fact]
    public void AcceptanceCUnresolvedConflictDoesNotFabricateCanonicalState()
    {
        var claims = new[]
        {
            Claim(ContextKnowledgeSourceTypes.Erp, "erp-1", "RUNNING", _now.AddMinutes(-1)),
            Claim(ContextKnowledgeSourceTypes.Mes, "mes-1", "PAUSED", _now.AddMinutes(-1)),
            Claim(ContextKnowledgeSourceTypes.Plc, "plc-1", "RUNNING", _now.AddMinutes(-1)),
        };

        var resolved = CanonicalOperationalStateResolver.Resolve(
            ContextKnowledgeScopeEntityTypes.Machine,
            _entityId,
            claims,
            _now,
            TimeSpan.FromHours(4));

        Assert.Equal(ContextKnowledgeResolutionStatuses.Conflicted, resolved.ResolutionStatus);
        Assert.Null(resolved.ResolvedStateValue);
        Assert.NotEmpty(resolved.ConflictReasons);
    }

    [Fact]
    public void AcceptanceEOneMillionHistoricalRecordsWorkerContextRemainsBounded()
    {
        var budget = new ContextBudget(MaxEventCount: 50, MaxHistoricalCases: 10, MaxKnowledgeRecords: 5, MaxPayloadBytes: 100_000);
        var scope = new ContextScope(_tenantId, _facilityId, ContextKnowledgeScopeEntityTypes.Line, Guid.NewGuid().ToString());
        var events = Enumerable.Range(0, 1_000_000)
            .Select(i => BuildEvent(Guid.NewGuid(), MixedProductionEventTypes.Good, _now.AddMinutes(-i)))
            .ToList();

        var package = ContextBuilderOrchestrator.Build(
            new ContextBuildRequest(scope, budget),
            null,
            events,
            [],
            [],
            _now);

        Assert.True(package.Items.Count <= budget.MaxEventCount + 2);
        Assert.True(package.Metrics.FinalPayloadBytes <= budget.MaxPayloadBytes);
    }

    [Fact]
    public void AcceptanceFCurrentMachineStateCannotBeOverriddenByHistoricalKnowledge()
    {
        var canonical = new CanonicalOperationalStateResult(
            ContextKnowledgeScopeEntityTypes.Machine,
            _entityId,
            ContextKnowledgeResolutionStatuses.Resolved,
            "RUNNING",
            0.95m,
            [],
            [],
            _now);

        var knowledge = new FactoryKnowledgeRecordSummary(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Stopped machine",
            "Previous fault",
            "Stop line",
            "STOPPED",
            0.9m,
            ContextKnowledgePromotionStatuses.Verified);

        var package = ContextBuilderOrchestrator.Build(
            new ContextBuildRequest(new ContextScope(_tenantId, _facilityId, ContextKnowledgeScopeEntityTypes.Machine, _entityId), new ContextBudget()),
            canonical,
            [],
            [],
            [knowledge],
            _now);

        Assert.Equal("RUNNING", package.CanonicalState?.ResolvedStateValue);
        Assert.Contains(package.Items, i => i.ItemKind == "canonical_state");
        Assert.DoesNotContain(package.Items, i => i.ItemKind == "verified_knowledge" && i.PriorityRank < 4);
    }

    [Fact]
    public void AcceptanceGVerifiedCaseKnowledgePromotionPasses()
    {
        var result = KnowledgePromotionGate.Evaluate(VerifiedPromotionRequest());
        Assert.True(result.Promoted);
        Assert.Equal(ContextKnowledgePromotionStatuses.Verified, result.PromotionStatus);
        Assert.Empty(result.RejectionReasons);
    }

    [Fact]
    public void AcceptanceHClosedButUnverifiedCaseDoesNotPromoteTrustedKnowledge()
    {
        var request = VerifiedPromotionRequest() with { HasVerifiedOutcome = false, ObservationCompleted = false };
        var result = KnowledgePromotionGate.Evaluate(request);
        Assert.False(result.Promoted);
        Assert.Equal(ContextKnowledgePromotionStatuses.LearningOnly, result.PromotionStatus);
        Assert.Contains("verified-outcome-required", result.RejectionReasons);
    }

    [Fact]
    public void AcceptanceIContextSnapshotIsDecisionTimeReconstructable()
    {
        var scope = new ContextScope(_tenantId, _facilityId, ContextKnowledgeScopeEntityTypes.Machine, _entityId);
        var item = new ContextItem(
            "canonical_state",
            _entityId,
            "Canonical state: RUNNING",
            new ContextProvenance(
                ContextKnowledgeSourceTypes.Plc,
                "plc-1",
                _tenantId,
                ContextKnowledgeScopeEntityTypes.Machine,
                _entityId,
                _now,
                _now,
                1m,
                0.95m,
                "RESOLVED",
                ContextKnowledgeDataTiers.HotOperational),
            1,
            ContextKnowledgeDataTiers.HotOperational);

        var metrics = new ContextBuildMetrics(12, 1, 0, 0, 0, 0, 0, 1, 64, null);
        var package = new WorkerContextPackage(scope, null, [item], [], metrics, null, _now);
        var snapshot = ContextSnapshotFactory.CreateSummary(
            Guid.NewGuid(),
            new ContextSnapshotRequest(scope, package, null, "worker-1"),
            [item],
            metrics,
            _now);

        Assert.False(string.IsNullOrWhiteSpace(snapshot.MaterializedContextJson));
        var materialized = JsonSerializer.Deserialize<JsonElement>(snapshot.MaterializedContextJson);
        Assert.True(materialized.TryGetProperty("items", out var items));
        Assert.Equal(1, items.GetArrayLength());
        Assert.Contains("RUNNING", snapshot.MaterializedContextJson);
    }

    [Fact]
    public void AcceptanceJHistoricalStorageGrowthContextSizeRemainsBounded()
    {
        var budget = new ContextBudget(MaxEventCount: 25, MaxPayloadBytes: 50_000);
        var events = Enumerable.Range(0, 500_000)
            .Select(i => BuildEvent(Guid.NewGuid(), MixedProductionEventTypes.Idle, _now.AddDays(-30).AddMinutes(i)))
            .ToList();

        var package = ContextBuilderOrchestrator.Build(
            new ContextBuildRequest(new ContextScope(_tenantId, _facilityId, ContextKnowledgeScopeEntityTypes.Line, Guid.NewGuid().ToString()), budget),
            null,
            events,
            [],
            [],
            _now);

        Assert.True(package.Items.Count <= budget.MaxEventCount);
        Assert.True(package.Metrics.StaleCount >= 0);
    }

    [Fact]
    public void AcceptanceKMixedProductionHierarchyContextRetrievalIncludesScopedEvents()
    {
        var lineId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var scope = new ContextScope(
            _tenantId,
            _facilityId,
            ContextKnowledgeScopeEntityTypes.Station,
            stationId.ToString(),
            LineId: lineId,
            StationId: stationId);

        var events = new List<CanonicalProductionEvent>
        {
            BuildEvent(stationId, MixedProductionEventTypes.Run, _now.AddMinutes(-5), lineId: lineId),
            BuildEvent(Guid.NewGuid(), MixedProductionEventTypes.Run, _now.AddMinutes(-4), lineId: Guid.NewGuid()),
        };

        var retrieved = HybridRetrievalEngine.Retrieve(
            new HybridRetrievalQuery(scope),
            null,
            events,
            [],
            [],
            _now,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.Contains(retrieved, i => i.ItemKind == "event" && i.ReferenceId == events[0].Id.ToString());
    }

    [Fact]
    public void DataQualityGateRejectsDuplicateAndStaleRecords()
    {
        var scope = new ContextScope(_tenantId, _facilityId, ContextKnowledgeScopeEntityTypes.Machine, _entityId);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var accepted = ContextDataQualityGate.Evaluate(
            _tenantId, scope, ContextKnowledgeSourceTypes.Plc, "plc-1", _now.AddMinutes(-1), _now,
            false, ContextKnowledgeScopeEntityTypes.Machine, _entityId, _now, TimeSpan.FromHours(4), seen);
        Assert.True(accepted.Accepted);

        var duplicate = ContextDataQualityGate.Evaluate(
            _tenantId, scope, ContextKnowledgeSourceTypes.Plc, "plc-1", _now.AddMinutes(-1), _now,
            false, ContextKnowledgeScopeEntityTypes.Machine, _entityId, _now, TimeSpan.FromHours(4), seen);
        Assert.False(duplicate.Accepted);
        Assert.Equal(ContextKnowledgeDataQualityReasons.Duplicate, duplicate.ReasonCode);

        var stale = ContextDataQualityGate.Evaluate(
            _tenantId, scope, ContextKnowledgeSourceTypes.Plc, "plc-2", _now.AddDays(-2), _now,
            false, ContextKnowledgeScopeEntityTypes.Machine, _entityId, _now, TimeSpan.FromHours(4), seen);
        Assert.False(stale.Accepted);
        Assert.Equal(ContextKnowledgeDataQualityReasons.Stale, stale.ReasonCode);
    }

    [Fact]
    public void DataLifecyclePolicyResolvesHotWarmColdTiers()
    {
        var policies = new[]
        {
            new DataLifecyclePolicyDefinition("telemetry", 24, 30, 365),
        };

        Assert.Equal(ContextKnowledgeLifecycleTiers.Hot,
            DataLifecyclePolicyEngine.ResolveTier("telemetry", _now.AddHours(-1), policies, _now));
        Assert.Equal(ContextKnowledgeLifecycleTiers.Warm,
            DataLifecyclePolicyEngine.ResolveTier("telemetry", _now.AddDays(-10), policies, _now));
        Assert.Equal(ContextKnowledgeLifecycleTiers.Cold,
            DataLifecyclePolicyEngine.ResolveTier("telemetry", _now.AddDays(-120), policies, _now));
    }

    [Fact]
    public void TelemetryDownsamplingBuildsMinuteHourDayAggregates()
    {
        var samples = Enumerable.Range(0, 60)
            .Select(i => new TelemetryDownsampleInput("res-1", "cycle_time", 10 + i, _now.AddMinutes(-i)))
            .ToList();

        var buckets = TelemetryDownsamplingEngine.BuildBuckets(samples, _now);
        Assert.Contains(buckets, b => b.Granularity == ContextKnowledgeAggregationGranularities.Minute);
        Assert.Contains(buckets, b => b.Granularity == ContextKnowledgeAggregationGranularities.Hour);
        Assert.Contains(buckets, b => b.Granularity == ContextKnowledgeAggregationGranularities.Day);
    }

    private static ContextSourceClaim Claim(string sourceType, string sourceRecordId, string state, DateTimeOffset observedAt) =>
        new(sourceType, sourceRecordId, state, observedAt, observedAt, 0.9m);

    private static KnowledgePromotionRequest VerifiedPromotionRequest() =>
        new(
            Guid.NewGuid(),
            "Cycle time drift",
            "Worn mold insert",
            "Reduce speed and schedule maintenance",
            ["Reduce injection speed"],
            ["evidence-1"],
            "{}",
            "{}",
            "RECOVERED",
            HasVerifiedOutcome: true,
            HasSufficientEvidence: true,
            HasUnresolvedConflict: false,
            ObservationCompleted: true,
            Confidence: 0.92m,
            ApplicabilityConstraintsJson: "{}");

    private static CanonicalProductionEvent BuildEvent(
        Guid stationId,
        string eventType,
        DateTimeOffset occurredAt,
        Guid? lineId = null)
    {
        return new CanonicalProductionEvent
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            FacilityId = Guid.NewGuid(),
            LineId = lineId,
            StationId = stationId,
            EventType = eventType,
            IdempotencyKey = $"{stationId:N}:{eventType}:{occurredAt:O}",
            OccurredAtUtc = occurredAt,
            ReceivedAtUtc = occurredAt,
        };
    }
}
