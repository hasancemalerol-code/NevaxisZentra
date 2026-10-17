using System.Text.Json;
using NevaxisZentra.Persistence.FactoryLearning;
using NevaxisZentra.Persistence.ProductionManager;
using NevaxisZentra.Persistence.ProductionPlanning;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.AdaptiveProductionPlanning;

public interface IAdaptivePlanningService
{
    Task<AdaptivePlanningWorkspaceSnapshot> GetWorkspaceAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<PlanBuildResult> BuildPlanAsync(Guid facilityId, BuildPlanRequest request, string subjectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlanSimulationResult>> SimulateAsync(Guid facilityId, SimulatePlanRequest request, string subjectId, CancellationToken cancellationToken);
    Task<ReplanEvaluationResult> EvaluateReplanAsync(Guid facilityId, EvaluateReplanRequest request, CancellationToken cancellationToken);
    Task<PlanPublishResult> PublishAsync(Guid facilityId, PublishPlanRequest request, string subjectId, CancellationToken cancellationToken);
    Task<PlanPublishResult> ApproveReplanAsync(Guid facilityId, ApproveReplanRequest request, string subjectId, CancellationToken cancellationToken);
    Task<PlanOutcomeVerificationResult> VerifyOutcomeAsync(Guid planVersionId, CancellationToken cancellationToken);
    Task<AdaptivePlanningPolicy> UpdatePolicyAsync(Guid facilityId, UpdateAdaptivePlanningPolicyRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlanDiffRow>> GetPlanDiffAsync(Guid fromVersionId, Guid toVersionId, CancellationToken cancellationToken);
}

public sealed class AdaptivePlanningService : IAdaptivePlanningService
{
    private readonly IAdaptivePlanningRepository repository;
    private readonly NevaxisZentraDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public AdaptivePlanningService(
        IAdaptivePlanningRepository repository,
        NevaxisZentraDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.repository = repository;
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public Task<AdaptivePlanningWorkspaceSnapshot> GetWorkspaceAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct => BuildWorkspaceAsync(facilityId, ct), cancellationToken);

    public Task<PlanBuildResult> BuildPlanAsync(
        Guid facilityId, BuildPlanRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => BuildPlanCoreAsync(facilityId, request, subjectId, ct),
            cancellationToken);

    public Task<IReadOnlyList<PlanSimulationResult>> SimulateAsync(
        Guid facilityId, SimulatePlanRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => SimulateCoreAsync(facilityId, request, subjectId, ct),
            cancellationToken);

    public Task<ReplanEvaluationResult> EvaluateReplanAsync(
        Guid facilityId, EvaluateReplanRequest request, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => EvaluateReplanCoreAsync(facilityId, request, ct),
            cancellationToken);

    public Task<PlanPublishResult> PublishAsync(
        Guid facilityId, PublishPlanRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => PublishCoreAsync(facilityId, request, subjectId, ct),
            cancellationToken);

    public Task<PlanPublishResult> ApproveReplanAsync(
        Guid facilityId, ApproveReplanRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ApproveReplanCoreAsync(facilityId, request, subjectId, ct),
            cancellationToken);

    public Task<PlanOutcomeVerificationResult> VerifyOutcomeAsync(
        Guid planVersionId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => VerifyOutcomeCoreAsync(planVersionId, ct),
            cancellationToken);

    public Task<AdaptivePlanningPolicy> UpdatePolicyAsync(
        Guid facilityId, UpdateAdaptivePlanningPolicyRequest request, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => UpdatePolicyCoreAsync(facilityId, request, ct),
            cancellationToken);

    public Task<IReadOnlyList<PlanDiffRow>> GetPlanDiffAsync(
        Guid fromVersionId, Guid toVersionId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var fromOps = await repository.GetOperationsAsync(fromVersionId, ct);
                var toOps = await repository.GetOperationsAsync(toVersionId, ct);
                return PlanDiffEngine.Diff(fromOps, toOps);
            },
            cancellationToken);

    private async Task<AdaptivePlanningWorkspaceSnapshot> BuildWorkspaceAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var policy = await repository.GetOrCreatePolicyAsync(tenantId, facilityId, cancellationToken);
        var plan = await dbContext.ProductionPlans
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.FacilityId == facilityId)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        ProductionPlanVersion? activeVersion = null;
        IReadOnlyList<ProductionPlanOperation> operations = [];
        if (plan?.ActivePlanVersionId is Guid versionId)
        {
            activeVersion = await repository.GetVersionAsync(versionId, cancellationToken);
            operations = await repository.GetOperationsAsync(versionId, cancellationToken);
        }

