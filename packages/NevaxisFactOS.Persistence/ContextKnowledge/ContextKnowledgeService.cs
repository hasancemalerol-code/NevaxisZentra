using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.ContextKnowledge;

public interface IContextKnowledgeService
{
    Task<CanonicalOperationalStateResult> ResolveCanonicalStateAsync(
        Guid facilityId,
        Guid tenantId,
        string entityType,
        string entityId,
        IReadOnlyList<ContextSourceClaim> claims,
        CancellationToken cancellationToken = default);

    Task<WorkerContextPackage> BuildWorkerContextAsync(
        ContextBuildRequest request,
        CancellationToken cancellationToken = default);

    Task<KnowledgePromotionResult> PromoteKnowledgeAsync(
        Guid facilityId,
        Guid tenantId,
        KnowledgePromotionRequest request,
        CancellationToken cancellationToken = default);

    Task<ContextSnapshotSummary> CreateSnapshotAsync(
        Guid facilityId,
        Guid tenantId,
        ContextSnapshotRequest request,
        CancellationToken cancellationToken = default);

    Task<ContextSnapshotSummary?> GetSnapshotAsync(
        Guid facilityId,
        Guid snapshotId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FactoryKnowledgeRecordSummary>> ListVerifiedKnowledgeAsync(
        Guid facilityId,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DataLifecyclePolicyDefinition>> ListLifecyclePoliciesAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<DataLifecyclePolicyDefinition> UpsertLifecyclePolicyAsync(
        Guid facilityId,
        Guid tenantId,
        DataLifecyclePolicyDefinition policy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryAggregateBucket>> ProjectTelemetryAggregatesAsync(
        Guid facilityId,
        IReadOnlyList<TelemetryDownsampleInput> samples,
        CancellationToken cancellationToken = default);
}

public sealed class ContextKnowledgeService : IContextKnowledgeService
{
    private readonly NevaxisFactOSDbContext db;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public ContextKnowledgeService(
        NevaxisFactOSDbContext db,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.db = db;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<CanonicalOperationalStateResult> ResolveCanonicalStateAsync(
        Guid facilityId,
        Guid tenantId,
        string entityType,
        string entityId,
        IReadOnlyList<ContextSourceClaim> claims,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ResolveCanonicalStateCoreAsync(facilityId, tenantId, entityType, entityId, claims, ct),
            cancellationToken);

    public Task<WorkerContextPackage> BuildWorkerContextAsync(
        ContextBuildRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => BuildWorkerContextCoreAsync(request, ct),
            cancellationToken);

    public Task<KnowledgePromotionResult> PromoteKnowledgeAsync(
        Guid facilityId,
        Guid tenantId,
        KnowledgePromotionRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => PromoteKnowledgeCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<ContextSnapshotSummary> CreateSnapshotAsync(
        Guid facilityId,
        Guid tenantId,
        ContextSnapshotRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CreateSnapshotCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<ContextSnapshotSummary?> GetSnapshotAsync(
        Guid facilityId,
        Guid snapshotId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetSnapshotCoreAsync(facilityId, snapshotId, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryKnowledgeRecordSummary>> ListVerifiedKnowledgeAsync(
        Guid facilityId,
        int take = 20,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListVerifiedKnowledgeCoreAsync(facilityId, take, ct),
            cancellationToken);

    public Task<IReadOnlyList<DataLifecyclePolicyDefinition>> ListLifecyclePoliciesAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListLifecyclePoliciesCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<DataLifecyclePolicyDefinition> UpsertLifecyclePolicyAsync(
        Guid facilityId,
        Guid tenantId,
        DataLifecyclePolicyDefinition policy,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => UpsertLifecyclePolicyCoreAsync(facilityId, tenantId, policy, ct),
            cancellationToken);

    public Task<IReadOnlyList<TelemetryAggregateBucket>> ProjectTelemetryAggregatesAsync(
        Guid facilityId,
        IReadOnlyList<TelemetryDownsampleInput> samples,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ProjectTelemetryAggregatesCoreAsync(facilityId, tenantId: Guid.Empty, samples, ct),
            cancellationToken);

    private async Task<CanonicalOperationalStateResult> ResolveCanonicalStateCoreAsync(
        Guid facilityId,
        Guid tenantId,
        string entityType,
        string entityId,
        IReadOnlyList<ContextSourceClaim> claims,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var resolved = CanonicalOperationalStateResolver.Resolve(entityType, entityId, claims, now, TimeSpan.FromHours(4));

        var existing = await db.CanonicalOperationalStateRecords.FirstOrDefaultAsync(
            r => r.FacilityId == facilityId && r.EntityType == entityType && r.EntityId == entityId,
            cancellationToken);

        if (existing is null)
        {
            existing = new CanonicalOperationalStateRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                CreatedAtUtc = now,
            };
            db.CanonicalOperationalStateRecords.Add(existing);
        }

        existing.EntityType = entityType;
        existing.EntityId = entityId;
        existing.ResolutionStatus = resolved.ResolutionStatus;
        existing.ResolvedStateValue = resolved.ResolvedStateValue;
        existing.Confidence = resolved.Confidence;
        existing.SourceClaimsJson = JsonSerializer.Serialize(resolved.SourceClaims);
        existing.ConflictReasonsJson = JsonSerializer.Serialize(resolved.ConflictReasons);
        existing.EvaluatedAtUtc = resolved.EvaluatedAtUtc;
        existing.UpdatedAtUtc = now;

        await db.SaveChangesAsync(cancellationToken);
        return resolved;
    }

    private async Task<WorkerContextPackage> BuildWorkerContextCoreAsync(
        ContextBuildRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var facilityId = request.Scope.FacilityId;

        CanonicalOperationalStateResult? canonical = null;
        var stateRow = await db.CanonicalOperationalStateRecords.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.FacilityId == facilityId
                    && r.EntityType == request.Scope.EntityType
                    && r.EntityId == request.Scope.EntityId,
                cancellationToken);

        if (stateRow is not null)
        {
            var claims = JsonSerializer.Deserialize<List<ContextSourceClaim>>(stateRow.SourceClaimsJson) ?? [];
            canonical = new CanonicalOperationalStateResult(
                stateRow.EntityType,
                stateRow.EntityId,
                stateRow.ResolutionStatus,
                stateRow.ResolvedStateValue,
                stateRow.Confidence,
                claims,
                JsonSerializer.Deserialize<List<string>>(stateRow.ConflictReasonsJson) ?? [],
                stateRow.EvaluatedAtUtc);
        }

        var eventsQuery = db.CanonicalProductionEvents.AsNoTracking()
            .Where(e => e.FacilityId == facilityId);

        if (request.Scope.LineId.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.LineId == request.Scope.LineId.Value);
        }

        if (request.Scope.StationId.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.StationId == request.Scope.StationId.Value);
        }

        var windowEnd = request.Scope.WindowEndUtc ?? now;
        var windowStart = request.Scope.WindowStartUtc ?? windowEnd.AddHours(-request.Budget.MaxTimeWindowHours);
        eventsQuery = eventsQuery.Where(e => e.OccurredAtUtc >= windowStart && e.OccurredAtUtc <= windowEnd);

        var events = await eventsQuery
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(request.Budget.MaxEventCount * 3)
            .ToListAsync(cancellationToken);

        var relatedCases = new List<DigitalWorkCase>();
        if (request.IncludeRelatedCases)
        {
            var caseQuery = db.DigitalWorkCases.AsNoTracking()
                .Where(c => c.FacilityId == facilityId && c.Status != DigitalWorkCaseStatuses.Closed);

            if (request.DigitalWorkCaseId.HasValue)
            {
                var caseId = request.DigitalWorkCaseId.Value;
                caseQuery = caseQuery.Where(c => c.Id == caseId || c.ParentCaseId == caseId || c.OrchestrationRootCaseId == caseId);
            }

            relatedCases = await caseQuery
                .OrderByDescending(c => c.UpdatedAtUtc)
                .Take(request.Budget.MaxHistoricalCases)
                .ToListAsync(cancellationToken);
        }

        var verifiedKnowledge = request.IncludeVerifiedKnowledge
            ? await ListVerifiedKnowledgeCoreAsync(facilityId, request.Budget.MaxKnowledgeRecords, cancellationToken)
            : [];

        return ContextBuilderOrchestrator.Build(
            request,
            canonical,
            events,
            relatedCases,
            verifiedKnowledge,
            now);
    }

    private async Task<KnowledgePromotionResult> PromoteKnowledgeCoreAsync(
        Guid facilityId,
        Guid tenantId,
        KnowledgePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var evaluation = KnowledgePromotionGate.Evaluate(request);
        if (!evaluation.Promoted)
        {
            var learningRecord = await UpsertKnowledgeRecordAsync(
                facilityId,
                tenantId,
                request,
                evaluation.PromotionStatus,
                cancellationToken);
            return evaluation with { KnowledgeRecordId = learningRecord.Id };
        }

        var record = await UpsertKnowledgeRecordAsync(
            facilityId,
            tenantId,
            request,
            ContextKnowledgePromotionStatuses.Verified,
            cancellationToken);

        return evaluation with { KnowledgeRecordId = record.Id };
    }

    private async Task<FactoryKnowledgeRecord> UpsertKnowledgeRecordAsync(
        Guid facilityId,
        Guid tenantId,
        KnowledgePromotionRequest request,
        string promotionStatus,
        CancellationToken cancellationToken)
    {
        var key = KnowledgePromotionGate.BuildKnowledgeKey(request.SourceCaseId, request.ProblemSymptom);
        var existing = await db.FactoryKnowledgeRecords.FirstOrDefaultAsync(
            r => r.FacilityId == facilityId && r.KnowledgeKey == key,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            existing = new FactoryKnowledgeRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                KnowledgeKey = key,
                CreatedAtUtc = now,
            };
            db.FactoryKnowledgeRecords.Add(existing);
        }

        existing.SourceCaseId = request.SourceCaseId;
        existing.ProblemSymptom = request.ProblemSymptom;
        existing.RootCause = request.RootCause;
        existing.DecisionSummary = request.DecisionSummary;
        existing.ActionsJson = JsonSerializer.Serialize(request.Actions);
        existing.EvidenceRefsJson = JsonSerializer.Serialize(request.EvidenceRefs);
        existing.BeforeStateJson = request.BeforeStateJson;
        existing.AfterStateJson = request.AfterStateJson;
        existing.VerifiedOutcome = request.VerifiedOutcome;
        existing.Confidence = request.Confidence;
        existing.ApplicabilityConstraintsJson = request.ApplicabilityConstraintsJson;
        existing.PromotionStatus = promotionStatus;
        existing.PromotedAtUtc = promotionStatus == ContextKnowledgePromotionStatuses.Verified ? now : null;

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task<ContextSnapshotSummary> CreateSnapshotCoreAsync(
        Guid facilityId,
        Guid tenantId,
        ContextSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var summary = ContextSnapshotFactory.CreateSummary(
            id,
            request,
            request.ContextPackage.Items,
            request.ContextPackage.Metrics,
            now);

        db.ContextSnapshotRecords.Add(new ContextSnapshotRecord
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            DigitalWorkCaseId = request.DigitalWorkCaseId,
            SnapshotCode = summary.SnapshotCode,
            ScopeJson = JsonSerializer.Serialize(request.Scope),
            BudgetJson = JsonSerializer.Serialize(new { request.Scope.WindowStartUtc, request.Scope.WindowEndUtc }),
            MaterializedContextJson = summary.MaterializedContextJson,
            ProvenanceJson = summary.ProvenanceJson,
            MetricsJson = summary.MetricsJson,
            EngineVersion = ContextKnowledgeEngineVersions.Snapshot,
            CreatedBySubjectId = request.CreatedBySubjectId,
            CreatedAtUtc = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        return summary;
    }

    private async Task<ContextSnapshotSummary?> GetSnapshotCoreAsync(
        Guid facilityId,
        Guid snapshotId,
        CancellationToken cancellationToken)
    {
        var row = await db.ContextSnapshotRecords.AsNoTracking()
            .FirstOrDefaultAsync(s => s.FacilityId == facilityId && s.Id == snapshotId, cancellationToken);

        if (row is null)
        {
            return null;
        }

        var metrics = JsonSerializer.Deserialize<ContextBuildMetrics>(row.MetricsJson)
            ?? new ContextBuildMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0, null);

        return new ContextSnapshotSummary(
            row.Id,
            row.SnapshotCode,
            row.DigitalWorkCaseId,
            row.CreatedAtUtc,
            JsonSerializer.Deserialize<List<object>>(row.MaterializedContextJson)?.Count ?? 0,
            row.MaterializedContextJson,
            row.ProvenanceJson,
            row.MetricsJson);
    }

    private async Task<IReadOnlyList<FactoryKnowledgeRecordSummary>> ListVerifiedKnowledgeCoreAsync(
        Guid facilityId,
        int take,
        CancellationToken cancellationToken)
    {
        return await db.FactoryKnowledgeRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.PromotionStatus == ContextKnowledgePromotionStatuses.Verified)
            .OrderByDescending(r => r.PromotedAtUtc)
            .Take(take)
            .Select(r => new FactoryKnowledgeRecordSummary(
                r.Id,
                r.SourceCaseId,
                r.ProblemSymptom,
                r.RootCause,
                r.DecisionSummary,
                r.VerifiedOutcome,
                r.Confidence,
                r.PromotionStatus))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DataLifecyclePolicyDefinition>> ListLifecyclePoliciesCoreAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        return await db.DataLifecyclePolicyRecords.AsNoTracking()
            .Where(p => p.FacilityId == facilityId && p.IsActive)
            .Select(p => new DataLifecyclePolicyDefinition(
                p.DataType,
                p.HotRetentionHours,
                p.WarmRetentionDays,
                p.ColdArchiveDays,
                p.IsActive))
            .ToListAsync(cancellationToken);
    }

    private async Task<DataLifecyclePolicyDefinition> UpsertLifecyclePolicyCoreAsync(
        Guid facilityId,
        Guid tenantId,
        DataLifecyclePolicyDefinition policy,
        CancellationToken cancellationToken)
    {
        var existing = await db.DataLifecyclePolicyRecords.FirstOrDefaultAsync(
            p => p.FacilityId == facilityId && p.DataType == policy.DataType,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            existing = new DataLifecyclePolicyRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                DataType = policy.DataType,
                CreatedAtUtc = now,
            };
            db.DataLifecyclePolicyRecords.Add(existing);
        }

        existing.HotRetentionHours = policy.HotRetentionHours;
        existing.WarmRetentionDays = policy.WarmRetentionDays;
        existing.ColdArchiveDays = policy.ColdArchiveDays;
        existing.IsActive = policy.IsActive;
        existing.UpdatedAtUtc = now;

        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private async Task<IReadOnlyList<TelemetryAggregateBucket>> ProjectTelemetryAggregatesCoreAsync(
        Guid facilityId,
        Guid tenantId,
        IReadOnlyList<TelemetryDownsampleInput> samples,
        CancellationToken cancellationToken)
    {
        var buckets = TelemetryDownsamplingEngine.BuildBuckets(samples, DateTimeOffset.UtcNow);
        if (tenantId != Guid.Empty)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var bucket in buckets)
            {
                db.TelemetryAggregateRecords.Add(new TelemetryAggregateRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    ResourceId = samples[0].ResourceId,
                    SignalType = samples[0].SignalType,
                    Granularity = bucket.Granularity,
                    WindowStartUtc = bucket.WindowStartUtc,
                    WindowEndUtc = bucket.WindowEndUtc,
                    AggregateValue = bucket.AggregateValue,
                    SampleCount = bucket.SampleCount,
                    PreserveRawWindow = bucket.PreserveRawWindow,
                    CreatedAtUtc = now,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        return buckets;
    }
}
