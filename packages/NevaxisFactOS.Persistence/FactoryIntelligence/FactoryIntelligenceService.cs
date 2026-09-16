using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryOrchestration;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;
using NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public interface IFactoryIntelligenceService
{
    Task<FactoryIntelligenceEvaluationResult> EvaluateAsync(
        FactoryIntelligenceEvaluationRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryIntelligenceOverview> GetOverviewAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FactoryIntelligenceRiskDetail> GetRiskDetailAsync(Guid riskId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryIntelligenceRiskCard>> GetEarlyWarningCardsAsync(
        Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryPredictionEvaluation>> GetRiskHistoryAsync(
        Guid facilityId, string? riskType, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryIntelligenceCalibrationSummary>> GetCalibrationAsync(
        Guid facilityId, CancellationToken cancellationToken);
    Task<FactoryPredictionEvaluation> EvaluateOutcomeAsync(
        Guid riskId, string outcome, bool interventionOccurred, string? notes, CancellationToken cancellationToken);
    Task<FactoryIntelligenceEvaluationResult> SimulateInj006StepAsync(
        int windowStep, string scenario, Guid facilityId, string subjectId, CancellationToken cancellationToken);
}

public sealed class FactoryIntelligenceService : IFactoryIntelligenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly IDigitalWorkforceRuntimeService runtime;
    private readonly IFactoryRecoveryIntelligenceService recoveryIntelligence;

    public FactoryIntelligenceService(
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        ITenantContextAccessor tenantContextAccessor,
        IDigitalWorkforceRuntimeService runtime,
        IFactoryRecoveryIntelligenceService recoveryIntelligence)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.tenantContextAccessor = tenantContextAccessor;
        this.runtime = runtime;
        this.recoveryIntelligence = recoveryIntelligence;
    }

    public Task<FactoryIntelligenceEvaluationResult> EvaluateAsync(
        FactoryIntelligenceEvaluationRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => EvaluateCoreAsync(request, subjectId, ct),
            cancellationToken);

    public Task<FactoryIntelligenceOverview> GetOverviewAsync(Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetOverviewCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<FactoryIntelligenceRiskDetail> GetRiskDetailAsync(Guid riskId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetRiskDetailCoreAsync(riskId, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryIntelligenceRiskCard>> GetEarlyWarningCardsAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetEarlyWarningCardsCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryPredictionEvaluation>> GetRiskHistoryAsync(
        Guid facilityId, string? riskType, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetRiskHistoryCoreAsync(facilityId, riskType, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryIntelligenceCalibrationSummary>> GetCalibrationAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetCalibrationCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<FactoryPredictionEvaluation> EvaluateOutcomeAsync(
        Guid riskId, string outcome, bool interventionOccurred, string? notes, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => EvaluateOutcomeCoreAsync(riskId, outcome, interventionOccurred, notes, ct),
            cancellationToken);

    public Task<FactoryIntelligenceEvaluationResult> SimulateInj006StepAsync(
        int windowStep, string scenario, Guid facilityId, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var tenantId = tenantContextAccessor.Current?.TenantId
                    ?? throw new FactoryIntelligenceException(
                        FactoryIntelligenceFailureCodes.ValidationFailed,
                        "Tenant context is required.");
                var signals = DevelopmentFactoryIntelligenceSimulator.BuildWindow(
                    windowStep, tenantId, facilityId, DateTimeOffset.UtcNow, scenario);
                return await EvaluateCoreAsync(
                    new FactoryIntelligenceEvaluationRequest(
                        tenantId,
                        facilityId,
                        DevelopmentFactoryIntelligenceSimulator.MachineCode,
                        DevelopmentFactoryIntelligenceSimulator.OrderReference,
                        DevelopmentFactoryIntelligenceSimulator.MaterialCode,
                        signals,
                        DateTimeOffset.UtcNow),
                    subjectId,
                    ct);
            },
            cancellationToken);

    private async Task<FactoryIntelligenceEvaluationResult> EvaluateCoreAsync(
        FactoryIntelligenceEvaluationRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var usableSignals = request.Signals
            .Where(s => s.DataQuality != FactoryIntelligenceDataQualityStates.Insufficient)
            .ToList();

        var persistedSignals = new List<FactoryPredictiveSignal>();
        var baselines = new List<FactoryBaselineResult>();

        foreach (var signal in usableSignals)
        {
            var recent = await dbContext.FactoryPredictiveSignals
                .Where(s =>
                    s.TenantId == request.TenantId
                    && s.FacilityId == request.FacilityId
                    && s.ResourceId == signal.ResourceId
                    && s.SignalType == signal.SignalType)
                .OrderByDescending(s => s.ObservedAtUtc)
                .Take(6)
                .Select(s => s.MeasuredValue)
                .ToListAsync(cancellationToken);

            var configuredReference = signal.SignalType == FactoryIntelligenceSignalTypes.CycleTimeDrift
                ? DevelopmentFactoryIntelligenceSimulator.CycleReferenceSec
                : (decimal?)null;

            var baseline = FactoryIntelligenceBaselineEngine.Compute(
                signal.SignalType,
                signal.MeasuredValue,
                configuredReference,
                recent,
                signal.DataQuality);
            baselines.Add(baseline);

            var entity = new FactoryPredictiveSignal
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                FacilityId = request.FacilityId,
                ResourceType = signal.ResourceType,
                ResourceId = signal.ResourceId,
                SignalType = signal.SignalType,
                ObservationWindow = "30m",
                MeasuredValue = signal.MeasuredValue,
                BaselineValue = baseline.Reference,
                DeviationPercent = baseline.DeviationPercent,
                Trend = baseline.Trend,
                DataQuality = baseline.DataQuality,
                SourceReferencesJson = JsonSerializer.Serialize(signal.SourceReferences ?? [], JsonOptions),
                EngineVersion = FactoryIntelligenceEngineVersions.RiskEngine,
                ObservedAtUtc = signal.ObservedAtUtc,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            dbContext.FactoryPredictiveSignals.Add(entity);
            persistedSignals.Add(entity);

            await UpsertBaselineEntityAsync(request, signal, baseline, cancellationToken);
        }

        var detectionInput = new FactoryRiskDetectionInput(
            request.TenantId,
            request.FacilityId,
            request.MachineCode,
            request.OrderReference,
            request.MaterialCode,
            usableSignals,
            baselines,
            request.EvaluatedAtUtc);

        var detections = FactoryIntelligenceRiskDetectionEngine.Detect(detectionInput);
        var risks = new List<FactoryPredictedRisk>();
        Guid? caseId = null;
        var caseCreatedOrUpdated = false;

        foreach (var detection in detections)
        {
            if (detection.DataQuality == FactoryIntelligenceDataQualityStates.Insufficient)
            {
                continue;
            }

            var idempotencyKey = $"{request.FacilityId:N}|{detection.CorrelationKey}";
            var existing = await dbContext.FactoryPredictedRisks
                .FirstOrDefaultAsync(r =>
                    r.TenantId == request.TenantId
                    && r.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (FactoryIntelligenceSuppressionEngine.ShouldSuppressDuplicate(
                    existing, detection, request.EvaluatedAtUtc))
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            if (existing is null)
            {
                existing = new FactoryPredictedRisk
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    FacilityId = request.FacilityId,
                    IdempotencyKey = idempotencyKey,
                    CorrelationKey = detection.CorrelationKey,
                    GeneratedAtUtc = now,
                    IsActive = true,
                };
                dbContext.FactoryPredictedRisks.Add(existing);
            }

            existing.RiskType = detection.RiskType;
            existing.RiskState = FactoryIntelligenceSuppressionEngine.ResolveEscalatedState(
                existing.RiskState, detection.RiskState);
            existing.AffectedOutcome = detection.AffectedOutcome;
            existing.ConfidenceBand = detection.ConfidenceBand;
            existing.Horizon = detection.Horizon;
            existing.Severity = detection.Severity;
            existing.TimeToImpactLabel = detection.TimeToImpactLabel;
            existing.ProjectedGapQuantity = detection.ProjectedGapQuantity;
            existing.OrderReference = request.OrderReference;
            existing.MachineCode = request.MachineCode;
            existing.MaterialCode = request.MaterialCode;
            existing.ContributingSignalsJson = JsonSerializer.Serialize(detection.ContributingSignals, JsonOptions);
            existing.ExplanationJson = JsonSerializer.Serialize(detection.Explanation, JsonOptions);
            existing.AssumptionsJson = JsonSerializer.Serialize(detection.Assumptions, JsonOptions);
            existing.DataQuality = detection.DataQuality;
            existing.DataClassification = FactoryIntelligenceDataClassifications.Predicted;
            existing.EngineVersion = FactoryIntelligenceEngineVersions.RiskEngine;
            existing.UpdatedAtUtc = now;
            existing.IsActive = true;
            existing.ResolvedAtUtc = null;
            risks.Add(existing);

            if (FactoryIntelligenceSuppressionEngine.ShouldOpenEarlyCase(existing.RiskState))
            {
                caseId = await EnsurePredictiveCaseAsync(existing, request, subjectId, cancellationToken);
                existing.LinkedCaseId = caseId;
                caseCreatedOrUpdated = true;
            }
        }

        await ResolveImprovedRisksAsync(request, detections, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new FactoryIntelligenceEvaluationResult(
            persistedSignals,
            await dbContext.FactoryOperationalBaselines
                .Where(b => b.TenantId == request.TenantId && b.FacilityId == request.FacilityId)
                .ToListAsync(cancellationToken),
            risks,
            detections,
            caseCreatedOrUpdated,
            caseId);
    }

    private async Task UpsertBaselineEntityAsync(
        FactoryIntelligenceEvaluationRequest request,
        FactorySignalWindowInput signal,
        FactoryBaselineResult baseline,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.FactoryOperationalBaselines
            .FirstOrDefaultAsync(b =>
                b.TenantId == request.TenantId
                && b.FacilityId == request.FacilityId
                && b.ResourceId == signal.ResourceId
                && b.SignalType == signal.SignalType,
                cancellationToken);

        if (entity is null)
        {
            entity = new FactoryOperationalBaseline
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                FacilityId = request.FacilityId,
                ResourceType = signal.ResourceType,
                ResourceId = signal.ResourceId,
                SignalType = signal.SignalType,
            };
            dbContext.FactoryOperationalBaselines.Add(entity);
        }

        entity.ReferenceValue = baseline.Reference;
        entity.CurrentValue = baseline.Current;
        entity.DeviationPercent = baseline.DeviationPercent;
        entity.Trend = baseline.Trend;
        entity.DataQuality = baseline.DataQuality;
        entity.BaselineSource = baseline.BaselineSource;
        entity.EngineVersion = FactoryIntelligenceEngineVersions.BaselineEngine;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private async Task<Guid> EnsurePredictiveCaseAsync(
        FactoryPredictedRisk risk,
        FactoryIntelligenceEvaluationRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        if (risk.LinkedCaseId is Guid linkedCaseId)
        {
            var existingCase = await dbContext.DigitalWorkCases
                .FirstOrDefaultAsync(c => c.Id == linkedCaseId, cancellationToken);
            if (existingCase is not null)
            {
                UpdatePredictiveCase(existingCase, risk);
                return existingCase.Id;
            }
        }

        var metadata = BuildCaseMetadata(risk, request);
        var workCase = await runtime.TriggerCaseAsync(
            new TriggerWorkCaseRequest(
                $"Erken uyarı: {risk.AffectedOutcome}",
                string.Join("\n", DeserializeList(risk.ExplanationJson)),
                FactoryIntelligenceMetadataKeys.SourceModule,
                FactoryIntelligenceDataClassifications.Predicted,
                risk.RiskState == FactoryIntelligenceRiskStates.Critical ? "critical" : "high",
                DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator,
                request.FacilityId,
                JsonSerializer.Serialize(metadata, JsonOptions)),
            subjectId,
            cancellationToken);

        workCase.Source = FactoryIntelligenceMetadataKeys.SourceModule;
        workCase.OrchestrationRootCaseId = workCase.Id;
        workCase.CorrelationKey = risk.CorrelationKey;
        workCase.PrimaryDomain = FactoryOrchestrationDomains.Production;
        workCase.BusinessImpact = risk.AffectedOutcome;
        workCase.RequiresApproval = true;
        workCase.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingHuman;
        workCase.BlockerSinceUtc = DateTimeOffset.UtcNow;

        var gap = risk.ProjectedGapQuantity ?? 96;
        var impact = new FactoryOutcomeImpactSnapshot(
            risk.AffectedOutcome,
            1200,
            gap,
            request.OrderReference,
            request.MachineCode,
            request.MaterialCode,
            risk.TimeToImpactLabel,
            risk.ConfidenceBand == FactoryIntelligenceConfidenceBands.High ? 0.85m : 0.65m,
            null,
            []);
        workCase.ImpactGraphJson = FactoryOutcomeImpactEngine.ToJson(impact);

        var recoveryPlan = FactoryOutcomeRecoveryPlanEngine.Build(workCase, impact, []);
        workCase.RecoveryPlanJson = FactoryOutcomeRecoveryPlanEngine.ToJson(recoveryPlan);

        await dbContext.SaveChangesAsync(cancellationToken);
        return workCase.Id;
    }

    private static void UpdatePredictiveCase(
        DigitalWorkCase workCase,
        FactoryPredictedRisk risk)
    {
        workCase.Severity = risk.RiskState == FactoryIntelligenceRiskStates.Critical ? "critical" : "high";
        workCase.MetadataJson = JsonSerializer.Serialize(BuildCaseMetadataFromRisk(risk), JsonOptions);
        workCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private async Task ResolveImprovedRisksAsync(
        FactoryIntelligenceEvaluationRequest request,
        IReadOnlyList<FactoryRiskDetectionResult> detections,
        CancellationToken cancellationToken)
    {
        var activeKeys = detections.Select(d => d.CorrelationKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var activeRisks = await dbContext.FactoryPredictedRisks
            .Where(r =>
                r.TenantId == request.TenantId
                && r.FacilityId == request.FacilityId
                && r.IsActive
                && r.MachineCode == request.MachineCode)
            .ToListAsync(cancellationToken);

        foreach (var risk in activeRisks.Where(r => !activeKeys.Contains(r.CorrelationKey)))
        {
            risk.RiskState = FactoryIntelligenceRiskStates.Resolved;
            risk.IsActive = false;
            risk.ResolvedAtUtc = DateTimeOffset.UtcNow;
            risk.UpdatedAtUtc = DateTimeOffset.UtcNow;

            dbContext.FactoryPredictionEvaluations.Add(new FactoryPredictionEvaluation
            {
                Id = Guid.NewGuid(),
                TenantId = risk.TenantId,
                FacilityId = risk.FacilityId,
                PredictedRiskId = risk.Id,
                EvaluationOutcome = FactoryIntelligenceEvaluationOutcomes.FalsePositive,
                PredictedOutcome = risk.AffectedOutcome,
                ActualOutcome = "Risk normalized without materialized failure",
                Horizon = risk.Horizon,
                ConfidenceBand = risk.ConfidenceBand,
                InterventionOccurred = false,
                Notes = "Signals normalized; outcome remained safe",
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
            });
        }
    }

    private async Task<FactoryIntelligenceOverview> GetOverviewCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var active = await dbContext.FactoryPredictedRisks
            .Where(r => r.FacilityId == facilityId && r.IsActive)
            .ToListAsync(cancellationToken);

        var evaluations = await dbContext.FactoryPredictionEvaluations
            .Where(e => e.FacilityId == facilityId && e.EvaluatedAtUtc >= since)
            .ToListAsync(cancellationToken);

        return new FactoryIntelligenceOverview(
            active.Count,
            active.Count(r => r.RiskState == FactoryIntelligenceRiskStates.Watch),
            active.Count(r => r.RiskState == FactoryIntelligenceRiskStates.Warning),
            active.Count(r => r.RiskState == FactoryIntelligenceRiskStates.Critical),
            evaluations.Count(e => e.EvaluationOutcome == FactoryIntelligenceEvaluationOutcomes.RiskAvertedAfterIntervention),
            evaluations.Count(e => e.EvaluationOutcome == FactoryIntelligenceEvaluationOutcomes.TruePositive),
            evaluations.Count(e => e.EvaluationOutcome == FactoryIntelligenceEvaluationOutcomes.FalsePositive),
            165,
            active.Any(r => r.DataQuality != FactoryIntelligenceDataQualityStates.Good)
                ? FactoryIntelligenceDataQualityStates.Partial
                : FactoryIntelligenceDataQualityStates.Good);
    }

    private async Task<IReadOnlyList<FactoryIntelligenceRiskCard>> GetEarlyWarningCardsCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var risks = await dbContext.FactoryPredictedRisks
            .Where(r => r.FacilityId == facilityId && r.IsActive)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return risks.Select(ProjectRiskCard).ToList();
    }

    private async Task<FactoryIntelligenceRiskDetail> GetRiskDetailCoreAsync(
        Guid riskId, CancellationToken cancellationToken)
    {
        var risk = await dbContext.FactoryPredictedRisks
            .FirstOrDefaultAsync(r => r.Id == riskId, cancellationToken)
            ?? throw new FactoryIntelligenceException(FactoryIntelligenceFailureCodes.NotFound, "Risk not found.");

        var similarCases = new List<string>();
        if (risk.LinkedCaseId is Guid caseId)
        {
            var workCase = await dbContext.DigitalWorkCases.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
            if (workCase is not null)
            {
                var intelligence = await recoveryIntelligence.BuildForCaseAsync(workCase, [], cancellationToken);
                similarCases.AddRange(intelligence.SimilarCases.Take(3).Select(c =>
                    $"{c.OrderReference}: {c.AppliedStrategy} ({c.SimilarityPercent}% benzer)"));
            }
        }

        return new FactoryIntelligenceRiskDetail(
            risk.Id,
            risk.RiskType,
            risk.RiskState,
            risk.MachineCode ?? string.Empty,
            risk.OrderReference,
            DeserializeList(risk.ExplanationJson).Where(e => e.StartsWith("OBSERVED", StringComparison.OrdinalIgnoreCase)).ToList(),
            DeserializeList(risk.ExplanationJson).Where(e => e.StartsWith("PREDICTED", StringComparison.OrdinalIgnoreCase)).ToList(),
            DeserializeList(risk.ExplanationJson),
            [risk.AffectedOutcome],
            risk.TimeToImpactLabel ?? "unknown",
            risk.ConfidenceBand,
            DeserializeList(risk.ExplanationJson),
            BuildRecommendedActions(risk),
            similarCases,
            DeserializeList(risk.AssumptionsJson),
            risk.DataQuality,
            risk.Horizon,
            risk.LinkedCaseId,
            risk.GeneratedAtUtc);
    }

    private async Task<IReadOnlyList<FactoryPredictionEvaluation>> GetRiskHistoryCoreAsync(
        Guid facilityId, string? riskType, CancellationToken cancellationToken)
    {
        var query = dbContext.FactoryPredictionEvaluations
            .Where(e => e.FacilityId == facilityId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(riskType))
        {
            var riskIds = dbContext.FactoryPredictedRisks
                .Where(r => r.FacilityId == facilityId && r.RiskType == riskType)
                .Select(r => r.Id);
            query = query.Where(e => riskIds.Contains(e.PredictedRiskId));
        }

        return await query
            .OrderByDescending(e => e.EvaluatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<FactoryIntelligenceCalibrationSummary>> GetCalibrationCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var risks = await dbContext.FactoryPredictedRisks
            .Where(r => r.FacilityId == facilityId)
            .ToListAsync(cancellationToken);
        var evaluations = await dbContext.FactoryPredictionEvaluations
            .Where(e => e.FacilityId == facilityId)
            .ToListAsync(cancellationToken);

        return risks
            .GroupBy(r => r.RiskType)
            .Select(group =>
            {
                var evals = evaluations.Where(e => group.Any(r => r.Id == e.PredictedRiskId)).ToList();
                return new FactoryIntelligenceCalibrationSummary(
                    group.Key,
                    group.Count(),
                    evals.Count(e => e.EvaluationOutcome == FactoryIntelligenceEvaluationOutcomes.TruePositive),
                    evals.Count(e => e.EvaluationOutcome == FactoryIntelligenceEvaluationOutcomes.RiskAvertedAfterIntervention),
                    evals.Count(e => e.EvaluationOutcome == FactoryIntelligenceEvaluationOutcomes.FalsePositive),
                    evals.Count(e => e.EvaluationOutcome == FactoryIntelligenceEvaluationOutcomes.MissedRisk),
                    165,
                    group.GroupBy(r => r.ConfidenceBand).OrderByDescending(g => g.Count()).First().Key);
            })
            .ToList();
    }

    private async Task<FactoryPredictionEvaluation> EvaluateOutcomeCoreAsync(
        Guid riskId,
        string outcome,
        bool interventionOccurred,
        string? notes,
        CancellationToken cancellationToken)
    {
        var risk = await dbContext.FactoryPredictedRisks
            .FirstOrDefaultAsync(r => r.Id == riskId, cancellationToken)
            ?? throw new FactoryIntelligenceException(FactoryIntelligenceFailureCodes.NotFound, "Risk not found.");

        var evaluationOutcome = outcome switch
        {
            "averted" => FactoryIntelligenceEvaluationOutcomes.RiskAvertedAfterIntervention,
            "confirmed" => FactoryIntelligenceEvaluationOutcomes.TruePositive,
            "false-alarm" => FactoryIntelligenceEvaluationOutcomes.FalsePositive,
            _ => FactoryIntelligenceEvaluationOutcomes.Pending,
        };

        if (interventionOccurred && evaluationOutcome == FactoryIntelligenceEvaluationOutcomes.TruePositive)
        {
            evaluationOutcome = FactoryIntelligenceEvaluationOutcomes.RiskAvertedAfterIntervention;
        }

        risk.IsActive = false;
        risk.RiskState = evaluationOutcome == FactoryIntelligenceEvaluationOutcomes.RiskAvertedAfterIntervention
            ? FactoryIntelligenceRiskStates.Averted
            : FactoryIntelligenceRiskStates.Resolved;
        risk.ResolvedAtUtc = DateTimeOffset.UtcNow;
        risk.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var evaluation = new FactoryPredictionEvaluation
        {
            Id = Guid.NewGuid(),
            TenantId = risk.TenantId,
            FacilityId = risk.FacilityId,
            PredictedRiskId = risk.Id,
            EvaluationOutcome = evaluationOutcome,
            PredictedOutcome = risk.AffectedOutcome,
            ActualOutcome = notes ?? outcome,
            Horizon = risk.Horizon,
            ConfidenceBand = risk.ConfidenceBand,
            InterventionOccurred = interventionOccurred,
            Notes = notes,
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.FactoryPredictionEvaluations.Add(evaluation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return evaluation;
    }

    private static FactoryIntelligenceRiskCard ProjectRiskCard(FactoryPredictedRisk risk)
    {
        var explanation = DeserializeList(risk.ExplanationJson);
        return new FactoryIntelligenceRiskCard(
            risk.Id,
            risk.LinkedCaseId,
            risk.RiskType,
            risk.RiskState,
            risk.MachineCode ?? string.Empty,
            risk.OrderReference,
            explanation.FirstOrDefault(e => e.StartsWith("OBSERVED", StringComparison.OrdinalIgnoreCase)) ?? "—",
            explanation.FirstOrDefault(e => e.StartsWith("PREDICTED", StringComparison.OrdinalIgnoreCase)) ?? risk.AffectedOutcome,
            risk.TimeToImpactLabel ?? "—",
            risk.ConfidenceBand,
            risk.Horizon,
            explanation,
            BuildRecommendedActions(risk),
            risk.DataQuality,
            risk.GeneratedAtUtc);
    }

    private static IReadOnlyList<string> BuildRecommendedActions(FactoryPredictedRisk risk) => risk.RiskType switch
    {
        FactoryIntelligenceRiskTypes.MaterialShortage =>
            ["Malzeme rezervasyonu", "Tedarik hızlandırma", "Üretim sırası koruma"],
        FactoryIntelligenceRiskTypes.MachineDegradation =>
            ["Çevrim kurtarma", "Mikro duruş inceleme", "Kapasite yeniden dağıtımı"],
        FactoryIntelligenceRiskTypes.CriticalOrderDelivery or FactoryIntelligenceRiskTypes.CompoundDelivery =>
            ["Önleyici kurtarma planı", "Alternatif kapasite", "Teslimat taahhüdü koruma"],
        FactoryIntelligenceRiskTypes.QualityDeterioration =>
            ["Kalite parametre inceleme", "İlişkili sinyal doğrulama"],
        FactoryIntelligenceRiskTypes.MaintenanceReliability =>
            ["Önleyici bakım planlama", "Duruş riski azaltma"],
        _ => ["Risk izleme", "Koordinatör inceleme"],
    };

    private static Dictionary<string, object?> BuildCaseMetadata(
        FactoryPredictedRisk risk,
        FactoryIntelligenceEvaluationRequest request)
    {
        var metadata = BuildCaseMetadataFromRisk(risk);
        metadata[FactoryIntelligenceMetadataKeys.OrderReference] = request.OrderReference;
        metadata[FactoryIntelligenceMetadataKeys.MachineCode] = request.MachineCode;
        metadata[FactoryIntelligenceMetadataKeys.MaterialCode] = request.MaterialCode;
        metadata[FactoryIntelligenceMetadataKeys.RequiredQuantity] = 1200;
        metadata[FactoryIntelligenceMetadataKeys.ProjectedQuantity] = 1200 - (risk.ProjectedGapQuantity ?? 0);
        return metadata;
    }

    private static Dictionary<string, object?> BuildCaseMetadataFromRisk(FactoryPredictedRisk risk) => new()
    {
        [FactoryIntelligenceMetadataKeys.PredictionMode] = FactoryIntelligenceMetadataKeys.PredictiveEarlyWarning,
        [FactoryIntelligenceMetadataKeys.DataClassification] = FactoryIntelligenceDataClassifications.Predicted,
        [FactoryIntelligenceMetadataKeys.RiskState] = risk.RiskState,
        [FactoryIntelligenceMetadataKeys.RiskType] = risk.RiskType,
        [FactoryIntelligenceMetadataKeys.ConfidenceBand] = risk.ConfidenceBand,
        [FactoryIntelligenceMetadataKeys.Horizon] = risk.Horizon,
        [FactoryIntelligenceMetadataKeys.TimeToImpactLabel] = risk.TimeToImpactLabel,
        [FactoryIntelligenceMetadataKeys.ProjectedGapQuantity] = risk.ProjectedGapQuantity,
        [FactoryIntelligenceMetadataKeys.PredictedRiskId] = risk.Id,
        [FactoryIntelligenceMetadataKeys.EngineVersion] = risk.EngineVersion,
        [FactoryIntelligenceMetadataKeys.Assumptions] = DeserializeList(risk.AssumptionsJson),
        [FactoryIntelligenceMetadataKeys.Evidence] = DeserializeList(risk.ExplanationJson),
        [FactoryIntelligenceMetadataKeys.PreventiveRecoveryPlan] = true,
    };

    private static List<string> DeserializeList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
