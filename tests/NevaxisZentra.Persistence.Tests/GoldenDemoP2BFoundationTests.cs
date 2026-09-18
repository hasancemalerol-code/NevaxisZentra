using NevaxisZentra.Persistence.GoldenDemoP1;
using NevaxisZentra.Persistence.GoldenDemoP2B;

namespace NevaxisZentra.Persistence.Tests;

public sealed class GoldenDemoP2BFoundationTests
{
    [Fact]
    public void AgentEvidenceUsesRuntimeCycleValues()
    {
        var workspace = CreateWorkspace(includeBaseline: true, includeEnergy: true, moldReference: "MOLD-PP-240");
        var finding = workspace.AgentFindings.Single(f => f.AgentCode == GoldenDemoAgentCodes.MachinePerformance);

        var detail = GoldenDemoP2BAgentEvidenceEngine.BuildDetail(finding, workspace, workspace.Plan);

        Assert.Equal("28.0", detail.CalculationInputs["referenceCycleSeconds"]);
        Assert.Equal("31.2", detail.CalculationInputs["currentCycleSeconds"]);
        Assert.Equal("3.2", detail.CalculationResults["cycleLossSeconds"]);
        Assert.Null(detail.InsufficientEvidenceState);
    }

    [Fact]
    public void AgentEvidenceSurfacesInsufficientBaseline()
    {
        var workspace = CreateWorkspace(includeBaseline: false, includeEnergy: true, moldReference: "MOLD-PP-240");
        var finding = workspace.AgentFindings.Single(f => f.AgentCode == GoldenDemoAgentCodes.MachinePerformance);

        var detail = GoldenDemoP2BAgentEvidenceEngine.BuildDetail(finding, workspace, workspace.Plan);

        Assert.Equal("INSUFFICIENT BASELINE", detail.InsufficientEvidenceState);
        Assert.Equal("low", detail.Confidence);
    }

    [Fact]
    public void AgentEvidenceSurfacesInsufficientMoldAndEnergy()
    {
        var workspace = CreateWorkspace(includeBaseline: true, includeEnergy: false, moldReference: null);
        var mold = GoldenDemoP2BAgentEvidenceEngine.BuildDetail(
            workspace.AgentFindings.Single(f => f.AgentCode == GoldenDemoAgentCodes.MoldHealth),
            workspace,
            workspace.Plan);
        var energy = GoldenDemoP2BAgentEvidenceEngine.BuildDetail(
            workspace.AgentFindings.Single(f => f.AgentCode == GoldenDemoAgentCodes.Energy),
            workspace,
            workspace.Plan);

        Assert.Equal("INSUFFICIENT MOLD EVIDENCE", mold.InsufficientEvidenceState);
        Assert.Equal("ENERGY DATA INSUFFICIENT", energy.InsufficientEvidenceState);
    }

    [Fact]
    public void OrchestratorUsesActualAgentFindingsAndRecovery()
    {
        var workspace = CreateWorkspace(includeBaseline: true, includeEnergy: true, moldReference: "MOLD-PP-240");

        var assessment = GoldenDemoP2BOrchestratorEngine.Build(workspace, workspace.Plan);

        Assert.Contains("Üretim Performans Ajanı", assessment.InputsUsed);
        Assert.Contains("Makine Performans Ajanı", assessment.InputsUsed);
        Assert.Equal(workspace.Recovery.CurrentExpectedShiftGoodParts, assessment.CurrentExpectedShiftGoodParts);
        Assert.Equal(workspace.Recovery.ProductionRecoveryGoodParts, assessment.ProductionRecoveryGoodParts);
        Assert.True(assessment.HumanApprovalRequired);
        Assert.Empty(assessment.InsufficientSpecialistEvidence);
    }

    [Fact]
    public void OrchestratorRespectsInsufficientSpecialistEvidence()
    {
        var workspace = CreateWorkspace(includeBaseline: true, includeEnergy: false, moldReference: null);

        var assessment = GoldenDemoP2BOrchestratorEngine.Build(workspace, workspace.Plan);

        Assert.Contains(assessment.InsufficientSpecialistEvidence, item => item.Contains("INSUFFICIENT MOLD EVIDENCE"));
        Assert.Contains(assessment.InsufficientSpecialistEvidence, item => item.Contains("ENERGY DATA INSUFFICIENT"));
    }

    [Fact]
    public void ActivityTimelineContainsOnlyRealDerivedEvents()
    {
        var workspace = CreateWorkspace(includeBaseline: true, includeEnergy: true, moldReference: "MOLD-PP-240");
        workspace.Plan!.ApprovedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10);
        workspace.Plan.ApprovedBySubjectId = "planning-pilot-reviewer";

        var timeline = GoldenDemoP2BActivityProjector.Build(workspace, workspace.Plan);

