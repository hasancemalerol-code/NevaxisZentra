using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.Tenancy;
using NevaxisZentra.Persistence.ThroughputIntelligence;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.ContinuousOptimization;

public interface IContinuousOptimizationService
{
    Task<ContinuousOptimizationTriggerResult> EvaluateTriggerAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        ContinuousOptimizationTriggerRequest trigger,
        CancellationToken cancellationToken = default);

    Task<ContinuousOptimizationControlCenter> GetControlCenterAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<ContinuousOptimizationHistorySnapshot> GetHistoryAsync(
        Guid facilityId,
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    Task<ContinuousOptimizationRunSummary?> GetRunAsync(
        Guid facilityId,
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<ContinuousOptimizationExplainability?> GetExplainabilityAsync(
        Guid facilityId,
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OptimizationOpportunity>> GetActiveOpportunitiesAsync(
        Guid facilityId,
        Guid? lineId,
        CancellationToken cancellationToken = default);

    Task<ContinuousOptimizationPolicySnapshot> UpsertPolicyAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertContinuousOptimizationPolicyRequest request,
        CancellationToken cancellationToken = default);

    Task<ContinuousOptimizationGoldenContinuousResult> RunGoldenContinuousDemoAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<ContinuousOptimizationGoldenNoOpResult> RunGoldenNoOpDemoAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken = default);
}

