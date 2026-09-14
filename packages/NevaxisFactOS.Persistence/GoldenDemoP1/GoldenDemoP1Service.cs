using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.GoldenDemoP1;

public interface IGoldenDemoP1Service
{
    Task<GoldenDemoWorkspaceSnapshot> GetWorkspaceAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken);
    Task<GoldenDemoWorkspaceSnapshot> RefreshAnalysisAsync(
        Guid eventId,
        CancellationToken cancellationToken);
    Task<GoldenDemoRecoveryPlan> ApprovePlanAsync(
        Guid eventId,
        ApproveRecoveryPlanRequest request,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoProductionEvent> ExecutePlanAsync(
        Guid eventId,
        ExecuteRecoveryPlanRequest request,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoProductionEvent> CompleteInterventionAsync(
        Guid eventId,
        CompleteInterventionRequest request,
        string subjectId,
        CancellationToken cancellationToken);
    Task<GoldenDemoVerificationRun> VerifyOutcomeAsync(
        Guid eventId,
        VerifyOutcomeRequest request,
        string subjectId,
        CancellationToken cancellationToken);
}

public sealed class GoldenDemoP1Service : IGoldenDemoP1Service
{
    private readonly IGoldenDemoP1Repository repository;
    private readonly NevaxisFactOSDbContext dbContext;

    public GoldenDemoP1Service(IGoldenDemoP1Repository repository, NevaxisFactOSDbContext dbContext)
    {
        this.repository = repository;
        this.dbContext = dbContext;
    }

    public async Task<GoldenDemoWorkspaceSnapshot> GetWorkspaceAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken)
    {
        var evt = await repository.GetActiveEventAsync(tenantId, facilityId, machineAssetId, cancellationToken);
        if (evt is null)
        {
            throw new GoldenDemoP1Exception(
                GoldenDemoP1FailureCodes.EventNotFound,
                "No active golden demo production event found for this facility.");
        }

        return await BuildSnapshotAsync(evt, cancellationToken);
    }

    public async Task<GoldenDemoWorkspaceSnapshot> RefreshAnalysisAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var evt = await repository.GetEventAsync(eventId, cancellationToken);
        if (evt is null)
        {
            throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.EventNotFound, "Production event not found.");
        }

        return await BuildSnapshotAsync(evt, cancellationToken, persistFindings: true);
    }

    public async Task<GoldenDemoRecoveryPlan> ApprovePlanAsync(
        Guid eventId,
        ApproveRecoveryPlanRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetPlanAsync(eventId, cancellationToken)
            ?? throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.EventNotFound, "Recovery plan not found.");
        plan.ApprovedBySubjectId = subjectId;
        plan.ApprovedAtUtc = DateTimeOffset.UtcNow;
        plan.Status = GoldenDemoPlanActionStatuses.InProgress;
        await repository.UpdatePlanAsync(plan, cancellationToken);
        return plan;
    }

    public async Task<GoldenDemoProductionEvent> ExecutePlanAsync(
        Guid eventId,
        ExecuteRecoveryPlanRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var evt = await repository.GetEventAsync(eventId, cancellationToken)
            ?? throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.EventNotFound, "Production event not found.");
        var plan = await repository.GetPlanAsync(eventId, cancellationToken);
        if (plan is null || plan.ApprovedAtUtc is null)
        {
            throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.InvalidState, "Recovery plan must be approved before execution.");
        }

        var actions = await repository.GetPlanActionsAsync(plan.Id, cancellationToken);
        foreach (var action in actions.Where(a => a.Status == GoldenDemoPlanActionStatuses.Planned))
        {
            action.Status = GoldenDemoPlanActionStatuses.InProgress;
        }

        evt.Status = GoldenDemoP1EventStatuses.InterventionInProgress;
        await repository.UpdateEventAsync(evt, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return evt;
    }

    public async Task<GoldenDemoProductionEvent> CompleteInterventionAsync(
        Guid eventId,
        CompleteInterventionRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var evt = await repository.GetEventAsync(eventId, cancellationToken)
            ?? throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.EventNotFound, "Production event not found.");
        var baseline = await repository.GetBaselineAsync(eventId, cancellationToken);
        var current = await repository.GetObservationAsync(eventId, "current", cancellationToken)
            ?? throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.InvalidState, "Current observation missing.");
        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, current);
        var recovery = ProductionRecoveryEngine.Calculate(decomposition, current, evt, DateTimeOffset.UtcNow);

        var postPhases = BuildRecoveredPhaseMap(current, baseline, decomposition);
        var post = new GoldenDemoCycleObservation
        {
            Id = Guid.NewGuid(),
            TenantId = evt.TenantId,
            FacilityId = evt.FacilityId,
            ProductionEventId = evt.Id,
            ObservationKind = "post-intervention",
            TotalCycleSeconds = postPhases.Values.Sum(),
            PhaseDurationsJson = CycleLossDecompositionEngine.SerializePhaseMap(postPhases),
            ProcessParametersJson = current.ProcessParametersJson,
            MicroStopCount = Math.Max(0, current.MicroStopCount - 2),
            MicroStopDurationSeconds = Math.Max(0, current.MicroStopDurationSeconds - 45m),
            GoodParts = current.GoodParts + Math.Max(1, recovery.ProductionRecoveryGoodParts / 4),
            RejectParts = current.RejectParts,
            AvailabilityPercent = Math.Min(100, current.AvailabilityPercent + 2m),
            KwhPerGoodPart = baseline?.ReferenceKwhPerGoodPart ?? current.KwhPerGoodPart,
            TotalEnergyKwh = current.TotalEnergyKwh,
            MachineState = "running",
            ObservedAtUtc = DateTimeOffset.UtcNow,
            WindowStartUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
            WindowEndUtc = DateTimeOffset.UtcNow,
        };

        await repository.AddObservationAsync(post, cancellationToken);

        var plan = await repository.GetPlanAsync(eventId, cancellationToken);
        if (plan is not null)
        {
            var actions = await repository.GetPlanActionsAsync(plan.Id, cancellationToken);
            foreach (var action in actions)
            {
                action.Status = GoldenDemoPlanActionStatuses.Completed;
                action.CompletedAtUtc = DateTimeOffset.UtcNow;
            }

            plan.Status = GoldenDemoPlanActionStatuses.Completed;
            await repository.UpdatePlanAsync(plan, cancellationToken);
        }

        evt.Status = GoldenDemoP1EventStatuses.ValidationRun;
        await repository.UpdateEventAsync(evt, cancellationToken);
        return evt;
    }

    public async Task<GoldenDemoVerificationRun> VerifyOutcomeAsync(
        Guid eventId,
        VerifyOutcomeRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var evt = await repository.GetEventAsync(eventId, cancellationToken)
            ?? throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.EventNotFound, "Production event not found.");
        var before = await repository.GetObservationAsync(eventId, "current", cancellationToken)
            ?? throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.InvalidState, "Before observation missing.");
        var after = await repository.GetObservationAsync(eventId, "post-intervention", cancellationToken);
        var baseline = await repository.GetBaselineAsync(eventId, cancellationToken);
        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, before);
        var recovery = ProductionRecoveryEngine.Calculate(decomposition, before, evt, DateTimeOffset.UtcNow);
        var verificationResult = OutcomeVerificationEngine.Verify(before, after, recovery);

        var run = new GoldenDemoVerificationRun
        {
            Id = Guid.NewGuid(),
            TenantId = evt.TenantId,
            FacilityId = evt.FacilityId,
            ProductionEventId = evt.Id,
            Result = verificationResult.Result,
            CriteriaJson = JsonSerializer.Serialize(verificationResult.Criteria),
            Summary = verificationResult.Summary,
            RecoveredGoodPartsPerShift = verificationResult.RecoveredGoodPartsPerShift,
            TriggeredBySubjectId = subjectId,
            VerifiedAtUtc = DateTimeOffset.UtcNow,
        };
        await repository.AddVerificationAsync(run, cancellationToken);

        evt.Status = verificationResult.Result == GoldenDemoVerificationResults.Verified
            ? GoldenDemoP1EventStatuses.Verified
            : GoldenDemoP1EventStatuses.ValidationRun;
        await repository.UpdateEventAsync(evt, cancellationToken);
        return run;
    }

    private async Task<GoldenDemoWorkspaceSnapshot> BuildSnapshotAsync(
        GoldenDemoProductionEvent evt,
        CancellationToken cancellationToken,
        bool persistFindings = false)
    {
        var baseline = await repository.GetBaselineAsync(evt.Id, cancellationToken);
        var current = await repository.GetObservationAsync(evt.Id, "current", cancellationToken)
            ?? throw new GoldenDemoP1Exception(GoldenDemoP1FailureCodes.InvalidState, "Current observation missing.");
        var post = await repository.GetObservationAsync(evt.Id, "post-intervention", cancellationToken);
        var plan = await repository.GetPlanAsync(evt.Id, cancellationToken);
        var actions = plan is null
            ? []
            : await repository.GetPlanActionsAsync(plan.Id, cancellationToken);
        var verification = await repository.GetLatestVerificationAsync(evt.Id, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, current);
        var contributors = RootContributorAnalysisEngine.Analyze(decomposition);
        var recovery = ProductionRecoveryEngine.Calculate(decomposition, current, evt, now);
        var whatIf = WhatIfDecisionEngine.BuildScenarios(recovery, decomposition, evt, current);

        var findings = await repository.GetFindingsAsync(evt.Id, cancellationToken);
        if (findings.Count == 0 || persistFindings)
        {
            findings = GoldenDemoAgentOrchestrationEngine.BuildFindings(
                evt.TenantId,
                evt.FacilityId,
                evt.Id,
                decomposition,
                contributors,
                recovery,
                current,
                baseline,
                now);
            if (persistFindings || findings.Count > 0)
            {
                await repository.SaveFindingsAsync(findings, cancellationToken);
            }
        }

        OutcomeVerificationResult? verificationResult = null;
        BeforeAfterComparisonResult? beforeAfter = null;
        if (post is not null)
        {
            verificationResult = OutcomeVerificationEngine.Verify(current, post, recovery);
            beforeAfter = BeforeAfterComparisonEngine.Compare(current, post, verificationResult);
        }

        var metrics = BuildMetrics(evt, current, baseline, recovery, decomposition);
        var business = BuildBusinessImpact(recovery, metrics, verification);

        return new GoldenDemoWorkspaceSnapshot(
            evt,
            baseline,
            current,
            post,
            decomposition,
            contributors,
            recovery,
            whatIf,
            plan,
            actions,
            findings,
            verification,
            beforeAfter,
            metrics,
            business);
    }

    private static Dictionary<string, decimal> BuildRecoveredPhaseMap(
        GoldenDemoCycleObservation current,
        GoldenDemoCycleBaseline? baseline,
        CycleLossDecompositionResult decomposition)
    {
        var currentMap = CycleLossDecompositionEngine.ParsePhaseMap(current.PhaseDurationsJson);
        if (baseline is null || !decomposition.HasBaseline)
        {
            return currentMap.ToDictionary(k => k.Key, k => k.Value);
        }

        var referenceMap = CycleLossDecompositionEngine.ParsePhaseMap(baseline.PhaseDurationsJson);
        var result = new Dictionary<string, decimal>();
        foreach (var phase in GoldenDemoP1PhaseCatalog.Phases)
        {
            currentMap.TryGetValue(phase.Code, out var currentValue);
            referenceMap.TryGetValue(phase.Code, out var referenceValue);
            var loss = currentValue - referenceValue;
            result[phase.Code] = loss > 0 ? referenceValue + (loss * 0.15m) : currentValue;
        }

        return result;
    }

    private static MachineIntelligenceMetrics BuildMetrics(
        GoldenDemoProductionEvent evt,
        GoldenDemoCycleObservation current,
        GoldenDemoCycleBaseline? baseline,
        ProductionRecoveryResult recovery,
        CycleLossDecompositionResult decomposition)
    {
        var hours = Math.Max(0.01, (current.ObservedAtUtc - evt.ShiftStartUtc).TotalHours);
        var goodPerHour = current.GoodParts / (decimal)hours;
        var projected = current.GoodParts + recovery.CurrentExpectedShiftGoodParts;
        var attainment = evt.ShiftTargetGoodParts > 0
            ? Math.Round((decimal)current.GoodParts / evt.ShiftTargetGoodParts * 100m, 1)
            : 0m;
        var totalParts = current.GoodParts + current.RejectParts;
        var rejectRate = totalParts > 0 ? Math.Round((decimal)current.RejectParts / totalParts * 100m, 1) : 0m;
        decimal? energyDeviation = null;
        if (current.KwhPerGoodPart.HasValue && baseline?.ReferenceKwhPerGoodPart is > 0)
        {
            energyDeviation = Math.Round(
                (current.KwhPerGoodPart.Value - baseline.ReferenceKwhPerGoodPart.Value) /
                baseline.ReferenceKwhPerGoodPart.Value * 100m,
                1);
        }

        var processParams = ParseStringDecimalMap(current.ProcessParametersJson);
        var unavailable = new Dictionary<string, string>();
        if (!processParams.ContainsKey("injection_pressure"))
        {
            unavailable["injection_pressure"] = "Unavailable — no live PLC signal in current integration scope.";
        }

        return new MachineIntelligenceMetrics(
            evt.ShiftTargetGoodParts,
            current.GoodParts,
            projected,
            attainment,
            current.RejectParts,
            rejectRate,
            Math.Round(goodPerHour, 1),
            current.MachineState,
            current.TotalCycleSeconds,
            decomposition.HasBaseline ? decomposition.ReferenceCycleSeconds : null,
            current.AvailabilityPercent,
            current.MicroStopCount,
            current.MicroStopDurationSeconds,
            evt.MoldReference,
            current.KwhPerGoodPart,
            baseline?.ReferenceKwhPerGoodPart,
            energyDeviation,
            processParams,
            unavailable);
    }

    private static BusinessImpactSummary BuildBusinessImpact(
        ProductionRecoveryResult recovery,
        MachineIntelligenceMetrics metrics,
        GoldenDemoVerificationRun? verification)
    {
        var capacityImpact = metrics.ShiftTargetGoodParts > 0
            ? Math.Round((decimal)recovery.ProductionRecoveryGoodParts / metrics.ShiftTargetGoodParts * 100m, 1)
            : 0m;
        var estimatedTry = recovery.ProductionRecoveryGoodParts * 125m;
        decimal? verifiedTry = verification?.Result == GoldenDemoVerificationResults.Verified
            ? verification.RecoveredGoodPartsPerShift * 125m
            : null;

        return new BusinessImpactSummary(
            recovery.ProductionRecoveryGoodParts,
            capacityImpact,
            metrics.EnergyIntensityDeviationPercent is > 0 ? -metrics.EnergyIntensityDeviationPercent : metrics.EnergyIntensityDeviationPercent,
            recovery.RecoverableCycleSeconds > 0 ? Math.Round(recovery.RecoverableCycleSeconds / 60m, 1) : null,
            verifiedTry,
            estimatedTry);
    }

    private static Dictionary<string, decimal?> ParseStringDecimalMap(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, decimal?>();
        }

        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, decimal>>(json) ?? new Dictionary<string, decimal>();
            return raw.ToDictionary(entry => entry.Key, entry => (decimal?)entry.Value);
        }
        catch (JsonException)
        {
            return new Dictionary<string, decimal?>();
        }
    }
}
