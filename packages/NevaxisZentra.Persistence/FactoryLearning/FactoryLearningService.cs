using System.Text.Json;
using NevaxisZentra.Persistence.ProductionManager;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.FactoryLearning;

public sealed record FactoryExperimentVerifyResult(
    FactoryExperiment Experiment,
    VerifiedImprovement? VerifiedImprovement);

public interface IFactoryLearningService
{
    Task<FactoryLearningWorkspaceSnapshot> GetWorkspaceAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FactoryLearningWorkspaceSnapshot> RefreshLossesAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FactoryRootCauseAnalysis> CreateRootCauseAsync(Guid facilityId, RootCauseDraftRequest request, CancellationToken cancellationToken);
    Task<FactoryExperiment> CreateExperimentAsync(Guid facilityId, CreateExperimentRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryExperiment> ApproveExperimentAsync(Guid experimentId, ApproveExperimentRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryExperiment> StartExperimentAsync(Guid experimentId, StartExperimentRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryExperiment> StopExperimentAsync(Guid experimentId, StopExperimentRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryExperiment> CompleteExperimentAsync(Guid experimentId, CompleteExperimentRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryExperimentVerifyResult> VerifyExperimentAsync(Guid experimentId, VerifyExperimentRequest request, string subjectId, CancellationToken cancellationToken);
    Task<VerifiedImprovement> StandardizeImprovementAsync(Guid verifiedImprovementId, StandardizeImprovementRequest request, string subjectId, CancellationToken cancellationToken);
    Task<SimulationImprovementResult> SimulateImprovementAsync(Guid facilityId, SimulateImprovementRequest request, CancellationToken cancellationToken);
    Task<ImprovementOpportunity?> GetOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken);
    Task<FactoryExperiment?> GetExperimentAsync(Guid experimentId, CancellationToken cancellationToken);
    Task<VerifiedImprovement?> GetVerifiedImprovementAsync(Guid verifiedImprovementId, CancellationToken cancellationToken);
    Task CheckSustainabilityAsync(Guid facilityId, CancellationToken cancellationToken);
}

public sealed class FactoryLearningService : IFactoryLearningService
{
    private readonly IFactoryLearningRepository repository;
    private readonly NevaxisZentraDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public FactoryLearningService(
        IFactoryLearningRepository repository,
        NevaxisZentraDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.repository = repository;
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public Task<FactoryLearningWorkspaceSnapshot> GetWorkspaceAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => BuildWorkspaceAsync(facilityId, ct),
            cancellationToken);

    public Task<FactoryLearningWorkspaceSnapshot> RefreshLossesAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RefreshLossesCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<FactoryRootCauseAnalysis> CreateRootCauseAsync(
        Guid facilityId, RootCauseDraftRequest request, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CreateRootCauseCoreAsync(facilityId, request, ct),
            cancellationToken);

    public Task<FactoryExperiment> CreateExperimentAsync(
        Guid facilityId, CreateExperimentRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CreateExperimentCoreAsync(facilityId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryExperiment> ApproveExperimentAsync(
        Guid experimentId, ApproveExperimentRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ApproveExperimentCoreAsync(experimentId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryExperiment> StartExperimentAsync(
        Guid experimentId, StartExperimentRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => StartExperimentCoreAsync(experimentId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryExperiment> StopExperimentAsync(
        Guid experimentId, StopExperimentRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => StopExperimentCoreAsync(experimentId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryExperiment> CompleteExperimentAsync(
        Guid experimentId, CompleteExperimentRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CompleteExperimentCoreAsync(experimentId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryExperimentVerifyResult> VerifyExperimentAsync(
        Guid experimentId, VerifyExperimentRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => VerifyExperimentCoreAsync(experimentId, request, subjectId, ct),
            cancellationToken);

    public Task<VerifiedImprovement> StandardizeImprovementAsync(
        Guid verifiedImprovementId, StandardizeImprovementRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => StandardizeImprovementCoreAsync(verifiedImprovementId, request, subjectId, ct),
            cancellationToken);

    public Task<SimulationImprovementResult> SimulateImprovementAsync(
        Guid facilityId, SimulateImprovementRequest request, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var opportunity = await repository.GetOpportunityAsync(request.OpportunityId, ct)
                    ?? throw new FactoryLearningException(FactoryLearningFailureCodes.NotFound, "Fırsat bulunamadı.");
                return ImprovementSimulationBridge.Simulate(opportunity, request.ScenarioType, request.InputJson);
            },
            cancellationToken);

    public Task<ImprovementOpportunity?> GetOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken) =>
        repository.GetOpportunityAsync(opportunityId, cancellationToken);

    public Task<FactoryExperiment?> GetExperimentAsync(Guid experimentId, CancellationToken cancellationToken) =>
        repository.GetExperimentAsync(experimentId, cancellationToken);

    public Task<VerifiedImprovement?> GetVerifiedImprovementAsync(
        Guid verifiedImprovementId, CancellationToken cancellationToken) =>
        repository.GetVerifiedImprovementAsync(verifiedImprovementId, cancellationToken);

    public Task CheckSustainabilityAsync(Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var verified = await repository.ListVerifiedImprovementsAsync(facilityId, ct);
                foreach (var item in verified.Where(v => v.SustainabilityStatus != "regressing"))
                {
                    var current = item.EffectivenessScore * 0.55m;
                    var status = ImprovementSustainabilityEngine.Evaluate(item.EffectivenessScore, current);
                    if (status == "regressing")
                    {
                        var tracked = await dbContext.VerifiedImprovements.FirstAsync(v => v.Id == item.Id, ct);
                        tracked.SustainabilityStatus = "regressing";
                        tracked.UpdatedAtUtc = DateTimeOffset.UtcNow;
                        tracked.ConcurrencyToken++;
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                return true;
            },
            cancellationToken);

    private async Task<FactoryLearningWorkspaceSnapshot> BuildWorkspaceAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var losses = await repository.ListLossesAsync(facilityId, 30, cancellationToken);
        var patterns = await repository.ListPatternsAsync(facilityId, cancellationToken);
        var opportunities = await repository.ListOpportunitiesAsync(facilityId, cancellationToken);
        var experiments = await repository.ListExperimentsAsync(facilityId, null, cancellationToken);
        var verified = await repository.ListVerifiedImprovementsAsync(facilityId, cancellationToken);
        var memories = await repository.ListMemoriesAsync(facilityId, cancellationToken);
        var state = await repository.GetLatestStateAsync(facilityId, cancellationToken);

        var todayLosses = losses.Where(l => l.LastSeenAtUtc.Date == DateTimeOffset.UtcNow.Date).ToList();
        var sevenDayLosses = losses.Where(l => l.LastSeenAtUtc >= DateTimeOffset.UtcNow.AddDays(-7)).ToList();

        return new FactoryLearningWorkspaceSnapshot(
            losses,
            patterns,
            opportunities,
            experiments.Where(e => e.Status is FactoryExperimentStatuses.Running or FactoryExperimentStatuses.Approved).ToList(),
            verified,
            memories,
            LossParetoEngine.Calculate(losses),
            FactoryLearningMemoryEngine.BuildInsights(memories),
            new FactoryLossCommandSummary(
                todayLosses.Sum(l => l.DurationMinutes),
                sevenDayLosses.Sum(l => l.DurationMinutes),
                losses.Sum(l => l.DurationMinutes),
                patterns.Count,
                experiments.Count(e => e.Status == FactoryExperimentStatuses.Running),
                verified.Count,
                verified.Count(v => v.SustainabilityStatus == "regressing")),
            state?.CapturedAtUtc ?? DateTimeOffset.UtcNow);
    }

    private async Task<FactoryLearningWorkspaceSnapshot> RefreshLossesCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var state = await repository.GetLatestStateAsync(facilityId, cancellationToken)
            ?? throw new FactoryLearningException(FactoryLearningFailureCodes.NotFound, "Fabrika durumu bulunamadı.");

        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var existing = await dbContext.FactoryLosses
            .Where(l => l.FacilityId == facilityId && l.LastSeenAtUtc >= since)
            .ToListAsync(cancellationToken);
        var detected = LossDetectionEngine.DetectFromState(state, tenantId, facilityId, existing);

        foreach (var loss in detected)
        {
            var prior = existing.FirstOrDefault(l => l.CorrelationKey == loss.CorrelationKey);
            if (prior is null)
            {
                dbContext.FactoryLosses.Add(loss);
            }
            else
            {
                prior.LastSeenAtUtc = loss.LastSeenAtUtc;
                prior.RecurrenceCount = loss.RecurrenceCount;
                prior.DurationMinutes += loss.DurationMinutes / 2;
                prior.UpdatedAtUtc = DateTimeOffset.UtcNow;
                prior.ConcurrencyToken++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var refreshedLosses = await repository.ListLossesAsync(facilityId, 30, cancellationToken);
        var patterns = FactoryPatternEngine.FindDeterministicPatterns(refreshedLosses);
        foreach (var pattern in patterns)
        {
            if (!await dbContext.FactoryLossPatterns.AnyAsync(p => p.PatternKey == pattern.PatternKey, cancellationToken))
            {
                dbContext.FactoryLossPatterns.Add(pattern);
            }
        }

        var allPatterns = await repository.ListPatternsAsync(facilityId, cancellationToken);
        var opportunities = ImprovementOpportunityEngine.Generate(allPatterns, refreshedLosses);
        foreach (var opportunity in opportunities)
        {
            if (!await dbContext.ImprovementOpportunities.AnyAsync(
                    o => o.PatternId == opportunity.PatternId && o.Title == opportunity.Title, cancellationToken))
            {
                opportunity.OwnerSubjectId = "production-pilot-operator";
                dbContext.ImprovementOpportunities.Add(opportunity);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildWorkspaceAsync(facilityId, cancellationToken);
    }

    private async Task<FactoryRootCauseAnalysis> CreateRootCauseCoreAsync(
        Guid facilityId, RootCauseDraftRequest request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var analysis = RootCauseWorkbenchEngine.Draft(request, tenantId, facilityId);
        dbContext.FactoryRootCauseAnalyses.Add(analysis);
        await dbContext.SaveChangesAsync(cancellationToken);
        return analysis;
    }

    private async Task<FactoryExperiment> CreateExperimentCoreAsync(
        Guid facilityId, CreateExperimentRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var opportunity = await repository.GetOpportunityAsync(request.OpportunityId, cancellationToken)
            ?? throw new FactoryLearningException(FactoryLearningFailureCodes.NotFound, "Fırsat bulunamadı.");

        var experiment = new FactoryExperiment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            OpportunityId = opportunity.Id,
            ExperimentType = request.ExperimentType,
            Title = request.Title,
            Hypothesis = request.Hypothesis,
            ScopeJson = request.ScopeJson,
            BaselineJson = request.BaselineJson,
            TargetJson = request.TargetJson,
            GuardrailsJson = request.GuardrailsJson,
            StopConditionsJson = request.StopConditionsJson,
            Status = ExperimentSafetyEngine.RequiresApproval(request.ExperimentType)
                ? FactoryExperimentStatuses.ApprovalPending
                : FactoryExperimentStatuses.Approved,
            OwnerSubjectId = subjectId,
            ApprovalState = ExperimentSafetyEngine.RequiresApproval(request.ExperimentType) ? "pending" : "not-required",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.FactoryExperiments.Add(experiment);
        opportunity.Status = ImprovementBacklogStatuses.Experimenting;
        opportunity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        opportunity.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return experiment;
    }

    private async Task<FactoryExperiment> ApproveExperimentCoreAsync(
        Guid experimentId, ApproveExperimentRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var experiment = await LoadExperimentForWriteAsync(experimentId, request.ConcurrencyToken, cancellationToken);
        if (!request.Approved)
        {
            experiment.Status = FactoryExperimentStatuses.Stopped;
            experiment.ApprovalState = "rejected";
        }
        else
        {
            experiment.Status = FactoryExperimentStatuses.Approved;
            experiment.ApprovalState = "approved";
            experiment.ApprovedBy = subjectId;
        }

        experiment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        experiment.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return experiment;
    }

    private async Task<FactoryExperiment> StartExperimentCoreAsync(
        Guid experimentId, StartExperimentRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var experiment = await LoadExperimentForWriteAsync(experimentId, request.ConcurrencyToken, cancellationToken);
        if (experiment.Status != FactoryExperimentStatuses.Approved &&
            experiment.Status != FactoryExperimentStatuses.ApprovalPending)
        {
            throw new FactoryLearningException(FactoryLearningFailureCodes.ValidationFailed, "Deney başlatılamaz.");
        }

        if (ExperimentSafetyEngine.RequiresApproval(experiment.ExperimentType) &&
            experiment.ApprovalState != "approved")
        {
            throw new FactoryLearningException(FactoryLearningFailureCodes.ApprovalRequired, "Onay gerekli.");
        }

        foreach (var metric in ParseBaselineMeasurements(experiment))
        {
            dbContext.FactoryExperimentMeasurements.Add(metric);
        }

        experiment.Status = FactoryExperimentStatuses.Running;
        experiment.StartedAtUtc = DateTimeOffset.UtcNow;
        experiment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        experiment.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return experiment;
    }

    private async Task<FactoryExperiment> StopExperimentCoreAsync(
        Guid experimentId, StopExperimentRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var experiment = await LoadExperimentForWriteAsync(experimentId, request.ConcurrencyToken, cancellationToken);
        experiment.Status = FactoryExperimentStatuses.Stopped;
        experiment.StoppedAtUtc = DateTimeOffset.UtcNow;
        experiment.Outcome = FactoryExperimentOutcomes.NegativeEffect;
        experiment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        experiment.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return experiment;
    }

    private async Task<FactoryExperiment> CompleteExperimentCoreAsync(
        Guid experimentId, CompleteExperimentRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var experiment = await LoadExperimentForWriteAsync(experimentId, request.ConcurrencyToken, cancellationToken);
        var tenantId = tenantContextAccessor.RequireTenantId();

        foreach (var input in request.Measurements)
        {
            dbContext.FactoryExperimentMeasurements.Add(new FactoryExperimentMeasurement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ExperimentId = experiment.Id,
                Phase = input.Phase,
                MetricKey = input.MetricKey,
                MetricValue = input.MetricValue,
                Unit = input.Unit,
                DataClassification = input.DataClassification,
                EvidenceJson = input.EvidenceJson,
                RecordedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        experiment.Status = FactoryExperimentStatuses.Completed;
        experiment.CompletedAtUtc = DateTimeOffset.UtcNow;
        experiment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        experiment.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return experiment;
    }

    private async Task<FactoryExperimentVerifyResult> VerifyExperimentCoreAsync(
        Guid experimentId, VerifyExperimentRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var experiment = await LoadExperimentForWriteAsync(experimentId, request.ConcurrencyToken, cancellationToken);
        var opportunity = await repository.GetOpportunityAsync(experiment.OpportunityId, cancellationToken)
            ?? throw new FactoryLearningException(FactoryLearningFailureCodes.NotFound, "Fırsat bulunamadı.");

        var measurements = await dbContext.FactoryExperimentMeasurements
            .Where(m => m.ExperimentId == experiment.Id)
            .ToListAsync(cancellationToken);

        var guardrailsOk = ExperimentSafetyEngine.ValidateGuardrails(experiment, measurements);
        var (beforeJson, afterJson, deltaJson, effectiveness) =
            ExperimentComparisonEngine.Compare(experiment, measurements);

        var outcome = request.Outcome;
        if (string.IsNullOrWhiteSpace(outcome))
        {
            outcome = ExperimentOutcomeEngine.Classify(
                effectiveness,
                guardrailsOk,
                measurements.Any(m => m.Phase == "baseline"),
                measurements.Any(m => m.Phase == "experiment"));
        }

        experiment.Outcome = outcome;
        experiment.Status = outcome == FactoryExperimentOutcomes.Inconclusive
            ? FactoryExperimentStatuses.Inconclusive
            : outcome == FactoryExperimentOutcomes.NegativeEffect
                ? FactoryExperimentStatuses.Failed
                : FactoryExperimentStatuses.Verified;
        experiment.VerifiedAtUtc = DateTimeOffset.UtcNow;
        experiment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        experiment.ConcurrencyToken++;

        VerifiedImprovement? verified = null;
        if (outcome is FactoryExperimentOutcomes.Success or FactoryExperimentOutcomes.PartialSuccess)
        {
            verified = new VerifiedImprovement
            {
                Id = Guid.NewGuid(),
                TenantId = experiment.TenantId,
                FacilityId = experiment.FacilityId,
                ExperimentId = experiment.Id,
                OpportunityId = opportunity.Id,
                Title = experiment.Title,
                OutcomeSummary = $"Doğrulanmış iyileştirme: {outcome}",
                BeforeJson = beforeJson,
                AfterJson = afterJson,
                DeltaJson = deltaJson,
                EffectivenessScore = effectiveness,
                ImpactClassification = FactoryLearningDataClassifications.Measured,
                VerificationEvidenceJson = JsonSerializer.Serialize(measurements.Select(m => new
                {
                    m.Phase,
                    m.MetricKey,
                    m.MetricValue,
                    m.DataClassification,
                })),
                VerifiedAtUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
            dbContext.VerifiedImprovements.Add(verified);

            var memoryQuality = outcome == FactoryExperimentOutcomes.Success
                ? LearningMemoryQuality.Verified
                : LearningMemoryQuality.Partial;
            dbContext.FactoryLearningMemories.Add(
                FactoryLearningMemoryEngine.CreateFromVerifiedImprovement(verified, experiment, opportunity, memoryQuality));

            opportunity.Status = ImprovementBacklogStatuses.Verified;
        }
        else if (outcome == FactoryExperimentOutcomes.NegativeEffect)
        {
            dbContext.FactoryLearningMemories.Add(new FactoryLearningMemory
            {
                Id = Guid.NewGuid(),
                TenantId = experiment.TenantId,
                FacilityId = experiment.FacilityId,
                ProblemPattern = opportunity.ProblemSummary,
                ContextJson = opportunity.AffectedScopeJson,
                InterventionSummary = experiment.Hypothesis,
                ExperimentSummary = experiment.Title,
                ResultSummary = "Negatif etki — standardizasyon yapılmadı",
                EffectivenessScore = effectiveness,
                MemoryQuality = LearningMemoryQuality.Failed,
                EvidenceJson = """[{"source":"experiment","classification":"measured"}]""",
                RecordedAtUtc = DateTimeOffset.UtcNow,
            });
            opportunity.Status = ImprovementBacklogStatuses.Rejected;
        }
        else
        {
            opportunity.Status = ImprovementBacklogStatuses.Inconclusive;
        }

        opportunity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        opportunity.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new FactoryExperimentVerifyResult(experiment, verified);
    }

    private async Task<VerifiedImprovement> StandardizeImprovementCoreAsync(
        Guid verifiedImprovementId, StandardizeImprovementRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var verified = await dbContext.VerifiedImprovements.FirstOrDefaultAsync(v => v.Id == verifiedImprovementId, cancellationToken)
            ?? throw new FactoryLearningException(FactoryLearningFailureCodes.NotFound, "Doğrulanmış iyileştirme bulunamadı.");

        if (verified.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new FactoryLearningException(FactoryLearningFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");
        }

        verified.StandardizationState = "standardized";
        verified.StandardizedAtUtc = DateTimeOffset.UtcNow;
        verified.UpdatedAtUtc = DateTimeOffset.UtcNow;
        verified.ConcurrencyToken++;

        var opportunity = await repository.GetOpportunityAsync(verified.OpportunityId, cancellationToken);
        if (opportunity is not null)
        {
            opportunity.Status = ImprovementBacklogStatuses.Standardized;
            opportunity.UpdatedAtUtc = DateTimeOffset.UtcNow;
            opportunity.ConcurrencyToken++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return verified;
    }

    private async Task<FactoryExperiment> LoadExperimentForWriteAsync(
        Guid experimentId, long concurrencyToken, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.FactoryExperiments.FirstOrDefaultAsync(e => e.Id == experimentId, cancellationToken)
            ?? throw new FactoryLearningException(FactoryLearningFailureCodes.NotFound, "Deney bulunamadı.");

        if (experiment.ConcurrencyToken != concurrencyToken)
        {
            throw new FactoryLearningException(FactoryLearningFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");
        }

        return experiment;
    }

    private static IEnumerable<FactoryExperimentMeasurement> ParseBaselineMeasurements(FactoryExperiment experiment)
    {
        decimal baselineMinutes = 42m;
        try
        {
            using var doc = JsonDocument.Parse(experiment.BaselineJson);
            if (doc.RootElement.TryGetProperty("durationMinutes", out var value))
            {
                baselineMinutes = value.GetDecimal();
            }
        }
        catch
        {
            // ignored
        }

        yield return new FactoryExperimentMeasurement
        {
            Id = Guid.NewGuid(),
            TenantId = experiment.TenantId,
            ExperimentId = experiment.Id,
            Phase = "baseline",
            MetricKey = "duration-minutes",
            MetricValue = baselineMinutes,
            Unit = "minutes",
            DataClassification = FactoryLearningDataClassifications.Measured,
            EvidenceJson = """[{"source":"baseline-capture","classification":"measured"}]""",
            RecordedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
