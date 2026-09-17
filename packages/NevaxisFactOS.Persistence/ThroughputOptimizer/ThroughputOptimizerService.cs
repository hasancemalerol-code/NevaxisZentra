using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.ContextKnowledge;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.ModelGateway;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.RealFactoryPilot;
using NevaxisFactOS.Persistence.ThroughputIntelligence;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.ThroughputOptimizer;

public interface IThroughputOptimizerService
{
    Task<ThroughputOptimizerWorkspace> ObserveLineAsync(
        Guid facilityId,
        Guid tenantId,
        Guid lineId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<ThroughputOptimizerWorkspace> GetWorkspaceAsync(
        Guid facilityId,
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<ThroughputOptimizerWorkspace> RunLifecycleStepAsync(
        Guid facilityId,
        Guid tenantId,
        Guid caseId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<ThroughputOptimizerGoldenDemoResult> RunGoldenDemoAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<ThroughputOptimizerExecutiveSummary> GetExecutiveSummaryAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);
}

public sealed class ThroughputOptimizerService : IThroughputOptimizerService
{
    private readonly NevaxisFactOSDbContext db;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly IThroughputIntelligenceService throughputIntelligence;
    private readonly IContextKnowledgeService contextKnowledge;
    private readonly IModelGatewayService modelGateway;
    private readonly IDigitalWorkforceRuntimeService workforceRuntime;
    private readonly IDigitalWorkforceRepository workforceRepository;

    public ThroughputOptimizerService(
        NevaxisFactOSDbContext db,
        TenantDatabaseSession tenantDatabaseSession,
        IThroughputIntelligenceService throughputIntelligence,
        IContextKnowledgeService contextKnowledge,
        IModelGatewayService modelGateway,
        IDigitalWorkforceRuntimeService workforceRuntime,
        IDigitalWorkforceRepository workforceRepository)
    {
        this.db = db;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.throughputIntelligence = throughputIntelligence;
        this.contextKnowledge = contextKnowledge;
        this.modelGateway = modelGateway;
        this.workforceRuntime = workforceRuntime;
        this.workforceRepository = workforceRepository;
    }

    public Task<ThroughputOptimizerWorkspace> ObserveLineAsync(
        Guid facilityId, Guid tenantId, Guid lineId, string subjectId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ObserveLineCoreAsync(facilityId, tenantId, lineId, subjectId, ct),
            cancellationToken);

    public Task<ThroughputOptimizerWorkspace> GetWorkspaceAsync(
        Guid facilityId, Guid caseId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetWorkspaceCoreAsync(facilityId, caseId, ct),
            cancellationToken);

    public Task<ThroughputOptimizerWorkspace> RunLifecycleStepAsync(
        Guid facilityId, Guid tenantId, Guid caseId, string subjectId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunLifecycleStepCoreAsync(facilityId, tenantId, caseId, subjectId, ct),
            cancellationToken);

    public Task<ThroughputOptimizerGoldenDemoResult> RunGoldenDemoAsync(
        Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenDemoCoreAsync(facilityId, tenantId, subjectId, ct),
            cancellationToken);

    public Task<ThroughputOptimizerExecutiveSummary> GetExecutiveSummaryAsync(
        Guid facilityId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetExecutiveSummaryCoreAsync(facilityId, ct),
            cancellationToken);

    private async Task<ThroughputOptimizerExecutiveSummary> GetExecutiveSummaryCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        var runRows = await (
            from run in db.ThroughputOptimizerRunRecords.AsNoTracking()
            join workCase in db.DigitalWorkCases.AsNoTracking() on run.DigitalWorkCaseId equals workCase.Id
            where run.FacilityId == facilityId
            orderby run.UpdatedAtUtc descending
            select new { run, workCase }
        ).ToListAsync(cancellationToken);

        var caseIds = runRows.Select(row => row.run.DigitalWorkCaseId).Distinct().ToList();
        var improvements = await db.VerifiedImprovementRecords.AsNoTracking()
            .Where(r => caseIds.Contains(r.DigitalWorkCaseId))
            .ToDictionaryAsync(r => r.DigitalWorkCaseId, cancellationToken);

        var pendingApprovalCaseIds = await db.DigitalWorkApprovals.AsNoTracking()
            .Where(a => a.Status == DigitalWorkApprovalStatuses.Pending && caseIds.Contains(a.CaseId))
            .Select(a => a.CaseId)
            .ToHashSetAsync(cancellationToken);

        var activeCases = new List<ThroughputOptimizerExecutiveCaseItem>();
        var verifiedToday = new List<ThroughputOptimizerExecutiveCaseItem>();

        foreach (var row in runRows)
        {
            improvements.TryGetValue(row.run.DigitalWorkCaseId, out var improvement);
            var item = MapExecutiveCaseItem(
                row.run,
                row.workCase,
                improvement,
                pendingApprovalCaseIds.Contains(row.run.DigitalWorkCaseId));

            if (row.workCase.Status != DigitalWorkCaseStatuses.Closed)
            {
                if (activeCases.All(existing => existing.CaseId != item.CaseId))
                {
                    activeCases.Add(item);
                }

                continue;
            }

            if (row.workCase.VerificationState != DigitalWorkVerificationResults.Passed)
            {
                continue;
            }

            var closedAt = row.workCase.ClosedAtUtc ?? improvement?.VerifiedAtUtc;
            if (closedAt is null || closedAt < todayStart)
            {
                continue;
            }

            if (verifiedToday.All(existing => existing.CaseId != item.CaseId))
            {
                verifiedToday.Add(item);
            }
        }

        var pendingApprovalCount = await db.DigitalWorkApprovals.AsNoTracking()
            .Where(a => a.Status == DigitalWorkApprovalStatuses.Pending)
            .Join(
                db.DigitalWorkCases.AsNoTracking().Where(c => c.FacilityId == facilityId && c.PrimaryDomain == "throughput"),
                approval => approval.CaseId,
                workCase => workCase.Id,
                (approval, _) => approval)
            .CountAsync(cancellationToken);

        return new ThroughputOptimizerExecutiveSummary(
            activeCases.Count,
            pendingApprovalCount,
            verifiedToday.Count,
            activeCases.Take(8).ToList(),
            verifiedToday.Take(8).ToList());
    }

    private static ThroughputOptimizerExecutiveCaseItem MapExecutiveCaseItem(
        ThroughputOptimizerRunRecord run,
        DigitalWorkCase workCase,
        VerifiedImprovementRecord? improvement,
        bool hasPendingApproval)
    {
        var observation = JsonSerializer.Deserialize<ThroughputGapObservation>(run.ObservationJson);
        var diagnosis = string.IsNullOrWhiteSpace(run.DiagnosisJson) || run.DiagnosisJson == "{}"
            ? null
            : JsonSerializer.Deserialize<ThroughputDiagnosisResult>(run.DiagnosisJson);
        var selection = string.IsNullOrWhiteSpace(run.SelectionJson) || run.SelectionJson == "{}"
            ? null
            : JsonSerializer.Deserialize<ThroughputInterventionSelection>(run.SelectionJson);

        decimal? verifiedGain = null;
        var verificationOutcome = improvement?.VerificationOutcome;
        if (improvement is not null
            && improvement.VerificationOutcome == ThroughputOptimizerVerificationOutcomes.VerifiedImprovement
            && improvement.ActualJson != "{}")
        {
            var baseline = JsonSerializer.Deserialize<VerifiedImprovementBaseline>(improvement.BaselineJson)!;
            var actual = JsonSerializer.Deserialize<VerifiedImprovementActual>(improvement.ActualJson)!;
            verifiedGain = actual.GoodThroughputPerHour - baseline.GoodThroughputPerHour;
        }

        return new ThroughputOptimizerExecutiveCaseItem(
            run.DigitalWorkCaseId,
            run.LineId,
            observation?.LineCode ?? "—",
            run.CurrentPhase,
            workCase.Status,
            diagnosis?.CurrentConstraintStationCode ?? observation?.CurrentConstraintStationCode,
            diagnosis?.ConstraintType ?? observation?.CurrentConstraintType,
            selection?.Selected.Title,
            selection?.Selected.InterventionType,
            hasPendingApproval,
            DescribeObservationState(run.CurrentPhase),
            verificationOutcome,
            verifiedGain,
            workCase.Status == DigitalWorkCaseStatuses.Closed
                && workCase.VerificationState == DigitalWorkVerificationResults.Passed,
            run.UpdatedAtUtc);
    }

    private static string DescribeObservationState(string phase) =>
        phase switch
        {
            ThroughputOptimizerPhases.Observe => "Gap gözlemi",
            ThroughputOptimizerPhases.ObserveResult => "Sonuç gözlemi",
            ThroughputOptimizerPhases.Approval => "Onay bekliyor",
            ThroughputOptimizerPhases.Execute => "Müdahale uygulanıyor",
            ThroughputOptimizerPhases.Verify => "Doğrulama",
            ThroughputOptimizerPhases.Closed => "Kapatıldı",
            _ => phase,
        };

    private async Task<ThroughputOptimizerWorkspace> ObserveLineCoreAsync(
        Guid facilityId, Guid tenantId, Guid lineId, string subjectId, CancellationToken cancellationToken,
        string? correlationKeyOverride = null)
    {
        var overview = await throughputIntelligence.GetLineOverviewAsync(facilityId, lineId, "recent", null, null, cancellationToken);
        var observation = ThroughputObserveEngine.Observe(overview)
            ?? throw new ThroughputOptimizerException(ThroughputOptimizerFailureCodes.GapNotDetected, "No meaningful throughput gap detected.");

        var correlationKey = correlationKeyOverride ?? ThroughputObserveEngine.BuildCorrelationKey(lineId);
        var existingRun = await db.ThroughputOptimizerRunRecords
            .Where(r => r.FacilityId == facilityId && r.CorrelationKey == correlationKey)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        DigitalWorkCase workCase;
        ThroughputOptimizerRunRecord run;

        if (existingRun is not null)
        {
            var existingCase = await db.DigitalWorkCases.FirstAsync(c => c.Id == existingRun.DigitalWorkCaseId, cancellationToken);
            if (existingCase.Status != DigitalWorkCaseStatuses.Closed)
            {
                return await BuildWorkspaceAsync(existingRun, existingCase, overview, cancellationToken);
            }
        }

        workCase = await workforceRuntime.TriggerCaseAsync(
            new TriggerWorkCaseRequest(
                $"Throughput gap on {overview.LineCode}",
                $"GOOD throughput {observation.CurrentGoodPerHour:F0}/h below target {observation.TargetGoodPerHour:F0}/h.",
                "throughput.gap",
                DigitalWorkforceSourceClassifications.Real,
                "high",
                DevelopmentPilotDigitalWorkforceIds.WorkerThroughputOptimizer,
                facilityId,
                JsonSerializer.Serialize(new { lineId, correlationKey })),
            subjectId,
            cancellationToken);

        workCase.CorrelationKey = correlationKey;
        workCase.PrimaryDomain = "throughput";
        await workforceRepository.SaveChangesAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        run = new ThroughputOptimizerRunRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            LineId = lineId,
            DigitalWorkCaseId = workCase.Id,
            CorrelationKey = correlationKey,
            CurrentPhase = ThroughputOptimizerPhases.Observe,
            ObservationJson = JsonSerializer.Serialize(observation),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.ThroughputOptimizerRunRecords.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildWorkspaceAsync(run, workCase, overview, cancellationToken);
    }

    private async Task<ThroughputOptimizerWorkspace> RunLifecycleStepCoreAsync(
        Guid facilityId, Guid tenantId, Guid caseId, string subjectId, CancellationToken cancellationToken)
    {
        var run = await db.ThroughputOptimizerRunRecords
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.DigitalWorkCaseId == caseId, cancellationToken)
            ?? throw new ThroughputOptimizerException(ThroughputOptimizerFailureCodes.NotFound, "Optimizer run not found.");

        var workCase = await db.DigitalWorkCases.FirstAsync(c => c.Id == caseId, cancellationToken);
        var overview = await throughputIntelligence.GetLineOverviewAsync(facilityId, run.LineId, "recent", null, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        switch (run.CurrentPhase)
        {
            case ThroughputOptimizerPhases.Observe:
                await AdvanceCase(workCase, DigitalWorkCaseStatuses.Analyzing, subjectId, cancellationToken);
                run.CurrentPhase = ThroughputOptimizerPhases.Diagnose;
                run.DiagnosisJson = JsonSerializer.Serialize(await DiagnoseAsync(facilityId, tenantId, run, overview, workCase, cancellationToken));
                break;

            case ThroughputOptimizerPhases.Diagnose:
                await AdvanceCase(workCase, DigitalWorkCaseStatuses.Deciding, subjectId, cancellationToken);
                run.CurrentPhase = ThroughputOptimizerPhases.Simulate;
                run.SimulationJson = JsonSerializer.Serialize(await SimulateAsync(facilityId, run, overview, tenantId, subjectId, cancellationToken));
                break;

            case ThroughputOptimizerPhases.Simulate:
                await AdvanceCase(workCase, DigitalWorkCaseStatuses.Planning, subjectId, cancellationToken);
                var candidates = JsonSerializer.Deserialize<List<ThroughputInterventionCandidate>>(run.SimulationJson) ?? [];
                var observation = JsonSerializer.Deserialize<ThroughputGapObservation>(run.ObservationJson)!;
                var selection = ThroughputInterventionSelectionEngine.Select(candidates, observation);
                run.SelectionJson = JsonSerializer.Serialize(selection);
                if (selection.Selected.RequiresApproval)
                {
                    await EnsureInterventionApprovalAsync(workCase, selection.Selected, cancellationToken);
                    run.CurrentPhase = ThroughputOptimizerPhases.Approval;
                }
                else
                {
                    run.CurrentPhase = ThroughputOptimizerPhases.Execute;
                }
                break;

            case ThroughputOptimizerPhases.Approval:
            {
                var approvalSelection = JsonSerializer.Deserialize<ThroughputInterventionSelection>(run.SelectionJson)!;
                var caseApprovals = (await workforceRuntime.ListApprovalsAsync(null, cancellationToken))
                    .Where(a => a.CaseId == caseId)
                    .ToList();

                if (caseApprovals.Any(a => a.Status == DigitalWorkApprovalStatuses.Rejected))
                {
                    run.CurrentPhase = ThroughputOptimizerPhases.Closed;
                    break;
                }

                if (caseApprovals.Any(a => a.Status == DigitalWorkApprovalStatuses.Pending))
                {
                    return await BuildWorkspaceAsync(run, workCase, overview, cancellationToken);
                }

                if (!caseApprovals.Any(a => a.Status == DigitalWorkApprovalStatuses.Approved))
                {
                    await EnsureInterventionApprovalAsync(workCase, approvalSelection.Selected, cancellationToken);
                    return await BuildWorkspaceAsync(run, workCase, overview, cancellationToken);
                }

                run.CurrentPhase = ThroughputOptimizerPhases.Execute;
                break;
            }

            case ThroughputOptimizerPhases.Execute:
                if (await RealFactoryShadowGuard.BlocksPhysicalActionsAsync(db, run.LineId, cancellationToken))
                {
                    throw new ThroughputOptimizerException(
                        ThroughputOptimizerFailureCodes.PolicyDenied,
                        "Shadow mode blocks physical/process-changing actions.");
                }

                await AdvanceCase(workCase, DigitalWorkCaseStatuses.Executing, subjectId, cancellationToken);
                run.ActionPlanJson = run.SelectionJson;
                run.CurrentPhase = ThroughputOptimizerPhases.ObserveResult;
                await CreateImprovementRecordAsync(run, overview, tenantId, facilityId, now, cancellationToken);
                break;

            case ThroughputOptimizerPhases.ObserveResult:
                run.CurrentPhase = ThroughputOptimizerPhases.Verify;
                break;

            case ThroughputOptimizerPhases.Verify:
                await VerifyAndCloseAsync(run, workCase, overview, tenantId, facilityId, subjectId, cancellationToken);
                run.CurrentPhase = ThroughputOptimizerPhases.Closed;
                break;
        }

        run.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return await BuildWorkspaceAsync(run, workCase, overview, cancellationToken);
    }

    private async Task<ThroughputDiagnosisResult> DiagnoseAsync(
        Guid facilityId, Guid tenantId, ThroughputOptimizerRunRecord run,
        ThroughputIntelligenceOverview overview, DigitalWorkCase workCase, CancellationToken cancellationToken)
    {
        var observation = JsonSerializer.Deserialize<ThroughputGapObservation>(run.ObservationJson)!;
        var contextPackage = await contextKnowledge.BuildWorkerContextAsync(
            new ContextBuildRequest(
                new ContextScope(tenantId, facilityId, "line", run.LineId.ToString(), LineId: run.LineId),
                new ContextBudget()),
            cancellationToken);

        var snapshot = await contextKnowledge.CreateSnapshotAsync(
            facilityId, tenantId,
            new ContextSnapshotRequest(
                new ContextScope(tenantId, facilityId, "line", run.LineId.ToString(), LineId: run.LineId),
                contextPackage,
                workCase.Id,
                "throughput-optimizer"),
            cancellationToken);

        run.ContextSnapshotId = snapshot.Id;

        var modelResponse = await modelGateway.InvokeAsync(
            facilityId, tenantId,
            new ModelGatewayRequest(
                tenantId, facilityId,
                DevelopmentPilotDigitalWorkforceIds.WorkerThroughputOptimizer,
                workCase.Id,
                ModelGatewayRequestClassifications.CrossModuleDiagnosis,
                "throughput-diagnosis",
                new ModelGatewayContextReference(
                    snapshot.Id,
                    contextPackage.CanonicalState?.ResolvedStateValue,
                    workCase.Id,
                    observation.CurrentConstraintStationCode,
                    null,
                    observation.DataQuality),
                2,
                true),
            cancellationToken);

        run.ModelIntelligencePath = modelResponse.Routing.IntelligencePath;
        return ThroughputDiagnoseEngine.Diagnose(observation, overview, modelResponse.Routing.IntelligencePath);
    }

    private async Task<List<ThroughputInterventionCandidate>> SimulateAsync(
        Guid facilityId, ThroughputOptimizerRunRecord run, ThroughputIntelligenceOverview overview,
        Guid tenantId, string subjectId, CancellationToken cancellationToken)
    {
        var stationCode = overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT")?.StationCode ?? "ST-30";
        var scenarios = new[]
        {
            (ThroughputIntelligenceScenarioTypes.AddOperator, "ADD-OP-01", "Add operator to constraint station"),
            (ThroughputIntelligenceScenarioTypes.MoveOperator, "MOVE-OP-01", "Move operator to welding station"),
            (ThroughputIntelligenceScenarioTypes.ReassignResource, "REASSIGN-01", "Reassign resource to assembly"),
        };

        var candidates = new List<ThroughputInterventionCandidate>();
        foreach (var (type, code, title) in scenarios)
        {
            var result = await throughputIntelligence.RunScenarioAsync(
                facilityId, run.LineId,
                new RunThroughputScenarioRequest(
                    code, type, title, "recent", null, null,
                    [new ScenarioIntervention(type, null, stationCode, null, null, null, null, null, null)],
                    ["mp3-throughput-optimizer"],
                    ["quality", "safety"],
                    false),
                tenantId, subjectId, cancellationToken);

            candidates.Add(new ThroughputInterventionCandidate(
                code, type, title,
                result.PredictedGoodThroughputPerHour,
                result.DeltaGoodThroughputPerHour,
                result.QualityImpactScore,
                result.OperationalRiskScore,
                type is ThroughputIntelligenceScenarioTypes.ReassignResource or ThroughputIntelligenceScenarioTypes.MoveOperator,
                result.Eligibility));
        }

        return candidates;
    }

    private async Task CreateImprovementRecordAsync(
        ThroughputOptimizerRunRecord run, ThroughputIntelligenceOverview overview,
        Guid tenantId, Guid facilityId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var baseline = VerifiedImprovementEngine.FromOverview(overview);
        var selection = JsonSerializer.Deserialize<ThroughputInterventionSelection>(run.SelectionJson)!;
        var observation = JsonSerializer.Deserialize<ThroughputGapObservation>(run.ObservationJson)!;
        var targetImprovement = Math.Min(
            selection.Selected.DeltaGoodThroughputPerHour,
            Math.Max(20m, observation.GapPerHour * 0.25m));

        if (!await db.VerifiedImprovementRecords.AnyAsync(r => r.DigitalWorkCaseId == run.DigitalWorkCaseId, cancellationToken))
        {
            db.VerifiedImprovementRecords.Add(new VerifiedImprovementRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                DigitalWorkCaseId = run.DigitalWorkCaseId,
                ThroughputOptimizerRunId = run.Id,
                BaselineJson = JsonSerializer.Serialize(baseline),
                TargetImprovementPerHour = targetImprovement,
                InterventionSummary = selection.Selected.Title,
                ObservationStartUtc = now,
                QualityGuardPassed = true,
                SafetyGuardPassed = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }
    }

    private async Task VerifyAndCloseAsync(
        ThroughputOptimizerRunRecord run, DigitalWorkCase workCase,
        ThroughputIntelligenceOverview overview, Guid tenantId, Guid facilityId,
        string subjectId, CancellationToken cancellationToken)
    {
        var improvement = await db.VerifiedImprovementRecords
            .FirstAsync(r => r.DigitalWorkCaseId == run.DigitalWorkCaseId, cancellationToken);

        var baseline = JsonSerializer.Deserialize<VerifiedImprovementBaseline>(improvement.BaselineJson)!;
        var actual = VerifiedImprovementEngine.FromOverviewAfter(overview);
        var observationComplete = improvement.ObservationStartUtc.HasValue;
        var outcome = VerifiedImprovementEngine.Evaluate(
            baseline,
            improvement.TargetImprovementPerHour,
            actual,
            observationComplete,
            improvement.QualityGuardPassed,
            improvement.SafetyGuardPassed);

        improvement.ActualJson = JsonSerializer.Serialize(actual);
        improvement.ObservationEndUtc = DateTimeOffset.UtcNow;
        improvement.VerificationOutcome = outcome;
        improvement.VerifiedAtUtc = DateTimeOffset.UtcNow;
        improvement.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await workforceRuntime.RecordVerificationAsync(
            workCase.Id,
            new RecordVerificationRequest(
                "throughput-target-met",
                "verified-improvement-engine",
                baseline.GoodThroughputPerHour.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                actual.GoodThroughputPerHour.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                DigitalWorkforceSourceClassifications.Real,
                0.95m,
                outcome == ThroughputOptimizerVerificationOutcomes.VerifiedImprovement
                    ? DigitalWorkVerificationResults.Passed
                    : DigitalWorkVerificationResults.Failed),
            subjectId,
            cancellationToken);

        if (workCase.Status == DigitalWorkCaseStatuses.Executing)
        {
            await AdvanceCase(workCase, DigitalWorkCaseStatuses.Verifying, subjectId, cancellationToken);
        }

        if (outcome == ThroughputOptimizerVerificationOutcomes.VerifiedImprovement)
        {
            await contextKnowledge.PromoteKnowledgeAsync(
                facilityId, tenantId,
                new KnowledgePromotionRequest(
                    workCase.Id,
                    $"Throughput gap on line {overview.LineCode}",
                    improvement.InterventionSummary,
                    "Throughput optimizer intervention",
                    [improvement.InterventionSummary],
                    [run.Id.ToString(), improvement.Id.ToString()],
                    improvement.BaselineJson,
                    improvement.ActualJson,
                    outcome,
                    HasVerifiedOutcome: true,
                    HasSufficientEvidence: true,
                    HasUnresolvedConflict: false,
                    ObservationCompleted: true,
                    Confidence: 0.9m,
                    ApplicabilityConstraintsJson: "{}"),
                cancellationToken);

            var (eligibility, reason, exportEligible) = VerifiedDatasetGovernanceEngine.Evaluate(
                outcome, true, true, false);
            db.VerifiedTrainingDatasetCandidateRecords.Add(new VerifiedTrainingDatasetCandidateRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                DigitalWorkCaseId = workCase.Id,
                VerifiedImprovementId = improvement.Id,
                EligibilityStatus = eligibility,
                IneligibilityReason = reason,
                RecordJson = JsonSerializer.Serialize(new { run.CorrelationKey, outcome }),
                VerificationLevel = "VERIFIED",
                DataQualityScore = overview.DataQuality,
                AnonymizationEligible = eligibility == VerifiedDatasetEligibilityStatuses.Eligible,
                ExportEligible = exportEligible,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });

            await AdvanceCase(workCase, DigitalWorkCaseStatuses.Closed, subjectId, cancellationToken);
            workCase.VerificationState = DigitalWorkVerificationResults.Passed;
        }
    }

    private async Task<ThroughputOptimizerGoldenDemoResult> RunGoldenDemoCoreAsync(
        Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken)
    {
        await throughputIntelligence.SeedMixLine01Mp2ExtensionAsync(facilityId, tenantId, cancellationToken);

        var line = await db.ProductionLines.AsNoTracking()
            .FirstAsync(l => l.FacilityId == facilityId && l.Code == MixedProductionFixtureCodes.LineCode, cancellationToken);

        await ThroughputIntelligenceFixtureSeeder.EnsureMixLine01Mp2EventHistoryAsync(
            db, tenantId, facilityId, line.Id, cancellationToken);
        await ThroughputOptimizerFixtureSeeder.RetirePostInterventionEventsAsync(db, line.Id, cancellationToken);

        var demoCorrelationKey = $"{ThroughputObserveEngine.BuildCorrelationKey(line.Id)}:golden-demo:{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        var workspace = await ObserveLineCoreAsync(
            facilityId, tenantId, line.Id, subjectId, cancellationToken, demoCorrelationKey);

        while (workspace.CurrentPhase != ThroughputOptimizerPhases.Closed)
        {
            workspace = await RunLifecycleStepCoreAsync(facilityId, tenantId, workspace.CaseId, subjectId, cancellationToken);

            if (workspace.CurrentPhase == ThroughputOptimizerPhases.Approval && workspace.HasPendingApproval)
            {
                var approval = (await workforceRuntime.ListApprovalsAsync(DigitalWorkApprovalStatuses.Pending, cancellationToken))
                    .First(a => a.CaseId == workspace.CaseId);
                await workforceRuntime.DecideApprovalAsync(
                    approval.Id,
                    new ApprovalDecisionRequest("approved", "Golden demo approval", approval.ConcurrencyToken),
                    subjectId,
                    cancellationToken);
                workspace = await RunLifecycleStepCoreAsync(facilityId, tenantId, workspace.CaseId, subjectId, cancellationToken);
            }

            if (workspace.CurrentPhase == ThroughputOptimizerPhases.ObserveResult)
            {
                await ThroughputOptimizerFixtureSeeder.SeedPostInterventionEventsAsync(
                    db, tenantId, facilityId, line.Id, cancellationToken);
                workspace = await RunLifecycleStepCoreAsync(facilityId, tenantId, workspace.CaseId, subjectId, cancellationToken);
            }
        }

        var knowledgeCreated = await db.FactoryKnowledgeRecords
            .AnyAsync(k => k.SourceCaseId == workspace.CaseId, cancellationToken);

        return new ThroughputOptimizerGoldenDemoResult(
            workspace.CaseId,
            line.Id,
            workspace.CurrentPhase,
            workspace.Improvement?.VerificationOutcome ?? "UNKNOWN",
            knowledgeCreated,
            "MP3 Golden Demo completed.");
    }

