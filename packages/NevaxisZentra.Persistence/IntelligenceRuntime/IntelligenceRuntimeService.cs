using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NevaxisZentra.Persistence.ModelGateway;
using NevaxisZentra.Persistence.Tenancy;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.IntelligenceRuntime;

public interface IIntelligenceRuntimeService
{
    Task<IntelligenceRouteResult> RouteIntelligenceAsync(
        Guid facilityId,
        Guid tenantId,
        IntelligenceRouteRequest request,
        CancellationToken cancellationToken = default);

    Task<IntelligenceRuntimeControlCenter> GetControlCenterAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<NevaxisDatasetBuildResult> BuildDatasetVersionAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<NevaxisModelRegistrySummary> RegisterModelAsync(
        Guid facilityId,
        Guid tenantId,
        RegisterNevaxisModelRequest request,
        CancellationToken cancellationToken = default);

    Task<NevaxisModelEvaluationSummary> EvaluateModelAsync(
        Guid facilityId,
        Guid tenantId,
        StartModelEvaluationRequest request,
        CancellationToken cancellationToken = default);

    Task<NevaxisModelEvaluationSummary> RunShadowAsync(
        Guid facilityId,
        Guid tenantId,
        StartModelShadowRequest request,
        CancellationToken cancellationToken = default);

    Task<NevaxisModelPromotionSimulationResult> PromoteModelAsync(
        Guid facilityId,
        Guid tenantId,
        PromoteModelRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantIntelligenceLearningPolicySnapshot> UpsertLearningPolicyAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertTenantIntelligenceLearningPolicyRequest request,
        CancellationToken cancellationToken = default);

    Task<IntelligenceGoldenRoutingResult> RunGoldenRoutingDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<IntelligenceGoldenDatasetResult> RunGoldenDatasetDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NevaxisModelPromotionSimulationResult>> RunGoldenPromotionSimulationAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<OpenAiLiveValidationResult> ValidateOpenAiLiveAsync(
        CancellationToken cancellationToken = default);

    Task RecordVerifiedOutcomeCostAsync(
        Guid facilityId,
        Guid tenantId,
        Guid? digitalWorkCaseId,
        Guid? optimizationRunId,
        Guid? scenarioRunId,
        string outcome,
        CancellationToken cancellationToken = default);
}