        var scenarios = plan is null ? [] : await repository.GetScenariosAsync(plan.Id, cancellationToken);
        var deviations = plan is null ? [] : await repository.GetDeviationsAsync(plan.Id, cancellationToken);
        var changes = plan is null ? [] : await repository.GetChangesAsync(plan.Id, 20, cancellationToken);
        var activity = plan is null ? [] : await repository.GetActivityAsync(plan.Id, 30, cancellationToken);
        var state = await FactoryStateProjector.CaptureAsync(dbContext, tenantId, facilityId, cancellationToken);
        var payload = activeVersion?.ConstraintSnapshotId is Guid snapId
            ? PlanningConstraintSnapshotEngine.ParsePayload(await dbContext.PlanningConstraintSnapshots
                .AsNoTracking()
                .FirstAsync(s => s.Id == snapId, cancellationToken))
            : new ConstraintSnapshotPayload([], [], [], [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        // Without planned operations there is nothing to measure: no adherence, no utilization, no at-risk orders.
        var adherence = operations.Count == 0 ? 0m : PlanAdherenceEngine.Compute(operations, DateTimeOffset.UtcNow);
        var versionsToday = plan is null ? [] : (await repository.GetVersionsForPlanAsync(plan.Id, cancellationToken))
            .Where(v => v.CreatedAtUtc.Date == DateTimeOffset.UtcNow.Date).ToList();
        var changesToday = changes.Where(c => c.CreatedAtUtc.Date == DateTimeOffset.UtcNow.Date).ToList();
        var deadlineRisks = LiveDeadlineRiskEngine.Evaluate(payload, operations, DateTimeOffset.UtcNow);
        var metrics = PlanStabilityMetricsEngine.Build(
            changesToday,
            versionsToday,
            adherence,
            operations.Count == 0 ? 0m : 72m,
            deadlineRisks.Count(r => r.RiskLevel == "at-risk"),
            deadlineRisks.Count(r => r.RiskLevel == "late"));
        var risks = new AdaptivePlanningRiskSummary(
            deadlineRisks,
            [],
            state.MachineStateJson.Contains("fault") ? ["IM-07 arıza"] : [],
            state.MaterialStateJson.Contains("shortage") ? ["PP-HF malzeme"] : [],
            state.MoldStateJson.Contains("mold") ? ["M-014 bakım"] : [],
            state.QualityStateJson.Contains("hold") ? ["Kalite hold"] : [],
            state.MaintenanceStateJson.Contains("open") ? ["Planlı bakım"] : []);

        return new AdaptivePlanningWorkspaceSnapshot(
            plan, activeVersion, operations, scenarios, deviations, changes, activity, policy, metrics, risks,
            payload.FreshnessUtc);
    }

    private async Task<PlanBuildResult> BuildPlanCoreAsync(
        Guid facilityId, BuildPlanRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var plan = await repository.GetPlanAsync(request.ProductionPlanId, cancellationToken)
            ?? throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.NotFound, "Üretim planı bulunamadı.");
        if (plan.FacilityId != facilityId)
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.Forbidden, "Tesis erişimi reddedildi.");