        Assert.Equal(timeline.OrderBy(i => i.TimestampUtc), timeline);
        Assert.Contains(timeline, item => item.Action == "Machine cycle loss decomposed");
        Assert.Contains(timeline, item => item.Action == "Recovery plan created");
        Assert.Contains(timeline, item => item.Action == "Approval granted");
        Assert.DoesNotContain(timeline, item => item.Action.Contains("thinking", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApprovalSheetReflectsPersistedPlanState()
    {
        var workspace = CreateWorkspace(includeBaseline: true, includeEnergy: true, moldReference: "MOLD-PP-240");
        workspace.Plan!.ApprovedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);
        workspace.Plan.ApprovedBySubjectId = "planning-pilot-reviewer";

        var sheet = GoldenDemoP2BApprovalSheetEngine.Build(workspace, workspace.Plan);

        Assert.True(sheet.IsApproved);
        Assert.Equal("approved", sheet.ApprovalStatus);
        Assert.Equal(workspace.Plan.Id.ToString(), sheet.AuditReference);
        Assert.Equal("planning-pilot-reviewer", sheet.ApprovedBySubjectId);
        Assert.NotEmpty(sheet.EvidenceReferences);
    }

    [Fact]
    public void PlanTraceabilityLinksActionsToSourceAgents()
    {
        var workspace = CreateWorkspace(includeBaseline: true, includeEnergy: true, moldReference: "MOLD-PP-240");

        var actions = GoldenDemoP2BPlanTraceabilityEngine.Build(workspace, workspace.Plan);

        Assert.Contains(actions, action => action.SourceAgentCode == GoldenDemoAgentCodes.MachinePerformance);
        Assert.Contains(actions, action => action.SourceAgentCode == GoldenDemoAgentCodes.ProcessOptimization);
        Assert.All(actions, action => Assert.StartsWith("plan-action:", action.WorkReferenceId));
    }

    [Fact]
    public void InterventionTimelineTracksApprovalAndVerificationStages()
    {
        var workspace = CreateWorkspace(includeBaseline: true, includeEnergy: true, moldReference: "MOLD-PP-240");
        workspace.Event.Status = GoldenDemoP1EventStatuses.Verified;
        workspace.Plan!.ApprovedAtUtc = DateTimeOffset.UtcNow.AddHours(-1);
        workspace.Plan.ApprovedBySubjectId = "planning-pilot-reviewer";
        var verification = new GoldenDemoVerificationRun
        {
            Id = Guid.NewGuid(),
            Result = GoldenDemoVerificationResults.Verified,
            VerifiedAtUtc = DateTimeOffset.UtcNow,
            TriggeredBySubjectId = "production-pilot-operator",
            Summary = "Verified recovery",
        };

        var timeline = GoldenDemoP2BInterventionTimelineEngine.Build(workspace, workspace.Plan, verification);

        Assert.Contains(timeline, stage => stage.StageCode == "approved");
        Assert.Contains(timeline, stage => stage.StageLabel == "Verified");
    }

    private static GoldenDemoWorkspaceSnapshot CreateWorkspace(
        bool includeBaseline,
        bool includeEnergy,
        string? moldReference)
    {
        var baseline = includeBaseline ? CreateBaseline() : null;
        var observation = CreateObservation(
            totalCycle: 31.2m,
            cooling: 13.5m,
            screw: 5.3m,
            mold: 4.7m,
            kwhPerGoodPart: includeEnergy ? 0.42m : null);
        observation.MicroStopCount = 8;
        observation.MicroStopDurationSeconds = 120m;
        observation.GoodParts = 168;
        observation.RejectParts = 8;
        observation.AvailabilityPercent = 88m;
        var productionEvent = new GoldenDemoProductionEvent
        {
            Id = Guid.NewGuid(),
            MachineCode = "INJ-001",
            MoldReference = moldReference,
            ShiftTargetGoodParts = 480,
            ShiftStartUtc = DateTimeOffset.UtcNow.AddHours(-4),
            ShiftEndUtc = DateTimeOffset.UtcNow.AddHours(4),
            Status = GoldenDemoP1EventStatuses.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, observation);
        var recovery = ProductionRecoveryEngine.Calculate(
            decomposition,
            observation,
            productionEvent,
            DateTimeOffset.UtcNow);
        var contributors = RootContributorAnalysisEngine.Analyze(decomposition);
        var now = DateTimeOffset.UtcNow;
        var findings = GoldenDemoAgentOrchestrationEngine.BuildFindings(
            Guid.NewGuid(),
            Guid.NewGuid(),
            productionEvent.Id,
            decomposition,
            contributors,
            recovery,
            observation,
            baseline,
            now);
        if (!includeEnergy)
        {
            findings = findings
                .Append(new GoldenDemoAgentFinding
                {
                    Id = Guid.NewGuid(),
                    AgentCode = GoldenDemoAgentCodes.Energy,
                    WorkerCode = "production-recovery",
                    Summary = "Energy intensity unavailable.",
                    EvidenceClassification = GoldenDemoImpactClassifications.Measured,
                    GeneratedAtUtc = now,
                })
                .ToList();
        }

        var metrics = new MachineIntelligenceMetrics(
            productionEvent.ShiftTargetGoodParts,
            observation.GoodParts,
            observation.GoodParts + recovery.CurrentExpectedShiftGoodParts,
            35m,
            observation.RejectParts,
            4.5m,
            112m,
            observation.MachineState,
            observation.TotalCycleSeconds,
            decomposition.HasBaseline ? decomposition.ReferenceCycleSeconds : null,
            observation.AvailabilityPercent,
            observation.MicroStopCount,
            observation.MicroStopDurationSeconds,
            productionEvent.MoldReference,
            includeEnergy ? observation.KwhPerGoodPart : null,
            baseline?.ReferenceKwhPerGoodPart,
            includeEnergy ? 10.5m : null,
            new Dictionary<string, decimal?>(),
            new Dictionary<string, string>());

        var businessImpact = new BusinessImpactSummary(
            recovery.ProductionRecoveryGoodParts,
            8.8m,
            includeEnergy ? -10.5m : null,
            2.2m,
            null,
            recovery.ProductionRecoveryGoodParts * 125m);

        var plan = new GoldenDemoRecoveryPlan
        {
            Id = Guid.NewGuid(),
            ProductionEventId = productionEvent.Id,
            RecommendationSummary = "Controlled technical intervention",
            RequiresApproval = true,
            Status = GoldenDemoPlanActionStatuses.Planned,
            CreatedAtUtc = now.AddMinutes(-20),
        };
        var planActions = new List<GoldenDemoRecoveryPlanAction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                RecoveryPlanId = plan.Id,
                ActionCode = "mold-movement-inspection",
                Title = "Mold movement inspection",
                ResponsibleRole = "maintenance",
                Status = GoldenDemoPlanActionStatuses.Planned,
                EvidenceRequired = "Cycle phase evidence",
                ValidationCriteria = "Cycle within band",
                SortOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                RecoveryPlanId = plan.Id,
                ActionCode = "cooling-circuit-inspection",
                Title = "Cooling circuit inspection",
                ResponsibleRole = "process-tech",
                Status = GoldenDemoPlanActionStatuses.Planned,
                EvidenceRequired = "Cooling phase deviation",
                ValidationCriteria = "Cooling stable",
                SortOrder = 2,
            },
        };

