using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.ContextKnowledge;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.RealFactoryPilot;
using NevaxisZentra.Persistence.ThroughputIntelligence;
using NevaxisZentra.Persistence.ThroughputOptimizer;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.ScenarioRecovery;

public interface IScenarioRecoveryService
{
    Task<ScenarioRecoveryWorkspace> CreateRunAsync(
        Guid facilityId,
        Guid tenantId,
        CreateScenarioRecoveryRunRequest request,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<ScenarioRecoveryWorkspace> GetWorkspaceAsync(
        Guid facilityId,
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<ScenarioRecoveryWorkspace> GenerateOptionsAsync(
        Guid facilityId,
        Guid tenantId,
        Guid runId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<ScenarioRecoveryWorkspace> SelectOptionAsync(
        Guid facilityId,
        Guid tenantId,
        Guid runId,
        SelectScenarioRecoveryOptionRequest request,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<ScenarioRecoveryWorkspace> AdvanceAsync(
        Guid facilityId,
        Guid tenantId,
        Guid runId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<ScenarioRecoveryGoldenDemoResult> RunGoldenRecoveryDemoAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScenarioRecoveryHistoryItem>> ListHistoryAsync(
        Guid facilityId,
        Guid? lineId,
        CancellationToken cancellationToken = default);

    Task<ScenarioMetricSnapshot> SimulateActionAsync(
        Guid facilityId,
        Guid tenantId,
        Guid lineId,
        SimulateScenarioRecoveryActionRequest request,
        string subjectId,
        CancellationToken cancellationToken = default);
}

public sealed class ScenarioRecoveryService : IScenarioRecoveryService
{
    private readonly NevaxisZentraDbContext db;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly IThroughputIntelligenceService throughputIntelligence;
    private readonly IContextKnowledgeService contextKnowledge;
    private readonly IDigitalWorkforceRuntimeService workforceRuntime;
    private readonly IDigitalWorkforceRepository workforceRepository;

    public ScenarioRecoveryService(
        NevaxisZentraDbContext db,
        TenantDatabaseSession tenantDatabaseSession,
        IThroughputIntelligenceService throughputIntelligence,
        IContextKnowledgeService contextKnowledge,
        IDigitalWorkforceRuntimeService workforceRuntime,
        IDigitalWorkforceRepository workforceRepository)
    {
        this.db = db;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.throughputIntelligence = throughputIntelligence;
        this.contextKnowledge = contextKnowledge;
        this.workforceRuntime = workforceRuntime;
        this.workforceRepository = workforceRepository;
    }

    public Task<ScenarioRecoveryWorkspace> CreateRunAsync(
        Guid facilityId, Guid tenantId, CreateScenarioRecoveryRunRequest request, string subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CreateRunCoreAsync(facilityId, tenantId, request, subjectId, ct),
            cancellationToken);

    public Task<ScenarioRecoveryWorkspace> GetWorkspaceAsync(
        Guid facilityId, Guid runId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetWorkspaceCoreAsync(facilityId, runId, ct),
            cancellationToken);

    public Task<ScenarioRecoveryWorkspace> GenerateOptionsAsync(
        Guid facilityId, Guid tenantId, Guid runId, string subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => GenerateOptionsCoreAsync(facilityId, tenantId, runId, subjectId, ct),
            cancellationToken);

    public Task<ScenarioRecoveryWorkspace> SelectOptionAsync(
        Guid facilityId, Guid tenantId, Guid runId, SelectScenarioRecoveryOptionRequest request, string subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => SelectOptionCoreAsync(facilityId, tenantId, runId, request, subjectId, ct),
            cancellationToken);

    public Task<ScenarioRecoveryWorkspace> AdvanceAsync(
        Guid facilityId, Guid tenantId, Guid runId, string subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => AdvanceCoreAsync(facilityId, tenantId, runId, subjectId, ct),
            cancellationToken);

    public Task<ScenarioRecoveryGoldenDemoResult> RunGoldenRecoveryDemoAsync(
        Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenRecoveryDemoCoreAsync(facilityId, tenantId, subjectId, ct),
            cancellationToken);

    public Task<IReadOnlyList<ScenarioRecoveryHistoryItem>> ListHistoryAsync(
        Guid facilityId, Guid? lineId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListHistoryCoreAsync(facilityId, lineId, ct),
            cancellationToken);

    public Task<ScenarioMetricSnapshot> SimulateActionAsync(
        Guid facilityId, Guid tenantId, Guid lineId, SimulateScenarioRecoveryActionRequest request, string subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => SimulateActionCoreAsync(facilityId, tenantId, lineId, request, subjectId, ct),
            cancellationToken);

    private async Task<ScenarioRecoveryWorkspace> CreateRunCoreAsync(
        Guid facilityId, Guid tenantId, CreateScenarioRecoveryRunRequest request, string subjectId,
        CancellationToken cancellationToken)
    {
        if (request.Objectives.Count == 0)
        {
            throw new ScenarioRecoveryException(
                ScenarioRecoveryFailureCodes.ValidationFailed,
                "At least one objective is required.");
        }

        var line = await db.ProductionLines.AsNoTracking()
            .FirstOrDefaultAsync(l => l.FacilityId == facilityId && l.Id == request.LineId, cancellationToken)
            ?? throw new ScenarioRecoveryException(ScenarioRecoveryFailureCodes.NotFound, "Line not found.");

        var overview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, request.LineId, "recent", null, null, cancellationToken);

        if (overview.DataQuality is ThroughputIntelligenceDataQualityStates.Insufficient)
        {
            throw new ScenarioRecoveryException(
                ScenarioRecoveryFailureCodes.InsufficientContext,
                "Insufficient context for scenario recovery.");
        }

        if (overview.DataQuality is ThroughputIntelligenceDataQualityStates.Conflicting)
        {
            throw new ScenarioRecoveryException(
                ScenarioRecoveryFailureCodes.ConflictedState,
                "Conflicted operational state blocks scenario simulation.");
        }

        var now = DateTimeOffset.UtcNow;
        var correlationKey = ScenarioRecoveryCorrelationEngine.BuildCorrelationKey(
            request.LineId,
            $"{request.EntryPoint.ToLowerInvariant()}:{now:yyyyMMddHHmmss}");

        var run = new ScenarioRecoveryRunRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            LineId = request.LineId,
            DigitalWorkCaseId = request.LinkedDigitalWorkCaseId,
            CorrelationKey = correlationKey,
            Category = request.Category,
            EntryPoint = request.EntryPoint,
            CurrentPhase = ScenarioRecoveryPhases.Draft,
            ExecutionState = ScenarioRecoveryExecutionStates.Simulated,
            ProblemJson = JsonSerializer.Serialize(request.Problem),
            ObjectivesJson = JsonSerializer.Serialize(request.Objectives),
            SourceEntityType = request.SourceEntityType ?? string.Empty,
            SourceEntityId = request.SourceEntityId ?? string.Empty,
            ConcurrencyToken = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.ScenarioRecoveryRunRecords.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        return await CaptureSnapshotAndAdvanceAsync(run, overview, tenantId, facilityId, subjectId, cancellationToken);
    }

    private async Task<ScenarioRecoveryWorkspace> CaptureSnapshotAndAdvanceAsync(
        ScenarioRecoveryRunRecord run,
        ThroughputIntelligenceOverview overview,
        Guid tenantId,
        Guid facilityId,
        string subjectId,
        CancellationToken cancellationToken)
    {
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
                run.DigitalWorkCaseId,
                "scenario-recovery"),
            cancellationToken);

        run.ContextSnapshotId = snapshot.Id;
        run.SnapshotSummaryJson = JsonSerializer.Serialize(new ScenarioRecoverySnapshotSummary(
            snapshot.Id,
            overview.DataQuality,
            DateTimeOffset.UtcNow,
            overview.LineCode,
            facilityId.ToString(),
            ["mp2-capacity-state", "ultra-c-constraints", "canonical-operational-state"]));
        run.CurrentPhase = ScenarioRecoveryPhases.SnapshotCaptured;
        run.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await BuildWorkspaceAsync(run, overview, cancellationToken);
    }

    private async Task<ScenarioRecoveryWorkspace> GenerateOptionsCoreAsync(
        Guid facilityId, Guid tenantId, Guid runId, string subjectId, CancellationToken cancellationToken)
    {
        var run = await LoadRunAsync(facilityId, runId, cancellationToken);
        if (run.CurrentPhase is not ScenarioRecoveryPhases.SnapshotCaptured
            and not ScenarioRecoveryPhases.OptionsGenerated
            and not ScenarioRecoveryPhases.Compared
            and not ScenarioRecoveryPhases.Recommended)
        {
            throw new ScenarioRecoveryException(
                ScenarioRecoveryFailureCodes.ValidationFailed,
                "Options can only be generated after snapshot capture.");
        }

        var overview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, run.LineId, "recent", null, null, cancellationToken);
        var objectives = JsonSerializer.Deserialize<List<ScenarioRecoveryObjective>>(run.ObjectivesJson) ?? [];
        var assemblyCode = overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT")?.StationCode
            ?? ThroughputIntelligenceFixtureCodes.AssemblyStationCode;

        var optionTemplates = ScenarioRecoveryOptionGenerator.GenerateGoldenRecoveryOptions(assemblyCode);
        var optionSnapshots = new List<ScenarioRecoveryOptionSnapshot>();

        await db.ScenarioRecoveryOptionRecords
            .Where(o => o.RunId == run.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var (label, title, category, actions) in optionTemplates)
        {
            var interventions = actions
                .Select(ScenarioRecoveryActionRegistry.ToScenarioIntervention)
                .ToList();

            var simulation = await throughputIntelligence.RunScenarioAsync(
                facilityId, run.LineId,
                new RunThroughputScenarioRequest(
                    label, category, title, "recent", null, null,
                    interventions,
                    ["ultra-d-scenario-recovery"],
                    ["quality", "safety", "maintenance"],
                    false),
                tenantId, subjectId, cancellationToken);

            var (feasibilityState, feasibilityReasons) = ScenarioRecoveryFeasibilityEngine.Evaluate(
                simulation, actions, overview.DataQuality);

            var metrics = label == ScenarioRecoveryOptionLabels.Baseline
                ? ScenarioRecoveryComparisonEngine.BuildMetrics(simulation, simulation)
                : ScenarioRecoveryComparisonEngine.BuildMetrics(
                    optionSnapshots.First(o => o.OptionLabel == ScenarioRecoveryOptionLabels.Baseline).Simulation,
                    simulation);

            var optionActions = actions.Select(a =>
            {
                var (state, reasons) = ScenarioRecoveryFeasibilityEngine.Evaluate(simulation, [a], overview.DataQuality);
                return new ScenarioRecoveryOptionAction(a, state, reasons);
            }).ToList();

            var requiresApproval = actions.Any(a => a.RequiresApproval)
                || feasibilityState == ScenarioRecoveryFeasibilityStates.FeasibleWithRisk;

            var snapshot = new ScenarioRecoveryOptionSnapshot(
                label,
                title,
                category,
                feasibilityState,
                feasibilityReasons,
                optionActions,
                simulation,
                metrics,
                simulation.Assumptions,
                requiresApproval,
                false,
                0m);

            optionSnapshots.Add(snapshot);

            db.ScenarioRecoveryOptionRecords.Add(new ScenarioRecoveryOptionRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                RunId = run.Id,
                OptionLabel = label,
                Title = title,
                Category = category,
                FeasibilityState = feasibilityState,
                ActionsJson = JsonSerializer.Serialize(actions),
                SimulationJson = JsonSerializer.Serialize(simulation),
                MetricsJson = JsonSerializer.Serialize(metrics),
                AssumptionsJson = JsonSerializer.Serialize(simulation.Assumptions),
                RequiresApproval = requiresApproval,
                FeasibilityReasonsJson = JsonSerializer.Serialize(feasibilityReasons),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        var dominated = ScenarioRecoveryDominanceEngine.MarkDominatedOptions(optionSnapshots);
        optionSnapshots = optionSnapshots.Select(o => o with
        {
            IsDominated = dominated.Contains(o.OptionLabel),
            RecommendationScore = ScenarioRecoveryRecommendationEngine.ScoreOption(o, objectives),
        }).ToList();

        foreach (var option in optionSnapshots)
        {
            var row = await db.ScenarioRecoveryOptionRecords
                .FirstAsync(r => r.RunId == run.Id && r.OptionLabel == option.OptionLabel, cancellationToken);
            row.IsDominated = option.IsDominated;
            row.RecommendationScore = option.RecommendationScore;
        }

        var baseline = optionSnapshots.First(o => o.OptionLabel == ScenarioRecoveryOptionLabels.Baseline);
        var recommendation = ScenarioRecoveryRecommendationEngine.Recommend(optionSnapshots, objectives);
        var tradeOffs = ScenarioRecoveryTradeOffEngine.Explain(optionSnapshots, baseline);
        var recommendedOption = optionSnapshots.First(o => o.OptionLabel == recommendation.RecommendedOptionLabel);

        var comparison = new ScenarioRecoveryComparisonMatrix(
            ["GOOD_THROUGHPUT", "SCRAP", "WIP", "UTILIZATION", "RISK", "QUALITY"],
            optionSnapshots,
            tradeOffs,
            recommendation,
            baseline.Simulation.ConstraintBefore?.StationCode,
            recommendedOption.Simulation.ConstraintAfter?.StationCode);

        run.ComparisonJson = JsonSerializer.Serialize(comparison);
        run.CurrentPhase = ScenarioRecoveryPhases.Recommended;
        run.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await BuildWorkspaceAsync(run, overview, cancellationToken);
    }

    private async Task<ScenarioRecoveryWorkspace> SelectOptionCoreAsync(
        Guid facilityId, Guid tenantId, Guid runId, SelectScenarioRecoveryOptionRequest request, string subjectId,
        CancellationToken cancellationToken)
    {
        var run = await LoadRunAsync(facilityId, runId, cancellationToken);
        if (run.CurrentPhase != ScenarioRecoveryPhases.Recommended && run.CurrentPhase != ScenarioRecoveryPhases.Selected)
        {
            throw new ScenarioRecoveryException(
                ScenarioRecoveryFailureCodes.ValidationFailed,
                "Option can only be selected after recommendation.");
        }

        var option = await db.ScenarioRecoveryOptionRecords
            .FirstOrDefaultAsync(o => o.RunId == run.Id && o.OptionLabel == request.OptionLabel, cancellationToken)
            ?? throw new ScenarioRecoveryException(ScenarioRecoveryFailureCodes.NotFound, "Option not found.");

        if (option.FeasibilityState == ScenarioRecoveryFeasibilityStates.Infeasible)
        {
            throw new ScenarioRecoveryException(ScenarioRecoveryFailureCodes.Infeasible, "Selected option is infeasible.");
        }

        var comparison = JsonSerializer.Deserialize<ScenarioRecoveryComparisonMatrix>(run.ComparisonJson)!;
        var selected = comparison.Options.First(o => o.OptionLabel == request.OptionLabel);
        var objectives = JsonSerializer.Deserialize<List<ScenarioRecoveryObjective>>(run.ObjectivesJson) ?? [];
        var problem = JsonSerializer.Deserialize<ScenarioRecoveryProblemStatement>(run.ProblemJson)!;

        DigitalWorkCase workCase;
        if (run.DigitalWorkCaseId.HasValue)
        {
            workCase = await db.DigitalWorkCases.FirstAsync(c => c.Id == run.DigitalWorkCaseId, cancellationToken);
        }
        else
        {
            workCase = await workforceRuntime.TriggerCaseAsync(
                new TriggerWorkCaseRequest(
                    problem.Summary,
                    $"Scenario recovery selected {request.OptionLabel}: {selected.Title}",
                    "scenario.recovery",
                    DigitalWorkforceSourceClassifications.Real,
                    "high",
                    DevelopmentPilotDigitalWorkforceIds.WorkerProductionRecovery,
                    facilityId,
                    JsonSerializer.Serialize(new { runId = run.Id, option = request.OptionLabel })),
                subjectId,
                cancellationToken);
            workCase.CorrelationKey = run.CorrelationKey;
            workCase.PrimaryDomain = "scenario-recovery";
            run.DigitalWorkCaseId = workCase.Id;
            await workforceRepository.SaveChangesAsync(cancellationToken);
        }

        var recoveryPlan = new ScenarioRecoveryRecoveryPlan(
            string.Join(", ", objectives.Select(o => o.Label)),
            request.OptionLabel,
            JsonSerializer.Deserialize<List<ScenarioRecoveryActionDefinition>>(option.ActionsJson) ?? [],
            ["mp2-capacity", "ultra-c-schedule"],
            selected.RequiresApproval ? ["controlled-intervention-approval"] : [],
            "controlled-recommendation",
            30,
            ["good-throughput-improvement", "quality-guard", "safety-guard", "otif-protection"]);

        run.SelectedOptionLabel = request.OptionLabel;
        run.RecoveryPlanJson = JsonSerializer.Serialize(recoveryPlan);
        run.CurrentPhase = selected.RequiresApproval
            ? ScenarioRecoveryPhases.Approval
            : ScenarioRecoveryPhases.Selected;
        run.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (selected.RequiresApproval)
        {
            await EnsureOptionApprovalAsync(workCase, selected, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        var overview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, run.LineId, "recent", null, null, cancellationToken);
        return await BuildWorkspaceAsync(run, overview, cancellationToken);
    }

    private async Task<ScenarioRecoveryWorkspace> AdvanceCoreAsync(
        Guid facilityId, Guid tenantId, Guid runId, string subjectId, CancellationToken cancellationToken)
    {
        var run = await LoadRunAsync(facilityId, runId, cancellationToken);
        var overview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, run.LineId, "recent", null, null, cancellationToken);
        var workCase = run.DigitalWorkCaseId.HasValue
            ? await db.DigitalWorkCases.FirstAsync(c => c.Id == run.DigitalWorkCaseId, cancellationToken)
            : null;
        var now = DateTimeOffset.UtcNow;

        switch (run.CurrentPhase)
        {
            case ScenarioRecoveryPhases.Approval when workCase is not null:
            {
                var approvals = (await workforceRuntime.ListApprovalsAsync(null, cancellationToken))
                    .Where(a => a.CaseId == workCase.Id)
                    .ToList();
                if (approvals.Any(a => a.Status == DigitalWorkApprovalStatuses.Rejected))
                {
                    run.CurrentPhase = ScenarioRecoveryPhases.Closed;
                    break;
                }

                if (approvals.Any(a => a.Status == DigitalWorkApprovalStatuses.Pending))
                {
                    break;
                }

                if (!approvals.Any(a => a.Status == DigitalWorkApprovalStatuses.Approved))
                {
                    var selected = await GetSelectedOptionSnapshotAsync(run, cancellationToken);
                    await EnsureOptionApprovalAsync(workCase, selected, cancellationToken);
                    break;
                }

                run.CurrentPhase = ScenarioRecoveryPhases.Selected;
                break;
            }

            case ScenarioRecoveryPhases.Selected when workCase is not null:
                if (await RealFactoryShadowGuard.BlocksPhysicalActionsAsync(db, run.LineId, cancellationToken))
                {
                    throw new ScenarioRecoveryException(
                        ScenarioRecoveryFailureCodes.ValidationFailed,
                        "Shadow mode blocks execution.");
                }

                await AdvanceCase(workCase, DigitalWorkCaseStatuses.Executing, subjectId, cancellationToken);
                run.CurrentPhase = ScenarioRecoveryPhases.Applied;
                run.ExecutionState = ScenarioRecoveryExecutionStates.Applied;
                await CreateObservationBaselineAsync(run, overview, now, cancellationToken);
                break;

            case ScenarioRecoveryPhases.Applied:
                run.CurrentPhase = ScenarioRecoveryPhases.Observing;
                run.ExecutionState = ScenarioRecoveryExecutionStates.Observing;
                break;

            case ScenarioRecoveryPhases.Observing:
                run.CurrentPhase = ScenarioRecoveryPhases.Verified;
                await VerifyAndCloseAsync(run, workCase!, overview, tenantId, facilityId, subjectId, cancellationToken);
                run.ExecutionState = ScenarioRecoveryExecutionStates.Verified;
                run.CurrentPhase = ScenarioRecoveryPhases.Closed;
                break;
        }

        run.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return await BuildWorkspaceAsync(run, overview, cancellationToken);
    }

    private async Task<ScenarioRecoveryGoldenDemoResult> RunGoldenRecoveryDemoCoreAsync(
        Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken)
    {
        await throughputIntelligence.SeedMixLine01Mp2ExtensionAsync(facilityId, tenantId, cancellationToken);
        var line = await db.ProductionLines.AsNoTracking()
            .FirstAsync(l => l.FacilityId == facilityId && l.Code == MixedProductionFixtureCodes.LineCode, cancellationToken);

        await ThroughputIntelligenceFixtureSeeder.EnsureMixLine01Mp2EventHistoryAsync(
            db, tenantId, facilityId, line.Id, cancellationToken);
        await ScenarioRecoveryFixtureSeeder.RetirePostInterventionEventsAsync(db, line.Id, cancellationToken);

        var overview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, line.Id, "recent", null, null, cancellationToken);
        var baselineThroughput = overview.Measurement.GoodUnitsPerHour;

        var createRequest = new CreateScenarioRecoveryRunRequest(
            line.Id,
            ScenarioRecoveryCategories.ProductionRecovery,
            ScenarioRecoveryEntryPoints.GoldenDemo,
            new ScenarioRecoveryProblemStatement(
                "MIX-LINE-01 throughput below target with Assembly bottleneck, material delay, and welding maintenance window",
                "ORD-418",
                overview.LineCode,
                ThroughputIntelligenceFixtureCodes.AssemblyStationCode,
                baselineThroughput,
                overview.ProductionGap.RequiredGoodPerHour,
                ["assembly-bottleneck", "material-eta-delay", "welding-maintenance-window"]),
            [
                new ScenarioRecoveryObjective(
                    ScenarioRecoveryObjectiveTypes.ProtectOtif, "Protect Order 418 delivery", 3, true),
                new ScenarioRecoveryObjective(
                    ScenarioRecoveryObjectiveTypes.MaximizeVerifiedThroughput, "Increase good throughput", 2, false),
            ],
            "line",
            line.Id.ToString(),
            null);

        var workspace = await CreateRunCoreAsync(facilityId, tenantId, createRequest, subjectId, cancellationToken);
        workspace = await GenerateOptionsCoreAsync(facilityId, tenantId, workspace.RunId, subjectId, cancellationToken);

        var recommended = JsonSerializer.Deserialize<ScenarioRecoveryComparisonMatrix>((
            await LoadRunAsync(facilityId, workspace.RunId, cancellationToken)).ComparisonJson)!;

        workspace = await SelectOptionCoreAsync(
            facilityId, tenantId, workspace.RunId,
            new SelectScenarioRecoveryOptionRequest(recommended.Recommendation.RecommendedOptionLabel, null),
            subjectId,
            cancellationToken);

        while (workspace.CurrentPhase != ScenarioRecoveryPhases.Closed)
        {
            if (workspace.CurrentPhase == ScenarioRecoveryPhases.Approval && workspace.HasPendingApproval)
            {
                var approval = (await workforceRuntime.ListApprovalsAsync(DigitalWorkApprovalStatuses.Pending, cancellationToken))
                    .First(a => a.CaseId == workspace.DigitalWorkCaseId);
                await workforceRuntime.DecideApprovalAsync(
                    approval.Id,
                    new ApprovalDecisionRequest("approved", "Golden recovery demo approval", approval.ConcurrencyToken),
                    subjectId,
                    cancellationToken);
            }

            if (workspace.CurrentPhase == ScenarioRecoveryPhases.Applied)
            {
                await ScenarioRecoveryFixtureSeeder.SeedPostInterventionEventsAsync(
                    db, tenantId, facilityId, line.Id, cancellationToken);
            }

            workspace = await AdvanceCoreAsync(facilityId, tenantId, workspace.RunId, subjectId, cancellationToken);
        }

        var run = await LoadRunAsync(facilityId, workspace.RunId, cancellationToken);
        var verification = JsonSerializer.Deserialize<ScenarioRecoveryVerificationSnapshot>(run.VerificationJson);
        var knowledgeCreated = run.DigitalWorkCaseId.HasValue && await db.FactoryKnowledgeRecords
            .AnyAsync(k => k.SourceCaseId == run.DigitalWorkCaseId, cancellationToken);

        return new ScenarioRecoveryGoldenDemoResult(
            run.Id,
            run.DigitalWorkCaseId ?? Guid.Empty,
            line.Id,
            run.CurrentPhase,
            verification?.Outcome ?? ScenarioRecoveryVerificationOutcomes.Inconclusive,
            knowledgeCreated,
            baselineThroughput,
            verification?.PredictedGoodThroughputPerHour ?? 0m,
            verification?.Actual?.GoodThroughputPerHour,
            verification?.ThroughputPredictionError,
            "ULTRA D Golden Recovery completed.");
    }

    private async Task<IReadOnlyList<ScenarioRecoveryHistoryItem>> ListHistoryCoreAsync(
        Guid facilityId, Guid? lineId, CancellationToken cancellationToken)
    {
        var query = db.ScenarioRecoveryRunRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId);
        if (lineId.HasValue)
        {
            query = query.Where(r => r.LineId == lineId.Value);
        }

        var rows = await query.OrderByDescending(r => r.UpdatedAtUtc).Take(50).ToListAsync(cancellationToken);
        return rows.Select(r =>
        {
            var problem = JsonSerializer.Deserialize<ScenarioRecoveryProblemStatement>(r.ProblemJson);
            var verification = string.IsNullOrWhiteSpace(r.VerificationJson) || r.VerificationJson == "{}"
                ? null
                : JsonSerializer.Deserialize<ScenarioRecoveryVerificationSnapshot>(r.VerificationJson);
            return new ScenarioRecoveryHistoryItem(
                r.Id,
                problem?.Summary ?? "—",
                r.Category,
                r.SelectedOptionLabel,
                r.CurrentPhase,
                verification?.Outcome,
                verification?.ThroughputPredictionError,
                r.CreatedAtUtc,
                r.UpdatedAtUtc);
        }).ToList();
    }

    private async Task<ScenarioMetricSnapshot> SimulateActionCoreAsync(
        Guid facilityId, Guid tenantId, Guid lineId, SimulateScenarioRecoveryActionRequest request, string subjectId,
        CancellationToken cancellationToken)
    {
        var action = ScenarioRecoveryActionRegistry.ValidateOrReject(
            request.ActionType, request.TargetEntityCode, request.Parameters);
        return await throughputIntelligence.RunScenarioAsync(
            facilityId, lineId,
            new RunThroughputScenarioRequest(
                $"single-{request.ActionType}",
                request.ActionType,
                $"Single action: {request.ActionType}",
                "recent", null, null,
                [ScenarioRecoveryActionRegistry.ToScenarioIntervention(action)],
                ["ultra-d-single-action"],
                ["quality", "safety"],
                false),
            tenantId, subjectId, cancellationToken);
    }

    private async Task<ScenarioRecoveryWorkspace> GetWorkspaceCoreAsync(
        Guid facilityId, Guid runId, CancellationToken cancellationToken)
    {
        var run = await LoadRunAsync(facilityId, runId, cancellationToken);
        var overview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, run.LineId, "recent", null, null, cancellationToken);
        return await BuildWorkspaceAsync(run, overview, cancellationToken);
    }