    private async Task<ThroughputOptimizerWorkspace> GetWorkspaceCoreAsync(
        Guid facilityId, Guid caseId, CancellationToken cancellationToken)
    {
        var run = await db.ThroughputOptimizerRunRecords
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.DigitalWorkCaseId == caseId, cancellationToken)
            ?? throw new ThroughputOptimizerException(ThroughputOptimizerFailureCodes.NotFound, "Optimizer run not found.");
        var workCase = await db.DigitalWorkCases.FirstAsync(c => c.Id == caseId, cancellationToken);
        var overview = await throughputIntelligence.GetLineOverviewAsync(facilityId, run.LineId, "recent", null, null, cancellationToken);
        return await BuildWorkspaceAsync(run, workCase, overview, cancellationToken);
    }

    private async Task<ThroughputOptimizerWorkspace> BuildWorkspaceAsync(
        ThroughputOptimizerRunRecord run, DigitalWorkCase workCase,
        ThroughputIntelligenceOverview overview, CancellationToken cancellationToken)
    {
        var observation = JsonSerializer.Deserialize<ThroughputGapObservation>(run.ObservationJson);
        var diagnosis = string.IsNullOrWhiteSpace(run.DiagnosisJson) || run.DiagnosisJson == "{}"
            ? null
            : JsonSerializer.Deserialize<ThroughputDiagnosisResult>(run.DiagnosisJson);
        var selection = string.IsNullOrWhiteSpace(run.SelectionJson) || run.SelectionJson == "{}"
            ? null
            : JsonSerializer.Deserialize<ThroughputInterventionSelection>(run.SelectionJson);

        var improvementRow = await db.VerifiedImprovementRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.DigitalWorkCaseId == run.DigitalWorkCaseId, cancellationToken);

        VerifiedImprovementSnapshot? improvement = null;
        if (improvementRow is not null)
        {
            improvement = new VerifiedImprovementSnapshot(
                improvementRow.Id,
                improvementRow.DigitalWorkCaseId,
                improvementRow.VerificationOutcome,
                JsonSerializer.Deserialize<VerifiedImprovementBaseline>(improvementRow.BaselineJson)!,
                improvementRow.TargetImprovementPerHour,
                improvementRow.InterventionSummary,
                improvementRow.ActualJson == "{}"
                    ? null
                    : JsonSerializer.Deserialize<VerifiedImprovementActual>(improvementRow.ActualJson),
                improvementRow.QualityGuardPassed,
                improvementRow.SafetyGuardPassed,
                improvementRow.ObservationStartUtc,
                improvementRow.ObservationEndUtc,
                improvementRow.VerifiedAtUtc);
        }

        var pendingApprovals = await workforceRuntime.ListApprovalsAsync(DigitalWorkApprovalStatuses.Pending, cancellationToken);
        var relatedCases = await db.DigitalWorkCases.AsNoTracking()
            .CountAsync(c => c.CorrelationKey == run.CorrelationKey && c.Id != run.DigitalWorkCaseId, cancellationToken);

        return new ThroughputOptimizerWorkspace(
            run.DigitalWorkCaseId,
            run.LineId,
            overview.LineCode,
            run.CurrentPhase,
            workCase.Status,
            observation,
            diagnosis,
            selection,
            improvement,
            run.ContextSnapshotId,
            run.ModelIntelligencePath,
            relatedCases,
            pendingApprovals.Any(a => a.CaseId == run.DigitalWorkCaseId),
            workCase.Status == DigitalWorkCaseStatuses.Closed &&
                workCase.VerificationState == DigitalWorkVerificationResults.Passed,
            run.UpdatedAtUtc);
    }

    private async Task AdvanceCase(
        DigitalWorkCase workCase, string targetStatus, string subjectId, CancellationToken cancellationToken)
    {
        if (workCase.Status == targetStatus)
        {
            return;
        }

        await workforceRuntime.AdvanceCaseAsync(
            workCase.Id,
            new AdvanceWorkCaseRequest(targetStatus, "throughput-optimizer", null, null, 2, false),
            subjectId,
            cancellationToken);
    }

    private async Task EnsureInterventionApprovalAsync(
        DigitalWorkCase workCase,
        ThroughputInterventionCandidate intervention,
        CancellationToken cancellationToken)
    {
        var hasPending = await db.DigitalWorkApprovals.AnyAsync(
            a => a.CaseId == workCase.Id && a.Status == DigitalWorkApprovalStatuses.Pending,
            cancellationToken);
        if (hasPending)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        db.DigitalWorkApprovals.Add(new DigitalWorkApproval
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            CaseId = workCase.Id,
            WorkerId = workCase.PrimaryWorkerId,
            Action = intervention.InterventionType,
            BusinessReason = $"Approve throughput intervention: {intervention.Title}",
            Impact = $"Predicted +{intervention.DeltaGoodThroughputPerHour:F1} GOOD/h",
            RiskLevel = intervention.OperationalRiskScore >= 0.5m ? 3 : 2,
            Status = DigitalWorkApprovalStatuses.Pending,
            CreatedAtUtc = now,
            ExpiresAtUtc = DigitalWorkforceApprovalGuard.ResolveExpiresAt(now),
            CaseConcurrencyTokenAtRequest = workCase.ConcurrencyToken,
            ConcurrencyToken = 1,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                scenarioCode = intervention.ScenarioCode,
                predictedThroughput = intervention.PredictedGoodThroughputPerHour,
                deltaThroughput = intervention.DeltaGoodThroughputPerHour,
            }),
        });

        workCase.RequiresApproval = true;
        workCase.Status = DigitalWorkCaseStatuses.AwaitingApproval;
        workCase.UpdatedAtUtc = now;
    }
}