        return new GoldenDemoWorkspaceSnapshot(
            productionEvent,
            baseline,
            observation,
            null,
            decomposition,
            contributors,
            recovery,
            [],
            plan,
            planActions,
            findings,
            null,
            null,
            metrics,
            businessImpact);
    }

    private static GoldenDemoCycleBaseline CreateBaseline() =>
        new()
        {
            ReferenceSource = GoldenDemoReferenceSources.StableHistoricalWindow,
            TotalCycleSeconds = 28.0m,
            PhaseDurationsJson = CycleLossDecompositionEngine.SerializePhaseMap(new Dictionary<string, decimal>
            {
                [GoldenDemoCyclePhaseCodes.Injection] = 2.5m,
                [GoldenDemoCyclePhaseCodes.Hold] = 3.0m,
                [GoldenDemoCyclePhaseCodes.Cooling] = 12.0m,
                [GoldenDemoCyclePhaseCodes.ScrewRecovery] = 4.5m,
                [GoldenDemoCyclePhaseCodes.MoldMovement] = 4.0m,
                [GoldenDemoCyclePhaseCodes.OtherProcess] = 2.0m,
            }),
        };

    private static GoldenDemoCycleObservation CreateObservation(
        decimal totalCycle,
        decimal cooling,
        decimal screw,
        decimal mold,
        decimal injection = 2.5m,
        decimal? kwhPerGoodPart = 0.42m)
    {
        var phases = new Dictionary<string, decimal>
        {
            [GoldenDemoCyclePhaseCodes.Injection] = injection,
            [GoldenDemoCyclePhaseCodes.Hold] = 3.0m,
            [GoldenDemoCyclePhaseCodes.Cooling] = cooling,
            [GoldenDemoCyclePhaseCodes.ScrewRecovery] = screw,
            [GoldenDemoCyclePhaseCodes.MoldMovement] = mold,
            [GoldenDemoCyclePhaseCodes.OtherProcess] = 2.0m,
        };

        return new GoldenDemoCycleObservation
        {
            TotalCycleSeconds = totalCycle,
            PhaseDurationsJson = CycleLossDecompositionEngine.SerializePhaseMap(phases),
            KwhPerGoodPart = kwhPerGoodPart,
            WindowStartUtc = DateTimeOffset.UtcNow.AddMinutes(-90),
            WindowEndUtc = DateTimeOffset.UtcNow,
        };
    }
}
