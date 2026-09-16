using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.MixedProduction;

namespace NevaxisFactOS.Persistence.ContextKnowledge;

public static class ContextDataTierClassifier
{
    public static string Classify(DateTimeOffset observedAtUtc, bool isVerifiedKnowledge, DateTimeOffset nowUtc)
    {
        if (isVerifiedKnowledge)
        {
            return ContextKnowledgeDataTiers.VerifiedFactoryKnowledge;
        }

        var age = nowUtc - observedAtUtc;
        return age <= TimeSpan.FromHours(4)
            ? ContextKnowledgeDataTiers.HotOperational
            : ContextKnowledgeDataTiers.HistoricalOperational;
    }
}

public static class CanonicalOperationalStateResolver
{
    public static CanonicalOperationalStateResult Resolve(
        string entityType,
        string entityId,
        IReadOnlyList<ContextSourceClaim> claims,
        DateTimeOffset nowUtc,
        TimeSpan staleThreshold)
    {
        var activeClaims = claims
            .Where(c => !c.IsQuarantined)
            .Where(c => nowUtc - c.ObservedAtUtc <= staleThreshold)
            .OrderByDescending(c => ContextKnowledgeDefaultSourcePriorities.Priorities.GetValueOrDefault(c.SourceType, 0))
            .ThenByDescending(c => c.ObservedAtUtc)
            .ToList();

        if (activeClaims.Count == 0)
        {
            return new CanonicalOperationalStateResult(
                entityType,
                entityId,
                ContextKnowledgeResolutionStatuses.Unknown,
                null,
                0m,
                claims,
                ["no-trusted-active-claims"],
                nowUtc);
        }

        var top = activeClaims[0];
        var conflicting = activeClaims
            .Where(c => !string.Equals(c.StateValue, top.StateValue, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (conflicting.Count > 0)
        {
            var reasons = conflicting
                .Select(c => $"{c.SourceType}:{c.StateValue}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new CanonicalOperationalStateResult(
                entityType,
                entityId,
                ContextKnowledgeResolutionStatuses.Conflicted,
                null,
                0m,
                claims,
                reasons,
                nowUtc);
        }

        var confidence = Math.Min(1m, top.Reliability + (activeClaims.Count > 1 ? 0.1m : 0m));
        return new CanonicalOperationalStateResult(
            entityType,
            entityId,
            ContextKnowledgeResolutionStatuses.Resolved,
            top.StateValue,
            confidence,
            claims,
            [],
            nowUtc);
    }
}

public static class ContextDataQualityGate
{
    public static ContextDataQualityResult Evaluate(
        Guid tenantId,
        ContextScope scope,
        string sourceType,
        string sourceRecordId,
        DateTimeOffset observedAtUtc,
        DateTimeOffset? ingestedAtUtc,
        bool isQuarantined,
        string? entityType,
        string? entityId,
        DateTimeOffset nowUtc,
        TimeSpan staleThreshold,
        HashSet<string> seenDedupKeys)
    {
        if (scope.TenantId != tenantId)
        {
            return Reject(ContextKnowledgeDataQualityReasons.InvalidTenant, "tenant mismatch");
        }

        if (isQuarantined)
        {
            return Reject(ContextKnowledgeDataQualityReasons.Quarantined, "quarantined source");
        }

        if (string.IsNullOrWhiteSpace(sourceRecordId))
        {
            return Reject(ContextKnowledgeDataQualityReasons.Incomplete, "missing source record id");
        }

        if (observedAtUtc > nowUtc.AddMinutes(5))
        {
            return Reject(ContextKnowledgeDataQualityReasons.InvalidTimestamp, "future timestamp");
        }

        if (nowUtc - observedAtUtc > staleThreshold && sourceType != ContextKnowledgeSourceTypes.VerifiedKnowledge)
        {
            return Reject(ContextKnowledgeDataQualityReasons.Stale, "stale observation");
        }

        if (!ContextKnowledgeDefaultSourcePriorities.Priorities.ContainsKey(sourceType))
        {
            return Reject(ContextKnowledgeDataQualityReasons.UntrustedSource, sourceType);
        }

        if (!string.IsNullOrWhiteSpace(entityType) && !string.IsNullOrWhiteSpace(entityId) &&
            !string.Equals(scope.EntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
            !IsRelatedEntity(scope, entityType, entityId))
        {
            return Reject(ContextKnowledgeDataQualityReasons.InvalidRelationship, $"{entityType}:{entityId}");
        }

        var dedupKey = ContextDeduplicationEngine.BuildDedupKey(tenantId, sourceType, sourceRecordId, entityType, entityId);
        if (seenDedupKeys.Contains(dedupKey))
        {
            return Reject(ContextKnowledgeDataQualityReasons.Duplicate, dedupKey);
        }

        seenDedupKeys.Add(dedupKey);
        return new ContextDataQualityResult(true, ContextKnowledgeDataQualityReasons.Accepted, ingestedAtUtc?.ToString("O"));
    }

    private static bool IsRelatedEntity(ContextScope scope, string entityType, string entityId) =>
        string.Equals(scope.EntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(scope.EntityId, entityId, StringComparison.OrdinalIgnoreCase);

    private static ContextDataQualityResult Reject(string code, string detail) =>
        new(false, code, detail);
}

public static class ContextDeduplicationEngine
{
    public static string BuildDedupKey(
        Guid tenantId,
        string sourceType,
        string sourceRecordId,
        string? entityType = null,
        string? entityId = null) =>
        $"{tenantId:N}:{sourceType}:{sourceRecordId}:{entityType}:{entityId}".ToLowerInvariant();

    public static string BuildEventSpineAlignedKey(Guid tenantId, Guid connectorId, string sourceEventId) =>
        $"{tenantId:N}:{connectorId:N}:{sourceEventId}";

    public static IReadOnlyList<T> DeduplicateByKey<T>(
        IEnumerable<T> items,
        Func<T, string> keySelector)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<T>();
        foreach (var item in items)
        {
            var key = keySelector(item);
            if (seen.Add(key))
            {
                result.Add(item);
            }
        }

        return result;
    }
}

public static class ContextPriorityEngine
{
    public static IReadOnlyList<ContextItem> Rank(IReadOnlyList<ContextItem> items) =>
        items
            .OrderBy(i => i.PriorityRank)
            .ThenByDescending(i => i.Provenance.FreshnessScore)
            .ThenByDescending(i => i.Provenance.Reliability)
            .ThenByDescending(i => i.Provenance.TimestampUtc)
            .ToList();
}

public static class ContextBudgetEngine
{
    public static IReadOnlyList<ContextItem> Apply(
        IReadOnlyList<ContextItem> rankedItems,
        ContextBudget budget)
    {
        var selected = new List<ContextItem>();
        var payloadBytes = 0;
        var eventCount = 0;
        var historicalCases = 0;
        var knowledgeCount = 0;

        foreach (var item in rankedItems)
        {
            var itemBytes = Encoding.UTF8.GetByteCount(item.Summary);
            if (payloadBytes + itemBytes > budget.MaxPayloadBytes)
            {
                continue;
            }

            if (item.ItemKind == "event" && eventCount >= budget.MaxEventCount)
            {
                continue;
            }

            if (item.ItemKind == "historical_case" && historicalCases >= budget.MaxHistoricalCases)
            {
                continue;
            }

            if (item.ItemKind == "verified_knowledge" && knowledgeCount >= budget.MaxKnowledgeRecords)
            {
                continue;
            }

            selected.Add(item);
            payloadBytes += itemBytes;
            switch (item.ItemKind)
            {
                case "event": eventCount++; break;
                case "historical_case": historicalCases++; break;
                case "verified_knowledge": knowledgeCount++; break;
            }
        }

        return selected;
    }
}

public static class HybridRetrievalEngine
{
    public static IReadOnlyList<ContextItem> Retrieve(
        HybridRetrievalQuery query,
        CanonicalOperationalStateResult? canonicalState,
        IReadOnlyList<CanonicalProductionEvent> events,
        IReadOnlyList<DigitalWorkCase> relatedCases,
        IReadOnlyList<FactoryKnowledgeRecordSummary> verifiedKnowledge,
        DateTimeOffset nowUtc,
        HashSet<string> seenDedupKeys)
    {
        var items = new List<ContextItem>();

        if (canonicalState is not null && canonicalState.ResolutionStatus == ContextKnowledgeResolutionStatuses.Resolved)
        {
            items.Add(new ContextItem(
                "canonical_state",
                $"{canonicalState.EntityType}:{canonicalState.EntityId}",
                $"Canonical state: {canonicalState.ResolvedStateValue}",
                new ContextProvenance(
                    ContextKnowledgeSourceTypes.CanonicalEvent,
                    canonicalState.EntityId,
                    query.Scope.TenantId,
                    canonicalState.EntityType,
                    canonicalState.EntityId,
                    canonicalState.EvaluatedAtUtc,
                    canonicalState.EvaluatedAtUtc,
                    1m,
                    canonicalState.Confidence,
                    "RESOLVED",
                    ContextKnowledgeDataTiers.HotOperational),
                1,
                ContextKnowledgeDataTiers.HotOperational));
        }

        var windowEnd = query.Scope.WindowEndUtc ?? nowUtc;
        var windowStart = query.Scope.WindowStartUtc ?? windowEnd.AddHours(-4);

        foreach (var evt in events
                     .Where(e => e.OccurredAtUtc >= windowStart && e.OccurredAtUtc <= windowEnd)
                     .OrderByDescending(e => e.OccurredAtUtc))
        {
            var quality = ContextDataQualityGate.Evaluate(
                query.Scope.TenantId,
                query.Scope,
                ContextKnowledgeSourceTypes.CanonicalEvent,
                evt.IdempotencyKey,
                evt.OccurredAtUtc,
                evt.ReceivedAtUtc,
                false,
                ContextKnowledgeScopeEntityTypes.Station,
                evt.StationId.ToString(),
                nowUtc,
                TimeSpan.FromHours(24),
                seenDedupKeys);

            if (!quality.Accepted)
            {
                continue;
            }

            seenDedupKeys.Add(ContextDeduplicationEngine.BuildDedupKey(
                query.Scope.TenantId,
                ContextKnowledgeSourceTypes.CanonicalEvent,
                evt.IdempotencyKey,
                ContextKnowledgeScopeEntityTypes.Station,
                evt.StationId.ToString()));

            var tier = ContextDataTierClassifier.Classify(evt.OccurredAtUtc, false, nowUtc);
            items.Add(new ContextItem(
                "event",
                evt.Id.ToString(),
                $"{evt.EventType} @ {evt.OccurredAtUtc:O}",
                new ContextProvenance(
                    ContextKnowledgeSourceTypes.CanonicalEvent,
                    evt.Id.ToString(),
                    evt.TenantId,
                    ContextKnowledgeScopeEntityTypes.Station,
                    evt.StationId?.ToString() ?? "—",
                    evt.OccurredAtUtc,
                    evt.ReceivedAtUtc,
                    FreshnessScore(evt.OccurredAtUtc, nowUtc),
                    0.9m,
                    "MEASURED",
                    tier),
                tier == ContextKnowledgeDataTiers.HotOperational ? 4 : 6,
                tier));
        }

        foreach (var caseRow in relatedCases.OrderByDescending(c => c.UpdatedAtUtc))
        {
            items.Add(new ContextItem(
                "related_case",
                caseRow.Id.ToString(),
                $"{caseRow.Title} ({caseRow.Status})",
                new ContextProvenance(
                    ContextKnowledgeSourceTypes.DigitalWorkCase,
                    caseRow.Id.ToString(),
                    caseRow.TenantId,
                    ContextKnowledgeScopeEntityTypes.DigitalWorkCase,
                    caseRow.Id.ToString(),
                    caseRow.UpdatedAtUtc,
                    caseRow.UpdatedAtUtc,
                    FreshnessScore(caseRow.UpdatedAtUtc, nowUtc),
                    0.85m,
                    caseRow.Status,
                    ContextKnowledgeDataTiers.HotOperational),
                5,
                ContextKnowledgeDataTiers.HotOperational));
        }

        foreach (var knowledge in verifiedKnowledge)
        {
            items.Add(new ContextItem(
                "verified_knowledge",
                knowledge.Id.ToString(),
                $"{knowledge.ProblemSymptom} → {knowledge.RootCause}",
                new ContextProvenance(
                    ContextKnowledgeSourceTypes.VerifiedKnowledge,
                    knowledge.Id.ToString(),
                    query.Scope.TenantId,
                    ContextKnowledgeScopeEntityTypes.DigitalWorkCase,
                    knowledge.SourceCaseId?.ToString() ?? "—",
                    nowUtc,
                    nowUtc,
                    0.5m,
                    knowledge.Confidence,
                    knowledge.PromotionStatus,
                    ContextKnowledgeDataTiers.VerifiedFactoryKnowledge),
                7,
                ContextKnowledgeDataTiers.VerifiedFactoryKnowledge));
        }

        return items.Take(query.MaxResults).ToList();
    }

    private static decimal FreshnessScore(DateTimeOffset observedAtUtc, DateTimeOffset nowUtc)
    {
        var hours = Math.Max(0, (nowUtc - observedAtUtc).TotalHours);
        return Math.Max(0m, 1m - (decimal)(hours / 48d));
    }
}

public static class KnowledgePromotionGate
{
    public static KnowledgePromotionResult Evaluate(KnowledgePromotionRequest request)
    {
        var rejections = new List<string>();

        if (!request.HasVerifiedOutcome)
        {
            rejections.Add("verified-outcome-required");
        }

        if (!request.HasSufficientEvidence)
        {
            rejections.Add("insufficient-evidence");
        }

        if (request.HasUnresolvedConflict)
        {
            rejections.Add("unresolved-conflict");
        }

        if (!request.ObservationCompleted)
        {
            rejections.Add("observation-incomplete");
        }

        if (string.IsNullOrWhiteSpace(request.ProblemSymptom) || string.IsNullOrWhiteSpace(request.RootCause))
        {
            rejections.Add("incomplete-knowledge-record");
        }

        if (rejections.Count > 0)
        {
            return new KnowledgePromotionResult(
                false,
                ContextKnowledgePromotionStatuses.LearningOnly,
                null,
                rejections);
        }

        return new KnowledgePromotionResult(
            true,
            ContextKnowledgePromotionStatuses.Verified,
            null,
            []);
    }

    public static string BuildKnowledgeKey(Guid sourceCaseId, string problemSymptom) =>
        $"{sourceCaseId:N}:{problemSymptom.Trim().ToLowerInvariant()}";
}

public static class ContextSnapshotFactory
{
    public static ContextSnapshotSummary CreateSummary(
        Guid id,
        ContextSnapshotRequest request,
        IReadOnlyList<ContextItem> selectedItems,
        ContextBuildMetrics metrics,
        DateTimeOffset createdAtUtc)
    {
        var snapshotCode = request.SnapshotCode
            ?? $"ctx-{createdAtUtc:yyyyMMddHHmmss}-{id:N}";
        var materialized = JsonSerializer.Serialize(new
        {
            canonicalState = request.ContextPackage.CanonicalState,
            items = selectedItems.Select(i => new { i.ItemKind, i.ReferenceId, i.Summary }).ToList(),
            failureState = request.ContextPackage.FailureState,
        });

        var provenance = JsonSerializer.Serialize(selectedItems.Select(i => i.Provenance).ToList());
        var metricsJson = JsonSerializer.Serialize(metrics);

        return new ContextSnapshotSummary(
            id,
            snapshotCode,
            request.DigitalWorkCaseId,
            createdAtUtc,
            selectedItems.Count,
            materialized,
            provenance,
            metricsJson);
    }
}

public static class TelemetryDownsamplingEngine
{
    public static IReadOnlyList<TelemetryAggregateBucket> BuildBuckets(
        IReadOnlyList<TelemetryDownsampleInput> samples,
        DateTimeOffset nowUtc)
    {
        if (samples.Count == 0)
        {
            return [];
        }

        var buckets = new List<TelemetryAggregateBucket>();
        buckets.Add(Aggregate(samples, ContextKnowledgeAggregationGranularities.Minute, TimeSpan.FromMinutes(1)));
        buckets.Add(Aggregate(samples, ContextKnowledgeAggregationGranularities.Hour, TimeSpan.FromHours(1)));
        buckets.Add(Aggregate(samples, ContextKnowledgeAggregationGranularities.Day, TimeSpan.FromDays(1)));

        if (samples.Any(s => s.PreserveRawWindow))
        {
            var preserve = samples.Where(s => s.PreserveRawWindow).ToList();
            buckets.Add(Aggregate(preserve, ContextKnowledgeAggregationGranularities.Raw, TimeSpan.Zero));
        }

        return buckets;
    }

    private static TelemetryAggregateBucket Aggregate(
        IReadOnlyList<TelemetryDownsampleInput> samples,
        string granularity,
        TimeSpan window)
    {
        var ordered = samples.OrderBy(s => s.ObservedAtUtc).ToList();
        var start = ordered[0].ObservedAtUtc;
        var end = window == TimeSpan.Zero ? ordered[^1].ObservedAtUtc : start + window;
        return new TelemetryAggregateBucket(
            granularity,
            start,
            end,
            ordered.Average(s => s.Value),
            ordered.Count,
            samples.Any(s => s.PreserveRawWindow));
    }
}

public static class DataLifecyclePolicyEngine
{
    public static string ResolveTier(
        string dataType,
        DateTimeOffset observedAtUtc,
        IReadOnlyList<DataLifecyclePolicyDefinition> policies,
        DateTimeOffset nowUtc)
    {
        var policy = policies.FirstOrDefault(p =>
            p.IsActive && string.Equals(p.DataType, dataType, StringComparison.OrdinalIgnoreCase));

        var hotHours = policy?.HotRetentionHours ?? 24;
        var warmDays = policy?.WarmRetentionDays ?? 30;

        var age = nowUtc - observedAtUtc;
        if (age <= TimeSpan.FromHours(hotHours))
        {
            return ContextKnowledgeLifecycleTiers.Hot;
        }

        if (age <= TimeSpan.FromDays(warmDays))
        {
            return ContextKnowledgeLifecycleTiers.Warm;
        }

        return ContextKnowledgeLifecycleTiers.Cold;
    }
}

public static class ContextBuilderOrchestrator
{
    public static WorkerContextPackage Build(
        ContextBuildRequest request,
        CanonicalOperationalStateResult? canonicalState,
        IReadOnlyList<CanonicalProductionEvent> events,
        IReadOnlyList<DigitalWorkCase> relatedCases,
        IReadOnlyList<FactoryKnowledgeRecordSummary> verifiedKnowledge,
        DateTimeOffset nowUtc)
    {
        var stopwatch = Stopwatch.StartNew();
        var seenDedupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rejected = 0;
        var duplicate = 0;
        var stale = 0;
        var conflict = 0;

        string? failureState = null;
        if (canonicalState?.ResolutionStatus == ContextKnowledgeResolutionStatuses.Conflicted)
        {
            failureState = ContextKnowledgeFailureStates.ConflictedState;
            conflict++;
        }
        else if (canonicalState?.ResolutionStatus == ContextKnowledgeResolutionStatuses.Unknown)
        {
            failureState = ContextKnowledgeFailureStates.InsufficientContext;
        }

        var retrievalQuery = new HybridRetrievalQuery(request.Scope, MaxResults: request.Budget.MaxEventCount * 2);
        var retrieved = HybridRetrievalEngine.Retrieve(
            retrievalQuery,
            canonicalState,
            events,
            relatedCases,
            request.IncludeVerifiedKnowledge ? verifiedKnowledge : [],
            nowUtc,
            seenDedupKeys);

        rejected = events.Count - retrieved.Count(i => i.ItemKind == "event");
        duplicate = Math.Max(0, events.Count - retrieved.Count);
        stale = retrieved.Count(i => i.DataTier == ContextKnowledgeDataTiers.HistoricalOperational);

        var ranked = ContextPriorityEngine.Rank(retrieved);
        var bounded = ContextBudgetEngine.Apply(ranked, request.Budget);

        if (bounded.Count == 0 && failureState is null)
        {
            failureState = ContextKnowledgeFailureStates.InsufficientContext;
        }

        // Historical knowledge must not override hot canonical state
        if (canonicalState?.ResolutionStatus == ContextKnowledgeResolutionStatuses.Resolved &&
            bounded.Any(i => i.ItemKind == "verified_knowledge" && i.PriorityRank < 4))
        {
            bounded = bounded
                .Where(i => i.ItemKind != "verified_knowledge" || i.PriorityRank >= 4)
                .ToList();
        }

        stopwatch.Stop();
        var payloadBytes = bounded.Sum(i => Encoding.UTF8.GetByteCount(i.Summary));
        var metrics = new ContextBuildMetrics(
            stopwatch.ElapsedMilliseconds,
            events.Count + relatedCases.Count + verifiedKnowledge.Count,
            rejected,
            duplicate,
            stale,
            conflict,
            bounded.Count(i => i.ItemKind == "verified_knowledge"),
            bounded.Count,
            payloadBytes,
            failureState);

        return new WorkerContextPackage(
            request.Scope,
            canonicalState,
            bounded,
            verifiedKnowledge,
            metrics,
            failureState,
            nowUtc);
    }
}

public static class ContextKnowledgeHashing
{
    public static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