        var now = DateTimeOffset.UtcNow;
        var state = await FactoryStateProjector.CaptureAsync(dbContext, tenantId, facilityId, cancellationToken);
        PlanningConflictGuard.Ensure(state);
        var snapshot = PlanningConstraintSnapshotEngine.Capture(
            tenantId, facilityId, request.PlanningHorizon, plan, state, now);
        var payload = PlanningConstraintSnapshotEngine.ParsePayload(snapshot);
        PlanningFreshnessGuard.Ensure(payload, now);

        var priorVersions = await repository.GetVersionsForPlanAsync(plan.Id, cancellationToken);
        var versionNumber = priorVersions.Count == 0 ? 1 : priorVersions.Max(v => v.VersionNumber) + 1;
        var version = new ProductionPlanVersion
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionPlanId = plan.Id,
            PreviousVersionId = plan.ActivePlanVersionId,
            VersionNumber = versionNumber,
            PlanningHorizon = request.PlanningHorizon,
            LifecycleStatus = AdaptivePlanLifecycleStatuses.Ready,
            ObjectiveProfileJson = request.ObjectiveProfileJson,
            ConstraintSnapshotId = snapshot.Id,
            CreatedBySubjectId = subjectId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyToken = 1,
        };

        var learning = await repository.GetChangeoverLearningAsync(facilityId, cancellationToken);
        var operations = FactorySchedulingEngine.Schedule(tenantId, version.Id, payload, plan, learning, now);
        var feasibility = PlanFeasibilityEngine.Evaluate(payload, operations);
        if (!feasibility.IsFeasible)
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.Infeasible, feasibility.Explanation ?? "Plan uygulanabilir değil.");

        var score = PlanningObjectiveEngine.Score(feasibility, operations, payload, request.ObjectiveProfileJson);
        version.Score = score;
        version.ConfidenceScore = 0.82m;
        version.ExpectedImpactJson = JsonSerializer.Serialize(new { score, operationCount = operations.Count });

        await repository.AddSnapshotAsync(snapshot, cancellationToken);
        await repository.AddVersionAsync(version, cancellationToken);
        await repository.AddOperationsAsync(operations, cancellationToken);
        await repository.AddActivityAsync(new PlanActivityEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionPlanId = plan.Id,
            EventType = "plan-built",
            Summary = $"Plan V{versionNumber} oluşturuldu",
            OccurredAtUtc = now,
        }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new PlanBuildResult(version, operations, snapshot, feasibility, score, "Mevcut Plan");
    }

    private async Task<IReadOnlyList<PlanSimulationResult>> SimulateCoreAsync(
        Guid facilityId, SimulatePlanRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var plan = await repository.GetPlanAsync(request.ProductionPlanId, cancellationToken)
            ?? throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.NotFound, "Üretim planı bulunamadı.");
        if (plan.FacilityId != facilityId)
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.Forbidden, "Tesis erişimi reddedildi.");

        var now = DateTimeOffset.UtcNow;
        var state = await FactoryStateProjector.CaptureAsync(dbContext, tenantId, facilityId, cancellationToken);
        var snapshot = PlanningConstraintSnapshotEngine.Capture(
            tenantId, facilityId, PlanningHorizons.Operational, plan, state, now);
        var payload = PlanningConstraintSnapshotEngine.ParsePayload(snapshot);
        var baselineOps = plan.ActivePlanVersionId is Guid activeId
            ? await repository.GetOperationsAsync(activeId, cancellationToken)
            : [];
        var learning = await repository.GetChangeoverLearningAsync(facilityId, cancellationToken);
        var baselineFeasibility = PlanFeasibilityEngine.Evaluate(payload, baselineOps);
        var baselineScore = PlanScoringEngine.ScoreScenario(baselineFeasibility, baselineOps, payload);

        await repository.AddSnapshotAsync(snapshot, cancellationToken);
        var results = new List<PlanSimulationResult>();
        var rank = 1;
        foreach (var strategy in AlternativePlanGenerator.Strategies(request.TriggerType).Take(request.MaxAlternatives))
        {
            var versionId = Guid.NewGuid();
            var candidateOps = FactorySchedulingEngine.Schedule(tenantId, versionId, payload, plan, learning, now);
            candidateOps = strategy.Transform(candidateOps);
            if (request.TriggerType == "machine-failure")
                candidateOps = MinimalPlanRepairEngine.RepairMachineFailure(candidateOps, "IM-07", payload);

            var feasibility = PlanFeasibilityEngine.Evaluate(payload, candidateOps);
            if (!feasibility.IsFeasible) continue;

            var score = PlanScoringEngine.ScoreScenario(feasibility, candidateOps, payload);
            var scenario = new PlanningScenario
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionPlanId = plan.Id,
                ConstraintSnapshotId = snapshot.Id,
                ScenarioCode = strategy.Code,
                Title = strategy.Title,
                TriggerType = request.TriggerType,
                Score = score,
                Rank = rank++,
                ComparisonJson = PlanComparisonEngine.BuildComparisonJson(baselineOps, candidateOps, baselineScore, score),
                Status = "proposed",
                CreatedAtUtc = now,
            };
            var metrics = new List<PlanningScenarioMetric>
            {
                new() { Id = Guid.NewGuid(), TenantId = tenantId, ScenarioId = scenario.Id, MetricKey = "delivery-impact", MetricValue = score, Unit = "score", DataClassification = PlanningDataClassifications.Derived },
                new() { Id = Guid.NewGuid(), TenantId = tenantId, ScenarioId = scenario.Id, MetricKey = "changeovers", MetricValue = candidateOps.Sum(o => o.SetupMinutes), Unit = "minutes", DataClassification = PlanningDataClassifications.Derived },
            };
            await repository.AddScenarioAsync(scenario, metrics, cancellationToken);
            results.Add(new PlanSimulationResult(scenario, metrics, scenario.ComparisonJson));
        }

        await repository.AddActivityAsync(new PlanActivityEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionPlanId = plan.Id,
            EventType = "simulated",
            Summary = $"{results.Count} alternatif oluşturuldu",
            OccurredAtUtc = now,
        }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return results;
    }

    private async Task<ReplanEvaluationResult> EvaluateReplanCoreAsync(
        Guid facilityId, EvaluateReplanRequest request, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var plan = await repository.GetPlanAsync(request.ProductionPlanId, cancellationToken)
            ?? throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.NotFound, "Üretim planı bulunamadı.");
        var policy = await repository.GetOrCreatePolicyAsync(tenantId, facilityId, cancellationToken);
        var state = await FactoryStateProjector.CaptureAsync(dbContext, tenantId, facilityId, cancellationToken);
        var snapshot = PlanningConstraintSnapshotEngine.Capture(
            tenantId, facilityId, PlanningHorizons.RealTimeRecovery, plan, state, DateTimeOffset.UtcNow);
        var payload = PlanningConstraintSnapshotEngine.ParsePayload(snapshot);
        var action = ReplanNecessityEngine.Evaluate(request.TriggerType, payload);
        var changes = await repository.GetChangesAsync(plan.Id, 1, cancellationToken);
        var lastReplan = changes.Count > 0 ? changes[0].CreatedAtUtc : (DateTimeOffset?)null;
        var nervousness = PlanNervousnessGuard.ShouldBlock(policy, lastReplan, 0.12m, DateTimeOffset.UtcNow);
        var currentOps = plan.ActivePlanVersionId is Guid vid
            ? await repository.GetOperationsAsync(vid, cancellationToken) : [];
        var proposed = MinimalPlanRepairEngine.RepairMachineFailure(currentOps, "IM-07", payload);
        var frozen = FrozenWindowEngine.WouldViolate(policy, proposed, currentOps, DateTimeOffset.UtcNow,
            request.TriggerType is "machine-failure" or "quality-hold");
        var alternatives = await repository.GetScenariosAsync(plan.Id, cancellationToken);
        return new ReplanEvaluationResult(
            nervousness || frozen ? ReplanActions.KeepCurrentPlan : action,
            frozen ? "Donmuş pencere içinde değişiklik reddedildi." : "Sapma değerlendirildi.",
            nervousness,
            frozen,
            alternatives);
    }

    private async Task<PlanPublishResult> PublishCoreAsync(
        Guid facilityId, PublishPlanRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var version = await repository.GetVersionAsync(request.PlanVersionId, cancellationToken)
            ?? throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.NotFound, "Plan sürümü bulunamadı.");
        if (version.FacilityId != facilityId)
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.Forbidden, "Tesis erişimi reddedildi.");
        if (version.ConcurrencyToken != request.ConcurrencyToken)
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");
        if (version.LifecycleStatus is AdaptivePlanLifecycleStatuses.Published or AdaptivePlanLifecycleStatuses.Active)
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.DuplicatePublish, "Plan sürümü zaten yayımlandı.");

        var plan = await repository.GetPlanAsync(version.ProductionPlanId, cancellationToken)!;
        var policy = await repository.GetOrCreatePolicyAsync(tenantId, facilityId, cancellationToken);
        var operations = await repository.GetOperationsAsync(version.Id, cancellationToken);
        var currentOps = plan!.ActivePlanVersionId is Guid activeId
            ? await repository.GetOperationsAsync(activeId, cancellationToken) : [];
        var snapshot = version.ConstraintSnapshotId is Guid snapId
            ? await dbContext.PlanningConstraintSnapshots.AsNoTracking().FirstAsync(s => s.Id == snapId, cancellationToken)
            : throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.ValidationFailed, "Constraint snapshot eksik.");
        var payload = PlanningConstraintSnapshotEngine.ParsePayload(snapshot);
        PlanningFreshnessGuard.Ensure(payload, DateTimeOffset.UtcNow);
        var revalidation = PlanRevalidationEngine.Revalidate(payload, operations);
        PlanSafetyDominanceEngine.Ensure(revalidation);

        var criticalIncident = request.OverrideFrozenWindow;
        if (!request.OverrideFrozenWindow &&
            FrozenWindowEngine.WouldViolate(policy, operations, currentOps, DateTimeOffset.UtcNow, criticalIncident))
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.FrozenWindow, "Donmuş pencere ihlali.");

        var diff = PlanDiffEngine.Diff(currentOps, operations);
        var affectedOrders = diff.Select(d => d.OrderId).Distinct().Count();
        var movement = diff.Where(d => d.Field == "start").Sum(d => Math.Abs(int.TryParse(d.ImpactSummary.Trim('+', ' ', 'm', 'i', 'n'), out var v) ? v : 0));
        var latenessImpact = 0.5m;
        var requiresApproval = PlanningAutonomyEngine.RequiresApproval(policy, affectedOrders, latenessImpact);
        if (requiresApproval && version.LifecycleStatus != AdaptivePlanLifecycleStatuses.ApprovalPending)
        {
            version.LifecycleStatus = AdaptivePlanLifecycleStatuses.ApprovalPending;
            version.UpdatedAtUtc = DateTimeOffset.UtcNow;
            version.ConcurrencyToken++;
            await repository.SaveChangesAsync(cancellationToken);
            return new PlanPublishResult(version, plan, new PlanChange(), false, true);
        }

        if (plan.ActivePlanVersionId is Guid priorActive)
        {
            var prior = await repository.GetVersionAsync(priorActive, cancellationToken);
            if (prior is not null)
            {
                prior.LifecycleStatus = AdaptivePlanLifecycleStatuses.Superseded;
                prior.SupersededAtUtc = DateTimeOffset.UtcNow;
                prior.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        var now = DateTimeOffset.UtcNow;
        version.LifecycleStatus = AdaptivePlanLifecycleStatuses.Active;
        version.PublishedAtUtc = now;
        version.UpdatedAtUtc = now;
        version.ConcurrencyToken++;
        plan.ActivePlanVersionId = version.Id;
        plan.AdaptiveLifecycleStatus = AdaptivePlanLifecycleStatuses.Active;
        plan.UpdatedAtUtc = now;

        var change = new PlanChange
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionPlanId = plan.Id,
            PlanVersionId = version.Id,
            FromVersionId = version.PreviousVersionId,
            ChangeType = request.OverrideFrozenWindow ? "publish-frozen-override" : "publish",
            TriggerType = "manual-publish",
            InitiatedBySubjectId = subjectId,
            DiffJson = JsonSerializer.Serialize(new { movedOperations = diff.Count, rows = diff }),
            ReasonJson = JsonSerializer.Serialize(new { approvalNotes = request.ApprovalNotes }),
            ExpectedBenefitJson = version.ExpectedImpactJson,
            CreatedAtUtc = now,
        };
        await repository.AddChangeAsync(change, cancellationToken);
        await repository.AddActivityAsync(new PlanActivityEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ProductionPlanId = plan.Id,
            EventType = "published",
            Summary = $"Plan V{version.VersionNumber} yayınlandı",
            OccurredAtUtc = now,
        }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var auto = PlanningAutonomyEngine.CanAutoPublish(policy, affectedOrders, movement, latenessImpact);
        return new PlanPublishResult(version, plan, change, auto, false);
    }

    private async Task<PlanPublishResult> ApproveReplanCoreAsync(
        Guid facilityId, ApproveReplanRequest request, string subjectId, CancellationToken cancellationToken)
    {
        if (!request.Approved)
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.ValidationFailed, "Replan reddedildi.");

        return await PublishCoreAsync(
            facilityId,
            new PublishPlanRequest(request.PlanVersionId, request.ConcurrencyToken, false, request.Notes),
            subjectId,
            cancellationToken);
    }

    private async Task<PlanOutcomeVerificationResult> VerifyOutcomeCoreAsync(
        Guid planVersionId, CancellationToken cancellationToken)
    {
        var version = await repository.GetVersionAsync(planVersionId, cancellationToken)
            ?? throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.NotFound, "Plan sürümü bulunamadı.");
        var operations = await repository.GetOperationsAsync(planVersionId, cancellationToken);
        var adherence = PlanAdherenceEngine.Compute(operations, DateTimeOffset.UtcNow);
        var outcome = PlanOutcomeVerificationEngine.Verify(version, operations, adherence);
        version.ActualImpactJson = outcome.ActualImpactJson;
        version.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var memory = PlanningLearningBridge.BuildMemory(version.TenantId, version.FacilityId, version, outcome);
        await repository.AddLearningMemoryAsync(memory, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return outcome with { LearningMemoryId = memory.Id };
    }

    private async Task<AdaptivePlanningPolicy> UpdatePolicyCoreAsync(
        Guid facilityId, UpdateAdaptivePlanningPolicyRequest request, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var policy = await repository.GetOrCreatePolicyAsync(tenantId, facilityId, cancellationToken);
        if (policy.ConcurrencyToken != request.ConcurrencyToken)
            throw new AdaptivePlanningException(AdaptivePlanningFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");
        if (request.FrozenWindowMinutes is int fw) policy.FrozenWindowMinutes = fw;
        if (request.NervousnessCooldownMinutes is int nc) policy.NervousnessCooldownMinutes = nc;
        if (request.MinBenefitThreshold is decimal mb) policy.MinBenefitThreshold = mb;
        if (request.AutonomyMode is not null) policy.AutonomyMode = request.AutonomyMode;
        if (request.PolicyEnvelopeJson is not null) policy.PolicyEnvelopeJson = request.PolicyEnvelopeJson;
        policy.UpdatedAtUtc = DateTimeOffset.UtcNow;
        policy.ConcurrencyToken++;
        await repository.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private Guid RequireTenantId() => tenantContextAccessor.RequireTenantId();
}