public sealed class IntelligenceRuntimeService(
    NevaxisZentraDbContext db,
    TenantDatabaseSession tenantDatabaseSession,
    IModelGatewayService modelGatewayService,
    OpenAiFrontierModelClient openAiClient) : IIntelligenceRuntimeService
{
    public Task<IntelligenceRouteResult> RouteIntelligenceAsync(
        Guid facilityId,
        Guid tenantId,
        IntelligenceRouteRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RouteIntelligenceCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<IntelligenceRuntimeControlCenter> GetControlCenterAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetControlCenterCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<NevaxisDatasetBuildResult> BuildDatasetVersionAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => BuildDatasetVersionCoreAsync(facilityId, tenantId, ct),
            cancellationToken);

    public Task<NevaxisModelRegistrySummary> RegisterModelAsync(
        Guid facilityId,
        Guid tenantId,
        RegisterNevaxisModelRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RegisterModelCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<NevaxisModelEvaluationSummary> EvaluateModelAsync(
        Guid facilityId,
        Guid tenantId,
        StartModelEvaluationRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => EvaluateModelCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<NevaxisModelEvaluationSummary> RunShadowAsync(
        Guid facilityId,
        Guid tenantId,
        StartModelShadowRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunShadowCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<NevaxisModelPromotionSimulationResult> PromoteModelAsync(
        Guid facilityId,
        Guid tenantId,
        PromoteModelRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => PromoteModelCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<TenantIntelligenceLearningPolicySnapshot> UpsertLearningPolicyAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertTenantIntelligenceLearningPolicyRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => UpsertLearningPolicyCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<IntelligenceGoldenRoutingResult> RunGoldenRoutingDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenRoutingDemoCoreAsync(facilityId, tenantId, ct),
            cancellationToken);

    public Task<IntelligenceGoldenDatasetResult> RunGoldenDatasetDemoAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenDatasetDemoCoreAsync(facilityId, tenantId, ct),
            cancellationToken);

    public Task<IReadOnlyList<NevaxisModelPromotionSimulationResult>> RunGoldenPromotionSimulationAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync<IReadOnlyList<NevaxisModelPromotionSimulationResult>>(
            ct => RunGoldenPromotionSimulationCoreAsync(facilityId, tenantId, ct),
            cancellationToken);

    public Task<OpenAiLiveValidationResult> ValidateOpenAiLiveAsync(
        CancellationToken cancellationToken = default) =>
        openAiClient.ValidateLiveAsync(cancellationToken);

    public Task RecordVerifiedOutcomeCostAsync(
        Guid facilityId,
        Guid tenantId,
        Guid? digitalWorkCaseId,
        Guid? optimizationRunId,
        Guid? scenarioRunId,
        string outcome,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await RecordVerifiedOutcomeCostCoreAsync(
                    facilityId, tenantId, digitalWorkCaseId, optimizationRunId, scenarioRunId, outcome, ct);
                return true;
            },
            cancellationToken);

    private async Task<IntelligenceRouteResult> RouteIntelligenceCoreAsync(
        Guid facilityId,
        Guid tenantId,
        IntelligenceRouteRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var task = IntelligenceTaskTaxonomy.Resolve(request.TaskType, request.RequestClassification);
        var budgetPolicy = await LoadBudgetPolicyAsync(facilityId, cancellationToken);
        var budget = budgetPolicy is null
            ? null
            : new IntelligenceBudgetPolicy(
                budgetPolicy.MaxModelCalls,
                budgetPolicy.MaxContextTokens,
                budgetPolicy.MaxOutputTokens,
                budgetPolicy.MaxEstimatedCost,
                budgetPolicy.MaxRetries,
                budgetPolicy.MaxSimulationCount);

        var dayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var modelCallsToday = await db.IntelligenceRuntimeRouteRecords.AsNoTracking()
            .CountAsync(
                r => r.FacilityId == facilityId && r.ModelInvoked && r.CreatedAtUtc >= dayStart,
                cancellationToken);

        var productionModel = await db.NevaxisModelRegistryRecords
            .Where(m => m.FacilityId == facilityId && m.Status == NevaxisModelStatuses.Production)
            .OrderByDescending(m => m.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var gatewayBudget = await LoadGatewayBudgetAsync(facilityId, request.DigitalWorkCaseId, cancellationToken);
        var gatewayRequest = BuildGatewayRequest(tenantId, facilityId, request, task);
        var gatewayRouting = ModelRouterEngine.Route(
            gatewayRequest with { CostBudgetRemaining = gatewayBudget.DailyRemaining },
            gatewayBudget);

        var routing = IntelligenceRoutingPolicyEngine.Decide(
            request,
            task,
            gatewayRouting,
            productionModel,
            budget,
            modelCallsToday);

        var failureState = routing.RouteType == IntelligenceRouteTypes.Deterministic
            && budget is not null
            && modelCallsToday >= budget.MaxModelCalls
            ? IntelligenceRuntimeFailureStates.BudgetExceeded
            : null;

        ModelGatewayResponse? gatewayResponse = null;
        if (routing.ModelInvoked)
        {
            gatewayResponse = await modelGatewayService.InvokeAsync(
                facilityId,
                tenantId,
                gatewayRequest,
                cancellationToken);
            if (!gatewayResponse.Succeeded)
            {
                failureState = gatewayResponse.FailureState ?? IntelligenceRuntimeFailureStates.SchemaFailure;
            }
        }

        var priceCatalog = await db.ProviderPriceCatalogRecords.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        var price = ProviderPriceCatalogEngine.Resolve(priceCatalog, routing.ProviderId, routing.ModelName);
        var inputTokens = gatewayResponse?.Routing is not null ? 0 : 0;
        var outputTokens = 0;
        var estimatedCost = gatewayResponse?.EstimatedCost ?? 0m;
        if (gatewayResponse is not null && estimatedCost == 0m && routing.ModelInvoked)
        {
            estimatedCost = ProviderPriceCatalogEngine.EstimateCost(price, inputTokens, outputTokens);
        }

        stopwatch.Stop();
        var routeId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.IntelligenceRuntimeRouteRecords.Add(new IntelligenceRuntimeRouteRecord
        {
            Id = routeId,
            TenantId = tenantId,
            FacilityId = facilityId,
            WorkerId = request.WorkerId,
            DigitalWorkCaseId = request.DigitalWorkCaseId,
            OptimizationRunId = request.OptimizationRunId,
            ScenarioRunId = request.ScenarioRunId,
            ContextSnapshotId = request.ContextSnapshotId,
            ModelGatewayAuditId = gatewayResponse?.AuditRecordId is not null && Guid.TryParse(gatewayResponse.AuditRecordId, out var auditId)
                ? auditId
                : null,
            TaskType = request.TaskType,
            RequestClassification = request.RequestClassification,
            RouteType = routing.RouteType,
            IntelligencePath = routing.IntelligencePath,
            ProviderId = routing.ProviderId,
            ModelName = routing.ModelName,
            RoutingReason = routing.RoutingReason,
            WhyModelCalledOrSkipped = routing.WhyModelCalledOrSkipped,
            ModelInvoked = routing.ModelInvoked,
            UsedFallback = routing.UsedFallback,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            EstimatedCost = estimatedCost,
            PriceVersion = price.PriceVersion,
            LatencyMs = stopwatch.ElapsedMilliseconds,
            CreatedAtUtc = now,
        });

        await db.SaveChangesAsync(cancellationToken);

        return new IntelligenceRouteResult(
            routing,
            failureState,
            failureState == IntelligenceRuntimeFailureStates.BudgetExceeded,
            stopwatch.ElapsedMilliseconds);
    }

    private async Task<IntelligenceRuntimeControlCenter> GetControlCenterCoreAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var routes = await db.IntelligenceRuntimeRouteRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        var metrics = IntelligenceDependencyMetricsEngine.Compute(routes);
        var recentRoutes = routes
            .Take(20)
            .Select(r => new IntelligenceRouteSummary(
                r.Id,
                r.TaskType,
                r.RouteType,
                r.RoutingReason,
                r.WhyModelCalledOrSkipped,
                r.ModelInvoked,
                r.EstimatedCost,
                r.LatencyMs,
                r.CreatedAtUtc))
            .ToList();

        var models = await db.NevaxisModelRegistryRecords.AsNoTracking()
            .Where(m => m.FacilityId == facilityId)
            .OrderByDescending(m => m.UpdatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        var modelSummaries = models
            .Select(MapModelSummary)
            .ToList();

        var datasets = await db.NevaxisVerifiedManufacturingDatasetRecords.AsNoTracking()
            .Where(d => d.FacilityId == facilityId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(10)
            .Select(d => new NevaxisDatasetVersionSummary(
                d.Id,
                d.VersionCode,
                d.SchemaVersion,
                d.ReadinessState,
                d.IncludedCaseCount,
                d.ExcludedCaseCount,
                d.TenantPolicy,
                d.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var evaluations = await db.NevaxisModelEvaluationRunRecords.AsNoTracking()
            .Where(e => e.FacilityId == facilityId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(20)
            .Join(
                db.NevaxisModelRegistryRecords.AsNoTracking(),
                evaluation => evaluation.ModelId,
                model => model.Id,
                (evaluation, model) => new NevaxisModelEvaluationSummary(
                    evaluation.Id,
                    evaluation.ModelId,
                    model.ModelCode,
                    evaluation.EvaluationMode,
                    evaluation.Outcome,
                    evaluation.StructuredOutputValidityRate,
                    evaluation.SafetyPassRate,
                    evaluation.HallucinatedEntityRate,
                    evaluation.EstimatedCost,
                    evaluation.LatencyMs,
                    evaluation.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var verifiedCosts = await db.VerifiedOutcomeIntelligenceCostRecords.AsNoTracking()
            .Where(v => v.FacilityId == facilityId)
            .OrderByDescending(v => v.UpdatedAtUtc)
            .Take(20)
            .Select(v => new VerifiedOutcomeCostSnapshot(
                v.DigitalWorkCaseId,
                v.OptimizationRunId,
                v.ScenarioRunId,
                v.Outcome,
                v.TotalIntelligenceCost,
                v.ModelCallCount,
                v.DeterministicRouteCount,
                v.CostPerVerifiedOutcome,
                v.VerifiedAtUtc))
            .ToListAsync(cancellationToken);

        return new IntelligenceRuntimeControlCenter(
            metrics,
            recentRoutes,
            modelSummaries,
            datasets,
            evaluations,
            verifiedCosts,
            DateTimeOffset.UtcNow);
    }

    private async Task<NevaxisDatasetBuildResult> BuildDatasetVersionCoreAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var policy = await EnsureLearningPolicyAsync(facilityId, tenantId, cancellationToken);
        var candidates = await db.VerifiedTrainingDatasetCandidateRecords.AsNoTracking()
            .Where(c => c.FacilityId == facilityId)
            .ToListAsync(cancellationToken);

        var improvements = await db.VerifiedImprovementRecords.AsNoTracking()
            .Where(v => v.FacilityId == facilityId)
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        var derived = new List<NevaxisVerifiedDatasetEntry>();
        var excluded = 0;

        foreach (var candidate in candidates)
        {
            if (!improvements.TryGetValue(candidate.VerifiedImprovementId, out var improvement))
            {
                excluded++;
                continue;
            }

            var evidenceComplete = !string.IsNullOrWhiteSpace(candidate.RecordJson) && candidate.RecordJson != "{}";
            var (eligible, reason, negativeLearning) = NevaxisVerifiedDatasetEngine.EvaluateEligibility(
                improvement.VerificationOutcome,
                evidenceComplete,
                candidate.DigitalWorkCaseId != Guid.Empty,
                !string.IsNullOrWhiteSpace(candidate.VerificationLevel),
                unresolvedConflict: false,
                observationCompleted: improvement.ObservationEndUtc.HasValue,
                qualityGuardPassed: improvement.QualityGuardPassed,
                safetyGuardPassed: improvement.SafetyGuardPassed,
                tenantPolicy: policy.LearningPolicy);

            if (!eligible)
            {
                excluded++;
                continue;
            }

            var entry = new NevaxisVerifiedDatasetEntry(
                candidate.DigitalWorkCaseId,
                candidate.VerifiedImprovementId,
                improvement.InterventionSummary,
                candidate.RecordJson,
                "constraint-summary",
                "root-cause",
                "selected-decision",
                improvement.InterventionSummary,
                improvement.VerificationOutcome,
                reason,
                candidate.VerificationLevel,
                "1.0",
                negativeLearning);

            derived.Add(NevaxisVerifiedDatasetEngine.SanitizeEntry(entry, "1.0"));
        }

        var verifiedCount = derived.Count(x => !x.NegativeLearning);
        var negativeCount = derived.Count(x => x.NegativeLearning);
        var readiness = NevaxisVerifiedDatasetEngine.EvaluateReadiness(verifiedCount, negativeCount, 2);
        var versionId = Guid.NewGuid();
        var versionCode = $"DS-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        var now = DateTimeOffset.UtcNow;
        var manifest = NevaxisVerifiedDatasetEngine.BuildManifest(
            versionId,
            versionCode,
            policy.LearningPolicy,
            "1.0",
            derived.Select(x => x.SourceCaseId).ToList());

        db.NevaxisVerifiedManufacturingDatasetRecords.Add(new NevaxisVerifiedManufacturingDatasetRecord
        {
            Id = versionId,
            TenantId = tenantId,
            FacilityId = facilityId,
            VersionCode = versionCode,
            ReadinessState = readiness,
            TenantPolicy = policy.LearningPolicy,
            IncludedCaseCount = derived.Count,
            ExcludedCaseCount = excluded,
            ManifestJson = manifest,
            QualityMetricsJson = JsonSerializer.Serialize(new { verifiedCount, negativeCount }),
            TaskDistributionJson = JsonSerializer.Serialize(NevaxisVerifiedDatasetEngine.BuildTaskDistribution(derived)),
            CreatedAtUtc = now,
        });

        foreach (var entry in derived)
        {
            db.NevaxisVerifiedManufacturingDatasetEntryRecords.Add(new NevaxisVerifiedManufacturingDatasetEntryRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                DatasetVersionId = versionId,
                SourceCaseId = entry.SourceCaseId,
                VerifiedImprovementId = entry.VerifiedImprovementId,
                IsNegativeLearning = entry.NegativeLearning,
                RecordJson = JsonSerializer.Serialize(entry),
                ProvenanceJson = JsonSerializer.Serialize(new { entry.Provenance, entry.SourceCaseId }),
                CreatedAtUtc = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return new NevaxisDatasetBuildResult(
            versionId,
            versionCode,
            readiness,
            derived.Count + excluded,
            excluded,
            derived.Count,
            $"Built dataset {versionCode} with readiness {readiness}.");
    }

    private async Task<NevaxisModelRegistrySummary> RegisterModelCoreAsync(
        Guid facilityId,
        Guid tenantId,
        RegisterNevaxisModelRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new NevaxisModelRegistryRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ModelCode = request.ModelCode,
            Version = request.Version,
            BaseModel = request.BaseModel,
            Status = NevaxisModelStatuses.Candidate,
            EvaluationStatus = NevaxisModelStatuses.Candidate,
            SafetyStatus = "PENDING",
            DeploymentType = request.DeploymentType,
            ContextLimit = request.ContextLimit,
            TaskCapabilitiesJson = JsonSerializer.Serialize(request.TaskCapabilities),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.NevaxisModelRegistryRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return MapModelSummary(record);
    }

    private async Task<NevaxisModelEvaluationSummary> EvaluateModelCoreAsync(
        Guid facilityId,
        Guid tenantId,
        StartModelEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        var model = await db.NevaxisModelRegistryRecords
            .FirstOrDefaultAsync(m => m.FacilityId == facilityId && m.Id == request.ModelId, cancellationToken)
            ?? throw new IntelligenceRuntimeException(IntelligenceRuntimeFailureStates.EvaluationFailed, "Model not found.");

        model.Status = NevaxisModelStatuses.Evaluating;
        model.EvaluationStatus = NevaxisModelStatuses.Evaluating;
        model.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var evaluation = NevaxisModelLifecycleEngine.EvaluateCandidate(model, request.IncludeSafetyBenchmark);
        evaluation.TenantId = tenantId;
        evaluation.FacilityId = facilityId;
        db.NevaxisModelEvaluationRunRecords.Add(evaluation);

        if (evaluation.Outcome != "PASS")
        {
            model.Status = NevaxisModelStatuses.Rejected;
            model.EvaluationStatus = IntelligenceRuntimeFailureStates.EvaluationFailed;
            model.SafetyStatus = evaluation.SafetyPassRate < 0.95m ? "FAILED" : "PASSED";
        }
        else
        {
            model.EvaluationStatus = "PASS";
            model.SafetyStatus = "PASSED";
        }

        model.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new NevaxisModelEvaluationSummary(
            evaluation.Id,
            model.Id,
            model.ModelCode,
            evaluation.EvaluationMode,
            evaluation.Outcome,
            evaluation.StructuredOutputValidityRate,
            evaluation.SafetyPassRate,
            evaluation.HallucinatedEntityRate,
            evaluation.EstimatedCost,
            evaluation.LatencyMs,
            evaluation.CreatedAtUtc);
    }

    private async Task<NevaxisModelEvaluationSummary> RunShadowCoreAsync(
        Guid facilityId,
        Guid tenantId,
        StartModelShadowRequest request,
        CancellationToken cancellationToken)
    {
        var model = await db.NevaxisModelRegistryRecords
            .FirstOrDefaultAsync(m => m.FacilityId == facilityId && m.Id == request.ModelId, cancellationToken)
            ?? throw new IntelligenceRuntimeException(IntelligenceRuntimeFailureStates.EvaluationFailed, "Model not found.");

        model.Status = NevaxisModelStatuses.Shadow;
        model.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var shadow = NevaxisModelLifecycleEngine.RunShadow(model, request.TaskType);
        shadow.TenantId = tenantId;
        shadow.FacilityId = facilityId;
        db.NevaxisModelEvaluationRunRecords.Add(shadow);
        await db.SaveChangesAsync(cancellationToken);

        return new NevaxisModelEvaluationSummary(
            shadow.Id,
            model.Id,
            model.ModelCode,
            shadow.EvaluationMode,
            shadow.Outcome,
            shadow.StructuredOutputValidityRate,
            shadow.SafetyPassRate,
            shadow.HallucinatedEntityRate,
            shadow.EstimatedCost,
            shadow.LatencyMs,
            shadow.CreatedAtUtc);
    }

    private async Task<NevaxisModelPromotionSimulationResult> PromoteModelCoreAsync(
        Guid facilityId,
        Guid tenantId,
        PromoteModelRequest request,
        CancellationToken cancellationToken)
    {
        var model = await db.NevaxisModelRegistryRecords
            .FirstOrDefaultAsync(m => m.FacilityId == facilityId && m.Id == request.ModelId, cancellationToken)
            ?? throw new IntelligenceRuntimeException(IntelligenceRuntimeFailureStates.PromotionBlocked, "Model not found.");

        var latestEvaluation = await db.NevaxisModelEvaluationRunRecords.AsNoTracking()
            .Where(e => e.FacilityId == facilityId && e.ModelId == model.Id)
            .OrderByDescending(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var (status, outcome, rollback) = NevaxisModelLifecycleEngine.PromoteOrRollback(
            model,
            request.TargetStatus,
            latestEvaluation);

        model.Status = status;
        model.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (status == NevaxisModelStatuses.Production)
        {
            model.PromotedAtUtc = DateTimeOffset.UtcNow;
        }

        if (rollback)
        {
            model.RetiredAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new NevaxisModelPromotionSimulationResult(
            model.Id,
            status,
            outcome,
            rollback,
            rollback
                ? "Automatic rollback triggered due to regression threshold."
                : outcome);
    }

    private async Task<TenantIntelligenceLearningPolicySnapshot> UpsertLearningPolicyCoreAsync(
        Guid facilityId,
        Guid tenantId,
        UpsertTenantIntelligenceLearningPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var policy = await db.TenantIntelligenceLearningPolicyRecords
            .FirstOrDefaultAsync(p => p.FacilityId == facilityId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (policy is null)
        {
            policy = new TenantIntelligenceLearningPolicyRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                CreatedAtUtc = now,
            };
            db.TenantIntelligenceLearningPolicyRecords.Add(policy);
        }

        policy.LearningPolicy = request.LearningPolicy;
        policy.AllowExport = request.AllowExport;
        policy.AllowTraining = request.AllowTraining;
        policy.AllowShadow = request.AllowShadow;
        policy.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);

        return new TenantIntelligenceLearningPolicySnapshot(
            policy.Id,
            policy.LearningPolicy,
            policy.AllowExport,
            policy.AllowTraining,
            policy.AllowShadow,
            policy.UpdatedAtUtc);
    }

    private async Task<IntelligenceGoldenRoutingResult> RunGoldenRoutingDemoCoreAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var queries = new (string Label, string TaskType, string ExpectedRoute, string? PreferredPath)[]
        {
            ("QUERY A", IntelligenceTaskTypes.StateLookup, IntelligenceRouteTypes.Deterministic, ModelGatewayIntelligencePaths.DeterministicEngine),
            ("QUERY B", IntelligenceTaskTypes.TrendAnalysis, IntelligenceRouteTypes.Analytics, null),
            ("QUERY C", IntelligenceTaskTypes.CaseSimilarity, IntelligenceRouteTypes.Retrieval, null),
            ("QUERY D", IntelligenceTaskTypes.RecoveryPlanDraft, IntelligenceRouteTypes.Simulation, ModelGatewayIntelligencePaths.DeterministicEngine),
            ("QUERY E", IntelligenceTaskTypes.CrossDomainDiagnosis, IntelligenceRouteTypes.FrontierModel, null),
        };

        var results = new List<IntelligenceGoldenRoutingQueryResult>();
        foreach (var query in queries)
        {
            var routeResult = await RouteIntelligenceCoreAsync(
                facilityId,
                tenantId,
                new IntelligenceRouteRequest(
                    query.TaskType,
                    IntelligenceRoutingPolicyEngine.MapGatewayRequestClassification(query.TaskType),
                    query.PreferredPath,
                    query.TaskType == IntelligenceTaskTypes.CrossDomainDiagnosis ? 2 : 1,
                    query.TaskType == IntelligenceTaskTypes.CrossDomainDiagnosis,
                    "TRUSTED",
                    10m,
                    null,
                    null,
                    null,
                    null,
                    null),
                cancellationToken);

            var expected = query.ExpectedRoute;
            if (query.Label == "QUERY E" && !openAiClient.IsConfigured)
            {
                expected = IntelligenceRouteTypes.Deterministic;
            }

            var passed = string.Equals(routeResult.Routing.RouteType, expected, StringComparison.OrdinalIgnoreCase)
                || (query.Label == "QUERY D" && routeResult.Routing.RouteType is IntelligenceRouteTypes.Solver or IntelligenceRouteTypes.Simulation or IntelligenceRouteTypes.Deterministic);

            results.Add(new IntelligenceGoldenRoutingQueryResult(
                query.Label,
                query.TaskType,
                query.ExpectedRoute,
                routeResult.Routing.RouteType,
                routeResult.Routing.ModelInvoked ? 1 : 0,
                passed));
        }

        var message = openAiClient.IsConfigured
            ? "Golden routing completed with live frontier configuration available."
            : "Golden routing completed; frontier live validation CONFIGURATION PENDING.";

        return new IntelligenceGoldenRoutingResult(results, message);
    }

    private async Task<IntelligenceGoldenDatasetResult> RunGoldenDatasetDemoCoreAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await UpsertLearningPolicyCoreAsync(
            facilityId,
            tenantId,
            new UpsertTenantIntelligenceLearningPolicyRequest(
                IntelligenceTenantLearningPolicies.TenantOnlyLearning,
                true,
                true,
                true),
            cancellationToken);

        var dataset = await BuildDatasetVersionCoreAsync(facilityId, tenantId, cancellationToken);
        var model = await RegisterModelCoreAsync(
            facilityId,
            tenantId,
            new RegisterNevaxisModelRequest(
                "nevaxis-golden-candidate",
                "1.0.0",
                "base-model-abstraction",
                [IntelligenceTaskTypes.Classification, IntelligenceTaskTypes.Summarization],
                "candidate",
                8192),
            cancellationToken);

        var evaluation = await EvaluateModelCoreAsync(
            facilityId,
            tenantId,
            new StartModelEvaluationRequest(model.ModelId, "candidate-vs-production", true),
            cancellationToken);

        var shadow = await RunShadowCoreAsync(
            facilityId,
            tenantId,
            new StartModelShadowRequest(model.ModelId, IntelligenceTaskTypes.Classification),
            cancellationToken);

        var promotion = await PromoteModelCoreAsync(
            facilityId,
            tenantId,
            new PromoteModelRequest(model.ModelId, NevaxisModelStatuses.Production, "golden-simulation"),
            cancellationToken);

        return new IntelligenceGoldenDatasetResult(
            dataset.DatasetVersionId,
            evaluation.EvaluationRunId,
            shadow.EvaluationRunId,
            promotion.Outcome,
            "Golden dataset and promotion simulation completed without real GPU training.");
    }

    private async Task<IReadOnlyList<NevaxisModelPromotionSimulationResult>> RunGoldenPromotionSimulationCoreAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var passing = await RegisterModelCoreAsync(
            facilityId,
            tenantId,
            new RegisterNevaxisModelRequest(
                "nevaxis-golden-pass",
                "1.0.0",
                "base-model-abstraction",
                [IntelligenceTaskTypes.Classification],
                "candidate",
                4096),
            cancellationToken);

        await EvaluateModelCoreAsync(
            facilityId,
            tenantId,
            new StartModelEvaluationRequest(passing.ModelId, "candidate-vs-production", true),
            cancellationToken);
        await RunShadowCoreAsync(
            facilityId,
            tenantId,
            new StartModelShadowRequest(passing.ModelId, IntelligenceTaskTypes.Classification),
            cancellationToken);
        var limited = await PromoteModelCoreAsync(
            facilityId,
            tenantId,
            new PromoteModelRequest(passing.ModelId, NevaxisModelStatuses.Limited, null),
            cancellationToken);
        var production = await PromoteModelCoreAsync(
            facilityId,
            tenantId,
            new PromoteModelRequest(passing.ModelId, NevaxisModelStatuses.Production, null),
            cancellationToken);

        var failing = await RegisterModelCoreAsync(
            facilityId,
            tenantId,
            new RegisterNevaxisModelRequest(
                "nevaxis-unsafe-candidate",
                "0.1.0",
                "base-model-abstraction",
                [IntelligenceTaskTypes.RootCauseCandidate],
                "candidate",
                4096),
            cancellationToken);

        await EvaluateModelCoreAsync(
            facilityId,
            tenantId,
            new StartModelEvaluationRequest(failing.ModelId, "candidate-vs-production", true),
            cancellationToken);

        var rollbackModel = await db.NevaxisModelRegistryRecords
            .FirstAsync(m => m.Id == production.ModelId, cancellationToken);
        rollbackModel.Status = NevaxisModelStatuses.Production;
        var rollbackEval = NevaxisModelLifecycleEngine.EvaluateCandidate(rollbackModel, true);
        rollbackEval.TenantId = tenantId;
        rollbackEval.FacilityId = facilityId;
        rollbackEval.ModelId = rollbackModel.Id;
        rollbackEval.HallucinatedEntityRate = 0.35m;
        rollbackEval.Outcome = IntelligenceRuntimeFailureStates.EvaluationFailed;
        db.NevaxisModelEvaluationRunRecords.Add(rollbackEval);
        var rollback = await PromoteModelCoreAsync(
            facilityId,
            tenantId,
            new PromoteModelRequest(production.ModelId, NevaxisModelStatuses.Retired, "regression-threshold"),
            cancellationToken);

        return [limited, production, rollback];
    }

    private async Task RecordVerifiedOutcomeCostCoreAsync(
        Guid facilityId,
        Guid tenantId,
        Guid? digitalWorkCaseId,
        Guid? optimizationRunId,
        Guid? scenarioRunId,
        string outcome,
        CancellationToken cancellationToken)
    {
        var routes = await db.IntelligenceRuntimeRouteRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId
                && (digitalWorkCaseId == null || r.DigitalWorkCaseId == digitalWorkCaseId)
                && (optimizationRunId == null || r.OptimizationRunId == optimizationRunId)
                && (scenarioRunId == null || r.ScenarioRunId == scenarioRunId))
            .ToListAsync(cancellationToken);

        var totalCost = routes.Sum(r => r.EstimatedCost);
        var modelCalls = routes.Count(r => r.ModelInvoked);
        var deterministicRoutes = routes.Count(r => r.RouteType == IntelligenceRouteTypes.Deterministic);
        var costPerOutcome = outcome == ThroughputOptimizerVerificationOutcomes.VerifiedImprovement && totalCost > 0
            ? totalCost
            : (decimal?)null;

        var existing = await db.VerifiedOutcomeIntelligenceCostRecords
            .FirstOrDefaultAsync(
                v => v.FacilityId == facilityId
                    && v.DigitalWorkCaseId == digitalWorkCaseId
                    && v.OptimizationRunId == optimizationRunId
                    && v.ScenarioRunId == scenarioRunId,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            db.VerifiedOutcomeIntelligenceCostRecords.Add(new VerifiedOutcomeIntelligenceCostRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                DigitalWorkCaseId = digitalWorkCaseId,
                OptimizationRunId = optimizationRunId,
                ScenarioRunId = scenarioRunId,
                Outcome = outcome,
                TotalIntelligenceCost = totalCost,
                ModelCallCount = modelCalls,
                DeterministicRouteCount = deterministicRoutes,
                CostPerVerifiedOutcome = costPerOutcome,
                VerifiedAtUtc = outcome == ThroughputOptimizerVerificationOutcomes.VerifiedImprovement ? now : null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }
        else
        {
            existing.Outcome = outcome;
            existing.TotalIntelligenceCost = totalCost;
            existing.ModelCallCount = modelCalls;
            existing.DeterministicRouteCount = deterministicRoutes;
            existing.CostPerVerifiedOutcome = costPerOutcome;
            existing.VerifiedAtUtc = outcome == ThroughputOptimizerVerificationOutcomes.VerifiedImprovement ? now : existing.VerifiedAtUtc;
            existing.UpdatedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static ModelGatewayRequest BuildGatewayRequest(
        Guid tenantId,
        Guid facilityId,
        IntelligenceRouteRequest request,
        IntelligenceTaskDefinition task) =>
        new(
            tenantId,
            facilityId,
            request.WorkerId,
            request.DigitalWorkCaseId,
            IntelligenceRoutingPolicyEngine.MapGatewayRequestClassification(request.TaskType),
            request.TaskType,
            new ModelGatewayContextReference(
                request.ContextSnapshotId,
                "canonical-state-summary",
                request.DigitalWorkCaseId,
                "constraint-summary",
                task.PreferredRoute == IntelligenceRouteTypes.Simulation ? "simulation-available" : null,
                request.ContextQualityState),
            request.ActionRiskLevel,
            request.RequiresStructuredOutput || task.StructuredOutputRequired,
            request.PreferredPath,
            request.CostBudgetRemaining);

    private async Task<ModelCostBudgetState> LoadGatewayBudgetAsync(
        Guid facilityId,
        Guid? digitalWorkCaseId,
        CancellationToken cancellationToken)
    {
        var policy = await db.ModelGatewayBudgetPolicyRecords.AsNoTracking()
            .FirstOrDefaultAsync(p => p.FacilityId == facilityId && p.IsActive, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var dailySpend = await db.ModelGatewayCostUsageRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.WindowStartUtc >= dayStart)
            .SumAsync(r => r.EstimatedCost, cancellationToken);
        var monthlySpend = await db.ModelGatewayCostUsageRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.WindowStartUtc >= monthStart)
            .SumAsync(r => r.EstimatedCost, cancellationToken);
        var caseSpend = digitalWorkCaseId.HasValue
            ? await db.ModelGatewayCostUsageRecords.AsNoTracking()
                .Where(r => r.FacilityId == facilityId && r.DigitalWorkCaseId == digitalWorkCaseId)
                .SumAsync(r => r.EstimatedCost, cancellationToken)
            : 0m;

        return ModelCostGovernanceEngine.Evaluate(dailySpend, monthlySpend, caseSpend, policy);
    }

    private async Task<IntelligenceRuntimeBudgetPolicyRecord?> LoadBudgetPolicyAsync(
        Guid facilityId,
        CancellationToken cancellationToken) =>
        await db.IntelligenceRuntimeBudgetPolicyRecords
            .AsNoTracking()
            .Where(p => p.FacilityId == facilityId)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<TenantIntelligenceLearningPolicyRecord> EnsureLearningPolicyAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var policy = await db.TenantIntelligenceLearningPolicyRecords
            .FirstOrDefaultAsync(p => p.FacilityId == facilityId, cancellationToken);

        if (policy is not null)
        {
            return policy;
        }

        var now = DateTimeOffset.UtcNow;
        policy = new TenantIntelligenceLearningPolicyRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            LearningPolicy = IntelligenceTenantLearningPolicies.NoModelTraining,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.TenantIntelligenceLearningPolicyRecords.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private static NevaxisModelRegistrySummary MapModelSummary(NevaxisModelRegistryRecord model)
    {
        IReadOnlyList<string> capabilities;
        try
        {
            capabilities = JsonSerializer.Deserialize<List<string>>(model.TaskCapabilitiesJson) ?? [];
        }
        catch
        {
            capabilities = [];
        }

        return new NevaxisModelRegistrySummary(
            model.Id,
            model.ModelCode,
            model.Version,
            model.BaseModel,
            model.Status,
            model.EvaluationStatus,
            model.SafetyStatus,
            capabilities,
            model.TrainingDatasetVersionId?.ToString(),
            model.UpdatedAtUtc);
    }
}

public static class IntelligenceRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraIntelligenceRuntime(this IServiceCollection services)
    {
        services.AddHttpClient(nameof(OpenAiFrontierModelClient));
        services.AddScoped<OpenAiFrontierModelClient>();
        services.AddScoped<IIntelligenceRuntimeService, IntelligenceRuntimeService>();
        return services;
    }
}
