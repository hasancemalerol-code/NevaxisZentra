using System.Text.Json;
using NevaxisFactOS.Persistence.ProductionManager;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.FactoryNetworkOptimization;

public interface IFactoryNetworkService
{
    Task<FactoryNetworkWorkspaceSnapshot> GetWorkspaceAsync(
        Guid networkId, IReadOnlyList<Guid> permittedFacilityIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<NetworkSimulationResult>> SimulateAsync(
        SimulateNetworkRequest request, string subjectId, CancellationToken cancellationToken);
    Task<NetworkAllocationEvaluation> EvaluateAllocationAsync(
        EvaluateNetworkAllocationRequest request, CancellationToken cancellationToken);
    Task<NetworkAllocationResult> ExecuteAllocationAsync(
        ExecuteNetworkAllocationRequest request, string subjectId, CancellationToken cancellationToken);
    Task<NetworkAllocationResult> ApproveAllocationAsync(
        ApproveNetworkAllocationRequest request, string subjectId, CancellationToken cancellationToken);
    Task<NetworkOutcomeVerificationResult> VerifyOutcomeAsync(
        Guid allocationId, CancellationToken cancellationToken);
    Task<FactoryNetworkPolicy> UpdatePolicyAsync(
        Guid networkId, UpdateFactoryNetworkPolicyRequest request, CancellationToken cancellationToken);
}

public sealed class FactoryNetworkService : IFactoryNetworkService
{
    private readonly IFactoryNetworkRepository repository;
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public FactoryNetworkService(
        IFactoryNetworkRepository repository,
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.repository = repository;
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public Task<FactoryNetworkWorkspaceSnapshot> GetWorkspaceAsync(
        Guid networkId, IReadOnlyList<Guid> permittedFacilityIds, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => BuildWorkspaceAsync(networkId, permittedFacilityIds, ct),
            cancellationToken);

    public Task<IReadOnlyList<NetworkSimulationResult>> SimulateAsync(
        SimulateNetworkRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => SimulateCoreAsync(request, subjectId, ct),
            cancellationToken);

    public Task<NetworkAllocationEvaluation> EvaluateAllocationAsync(
        EvaluateNetworkAllocationRequest request, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => EvaluateAllocationCoreAsync(request, ct),
            cancellationToken);

    public Task<NetworkAllocationResult> ExecuteAllocationAsync(
        ExecuteNetworkAllocationRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ExecuteAllocationCoreAsync(request, subjectId, ct),
            cancellationToken);

    public Task<NetworkAllocationResult> ApproveAllocationAsync(
        ApproveNetworkAllocationRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ApproveAllocationCoreAsync(request, subjectId, ct),
            cancellationToken);

    public Task<NetworkOutcomeVerificationResult> VerifyOutcomeAsync(
        Guid allocationId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => VerifyOutcomeCoreAsync(allocationId, ct),
            cancellationToken);

    public Task<FactoryNetworkPolicy> UpdatePolicyAsync(
        Guid networkId, UpdateFactoryNetworkPolicyRequest request, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => UpdatePolicyCoreAsync(networkId, request, ct),
            cancellationToken);

    private async Task<FactoryNetworkWorkspaceSnapshot> BuildWorkspaceAsync(
        Guid networkId, IReadOnlyList<Guid> permittedFacilityIds, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var network = await repository.GetNetworkAsync(networkId, cancellationToken)
            ?? throw new FactoryNetworkException(FactoryNetworkFailureCodes.NotFound, "Fabrika ağı bulunamadı.");

        var facilities = await repository.GetNetworkFacilitiesAsync(networkId, permittedFacilityIds, cancellationToken);
        if (facilities.Count == 0)
            throw new FactoryNetworkException(FactoryNetworkFailureCodes.FacilityScopeDenied, "Tesis erişimi reddedildi.");

        var facilityIds = facilities.Select(f => f.FacilityId).ToList();
        var profiles = (await repository.GetCapabilityProfilesAsync(tenantId, facilityIds, cancellationToken))
            .ToDictionary(p => p.FacilityId);
        var contexts = await NetworkFactoryStateProjector.CaptureAsync(
            dbContext, tenantId, facilityIds, profiles, cancellationToken);
        NetworkConflictGuard.Ensure(contexts);

        var capacitySnapshot = await repository.GetLatestCapacitySnapshotAsync(networkId, cancellationToken);
        var demand = NetworkDemandEngine.ParseSnapshot(capacitySnapshot);
        var capacityMap = NetworkCapacityMapEngine.Build(contexts, capacitySnapshot);
        var imbalance = LoadImbalanceDetectionEngine.ComputeImbalancePercent(capacityMap);
        var opportunities = CrossPlantOpportunityEngine.Detect(contexts, demand, capacityMap);
        var policy = await repository.GetOrCreatePolicyAsync(tenantId, networkId, cancellationToken);
        var scenarios = await repository.GetScenariosAsync(networkId, 20, cancellationToken);
        var activeAllocations = await repository.GetActiveAllocationsAsync(networkId, cancellationToken);
        var activity = await repository.GetActivityAsync(networkId, 30, cancellationToken);
        var resilience = NetworkResilienceEngine.Build(contexts, capacityMap);
        var metrics = NetworkMetricsEngine.Build(capacityMap, demand, activeAllocations, imbalance);
        var freshness = contexts.Count == 0
            ? DateTimeOffset.UtcNow
            : contexts.Min(c => c.FreshnessUtc);

        return new FactoryNetworkWorkspaceSnapshot(
            network,
            facilities,
            profiles.Values.ToList(),
            contexts.Select(NetworkFactoryStateProjector.ToStateRow).ToList(),
            capacityMap,
            demand,
            opportunities,
            scenarios,
            activeAllocations,
            activity,
            policy,
            metrics,
            resilience,
            freshness);
    }

    private async Task<IReadOnlyList<NetworkSimulationResult>> SimulateCoreAsync(
        SimulateNetworkRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var network = await repository.GetNetworkAsync(request.NetworkId, cancellationToken)
            ?? throw new FactoryNetworkException(FactoryNetworkFailureCodes.NotFound, "Fabrika ağı bulunamadı.");

        var facilities = await repository.GetNetworkFacilitiesAsync(request.NetworkId, null, cancellationToken);
        var facilityIds = facilities.Select(f => f.FacilityId).ToList();
        var profiles = (await repository.GetCapabilityProfilesAsync(tenantId, facilityIds, cancellationToken))
            .ToDictionary(p => p.FacilityId);
        var contexts = await NetworkFactoryStateProjector.CaptureAsync(
            dbContext, tenantId, facilityIds, profiles, cancellationToken);
        NetworkConflictGuard.Ensure(contexts);

        var snapshot = await repository.GetLatestCapacitySnapshotAsync(request.NetworkId, cancellationToken);
        var demandRows = NetworkDemandEngine.ParseSnapshot(snapshot);
        var demand = demandRows.FirstOrDefault(d => d.OrderId == request.OrderId)
            ?? new NetworkDemandRow(
                request.OrderId,
                request.SourceFacilityId,
                request.Quantity,
                DateTimeOffset.UtcNow.AddDays(1),
                "medium",
                "injection-molding",
                true,
                true);

        var now = DateTimeOffset.UtcNow;
        NetworkFreshnessGuard.Ensure(contexts.Min(c => c.FreshnessUtc), now, false);
        var results = NetworkWhatIfSimulator.Simulate(tenantId, network.Id, request, contexts, demand, now);

        foreach (var result in results)
            await repository.AddScenarioAsync(result.Scenario, cancellationToken);

        await repository.AddActivityAsync(new NetworkActivityEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NetworkId = network.Id,
            EventType = "simulated",
            Summary = $"{results.Count} ağ senaryosu oluşturuldu ({subjectId})",
            OccurredAtUtc = now,
        }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return results;
    }

    private async Task<NetworkAllocationEvaluation> EvaluateAllocationCoreAsync(
        EvaluateNetworkAllocationRequest request, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var network = await repository.GetNetworkAsync(request.NetworkId, cancellationToken)
            ?? throw new FactoryNetworkException(FactoryNetworkFailureCodes.NotFound, "Fabrika ağı bulunamadı.");

        var facilities = await repository.GetNetworkFacilitiesAsync(request.NetworkId, null, cancellationToken);
        var facilityIds = facilities.Select(f => f.FacilityId).ToList();
        var profiles = (await repository.GetCapabilityProfilesAsync(tenantId, facilityIds, cancellationToken))
            .ToDictionary(p => p.FacilityId);
        var contexts = await NetworkFactoryStateProjector.CaptureAsync(
            dbContext, tenantId, facilityIds, profiles, cancellationToken);
        var snapshot = await repository.GetLatestCapacitySnapshotAsync(request.NetworkId, cancellationToken);
        var demand = NetworkDemandEngine.ParseSnapshot(snapshot);
        var sourceContext = contexts.OrderByDescending(c => c.LoadPercent).First();
        var destinationContext = contexts
            .OrderByDescending(c => c.AvailablePercent)
            .First(c => c.FacilityId != sourceContext.FacilityId);
        var order = demand.Count > 0 ? demand[0] : new NetworkDemandRow(
            "PO-1842",
            sourceContext.FacilityId,
            100m,
            DateTimeOffset.UtcNow.AddDays(1),
            "medium",
            "injection-molding",
            true,
            true);
        var policy = await repository.GetOrCreatePolicyAsync(tenantId, request.NetworkId, cancellationToken);

        if (NetworkPolicyEnvelopeEngine.IsProhibitedPair(policy, sourceContext.FacilityId, destinationContext.FacilityId))
            throw new FactoryNetworkException(FactoryNetworkFailureCodes.PolicyViolation, "Politika bu tesis çiftini yasaklıyor.");

        var logistics = NetworkLogisticsImpactEngine.Classify(sourceContext, destinationContext);
        var comparisons = contexts.Select(ctx =>
        {
            var feasibility = CrossPlantFeasibilityEngine.Evaluate(sourceContext, ctx, order, NetworkOptionActions.MoveFullOrder);
            return NetworkScoringEngine.Compare(ctx, order, feasibility, logistics);
        }).OrderByDescending(c => c.ConfidenceScore).ToList();

        var alternatives = (await repository.GetScenariosAsync(request.NetworkId, 10, cancellationToken)).ToList();
        var best = comparisons.FirstOrDefault(c => c.Feasible) ?? comparisons.First();
        var recommended = best.Feasible ? NetworkOptionActions.MoveFullOrder : NetworkOptionActions.KeepCurrentFacility;

        return new NetworkAllocationEvaluation(
            recommended,
            $"Tetikleyici: {request.TriggerType}",
            best.RecommendationReason,
            comparisons,
            alternatives);
    }

    private async Task<NetworkAllocationResult> ExecuteAllocationCoreAsync(
        ExecuteNetworkAllocationRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var allocation = await repository.GetAllocationAsync(request.AllocationId, cancellationToken)
            ?? throw new FactoryNetworkException(FactoryNetworkFailureCodes.NotFound, "Tahsis bulunamadı.");

        if (allocation.ConcurrencyToken != request.ConcurrencyToken)
            throw new FactoryNetworkException(FactoryNetworkFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");

        if (allocation.Status is NetworkAllocationStatuses.Published or NetworkAllocationStatuses.Verified)
            throw new FactoryNetworkException(FactoryNetworkFailureCodes.DuplicateExecution, "Tahsis zaten yürütüldü.");

        var items = await repository.GetAllocationItemsAsync(allocation.Id, cancellationToken);
        var policy = await repository.GetOrCreatePolicyAsync(tenantId, allocation.NetworkId, cancellationToken);
        var contexts = await LoadContextsAsync(tenantId, allocation.NetworkId, cancellationToken);
        NetworkFreshnessGuard.Ensure(contexts.Min(c => c.FreshnessUtc), DateTimeOffset.UtcNow, request.OverrideStaleGuard);

        if (NetworkPolicyEnvelopeEngine.IsProhibitedPair(policy, allocation.SourceFacilityId, allocation.DestinationFacilityId))
            throw new FactoryNetworkException(FactoryNetworkFailureCodes.PolicyViolation, "Politika bu tesis çiftini yasaklıyor.");

        var affectedOrders = items.Select(i => i.OrderId).Distinct().Count();
        var benefitScore = 0.72m;
        var requiresApproval = NetworkAutonomyEngine.RequiresApproval(policy, affectedOrders, benefitScore);

        if (requiresApproval && allocation.Status != NetworkAllocationStatuses.ApprovalPending)
        {
            allocation.Status = NetworkAllocationStatuses.ApprovalPending;
            allocation.UpdatedAtUtc = DateTimeOffset.UtcNow;
            allocation.ConcurrencyToken++;
            await repository.SaveChangesAsync(cancellationToken);
            return new NetworkAllocationResult(allocation, items, true, false);
        }

        var now = DateTimeOffset.UtcNow;
        var decision = await CreateFactoryDecisionAsync(
            tenantId, allocation, contexts, subjectId, now, cancellationToken);
        allocation.FactoryDecisionId = decision.Id;

        var (sourceVersionId, destinationVersionId) = await NetworkToPlanningBridge.ApplyAllocationAsync(
            dbContext, tenantId, allocation, items, cancellationToken);
        foreach (var item in items)
        {
            item.SourcePlanVersionId = sourceVersionId;
            item.DestinationPlanVersionId = destinationVersionId;
            item.Status = "published";
        }

        allocation.Status = NetworkAllocationStatuses.Published;
        allocation.PublishedAtUtc = now;
        allocation.UpdatedAtUtc = now;
        allocation.ConcurrencyToken++;

        await repository.AddActivityAsync(new NetworkActivityEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NetworkId = allocation.NetworkId,
            EventType = "executed",
            Summary = $"Tahsis yürütüldü: {allocation.ActionType}",
            OccurredAtUtc = now,
        }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var autoExecuted = NetworkAutonomyEngine.CanAutoExecute(policy, affectedOrders, benefitScore);
        return new NetworkAllocationResult(allocation, items, false, autoExecuted);
    }

    private async Task<NetworkAllocationResult> ApproveAllocationCoreAsync(
        ApproveNetworkAllocationRequest request, string subjectId, CancellationToken cancellationToken)
    {
        if (!request.Approved)
        {
            var allocation = await repository.GetAllocationAsync(request.AllocationId, cancellationToken)
                ?? throw new FactoryNetworkException(FactoryNetworkFailureCodes.NotFound, "Tahsis bulunamadı.");
            if (allocation.ConcurrencyToken != request.ConcurrencyToken)
                throw new FactoryNetworkException(FactoryNetworkFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");

            allocation.Status = NetworkAllocationStatuses.Cancelled;
            allocation.UpdatedAtUtc = DateTimeOffset.UtcNow;
            allocation.ConcurrencyToken++;
            await repository.SaveChangesAsync(cancellationToken);
            return new NetworkAllocationResult(allocation, await repository.GetAllocationItemsAsync(allocation.Id, cancellationToken), false, false);
        }

        return await ExecuteAllocationCoreAsync(
            new ExecuteNetworkAllocationRequest(request.AllocationId, request.ConcurrencyToken, false),
            subjectId,
            cancellationToken);
    }

    private async Task<NetworkOutcomeVerificationResult> VerifyOutcomeCoreAsync(
        Guid allocationId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var allocation = await repository.GetAllocationAsync(allocationId, cancellationToken)
            ?? throw new FactoryNetworkException(FactoryNetworkFailureCodes.NotFound, "Tahsis bulunamadı.");

        if (allocation.Status != NetworkAllocationStatuses.Published)
            throw new FactoryNetworkException(FactoryNetworkFailureCodes.ValidationFailed, "Yalnızca yayımlanmış tahsis doğrulanabilir.");

        var items = await repository.GetAllocationItemsAsync(allocationId, cancellationToken);
        var outcome = NetworkOutcomeVerificationEngine.Verify(allocation, items, 78m);
        NetworkQuantityConservationEngine.Ensure(outcome.QuantityConservationPercent);

        allocation.ActualImpactJson = outcome.ActualImpactJson;
        allocation.Status = NetworkAllocationStatuses.Verified;
        allocation.VerifiedAtUtc = DateTimeOffset.UtcNow;
        allocation.UpdatedAtUtc = DateTimeOffset.UtcNow;
        allocation.ConcurrencyToken++;

        var memory = NetworkLearningBridge.BuildMemory(
            tenantId, allocation.DestinationFacilityId, allocation, outcome);
        await repository.AddLearningMemoryAsync(memory, cancellationToken);
        await repository.AddActivityAsync(new NetworkActivityEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NetworkId = allocation.NetworkId,
            EventType = "verified",
            Summary = $"Tahsis doğrulandı: {outcome.OutcomeStatus}",
            OccurredAtUtc = DateTimeOffset.UtcNow,
        }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return outcome with { LearningMemoryId = memory.Id };
    }

    private async Task<FactoryNetworkPolicy> UpdatePolicyCoreAsync(
        Guid networkId, UpdateFactoryNetworkPolicyRequest request, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var policy = await repository.GetOrCreatePolicyAsync(tenantId, networkId, cancellationToken);
        if (policy.ConcurrencyToken != request.ConcurrencyToken)
            throw new FactoryNetworkException(FactoryNetworkFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");

        if (request.AutonomyMode is not null) policy.AutonomyMode = request.AutonomyMode;
        if (request.PolicyEnvelopeJson is not null) policy.PolicyEnvelopeJson = request.PolicyEnvelopeJson;
        policy.UpdatedAtUtc = DateTimeOffset.UtcNow;
        policy.ConcurrencyToken++;
        await repository.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private async Task<IReadOnlyList<NetworkFacilityContext>> LoadContextsAsync(
        Guid tenantId, Guid networkId, CancellationToken cancellationToken)
    {
        var facilities = await repository.GetNetworkFacilitiesAsync(networkId, null, cancellationToken);
        var facilityIds = facilities.Select(f => f.FacilityId).ToList();
        var profiles = (await repository.GetCapabilityProfilesAsync(tenantId, facilityIds, cancellationToken))
            .ToDictionary(p => p.FacilityId);
        return await NetworkFactoryStateProjector.CaptureAsync(
            dbContext, tenantId, facilityIds, profiles, cancellationToken);
    }

    private async Task<FactoryDecision> CreateFactoryDecisionAsync(
        Guid tenantId,
        NetworkAllocation allocation,
        IReadOnlyList<NetworkFacilityContext> contexts,
        string subjectId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"network-allocation:{allocation.Id:N}";
        var existing = await dbContext.FactoryDecisions
            .FirstOrDefaultAsync(
                d => d.TenantId == tenantId && d.ExecutionIdempotencyKey == idempotencyKey,
                cancellationToken);
        if (existing is not null)
            return existing;

        var source = contexts.First(c => c.FacilityId == allocation.SourceFacilityId);
        var state = source.State;
        var decision = new FactoryDecision
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = allocation.SourceFacilityId,
            StateSnapshotId = state.Id,
            DecisionType = NetworkDecisionTypes.CrossPlantReallocation,
            Title = $"Ağ tahsisi: {allocation.ActionType}",
            ProblemSummary = $"Çapraz tesis tahsisi {allocation.SourceFacilityId} → {allocation.DestinationFacilityId}",
            Status = ProductionManagerDecisionStatuses.Executing,
            AutonomyMode = ProductionManagerAutonomyModes.Limited,
            ConfidenceBand = ProductionManagerConfidenceBands.Medium,
            ConfidenceScore = 0.78m,
            ExpectedOutcomeJson = allocation.ExpectedImpactJson,
            VerificationCriteriaJson = """["quantity-conservation","plan-adherence"]""",
            ApprovalState = "approved",
            ExecutionIdempotencyKey = idempotencyKey,
            ExecutedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyToken = 1,
        };

        var exists = await dbContext.FactoryStateSnapshots
            .AnyAsync(s => s.Id == state.Id, cancellationToken);
        if (!exists)
            dbContext.FactoryStateSnapshots.Add(state);
        dbContext.FactoryDecisions.Add(decision);
        await dbContext.SaveChangesAsync(cancellationToken);
        return decision;
    }

    private Guid RequireTenantId() => tenantContextAccessor.RequireTenantId();
}
