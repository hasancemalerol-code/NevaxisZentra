using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.FactoryDecisionCenter;
using NevaxisZentra.Persistence.FactoryRecoveryIntelligence;

namespace NevaxisZentra.Persistence.FactoryOutcomeExecution;

public interface IFactoryOutcomeLearningService
{
    Task RecordVerifiedOutcomeAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        FactoryOutcomeVerificationSnapshot snapshot,
        CancellationToken cancellationToken);

    Task RecordFailureIntelligenceAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        string disposition,
        string failureSummary,
        CancellationToken cancellationToken);
}

public sealed class FactoryOutcomeLearningService : IFactoryOutcomeLearningService
{
    private readonly NevaxisZentraDbContext dbContext;

    public FactoryOutcomeLearningService(NevaxisZentraDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task RecordVerifiedOutcomeAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        FactoryOutcomeVerificationSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!snapshot.Passed)
        {
            return;
        }

        await UpsertLearningRecordAsync(
            rootCase,
            childCases,
            FactoryRecoveryLearningDisposition.VerifiedSuccess,
            null,
            cancellationToken);
    }

    public async Task RecordFailureIntelligenceAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        string disposition,
        string failureSummary,
        CancellationToken cancellationToken)
    {
        if (string.Equals(disposition, FactoryRecoveryLearningDisposition.VerifiedSuccess, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await UpsertLearningRecordAsync(
            rootCase,
            childCases,
            disposition,
            failureSummary,
            cancellationToken);
    }

    private async Task UpsertLearningRecordAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        string disposition,
        string? failureSummary,
        CancellationToken cancellationToken)
    {
        var impact = FactoryOutcomeImpactEngine.FromJson(rootCase.ImpactGraphJson);
        var pattern = FactoryRecoveryPatternEngine.Extract(rootCase, childCases, impact);
        var metadata = FactoryDecisionCenterProjectionEngine.ParseMetadata(rootCase.MetadataJson);
        var plan = FactoryOutcomeRecoveryPlanEngine.FromJson(rootCase.RecoveryPlanJson);
        var selected = FactoryOutcomeRecoveryPlanEngine.ResolveSelectedPlan(plan);

        var problemPattern = !string.IsNullOrWhiteSpace(pattern.OrderReference)
            ? $"order-risk:{pattern.OrderReference}"
            : $"outcome:{rootCase.PrimaryDomain}";

        int? recovered = null;
        if (metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.VerifiedProjection, out var verifiedEl) &&
            verifiedEl.TryGetInt32(out var verifiedQty))
        {
            recovered = verifiedQty;
        }
        else if (metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.ProjectedQuantity, out var projectedEl) &&
                 projectedEl.TryGetInt32(out var projectedQty))
        {
            recovered = projectedQty;
        }

        var required = metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.RequiredQuantity, out var requiredEl) &&
                       requiredEl.TryGetInt32(out var requiredQty)
            ? requiredQty
            : FactoryDecisionCenterConstants.RequiredQuantity;

        var deliveryProtected = metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.DeliveryProtected, out var protectedEl) &&
                                protectedEl.ValueKind == JsonValueKind.True;

        int? timeToRecovery = rootCase.StartedAtUtc.HasValue && rootCase.VerifiedAtUtc.HasValue
            ? (int)Math.Max(0, (rootCase.VerifiedAtUtc.Value - rootCase.StartedAtUtc.Value).TotalMinutes)
            : null;

        int? timeToVerified = timeToRecovery;
        int? timeToFirstAction = rootCase.StartedAtUtc.HasValue
            ? (int)Math.Max(0, (DateTimeOffset.UtcNow - rootCase.StartedAtUtc.Value).TotalMinutes)
            : null;

        var latest = await dbContext.FactoryOutcomeLearningRecords
            .Where(r => r.TenantId == rootCase.TenantId && r.SourceCaseId == rootCase.Id)
            .OrderByDescending(r => r.RecordVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is not null &&
            string.Equals(latest.LearningDisposition, disposition, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(latest.VerificationResult, disposition == FactoryRecoveryLearningDisposition.VerifiedSuccess
                ? DigitalWorkVerificationResults.Passed
                : DigitalWorkVerificationResults.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var recordVersion = (latest?.RecordVersion ?? 0) + 1;
        var idempotencyKey = $"{rootCase.Id:N}:{disposition}:v{recordVersion}";

        var failureModes = disposition == FactoryRecoveryLearningDisposition.VerifiedSuccess
            ? []
            : new List<string> { failureSummary ?? rootCase.Description };

        var evidenceStrength = FactoryRecoverySimilarityEngine.ResolveEvidenceStrength(
            disposition == FactoryRecoveryLearningDisposition.VerifiedSuccess ? 1 : 0,
            0);

        dbContext.FactoryOutcomeLearningRecords.Add(new FactoryOutcomeLearningRecord
        {
            Id = Guid.NewGuid(),
            TenantId = rootCase.TenantId,
            FacilityId = rootCase.FacilityId ?? Guid.Empty,
            SourceCaseId = rootCase.Id,
            RecordVersion = recordVersion,
            IdempotencyKey = idempotencyKey,
            LogicVersion = FactoryRecoveryIntelligenceConstants.LogicVersion,
            LearningDisposition = disposition,
            ProblemPattern = problemPattern,
            CausePattern = metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.PrimaryCause, out var causeEl)
                ? causeEl.GetString() ?? rootCase.Description
                : rootCase.Description,
            OutcomeType = pattern.OutcomeType,
            EventCategory = pattern.EventCategory,
            MachineCode = pattern.MachineCode,
            OrderReference = pattern.OrderReference,
            MaterialCode = pattern.MaterialCode,
            MoldCode = pattern.MoldCode,
            Severity = rootCase.Severity,
            PatternKey = pattern.PatternKey,
            PatternJson = FactoryRecoveryPatternEngine.Serialize(pattern),
            SelectedStrategyKey = selected?.Strategy ?? plan.SelectedPlanKey,
            AlternativeStrategyKey = plan.Plans.FirstOrDefault(p => p.PlanKey != plan.SelectedPlanKey)?.Strategy,
            ActionsJson = JsonSerializer.Serialize(selected?.Steps.Select(s => s.ActionKey) ?? []),
            WorkersJson = JsonSerializer.Serialize(pattern.WorkerCodes),
            BlockersJson = JsonSerializer.Serialize(pattern.BlockerProfile),
            FailureModesJson = JsonSerializer.Serialize(failureModes),
            ImpactBeforeJson = rootCase.ImpactGraphJson,
            ActualRecoveredQuantity = recovered,
            TimeToRecoveryMinutes = timeToRecovery,
            TimeToFirstActionMinutes = timeToFirstAction,
            TimeToVerifiedMinutes = timeToVerified,
            DeliveryProtected = deliveryProtected || recovered >= required,
            EvidenceStrength = evidenceStrength,
            VerificationResult = disposition == FactoryRecoveryLearningDisposition.VerifiedSuccess
                ? DigitalWorkVerificationResults.Passed
                : DigitalWorkVerificationResults.Failed,
            FailureSummary = failureSummary,
            VerifiedAtUtc = rootCase.VerifiedAtUtc ?? DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
