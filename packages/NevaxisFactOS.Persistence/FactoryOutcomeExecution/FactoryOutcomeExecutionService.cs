using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;
using NevaxisFactOS.Persistence.FactoryOrchestration;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public interface IFactoryOutcomeExecutionService
{
    Task InitializeActionsFromPlanAsync(DigitalWorkCase rootCase, CancellationToken cancellationToken);
    Task<FactoryOutcomeExecutionProgress> ExecutePendingActionsAsync(
        Guid rootCaseId, string subjectId, CancellationToken cancellationToken);
    Task<FactoryOutcomeExecutionProgress> GetProgressAsync(Guid rootCaseId, CancellationToken cancellationToken);
    Task<DigitalWorkCase> VerifyAndCloseAsync(
        Guid rootCaseId, int? measuredProjection, long concurrencyToken, string subjectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryOutcomeSimilarCase>> FindSimilarVerifiedCasesAsync(
        Guid facilityId, string problemPattern, CancellationToken cancellationToken);
}

public sealed class FactoryOutcomeExecutionService : IFactoryOutcomeExecutionService
{
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly IFactoryOutcomeExecutionRepository repository;
    private readonly IFactoryOutcomeAiTaskSyncService aiTaskSync;
    private readonly IFactoryOutcomeLearningService learningService;
    private readonly IDigitalWorkforceRuntimeService runtime;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public FactoryOutcomeExecutionService(
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        IFactoryOutcomeExecutionRepository repository,
        IFactoryOutcomeAiTaskSyncService aiTaskSync,
        IFactoryOutcomeLearningService learningService,
        IDigitalWorkforceRuntimeService runtime,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.repository = repository;
        this.aiTaskSync = aiTaskSync;
        this.learningService = learningService;
        this.runtime = runtime;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task InitializeActionsFromPlanAsync(DigitalWorkCase rootCase, CancellationToken cancellationToken)
    {
        await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await InitializeActionsFromPlanCoreAsync(rootCase, ct);
                return 0;
            },
            cancellationToken);
    }

    public Task<FactoryOutcomeExecutionProgress> ExecutePendingActionsAsync(
        Guid rootCaseId, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ExecutePendingActionsCoreAsync(rootCaseId, subjectId, ct),
            cancellationToken);

    public Task<FactoryOutcomeExecutionProgress> GetProgressAsync(
        Guid rootCaseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => BuildProgressAsync(rootCaseId, ct),
            cancellationToken);

    public Task<DigitalWorkCase> VerifyAndCloseAsync(
        Guid rootCaseId, int? measuredProjection, long concurrencyToken, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => VerifyAndCloseCoreAsync(rootCaseId, measuredProjection, concurrencyToken, subjectId, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryOutcomeSimilarCase>> FindSimilarVerifiedCasesAsync(
        Guid facilityId, string problemPattern, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryOutcomeSimilarCase>>(
            async ct =>
            {
                var tenantId = tenantContextAccessor.RequireTenantId();
                var records = await repository.FindSimilarLearningRecordsAsync(
                    tenantId, facilityId, problemPattern, 5, ct);
                return records.Select(r => new FactoryOutcomeSimilarCase(
                    r.Id,
                    r.ProblemPattern,
                    r.SelectedStrategyKey,
                    r.ActualRecoveredQuantity,
                    r.VerificationResult,
                    r.VerifiedAtUtc)).ToList();
            },
            cancellationToken);

    private async Task InitializeActionsFromPlanCoreAsync(
        DigitalWorkCase rootCase, CancellationToken cancellationToken)
    {
        var plan = FactoryOutcomeRecoveryPlanEngine.FromJson(rootCase.RecoveryPlanJson);
        var selected = FactoryOutcomeRecoveryPlanEngine.ResolveSelectedPlan(plan);
        if (selected is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var step in selected.Steps.OrderBy(s => s.Order))
        {
            var idempotencyKey = $"{rootCase.Id:N}:{step.ActionKey}";
            var exists = await repository.GetActionByKeyAsync(rootCase.TenantId, rootCase.Id, step.ActionKey, cancellationToken);
            if (exists is not null)
            {
                continue;
            }

            Guid? childCaseId = Guid.TryParse(step.LinkedCaseId, out var parsedChild) ? parsedChild : null;
            dbContext.FactoryOutcomeExecutionActions.Add(new FactoryOutcomeExecutionAction
            {
                Id = Guid.NewGuid(),
                TenantId = rootCase.TenantId,
                FacilityId = rootCase.FacilityId ?? Guid.Empty,
                RootCaseId = rootCase.Id,
                ChildCaseId = childCaseId == rootCase.Id ? null : childCaseId,
                ActionKey = step.ActionKey,
                WorkerCode = step.WorkerCode,
                Title = step.Title,
                ExecutionOrder = step.Order,
                Status = step.RequiresApproval
                    ? FactoryOutcomeExecutionActionStatuses.WaitingApproval
                    : FactoryOutcomeExecutionActionStatuses.Pending,
                IdempotencyKey = idempotencyKey,
                DependsOnActionKeysJson = JsonSerializer.Serialize(step.DependsOnActionKeys),
                MaxAttempts = 3,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ConcurrencyToken = 1,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<FactoryOutcomeExecutionProgress> ExecutePendingActionsCoreAsync(
        Guid rootCaseId, string subjectId, CancellationToken cancellationToken)
    {
        var rootCase = await LoadRootCaseAsync(rootCaseId, cancellationToken);
        if (rootCase.Approvals.Any(a => a.Status == DigitalWorkApprovalStatuses.Pending))
        {
            throw new FactoryOutcomeExecutionException(
                FactoryOutcomeExecutionFailureCodes.ApprovalRequired,
                "İnsan onayı bekleniyor.");
        }

        var actions = await repository.ListActionsAsync(rootCaseId, cancellationToken);
        var children = await dbContext.DigitalWorkCases
            .Include(c => c.PrimaryWorker)
            .Where(c => c.OrchestrationRootCaseId == rootCaseId && c.Id != rootCaseId)
            .ToListAsync(cancellationToken);

        foreach (var action in actions.Where(a =>
                     a.Status is FactoryOutcomeExecutionActionStatuses.Pending or FactoryOutcomeExecutionActionStatuses.Failed))
        {
            if (action.NextRetryAtUtc.HasValue && action.NextRetryAtUtc > DateTimeOffset.UtcNow)
            {
                continue;
            }

            if (!DependenciesSatisfied(action, actions))
            {
                action.Status = FactoryOutcomeExecutionActionStatuses.Blocked;
                action.UpdatedAtUtc = DateTimeOffset.UtcNow;
                continue;
            }

            if (action.Status == FactoryOutcomeExecutionActionStatuses.Failed &&
                action.AttemptCount >= action.MaxAttempts)
            {
                continue;
            }

            await ExecuteActionAsync(rootCase, action, children, subjectId, cancellationToken);
        }

        var progress = await BuildProgressAsync(rootCaseId, cancellationToken);
        if (progress.ReadyForVerification)
        {
            rootCase = await LoadRootCaseAsync(rootCaseId, cancellationToken);
            rootCase.Status = DigitalWorkCaseStatuses.Verifying;
            rootCase.CurrentStage = DigitalWorkCaseStages.Verify;
            rootCase.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingMachineVerification;
            rootCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
            rootCase.ConcurrencyToken++;
            await dbContext.SaveChangesAsync(cancellationToken);
            await aiTaskSync.SyncAfterExecutionProgressAsync(rootCase, children, subjectId, cancellationToken);
        }

        return progress;
    }

    private async Task ExecuteActionAsync(
        DigitalWorkCase rootCase,
        FactoryOutcomeExecutionAction action,
        IReadOnlyList<DigitalWorkCase> children,
        string subjectId,
        CancellationToken cancellationToken)
    {
        if (action.Status == FactoryOutcomeExecutionActionStatuses.Completed)
        {
            throw new FactoryOutcomeExecutionException(
                FactoryOutcomeExecutionFailureCodes.DuplicateExecution,
                "Aksiyon zaten tamamlandı.");
        }

        action.Status = FactoryOutcomeExecutionActionStatuses.Executing;
        action.AttemptCount++;
        action.StartedAtUtc = DateTimeOffset.UtcNow;
        action.UpdatedAtUtc = DateTimeOffset.UtcNow;
        action.ConcurrencyToken++;

        var child = action.ChildCaseId.HasValue
            ? children.FirstOrDefault(c => c.Id == action.ChildCaseId.Value)
            : children.FirstOrDefault(c => string.Equals(c.PrimaryWorker?.Code, action.WorkerCode, StringComparison.OrdinalIgnoreCase));

        try
        {
            if (child is not null)
            {
                child.Status = DigitalWorkCaseStatuses.Executing;
                child.CurrentStage = DigitalWorkCaseStages.Act;
                child.UpdatedAtUtc = DateTimeOffset.UtcNow;
                child.ConcurrencyToken++;

                await runtime.RecordVerificationAsync(
                    child.Id,
                    new RecordVerificationRequest(
                        ResolveVerificationType(action.WorkerCode),
                        FactoryOutcomeExecutionConstants.SourceModule,
                        "completed",
                        "completed",
                        child.SourceClassification,
                        0.94m,
                        DigitalWorkVerificationResults.Passed),
                    subjectId,
                    cancellationToken);

                child.Status = DigitalWorkCaseStatuses.Closed;
                child.VerificationState = DigitalWorkVerificationResults.Passed;
                child.ClosedAtUtc = DateTimeOffset.UtcNow;
                child.UpdatedAtUtc = DateTimeOffset.UtcNow;
                child.ConcurrencyToken++;
            }

            action.Status = FactoryOutcomeExecutionActionStatuses.Completed;
            action.CompletedAtUtc = DateTimeOffset.UtcNow;
            action.LastError = null;
            action.NextRetryAtUtc = null;
        }
        catch (Exception ex)
        {
            action.Status = FactoryOutcomeExecutionActionStatuses.Failed;
            action.LastError = ex.Message;
            action.NextRetryAtUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Min(30, action.AttemptCount * 2));
            rootCase.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingHuman;
            rootCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        action.UpdatedAtUtc = DateTimeOffset.UtcNow;
        action.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        await aiTaskSync.SyncActionCompletedAsync(rootCase, action, subjectId, cancellationToken);
    }

    private async Task<FactoryOutcomeExecutionProgress> BuildProgressAsync(
        Guid rootCaseId, CancellationToken cancellationToken)
    {
        var actions = await repository.ListActionsAsync(rootCaseId, cancellationToken);
        var rootCase = await dbContext.DigitalWorkCases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == rootCaseId, cancellationToken);

        var completed = actions.Count(a => a.Status == FactoryOutcomeExecutionActionStatuses.Completed);
        var failed = actions.Count(a => a.Status == FactoryOutcomeExecutionActionStatuses.Failed);
        var blocked = actions.Count(a => a.Status == FactoryOutcomeExecutionActionStatuses.Blocked);
        var ready = actions.Count > 0 && completed == actions.Count;

        return new FactoryOutcomeExecutionProgress(
            rootCaseId,
            actions.Count,
            completed,
            failed,
            blocked,
            rootCase?.CurrentBlockerCode,
            ready);
    }

    private async Task<DigitalWorkCase> VerifyAndCloseCoreAsync(
        Guid rootCaseId, int? measuredProjection, long concurrencyToken, string subjectId, CancellationToken cancellationToken)
    {
        var rootCase = await LoadRootCaseAsync(rootCaseId, cancellationToken);
        if (rootCase.ConcurrencyToken != concurrencyToken)
        {
            throw new FactoryOutcomeExecutionException(
                FactoryOutcomeExecutionFailureCodes.ConcurrencyConflict,
                "Eşzamanlılık çakışması.");
        }

        var children = await dbContext.DigitalWorkCases
            .Where(c => c.OrchestrationRootCaseId == rootCaseId && c.Id != rootCaseId)
            .ToListAsync(cancellationToken);
        var verifications = await dbContext.DigitalWorkVerifications
            .Where(v => v.CaseId == rootCaseId || children.Select(c => c.Id).Contains(v.CaseId))
            .ToListAsync(cancellationToken);

        var snapshot = FactoryOutcomeVerificationEngine.Evaluate(
            rootCase, children, verifications, measuredProjection, null);

        rootCase.OutcomeVerificationJson = FactoryOutcomeVerificationEngine.ToJson(snapshot);
        if (!FactoryOutcomeVerificationEngine.CanCloseOutcomeCase(rootCase, children, verifications, snapshot))
        {
            rootCase.Status = DigitalWorkCaseStatuses.Executing;
            rootCase.VerificationState = DigitalWorkVerificationResults.Failed;
            rootCase.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingMachineVerification;
            rootCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
            rootCase.ConcurrencyToken++;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new FactoryOutcomeExecutionException(
                FactoryOutcomeExecutionFailureCodes.VerificationFailed,
                snapshot.Summary);
        }

        rootCase.VerificationState = DigitalWorkVerificationResults.Passed;
        rootCase.VerifiedAtUtc = DateTimeOffset.UtcNow;
        rootCase.Status = DigitalWorkCaseStatuses.Closed;
        rootCase.CurrentStage = DigitalWorkCaseStages.Close;
        rootCase.ClosedAtUtc = DateTimeOffset.UtcNow;
        rootCase.CurrentBlockerCode = FactoryCaseBlockerCodes.None;
        rootCase.Outcome = "verified";
        rootCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        rootCase.ConcurrencyToken++;

        await runtime.RecordVerificationAsync(
            rootCaseId,
            new RecordVerificationRequest(
                "outcome-verified",
                FactoryOutcomeExecutionConstants.SourceModule,
                measuredProjection?.ToString(CultureInfo.InvariantCulture) ?? "required",
                measuredProjection?.ToString(CultureInfo.InvariantCulture) ?? "measured",
                rootCase.SourceClassification,
                0.96m,
                DigitalWorkVerificationResults.Passed),
            subjectId,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await learningService.RecordVerifiedOutcomeAsync(rootCase, children, snapshot, cancellationToken);
        await aiTaskSync.SyncAfterOutcomeVerifiedAsync(rootCase, subjectId, cancellationToken);
        return rootCase;
    }

    private async Task<DigitalWorkCase> LoadRootCaseAsync(Guid rootCaseId, CancellationToken cancellationToken) =>
        await dbContext.DigitalWorkCases
            .Include(c => c.Approvals)
            .FirstOrDefaultAsync(c => c.Id == rootCaseId, cancellationToken)
        ?? throw new FactoryOutcomeExecutionException(FactoryOutcomeExecutionFailureCodes.NotFound, "Sonuç vakası bulunamadı.");

    private static bool DependenciesSatisfied(
        FactoryOutcomeExecutionAction action,
        IReadOnlyList<FactoryOutcomeExecutionAction> actions)
    {
        var dependencies = JsonSerializer.Deserialize<List<string>>(action.DependsOnActionKeysJson) ?? [];
        return dependencies.All(dep =>
            actions.Any(a => a.ActionKey == dep && a.Status == FactoryOutcomeExecutionActionStatuses.Completed));
    }

    private static string ResolveVerificationType(string workerCode) => workerCode switch
    {
        DigitalWorkforceWorkerCodes.ProductionRecovery => "production-plan-validated",
        DigitalWorkforceWorkerCodes.MaintenanceResolution => "machine-operational",
        DigitalWorkforceWorkerCodes.MaterialShortage => "material-available",
        DigitalWorkforceWorkerCodes.QualityNcr => "quality-outcome-verified",
        _ => "outcome-verified",
    };
}
