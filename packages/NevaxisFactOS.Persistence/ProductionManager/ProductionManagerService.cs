using System.Text.Json;
using NevaxisFactOS.Persistence.AiDecisions;
using NevaxisFactOS.Persistence.FactoryOrchestration;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.ProductionManager;

public interface IProductionManagerService
{
    Task<ProductionManagerWorkspaceSnapshot> GetWorkspaceAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FactoryDecision> CreateDecisionAsync(Guid facilityId, string decisionType, string? correlationKey, string subjectId, CancellationToken cancellationToken);
    Task<SimulationResult> SimulateAsync(Guid facilityId, SimulationRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryDecision> ApproveDecisionAsync(Guid decisionId, ApproveDecisionRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryDecision> ExecuteDecisionAsync(Guid decisionId, ExecuteDecisionRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryDecision> VerifyDecisionAsync(Guid decisionId, VerifyDecisionOutcomeRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryDecision?> GetDecisionDetailAsync(Guid decisionId, CancellationToken cancellationToken);
}

public sealed class ProductionManagerService : IProductionManagerService
{
    private readonly IProductionManagerRepository repository;
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly IFactoryOrchestrationService factoryOrchestration;
    private readonly IAiDecisionLedgerWriter? ledgerWriter;

    public ProductionManagerService(
        IProductionManagerRepository repository,
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        ITenantContextAccessor tenantContextAccessor,
        IFactoryOrchestrationService factoryOrchestration,
        IAiDecisionLedgerWriter? ledgerWriter = null)
    {
        this.repository = repository;
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.tenantContextAccessor = tenantContextAccessor;
        this.factoryOrchestration = factoryOrchestration;
        this.ledgerWriter = ledgerWriter;
    }

    public Task<ProductionManagerWorkspaceSnapshot> GetWorkspaceAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetWorkspaceCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<FactoryDecision> CreateDecisionAsync(
        Guid facilityId, string decisionType, string? correlationKey, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CreateDecisionCoreAsync(facilityId, decisionType, correlationKey, subjectId, ct),
            cancellationToken);

    public Task<SimulationResult> SimulateAsync(
        Guid facilityId, SimulationRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => SimulateCoreAsync(facilityId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryDecision> ApproveDecisionAsync(
        Guid decisionId, ApproveDecisionRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ApproveDecisionCoreAsync(decisionId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryDecision> ExecuteDecisionAsync(
        Guid decisionId, ExecuteDecisionRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ExecuteDecisionCoreAsync(decisionId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryDecision> VerifyDecisionAsync(
        Guid decisionId, VerifyDecisionOutcomeRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => VerifyDecisionCoreAsync(decisionId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryDecision?> GetDecisionDetailAsync(
        Guid decisionId, CancellationToken cancellationToken) =>
        repository.GetDecisionAsync(decisionId, cancellationToken);

    private async Task<ProductionManagerWorkspaceSnapshot> GetWorkspaceCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var policy = await repository.GetOrCreatePolicyAsync(facilityId, cancellationToken);
        var state = await repository.CaptureStateAsync(facilityId, cancellationToken);
        var existingPredictions = await repository.ListActivePredictionsAsync(facilityId, cancellationToken);
        var newPredictions = FactoryPredictionEngine.Evaluate(state, existingPredictions);

        foreach (var prediction in newPredictions)
        {
            dbContext.FactoryPredictions.Add(prediction);
        }

        if (newPredictions.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var allPredictions = existingPredictions.Concat(newPredictions).ToList();
        var opportunities = DecisionOpportunityEngine.Evaluate(allPredictions);
        var pending = await repository.ListDecisionsAsync(
            facilityId, ProductionManagerDecisionStatuses.PendingApproval, 20, cancellationToken);
        var recent = await repository.ListDecisionsAsync(facilityId, null, 10, cancellationToken);
        var bottlenecks = BottleneckDetector.Detect(state);

        var today = DateTimeOffset.UtcNow.Date;
        var verifiedToday = recent.Count(d =>
            d.Status == ProductionManagerDecisionStatuses.Verified &&
            d.VerifiedAtUtc?.Date == today);

        return new ProductionManagerWorkspaceSnapshot(
            state,
            allPredictions,
            opportunities,
            pending,
            recent,
            bottlenecks,
            policy,
            new ProductionManagerExecutiveSummary(
                bottlenecks.Count > 0 ? "Dikkat Gerekli" : "Stabil",
                allPredictions.Count(p => p.Severity is "critical" or "high"),
                allPredictions.Count,
                pending.Count,
                recent.Count(d => d.Status == ProductionManagerDecisionStatuses.Executing),
                verifiedToday));
    }

    private async Task<FactoryDecision> CreateDecisionCoreAsync(
        Guid facilityId, string decisionType, string? correlationKey, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();

        var policy = await repository.GetOrCreatePolicyAsync(facilityId, cancellationToken);
        var state = await repository.CaptureStateAsync(facilityId, cancellationToken);
        var memories = await repository.ListMemoriesAsync(facilityId, decisionType, cancellationToken);
        var similarMemories = DecisionMemoryEngine.FindSimilar(memories, decisionType, state.ProductionStateJson)
            .Where(m => DecisionMemoryEngine.RevalidateConditions(m, state))
            .ToList();
        var (confidence, band) = DecisionConfidenceEngine.Evaluate(state, similarMemories);

        var optionDrafts = DecisionOptionGenerator.Generate(decisionType, state, policy);
        var ranked = DecisionScoringEngine.ScoreAndRank(optionDrafts, decisionType);
        var best = ranked.FirstOrDefault(o => !o.IsRejected)
            ?? throw new ProductionManagerException(ProductionManagerFailureCodes.ConstraintViolation, "Geçerli seçenek yok.");

        var now = DateTimeOffset.UtcNow;
        var decision = new FactoryDecision
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            StateSnapshotId = state.Id,
            DecisionType = decisionType,
            Title = best.Title,
            ProblemSummary = correlationKey ?? decisionType,
            Status = ProductionManagerDecisionStatuses.Proposed,
            AutonomyMode = policy.AutonomyMode,
            ConfidenceScore = confidence,
            ConfidenceBand = band,
            ExpectedOutcomeJson = best.ExpectedOutcomeJson,
            VerificationCriteriaJson = """["production-resumed","deadline-met"]""",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        var rank = 1;
        foreach (var draft in ranked)
        {
            decision.Options.Add(new FactoryDecisionOption
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                DecisionId = decision.Id,
                OptionCode = draft.OptionCode,
                Title = draft.Title,
                Description = draft.Description,
                ExpectedOutcomeJson = draft.ExpectedOutcomeJson,
                RisksJson = JsonSerializer.Serialize(draft.Risks),
                Score = DecisionScoringEngine.GetScore(draft, decisionType),
                RequiresApproval = draft.RequiresApproval,
                IsRejected = draft.IsRejected,
                RejectionReason = draft.RejectionReason,
                Rank = draft.IsRejected ? 99 : rank++,
                CreatedAtUtc = now,
            });
        }

        var selected = decision.Options.Where(o => !o.IsRejected).OrderByDescending(o => o.Score).First();
        decision.SelectedOptionId = selected.Id;

        var requiresApproval = AutonomyGuardEngine.RequiresApproval(policy, decision, selected, state);
        decision.ApprovalState = requiresApproval ? "pending" : "not-required";
        decision.Status = requiresApproval
            ? ProductionManagerDecisionStatuses.PendingApproval
            : ProductionManagerDecisionStatuses.Proposed;

        if (ledgerWriter is not null)
        {
            var ledgerResult = await ledgerWriter.WriteAsync(new AiDecisionWriteRequest(
                decisionType,
                "production-manager.decide",
                "deterministic",
                "production-manager-engine",
                selected.OptionCode,
                selected.Description,
                AiDecisionRiskLevel.Medium,
                requiresApproval,
                requiresApproval ? AiDecisionStatus.ReviewRequired : AiDecisionStatus.Proposed,
                [
                    new AiDecisionEvidence(
                        "factory-state-snapshot",
                        state.Id.ToString(),
                        "production-manager",
                        DateTimeOffset.UtcNow,
                        "1",
                        Summary: ProductionManagerDataClassifications.Observed),
                ],
                ConfidenceScore: confidence), cancellationToken);
            if (ledgerResult.Succeeded)
            {
                decision.AiDecisionLedgerId = ledgerResult.DecisionRecordId;
            }
        }

        dbContext.FactoryDecisions.Add(decision);
        await dbContext.SaveChangesAsync(cancellationToken);
        return decision;
    }

    private async Task<SimulationResult> SimulateCoreAsync(
        Guid facilityId, SimulationRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();

        var state = await repository.CaptureStateAsync(facilityId, cancellationToken);
        var result = WhatIfSimulator.Simulate(state, request);

        dbContext.FactorySimulations.Add(new FactorySimulation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            StateSnapshotId = state.Id,
            ScenarioType = request.ScenarioType,
            InputJson = JsonSerializer.Serialize(request),
            OutputJson = result.OutputJson,
            ComparisonJson = result.ComparisonJson,
            CreatedBy = subjectId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<FactoryDecision> ApproveDecisionCoreAsync(
        Guid decisionId, ApproveDecisionRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var decision = await LoadDecisionForWriteAsync(decisionId, request.ConcurrencyToken, cancellationToken);
        if (!request.Approved)
        {
            decision.Status = ProductionManagerDecisionStatuses.Cancelled;
            decision.ApprovalState = "rejected";
            decision.UpdatedAtUtc = DateTimeOffset.UtcNow;
            decision.ConcurrencyToken++;
            await dbContext.SaveChangesAsync(cancellationToken);
            return decision;
        }

        decision.Status = ProductionManagerDecisionStatuses.Approved;
        decision.ApprovalState = "approved";
        decision.SelectedBy = subjectId;
        decision.UpdatedAtUtc = DateTimeOffset.UtcNow;
        decision.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return decision;
    }

    private async Task<FactoryDecision> ExecuteDecisionCoreAsync(
        Guid decisionId, ExecuteDecisionRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var decision = await LoadDecisionForWriteAsync(decisionId, request.ConcurrencyToken, cancellationToken);

        if (!string.IsNullOrWhiteSpace(decision.ExecutionIdempotencyKey) &&
            decision.ExecutedAtUtc.HasValue)
        {
            throw new ProductionManagerException(ProductionManagerFailureCodes.AlreadyExecuted, "Karar zaten uygulandı.");
        }

        var policy = await repository.GetOrCreatePolicyAsync(decision.FacilityId, cancellationToken);
        var state = await dbContext.FactoryStateSnapshots.FirstAsync(s => s.Id == decision.StateSnapshotId, cancellationToken);

        if (DateTimeOffset.UtcNow - state.CapturedAtUtc > TimeSpan.FromMinutes(policy.StaleDataMaxMinutes))
        {
            throw new ProductionManagerException(ProductionManagerFailureCodes.StaleData, "Karar anındaki durum güncel değil; yeniden doğrulama gerekli.");
        }

        var optionId = request.SelectedOptionId ?? decision.SelectedOptionId
            ?? throw new ProductionManagerException(ProductionManagerFailureCodes.ValidationFailed, "Seçenek gerekli.");
        var option = decision.Options.First(o => o.Id == optionId);

        if (!AutonomyGuardEngine.CanExecuteAutonomously(policy, decision, option, state) &&
            decision.Status != ProductionManagerDecisionStatuses.Approved)
        {
            throw new ProductionManagerException(ProductionManagerFailureCodes.AutonomyDenied, "Onay veya yetki gerekli.");
        }

        var idempotencyKey = request.IdempotencyKey ?? $"{decision.Id:N}";
        if (await dbContext.FactoryDecisions.AnyAsync(
                d => d.TenantId == decision.TenantId && d.ExecutionIdempotencyKey == idempotencyKey, cancellationToken))
        {
            throw new ProductionManagerException(ProductionManagerFailureCodes.AlreadyExecuted, "İdempotent anahtar kullanıldı.");
        }

        var routeResult = await factoryOrchestration.RouteEventAsync(
            new FactoryDomainEvent(
                MapDecisionToEvent(decision.DecisionType),
                decision.Title,
                decision.ProblemSummary,
                "high",
                ProductionManagerDataClassifications.Observed,
                decision.FacilityId,
                "production-manager",
                decision.Id.ToString(),
                "IM-07",
                "PO-1842",
                decision.DecisionType.Contains("mold") ? "M-014" : null,
                decision.DecisionType.Contains("material") ? "PP-HF" : null,
                """{"impactedDomains":["production","planning"]}"""),
            subjectId,
            cancellationToken);

        decision.FactoryCaseId = routeResult.Case.Id;
        decision.ExecutionIdempotencyKey = idempotencyKey;
        decision.ExecutedAtUtc = DateTimeOffset.UtcNow;
        decision.Status = ProductionManagerDecisionStatuses.Executing;
        decision.UpdatedAtUtc = DateTimeOffset.UtcNow;
        decision.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return decision;
    }

    private async Task<FactoryDecision> VerifyDecisionCoreAsync(
        Guid decisionId, VerifyDecisionOutcomeRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var decision = await LoadDecisionForWriteAsync(decisionId, request.ConcurrencyToken, cancellationToken);
        var option = decision.Options.First(o => o.Id == decision.SelectedOptionId);

        var outcome = ExpectedVsActualEngine.Compare(decision, request.ActualOutcomeJson);
        dbContext.FactoryDecisionOutcomes.Add(outcome);

        var state = await dbContext.FactoryStateSnapshots.FirstAsync(s => s.Id == decision.StateSnapshotId, cancellationToken);
        dbContext.FactoryDecisionMemories.Add(
            DecisionMemoryEngine.CreateFromOutcome(decision, option, outcome, state));

        decision.ActualOutcomeJson = request.ActualOutcomeJson;
        decision.EffectivenessScore = outcome.EffectivenessScore;
        decision.DecisionResult = outcome.Result;
        decision.VerifiedAtUtc = DateTimeOffset.UtcNow;
        decision.Status = outcome.Result == "failed"
            ? ProductionManagerDecisionStatuses.Failed
            : ProductionManagerDecisionStatuses.Verified;
        decision.UpdatedAtUtc = DateTimeOffset.UtcNow;
        decision.ConcurrencyToken++;

        await dbContext.SaveChangesAsync(cancellationToken);
        return decision;
    }

    private async Task<FactoryDecision> LoadDecisionForWriteAsync(
        Guid decisionId, long concurrencyToken, CancellationToken cancellationToken)
    {
        var decision = await dbContext.FactoryDecisions
            .Include(d => d.Options)
            .FirstOrDefaultAsync(d => d.Id == decisionId, cancellationToken)
            ?? throw new ProductionManagerException(ProductionManagerFailureCodes.NotFound, "Karar bulunamadı.");

        if (decision.ConcurrencyToken != concurrencyToken)
        {
            throw new ProductionManagerException(ProductionManagerFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");
        }

        return decision;
    }

    private static string MapDecisionToEvent(string decisionType) => decisionType switch
    {
        ProductionManagerDecisionTypes.MachineRisk => "maintenance.machine-fault",
        ProductionManagerDecisionTypes.MaterialDepletion => "material.shortage",
        ProductionManagerDecisionTypes.QualityEscalation => "quality.ncr",
        ProductionManagerDecisionTypes.MoldRisk => "mold.lifecycle",
        ProductionManagerDecisionTypes.CapacityOverload => "production.recovery",
        _ => "production.recovery",
    };
}