    private async Task<ScenarioRecoveryRunRecord> LoadRunAsync(
        Guid facilityId, Guid runId, CancellationToken cancellationToken) =>
        await db.ScenarioRecoveryRunRecords
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.Id == runId, cancellationToken)
        ?? throw new ScenarioRecoveryException(ScenarioRecoveryFailureCodes.NotFound, "Scenario run not found.");

    private static Task<ScenarioRecoveryOptionSnapshot> GetSelectedOptionSnapshotAsync(
        ScenarioRecoveryRunRecord run, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var comparison = JsonSerializer.Deserialize<ScenarioRecoveryComparisonMatrix>(run.ComparisonJson)!;
        return Task.FromResult(comparison.Options.First(o => o.OptionLabel == run.SelectedOptionLabel));
    }

    private static async Task CreateObservationBaselineAsync(
        ScenarioRecoveryRunRecord run,
        ThroughputIntelligenceOverview overview,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var selected = await GetSelectedOptionSnapshotAsync(run, cancellationToken);
        var baseline = VerifiedImprovementEngine.FromOverview(overview);

        var verification = new ScenarioRecoveryVerificationSnapshot(
            ScenarioRecoveryVerificationOutcomes.Inconclusive,
            baseline,
            selected.Simulation.PredictedGoodThroughputPerHour,
            null,
            null,
            true,
            true,
            now,
            null,
            null);

        run.VerificationJson = JsonSerializer.Serialize(verification);
    }

    private async Task VerifyAndCloseAsync(
        ScenarioRecoveryRunRecord run,
        DigitalWorkCase workCase,
        ThroughputIntelligenceOverview overview,
        Guid tenantId,
        Guid facilityId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var verification = JsonSerializer.Deserialize<ScenarioRecoveryVerificationSnapshot>(run.VerificationJson)!;
        var actual = VerifiedImprovementEngine.FromOverviewAfter(overview);
        var outcome = VerifiedImprovementEngine.Evaluate(
            verification.Baseline,
            verification.PredictedGoodThroughputPerHour - verification.Baseline.GoodThroughputPerHour,
            actual,
            verification.ObservationStartUtc.HasValue,
            verification.QualityGuardPassed,
            verification.SafetyGuardPassed);
        var mappedOutcome = ScenarioRecoveryVerificationMapper.MapOutcome(outcome);
        var predictionError = actual.GoodThroughputPerHour - verification.PredictedGoodThroughputPerHour;

        verification = verification with
        {
            Outcome = mappedOutcome,
            Actual = actual,
            ThroughputPredictionError = predictionError,
            ObservationEndUtc = DateTimeOffset.UtcNow,
            VerifiedAtUtc = DateTimeOffset.UtcNow,
        };
        run.VerificationJson = JsonSerializer.Serialize(verification);

        db.ScenarioRecoveryPredictionAccuracyRecords.Add(new ScenarioRecoveryPredictionAccuracyRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            RunId = run.Id,
            DigitalWorkCaseId = workCase.Id,
            MetricCode = "GOOD_THROUGHPUT",
            PredictedValue = verification.PredictedGoodThroughputPerHour,
            ActualValue = actual.GoodThroughputPerHour,
            PredictionError = predictionError,
            VerificationOutcome = mappedOutcome,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await workforceRuntime.RecordVerificationAsync(
            workCase.Id,
            new RecordVerificationRequest(
                "scenario-recovery-target-met",
                "scenario-recovery-verification-engine",
                verification.Baseline.GoodThroughputPerHour.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                actual.GoodThroughputPerHour.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                DigitalWorkforceSourceClassifications.Real,
                0.95m,
                mappedOutcome == ScenarioRecoveryVerificationOutcomes.VerifiedSuccess
                    ? DigitalWorkVerificationResults.Passed
                    : DigitalWorkVerificationResults.Failed),
            subjectId,
            cancellationToken);

        if (mappedOutcome == ScenarioRecoveryVerificationOutcomes.VerifiedSuccess)
        {
            var selected = await GetSelectedOptionSnapshotAsync(run, cancellationToken);
            await contextKnowledge.PromoteKnowledgeAsync(
                facilityId, tenantId,
                new KnowledgePromotionRequest(
                    workCase.Id,
                    JsonSerializer.Deserialize<ScenarioRecoveryProblemStatement>(run.ProblemJson)!.Summary,
                    selected.Title,
                    "Verified scenario recovery intervention",
                    [selected.Title],
                    [run.Id.ToString()],
                    JsonSerializer.Serialize(verification.Baseline),
                    JsonSerializer.Serialize(actual),
                    mappedOutcome,
                    HasVerifiedOutcome: true,
                    HasSufficientEvidence: true,
                    HasUnresolvedConflict: false,
                    ObservationCompleted: true,
                    Confidence: 0.9m,
                    ApplicabilityConstraintsJson: "{}"),
                cancellationToken);

            await AdvanceCase(workCase, DigitalWorkCaseStatuses.Closed, subjectId, cancellationToken);
            workCase.VerificationState = DigitalWorkVerificationResults.Passed;
        }
    }

    private async Task EnsureOptionApprovalAsync(
        DigitalWorkCase workCase,
        ScenarioRecoveryOptionSnapshot option,
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
            Action = option.Category,
            BusinessReason = $"Approve scenario recovery option: {option.Title}",
            Impact = $"Predicted +{option.Simulation.DeltaGoodThroughputPerHour:F1} GOOD/h",
            RiskLevel = option.Simulation.OperationalRiskScore >= 0.5m ? 3 : 2,
            Status = DigitalWorkApprovalStatuses.Pending,
            CreatedAtUtc = now,
            ExpiresAtUtc = DigitalWorkforceApprovalGuard.ResolveExpiresAt(now),
            CaseConcurrencyTokenAtRequest = workCase.ConcurrencyToken,
            ConcurrencyToken = 1,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                option.OptionLabel,
                option.Title,
                constraintBefore = option.Simulation.ConstraintBefore?.StationCode,
                constraintAfter = option.Simulation.ConstraintAfter?.StationCode,
            }),
        });
    }

    private async Task AdvanceCase(
        DigitalWorkCase workCase, string status, string subjectId, CancellationToken cancellationToken)
    {
        workCase.Status = status;
        workCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        workCase.ConcurrencyToken += 1;
        await workforceRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<ScenarioRecoveryWorkspace> BuildWorkspaceAsync(
        ScenarioRecoveryRunRecord run,
        ThroughputIntelligenceOverview overview,
        CancellationToken cancellationToken)
    {
        var problem = JsonSerializer.Deserialize<ScenarioRecoveryProblemStatement>(run.ProblemJson)!;
        var objectives = JsonSerializer.Deserialize<List<ScenarioRecoveryObjective>>(run.ObjectivesJson) ?? [];
        var snapshot = string.IsNullOrWhiteSpace(run.SnapshotSummaryJson) || run.SnapshotSummaryJson == "{}"
            ? null
            : JsonSerializer.Deserialize<ScenarioRecoverySnapshotSummary>(run.SnapshotSummaryJson);
        var comparison = string.IsNullOrWhiteSpace(run.ComparisonJson) || run.ComparisonJson == "{}"
            ? null
            : JsonSerializer.Deserialize<ScenarioRecoveryComparisonMatrix>(run.ComparisonJson);
        var recoveryPlan = string.IsNullOrWhiteSpace(run.RecoveryPlanJson) || run.RecoveryPlanJson == "{}"
            ? null
            : JsonSerializer.Deserialize<ScenarioRecoveryRecoveryPlan>(run.RecoveryPlanJson);
        var verification = string.IsNullOrWhiteSpace(run.VerificationJson) || run.VerificationJson == "{}"
            ? null
            : JsonSerializer.Deserialize<ScenarioRecoveryVerificationSnapshot>(run.VerificationJson);

        var hasPendingApproval = run.DigitalWorkCaseId.HasValue && await db.DigitalWorkApprovals.AsNoTracking()
            .AnyAsync(a => a.CaseId == run.DigitalWorkCaseId && a.Status == DigitalWorkApprovalStatuses.Pending, cancellationToken);

        return new ScenarioRecoveryWorkspace(
            run.Id,
            run.DigitalWorkCaseId,
            run.LineId,
            overview.LineCode,
            run.CurrentPhase,
            run.ExecutionState,
            run.Category,
            run.EntryPoint,
            problem,
            objectives,
            snapshot,
            comparison,
            string.IsNullOrWhiteSpace(run.SelectedOptionLabel) ? null : run.SelectedOptionLabel,
            recoveryPlan,
            verification,
            hasPendingApproval,
            run.CurrentPhase == ScenarioRecoveryPhases.Closed
                && verification?.Outcome == ScenarioRecoveryVerificationOutcomes.VerifiedSuccess,
            run.EngineVersion,
            run.UpdatedAtUtc);
    }
}
