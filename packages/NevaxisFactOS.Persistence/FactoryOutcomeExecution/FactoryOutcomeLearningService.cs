using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public interface IFactoryOutcomeLearningService
{
    Task RecordVerifiedOutcomeAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        FactoryOutcomeVerificationSnapshot snapshot,
        CancellationToken cancellationToken);
}

public sealed class FactoryOutcomeLearningService : IFactoryOutcomeLearningService
{
    private readonly NevaxisFactOSDbContext dbContext;

    public FactoryOutcomeLearningService(NevaxisFactOSDbContext dbContext)
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

        var metadata = FactoryDecisionCenterProjectionEngine.ParseMetadata(rootCase.MetadataJson);
        var plan = FactoryOutcomeRecoveryPlanEngine.FromJson(rootCase.RecoveryPlanJson);
        var selected = plan.Plans.FirstOrDefault(p => p.PlanKey == plan.SelectedPlanKey);
        var problemPattern = metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.OrderReference, out var orderEl)
            ? $"order-risk:{orderEl.GetString()}"
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

        int? timeToRecovery = rootCase.StartedAtUtc.HasValue && rootCase.VerifiedAtUtc.HasValue
            ? (int)Math.Max(0, (rootCase.VerifiedAtUtc.Value - rootCase.StartedAtUtc.Value).TotalMinutes)
            : null;

        var exists = await dbContext.FactoryOutcomeLearningRecords.AnyAsync(
            r => r.SourceCaseId == rootCase.Id,
            cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.FactoryOutcomeLearningRecords.Add(new FactoryOutcomeLearningRecord
        {
            Id = Guid.NewGuid(),
            TenantId = rootCase.TenantId,
            FacilityId = rootCase.FacilityId ?? Guid.Empty,
            SourceCaseId = rootCase.Id,
            ProblemPattern = problemPattern,
            CausePattern = metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.PrimaryCause, out var causeEl)
                ? causeEl.GetString() ?? rootCase.Description
                : rootCase.Description,
            SelectedStrategyKey = selected?.Strategy ?? plan.SelectedPlanKey,
            AlternativeStrategyKey = plan.Plans.FirstOrDefault(p => p.PlanKey != plan.SelectedPlanKey)?.Strategy,
            ActionsJson = JsonSerializer.Serialize(selected?.Steps.Select(s => s.ActionKey) ?? []),
            ActualRecoveredQuantity = recovered,
            TimeToRecoveryMinutes = timeToRecovery,
            VerificationResult = DigitalWorkVerificationResults.Passed,
            FailureSummary = null,
            VerifiedAtUtc = rootCase.VerifiedAtUtc ?? DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