public sealed class ContinuousOptimizationService(
    NevaxisZentraDbContext db,
    TenantDatabaseSession tenantDatabaseSession,
    IThroughputIntelligenceService throughputIntelligence,
    IThroughputOptimizerService throughputOptimizer,
    IDigitalWorkforceRuntimeService workforceRuntime) : IContinuousOptimizationService
{
    public Task<ContinuousOptimizationTriggerResult> EvaluateTriggerAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        ContinuousOptimizationTriggerRequest trigger,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => EvaluateTriggerCoreAsync(facilityId, tenantId, subjectId, trigger, ct),
            cancellationToken);

    public Task<ContinuousOptimizationControlCenter> GetControlCenterAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetControlCenterCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<ContinuousOptimizationHistorySnapshot> GetHistoryAsync(
        Guid facilityId,
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetHistoryCoreAsync(facilityId, entityType, entityId, ct),
            cancellationToken);

    public Task<ContinuousOptimizationRunSummary?> GetRunAsync(
        Guid facilityId,
        Guid runId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetRunCoreAsync(facilityId, runId, ct),
            cancellationToken);

    public Task<ContinuousOptimizationExplainability?> GetExplainabilityAsync(
        Guid facilityId,
        Guid runId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetExplainabilityCoreAsync(facilityId, runId, ct),
            cancellationToken);

    public Task<IReadOnlyList<OptimizationOpportunity>> GetActiveOpportunitiesAsync(
        Guid facilityId,
        Guid? lineId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<OptimizationOpportunity>>(
            ct => GetActiveOpportunitiesCoreAsync(facilityId, lineId, ct),
            cancellationToken);

    public Task<ContinuousOptimizationPolicySnapshot> UpsertPolicyAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertContinuousOptimizationPolicyRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => UpsertPolicyCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<ContinuousOptimizationGoldenContinuousResult> RunGoldenContinuousDemoAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenContinuousDemoCoreAsync(facilityId, tenantId, subjectId, ct),
            cancellationToken);

    public Task<ContinuousOptimizationGoldenNoOpResult> RunGoldenNoOpDemoAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenNoOpDemoCoreAsync(facilityId, tenantId, subjectId, ct),
            cancellationToken);

    private async Task<ContinuousOptimizationTriggerResult> EvaluateTriggerCoreAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        ContinuousOptimizationTriggerRequest trigger,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var lineId = await ResolveLineIdAsync(facilityId, trigger, cancellationToken);
        var policy = await EnsurePolicyAsync(facilityId, tenantId, trigger.OptimizationType, cancellationToken);
        var thresholds = ContinuousOptimizationQualificationEngine.ResolveThresholds(policy);
        var correlationKey = lineId.HasValue
            ? ContinuousOptimizationQualificationEngine.BuildCorrelationKey(lineId.Value, trigger.TriggerType)
            : $"{trigger.EntityType}:{trigger.EntityId}:{trigger.TriggerType}";

        var activeRun = await db.ContinuousOptimizationRunRecords
            .Where(r => r.FacilityId == facilityId && r.CorrelationKey == correlationKey)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeRun is not null && !ContinuousOptimizationQualificationEngine.IsTerminalPhase(activeRun.CurrentPhase))
        {
            var existingQualification = new ContinuousOptimizationQualificationResult(
                false,
                ContinuousOptimizationFailureCodes.DuplicateOpportunity,
                "Active optimization run already exists for this condition.",
                thresholds);
            return BuildTriggerResult(
                ContinuousOptimizationOutcomes.Blocked,
                ContinuousOptimizationFailureCodes.DuplicateOpportunity,
                existingQualification,
                null,
                MapRunSummary(activeRun, null),
                false,
                stopwatch.ElapsedMilliseconds);
        }

        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var lastRunAtUtc = await db.ContinuousOptimizationRunRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.CorrelationKey == correlationKey)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .Select(r => (DateTimeOffset?)r.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var runsToday = lineId.HasValue
            ? await db.ContinuousOptimizationRunRecords.AsNoTracking()
                .CountAsync(
                    r => r.FacilityId == facilityId
                        && r.LineId == lineId
                        && r.CreatedAtUtc >= todayStart,
                    cancellationToken)
            : await db.ContinuousOptimizationRunRecords.AsNoTracking()
                .CountAsync(
                    r => r.FacilityId == facilityId
                        && r.EntityType == trigger.EntityType
                        && r.EntityId == trigger.EntityId
                        && r.CreatedAtUtc >= todayStart,
                    cancellationToken);

        var qualification = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger,
            thresholds,
            policy.IsEnabled,
            policy.IsPaused,
            activeRun is not null && !ContinuousOptimizationQualificationEngine.IsTerminalPhase(activeRun.CurrentPhase)
                ? activeRun
                : null,
            lastRunAtUtc,
            runsToday);

        if (!qualification.Qualified)
        {
            return BuildTriggerResult(
                qualification.ReasonCode,
                qualification.ReasonCode,
                qualification,
                null,
                null,
                false,
                stopwatch.ElapsedMilliseconds);
        }

        OptimizationOpportunity? opportunity = null;
        string? lineCode = null;
        if (trigger.OptimizationType == ContinuousOptimizationTypes.Throughput && lineId.HasValue)
        {
            var overview = await throughputIntelligence.GetLineOverviewAsync(
                facilityId, lineId.Value, "recent", null, null, cancellationToken);
            lineCode = overview.LineCode;
            opportunity = ContinuousOptimizationQualificationEngine.BuildThroughputOpportunity(trigger, overview, thresholds);
            if (!ContinuousOptimizationOpportunityEngine.MeetsMinimumValueThreshold(opportunity, thresholds))
            {
                var belowThreshold = new ContinuousOptimizationQualificationResult(
                    false,
                    ContinuousOptimizationOutcomes.NoSignificantOpportunity,
                    "Expected benefit below minimum value threshold.",
                    thresholds);
                return BuildTriggerResult(
                    ContinuousOptimizationOutcomes.NoSignificantOpportunity,
                    ContinuousOptimizationOutcomes.NoSignificantOpportunity,
                    belowThreshold,
                    null,
                    null,
                    false,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        else
        {
            throw new ContinuousOptimizationException(
                ContinuousOptimizationFailureCodes.InsufficientContext,
                "Only throughput optimization is currently supported for automated execution.");
        }

        var now = DateTimeOffset.UtcNow;
        var runRecord = new ContinuousOptimizationRunRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            LineId = lineId,
            OptimizationType = trigger.OptimizationType,
            TriggerType = trigger.TriggerType,
            EntityType = trigger.EntityType,
            EntityId = trigger.EntityId,
            CurrentPhase = ContinuousOptimizationPhases.Qualifying,
            CorrelationKey = correlationKey,
            QualificationJson = JsonSerializer.Serialize(qualification),
            OpportunityJson = JsonSerializer.Serialize(opportunity),
            ExplainabilityJson = JsonSerializer.Serialize(
                ContinuousOptimizationQualificationEngine.BuildExplainability(trigger, opportunity!, null)),
            MetricsJson = JsonSerializer.Serialize(new { ruleComputeMs = stopwatch.ElapsedMilliseconds }),
            EngineVersion = ContinuousOptimizationEngineVersions.Controller,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.ContinuousOptimizationRunRecords.Add(runRecord);

        db.ContinuousOptimizationOpportunityRecords.Add(new ContinuousOptimizationOpportunityRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            RunId = runRecord.Id,
            OpportunityType = opportunity!.OpportunityType,
            ScopeEntityType = opportunity.ScopeEntityType,
            ScopeEntityId = opportunity.ScopeEntityId,
            Title = opportunity.Title,
            Summary = opportunity.Summary,
            EstimatedBenefit = opportunity.EstimatedBenefit,
            Confidence = opportunity.Confidence,
            RiskClass = opportunity.RiskClass,
            EvidenceJson = JsonSerializer.Serialize(opportunity.Evidence),
            ExpiresAtUtc = opportunity.ExpiresAtUtc,
            CreatedAtUtc = now,
        });

        runRecord.CurrentPhase = ContinuousOptimizationPhases.Analyzing;
        await db.SaveChangesAsync(cancellationToken);

        var workspace = await throughputOptimizer.ObserveLineAsync(
            facilityId, tenantId, lineId!.Value, subjectId, cancellationToken);

        var optimizerRun = await db.ThroughputOptimizerRunRecords
            .FirstAsync(r => r.DigitalWorkCaseId == workspace.CaseId, cancellationToken);

        runRecord.ThroughputOptimizerRunId = optimizerRun.Id;
        runRecord.DigitalWorkCaseId = workspace.CaseId;
        runRecord.CurrentPhase = MapOptimizerPhase(workspace.CurrentPhase);
        runRecord.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return BuildTriggerResult(
            "qualified",
            null,
            qualification,
            opportunity,
            MapRunSummary(runRecord, lineCode),
            false,
            stopwatch.ElapsedMilliseconds);
    }

    private async Task<ContinuousOptimizationGoldenContinuousResult> RunGoldenContinuousDemoCoreAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        await throughputIntelligence.SeedMixLine01Mp2ExtensionAsync(facilityId, tenantId, cancellationToken);

        var line = await db.ProductionLines.AsNoTracking()
            .FirstAsync(l => l.FacilityId == facilityId && l.Code == MixedProductionFixtureCodes.LineCode, cancellationToken);

        await ThroughputIntelligenceFixtureSeeder.EnsureMixLine01Mp2EventHistoryAsync(
            db, tenantId, facilityId, line.Id, cancellationToken);
        await ThroughputOptimizerFixtureSeeder.RetirePostInterventionEventsAsync(db, line.Id, cancellationToken);

        var overview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, line.Id, "recent", null, null, cancellationToken);
        var gap = overview.ProductionGap.GapPerHour;

        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.ThroughputDeviationChanged,
            ContinuousOptimizationTypes.Throughput,
            "line",
            line.Id.ToString(),
            line.Id,
            gap,
            overview.ProductionGap.CurrentGoodPerHour,
            overview.ProductionGap.RequiredGoodPerHour,
            "Golden continuous optimization trigger",
            DateTimeOffset.UtcNow);

        var first = await EvaluateTriggerCoreAsync(facilityId, tenantId, subjectId, trigger, cancellationToken);
        if (first.Run is null)
        {
            throw new ContinuousOptimizationException(
                ContinuousOptimizationFailureCodes.ValidationFailed,
                $"Golden continuous trigger did not create run: {first.ResultState}");
        }

        var duplicate = await EvaluateTriggerCoreAsync(facilityId, tenantId, subjectId, trigger, cancellationToken);
        if (duplicate.Run?.RunId != first.Run.RunId && duplicate.Qualification.Qualified)
        {
            throw new ContinuousOptimizationException(
                ContinuousOptimizationFailureCodes.DuplicateOpportunity,
                "Duplicate golden trigger created a second optimization run.");
        }

        var caseId = first.Run.DigitalWorkCaseId
            ?? throw new ContinuousOptimizationException(
                ContinuousOptimizationFailureCodes.ValidationFailed,
                "Golden continuous run missing DigitalWorkCase linkage.");

        var workspace = await throughputOptimizer.GetWorkspaceAsync(facilityId, caseId, cancellationToken);
        while (workspace.CurrentPhase != ThroughputOptimizerPhases.Closed)
        {
            workspace = await throughputOptimizer.RunLifecycleStepAsync(
                facilityId, tenantId, caseId, subjectId, cancellationToken);

            if (workspace.CurrentPhase == ThroughputOptimizerPhases.Approval && workspace.HasPendingApproval)
            {
                var approval = (await workforceRuntime.ListApprovalsAsync(DigitalWorkApprovalStatuses.Pending, cancellationToken))
                    .First(a => a.CaseId == caseId);
                await workforceRuntime.DecideApprovalAsync(
                    approval.Id,
                    new ApprovalDecisionRequest("approved", "Golden continuous optimization approval", approval.ConcurrencyToken),
                    subjectId,
                    cancellationToken);
                workspace = await throughputOptimizer.RunLifecycleStepAsync(
                    facilityId, tenantId, caseId, subjectId, cancellationToken);
            }

            if (workspace.CurrentPhase == ThroughputOptimizerPhases.ObserveResult)
            {
                await ThroughputOptimizerFixtureSeeder.SeedPostInterventionEventsAsync(
                    db, tenantId, facilityId, line.Id, cancellationToken);
                workspace = await throughputOptimizer.RunLifecycleStepAsync(
                    facilityId, tenantId, caseId, subjectId, cancellationToken);
            }
        }

        var controllerRun = await db.ContinuousOptimizationRunRecords
            .FirstAsync(r => r.Id == first.Run.RunId, cancellationToken);
        controllerRun.CurrentPhase = ContinuousOptimizationPhases.Verified;
        controllerRun.Outcome = workspace.Improvement?.VerificationOutcome == ThroughputOptimizerVerificationOutcomes.VerifiedImprovement
            ? ContinuousOptimizationOutcomes.VerifiedGain
            : ContinuousOptimizationOutcomes.NoMeaningfulGain;
        controllerRun.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var knowledgePromoted = await db.FactoryKnowledgeRecords
            .AnyAsync(k => k.SourceCaseId == caseId, cancellationToken);

        return new ContinuousOptimizationGoldenContinuousResult(
            controllerRun.Id,
            caseId,
            line.Id,
            controllerRun.CurrentPhase,
            controllerRun.Outcome,
            knowledgePromoted,
            "Golden continuous optimization completed.");
    }

    private async Task<ContinuousOptimizationGoldenNoOpResult> RunGoldenNoOpDemoCoreAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var line = await db.ProductionLines.AsNoTracking()
            .FirstOrDefaultAsync(l => l.FacilityId == facilityId && l.Code == MixedProductionFixtureCodes.LineCode, cancellationToken);

        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.InsignificantVariation,
            ContinuousOptimizationTypes.Throughput,
            "line",
            line?.Id.ToString() ?? Guid.Empty.ToString(),
            line?.Id,
            0.1m,
            100m,
            100m,
            "Golden no-op insignificant variation",
            DateTimeOffset.UtcNow);

        var beforeRuns = await db.ContinuousOptimizationRunRecords.CountAsync(cancellationToken);
        var beforeCases = await db.DigitalWorkCases.CountAsync(cancellationToken);

        var result = await EvaluateTriggerCoreAsync(facilityId, tenantId, subjectId, trigger, cancellationToken);

        var afterRuns = await db.ContinuousOptimizationRunRecords.CountAsync(cancellationToken);
        var afterCases = await db.DigitalWorkCases.CountAsync(cancellationToken);

        return new ContinuousOptimizationGoldenNoOpResult(
            result.ResultState,
            result.Qualification.ReasonCode,
            afterRuns > beforeRuns,
            afterCases > beforeCases,
            result.ModelInvoked,
            "Golden no-op completed.");
    }

    private async Task<ContinuousOptimizationControlCenter> GetControlCenterCoreAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var runs = await (
            from run in db.ContinuousOptimizationRunRecords.AsNoTracking()
            where run.FacilityId == facilityId
            orderby run.UpdatedAtUtc descending
            select run).Take(120).ToListAsync(cancellationToken);

        var lineCodes = await db.ProductionLines.AsNoTracking()
            .Where(l => l.FacilityId == facilityId)
            .ToDictionaryAsync(l => l.Id, l => l.Code, cancellationToken);

        var summaries = runs
            .Select(r => MapRunSummary(r, r.LineId.HasValue && lineCodes.TryGetValue(r.LineId.Value, out var code) ? code : null))
            .ToList();

        var activePhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContinuousOptimizationPhases.Detected,
            ContinuousOptimizationPhases.Qualifying,
            ContinuousOptimizationPhases.Analyzing,
            ContinuousOptimizationPhases.Simulating,
            ContinuousOptimizationPhases.Recommended,
            ContinuousOptimizationPhases.Executing,
            ContinuousOptimizationPhases.Observing,
            ContinuousOptimizationPhases.Verifying,
        };

        var opportunities = await GetActiveOpportunitiesCoreAsync(facilityId, null, cancellationToken);
        var detected = runs.Count;
        var qualified = runs.Count(r => r.CurrentPhase != ContinuousOptimizationPhases.NoAction);
        var suppressed = runs.Count(r =>
            r.Outcome == ContinuousOptimizationOutcomes.NoSignificantOpportunity
            || r.FailureState == ContinuousOptimizationFailureCodes.CooldownActive);
        var verified = runs.Count(r => r.Outcome == ContinuousOptimizationOutcomes.VerifiedGain);
        var noGain = runs.Count(r => r.Outcome == ContinuousOptimizationOutcomes.NoMeaningfulGain);

        return new ContinuousOptimizationControlCenter(
            summaries.Where(s => activePhases.Contains(s.CurrentPhase)).Take(12).ToList(),
            opportunities.Take(12).ToList(),
            summaries.Where(s => s.CurrentPhase == ContinuousOptimizationPhases.ApprovalRequired).Take(8).ToList(),
            summaries.Where(s => s.CurrentPhase == ContinuousOptimizationPhases.Observing).Take(8).ToList(),
            summaries
                .Where(s => s.Outcome == ContinuousOptimizationOutcomes.VerifiedGain)
                .Select(s => new ContinuousOptimizationVerifiedGainItem(
                    s.RunId,
                    s.DigitalWorkCaseId,
                    s.LineCode ?? "—",
                    s.OptimizationType,
                    s.Outcome ?? ContinuousOptimizationOutcomes.VerifiedGain,
                    null,
                    "Verified throughput improvement",
                    s.UpdatedAtUtc))
                .Take(12)
                .ToList(),
            summaries.Where(s =>
                    s.Outcome is ContinuousOptimizationOutcomes.Failed or ContinuousOptimizationOutcomes.Blocked
                    || s.CurrentPhase is ContinuousOptimizationPhases.Failed or ContinuousOptimizationPhases.Cancelled)
                .Take(8)
                .ToList(),
            new ContinuousOptimizationKpiSnapshot(
                detected,
                qualified,
                suppressed,
                summaries.Count(s => activePhases.Contains(s.CurrentPhase)),
                verified,
                noGain,
                detected == 0 ? 0m : Math.Round((decimal)qualified / detected, 2),
                qualified == 0 ? 0m : Math.Round((decimal)verified / qualified, 2)),
            DateTimeOffset.UtcNow);
    }

    private async Task<ContinuousOptimizationHistorySnapshot> GetHistoryCoreAsync(
        Guid facilityId,
        string entityType,
        string entityId,
        CancellationToken cancellationToken)
    {
        var runs = await db.ContinuousOptimizationRunRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.EntityType == entityType && r.EntityId == entityId)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .Take(40)
            .ToListAsync(cancellationToken);

        var items = runs.Select(r =>
        {
            var opportunity = DeserializeOpportunity(r.OpportunityJson);
            return new ContinuousOptimizationHistoryItem(
                r.Id,
                r.OptimizationType,
                r.TriggerType,
                r.CurrentPhase,
                string.IsNullOrWhiteSpace(r.Outcome) ? null : r.Outcome,
                opportunity?.Title ?? r.TriggerType,
                r.CreatedAtUtc,
                r.UpdatedAtUtc);
        }).ToList();

        return new ContinuousOptimizationHistorySnapshot(entityType, entityId, items);
    }

    private async Task<ContinuousOptimizationRunSummary?> GetRunCoreAsync(
        Guid facilityId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var run = await db.ContinuousOptimizationRunRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.Id == runId, cancellationToken);
        if (run is null)
        {
            return null;
        }

        string? lineCode = null;
        if (run.LineId.HasValue)
        {
            lineCode = await db.ProductionLines.AsNoTracking()
                .Where(l => l.Id == run.LineId.Value)
                .Select(l => l.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return MapRunSummary(run, lineCode);
    }

    private async Task<ContinuousOptimizationExplainability?> GetExplainabilityCoreAsync(
        Guid facilityId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var run = await db.ContinuousOptimizationRunRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.Id == runId, cancellationToken);
        if (run is null || string.IsNullOrWhiteSpace(run.ExplainabilityJson) || run.ExplainabilityJson == "{}")
        {
            return null;
        }

        return JsonSerializer.Deserialize<ContinuousOptimizationExplainability>(run.ExplainabilityJson);
    }

    private async Task<IReadOnlyList<OptimizationOpportunity>> GetActiveOpportunitiesCoreAsync(
        Guid facilityId,
        Guid? lineId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var terminalPhases = new[]
        {
            ContinuousOptimizationPhases.Verified,
            ContinuousOptimizationPhases.NotVerified,
            ContinuousOptimizationPhases.NoAction,
            ContinuousOptimizationPhases.Failed,
            ContinuousOptimizationPhases.Cancelled,
        };

        var rows = await (
            from opportunity in db.ContinuousOptimizationOpportunityRecords.AsNoTracking()
            join run in db.ContinuousOptimizationRunRecords.AsNoTracking() on opportunity.RunId equals run.Id
            where opportunity.FacilityId == facilityId
                && opportunity.ExpiresAtUtc >= now
                && !terminalPhases.Contains(run.CurrentPhase)
                && (lineId == null || run.LineId == lineId)
            orderby opportunity.EstimatedBenefit descending
            select new { opportunity, run }).Take(20).ToListAsync(cancellationToken);
        var mapped = rows.Select(row => new OptimizationOpportunity(
            row.opportunity.OpportunityType,
            row.opportunity.ScopeEntityType,
            row.opportunity.ScopeEntityId,
            row.run.TriggerType,
            row.opportunity.Title,
            row.opportunity.Summary,
            DeserializeOpportunity(row.run.OpportunityJson)?.BaselineValue ?? 0m,
            DeserializeOpportunity(row.run.OpportunityJson)?.CurrentValue ?? 0m,
            row.opportunity.EstimatedBenefit,
            row.opportunity.Confidence,
            row.opportunity.RiskClass,
            JsonSerializer.Deserialize<List<string>>(row.opportunity.EvidenceJson) ?? [],
            DeserializeOpportunity(row.run.OpportunityJson)?.RequiredActions ?? [],
            DeserializeOpportunity(row.run.OpportunityJson)?.Constraints ?? [],
            row.opportunity.ExpiresAtUtc)).ToList();

        return ContinuousOptimizationOpportunityEngine.Prioritize(mapped).AsReadOnly();
    }

    private async Task<ContinuousOptimizationPolicySnapshot> UpsertPolicyCoreAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertContinuousOptimizationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await db.ContinuousOptimizationPolicyRecords
            .FirstOrDefaultAsync(
                p => p.FacilityId == facilityId
                    && p.OptimizationType == request.OptimizationType
                    && p.ScopeType == request.ScopeType
                    && p.ScopeId == request.ScopeId,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            existing = new ContinuousOptimizationPolicyRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                OptimizationType = request.OptimizationType,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                CreatedAtUtc = now,
            };
            db.ContinuousOptimizationPolicyRecords.Add(existing);
        }

        existing.IsEnabled = request.IsEnabled;
        existing.IsPaused = request.IsPaused;
        existing.ThresholdsJson = JsonSerializer.Serialize(request.Thresholds);
        existing.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);

        return new ContinuousOptimizationPolicySnapshot(
            existing.Id,
            existing.OptimizationType,
            existing.ScopeType,
            existing.ScopeId,
            existing.IsEnabled,
            existing.IsPaused,
            request.Thresholds);
    }

    private async Task<ContinuousOptimizationPolicyRecord> EnsurePolicyAsync(
        Guid facilityId,
        Guid tenantId,
        string optimizationType,
        CancellationToken cancellationToken)
    {
        var policy = await db.ContinuousOptimizationPolicyRecords
            .FirstOrDefaultAsync(
                p => p.FacilityId == facilityId
                    && p.OptimizationType == optimizationType
                    && p.ScopeType == "facility"
                    && p.ScopeId == facilityId.ToString(),
                cancellationToken);

        if (policy is not null)
        {
            return policy;
        }

        var now = DateTimeOffset.UtcNow;
        policy = new ContinuousOptimizationPolicyRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            OptimizationType = optimizationType,
            ScopeType = "facility",
            ScopeId = facilityId.ToString(),
            IsEnabled = true,
            ThresholdsJson = JsonSerializer.Serialize(new ContinuousOptimizationPolicyThresholds()),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.ContinuousOptimizationPolicyRecords.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private async Task<Guid?> ResolveLineIdAsync(
        Guid facilityId,
        ContinuousOptimizationTriggerRequest trigger,
        CancellationToken cancellationToken)
    {
        if (trigger.LineId.HasValue)
        {
            return trigger.LineId;
        }

        if (trigger.EntityType.Equals("line", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(trigger.EntityId, out var parsedLineId))
        {
            return parsedLineId;
        }

        if (trigger.OptimizationType == ContinuousOptimizationTypes.Throughput)
        {
            var line = await db.ProductionLines.AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.FacilityId == facilityId && l.Code == MixedProductionFixtureCodes.LineCode,
                    cancellationToken);
            return line?.Id;
        }

        return null;
    }

    private static ContinuousOptimizationTriggerResult BuildTriggerResult(
        string resultState,
        string? failureState,
        ContinuousOptimizationQualificationResult qualification,
        OptimizationOpportunity? opportunity,
        ContinuousOptimizationRunSummary? run,
        bool modelInvoked,
        long latencyMs) =>
        new(resultState, failureState, qualification, opportunity, run, modelInvoked, latencyMs);

    private static ContinuousOptimizationRunSummary MapRunSummary(
        ContinuousOptimizationRunRecord run,
        string? lineCode) =>
        new(
            run.Id,
            run.CurrentPhase,
            run.OptimizationType,
            run.TriggerType,
            string.IsNullOrWhiteSpace(run.Outcome) ? null : run.Outcome,
            run.DigitalWorkCaseId,
            run.ThroughputOptimizerRunId,
            run.LineId,
            lineCode,
            run.UpdatedAtUtc);

    private static string MapOptimizerPhase(string optimizerPhase) =>
        optimizerPhase switch
        {
            ThroughputOptimizerPhases.Observe => ContinuousOptimizationPhases.Analyzing,
            ThroughputOptimizerPhases.Diagnose => ContinuousOptimizationPhases.Analyzing,
            ThroughputOptimizerPhases.Simulate => ContinuousOptimizationPhases.Simulating,
            ThroughputOptimizerPhases.Decide => ContinuousOptimizationPhases.Recommended,
            ThroughputOptimizerPhases.Approval => ContinuousOptimizationPhases.ApprovalRequired,
            ThroughputOptimizerPhases.Execute => ContinuousOptimizationPhases.Executing,
            ThroughputOptimizerPhases.ObserveResult => ContinuousOptimizationPhases.Observing,
            ThroughputOptimizerPhases.Verify => ContinuousOptimizationPhases.Verifying,
            ThroughputOptimizerPhases.Closed => ContinuousOptimizationPhases.Verified,
            _ => ContinuousOptimizationPhases.Analyzing,
        };

    private static OptimizationOpportunity? DeserializeOpportunity(string json) =>
        string.IsNullOrWhiteSpace(json) || json == "{}"
            ? null
            : JsonSerializer.Deserialize<OptimizationOpportunity>(json);
}

public static class ContinuousOptimizationServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraContinuousOptimization(this IServiceCollection services)
    {
        services.AddScoped<IContinuousOptimizationService, ContinuousOptimizationService>();
        return services;
    }
}
