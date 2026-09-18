using System.Text.Json;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.FactoryOrchestration;

public interface IFactoryOrchestrationService
{
    Task<FactoryEventRouteResult> RouteEventAsync(FactoryDomainEvent eventData, string subjectId, CancellationToken cancellationToken);
    Task<FactoryCaseDetailSnapshot> GetCaseDetailAsync(Guid caseId, CancellationToken cancellationToken);
    Task<DigitalWorkCase> VerifyOutcomeAsync(Guid caseId, VerifyFactoryOutcomeRequest request, string subjectId, CancellationToken cancellationToken);
    Task<DigitalWorkCase> ReopenCaseAsync(Guid caseId, ReopenFactoryCaseRequest request, string subjectId, CancellationToken cancellationToken);
    Task<DigitalWorkCase> EscalateCaseAsync(Guid caseId, string reason, string subjectId, CancellationToken cancellationToken);
}

public sealed class FactoryOrchestrationService : IFactoryOrchestrationService
{
    private readonly IFactoryEventRouter eventRouter;
    private readonly IFactoryOrchestrationRepository repository;
    private readonly NevaxisZentraDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly IDigitalWorkforceRuntimeService runtime;

    public FactoryOrchestrationService(
        IFactoryEventRouter eventRouter,
        IFactoryOrchestrationRepository repository,
        NevaxisZentraDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        IDigitalWorkforceRuntimeService runtime)
    {
        this.eventRouter = eventRouter;
        this.repository = repository;
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.runtime = runtime;
    }

    public Task<FactoryEventRouteResult> RouteEventAsync(
        FactoryDomainEvent eventData, string subjectId, CancellationToken cancellationToken) =>
        eventRouter.RouteAsync(eventData, subjectId, cancellationToken);

    public Task<FactoryCaseDetailSnapshot> GetCaseDetailAsync(
        Guid caseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetCaseDetailCoreAsync(caseId, ct),
            cancellationToken);

    public Task<DigitalWorkCase> VerifyOutcomeAsync(
        Guid caseId, VerifyFactoryOutcomeRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => VerifyOutcomeCoreAsync(caseId, request, subjectId, ct),
            cancellationToken);

    public Task<DigitalWorkCase> ReopenCaseAsync(
        Guid caseId, ReopenFactoryCaseRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ReopenCaseCoreAsync(caseId, request, subjectId, ct),
            cancellationToken);

    public Task<DigitalWorkCase> EscalateCaseAsync(
        Guid caseId, string reason, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => EscalateCaseCoreAsync(caseId, reason, subjectId, ct),
            cancellationToken);

    private async Task<FactoryCaseDetailSnapshot> GetCaseDetailCoreAsync(
        Guid caseId, CancellationToken cancellationToken)
    {
        var root = await repository.GetRootCaseAsync(caseId, cancellationToken)
            ?? throw new FactoryOrchestrationException(FactoryOrchestrationFailureCodes.NotFound, "Case not found.");

        var children = await repository.ListChildCasesAsync(caseId, cancellationToken);
        var dependencies = await repository.ListDependenciesAsync(caseId, cancellationToken);
        var blocker = FactoryBlockerEngine.ResolvePrimaryBlocker(root, children);
        var recoveryPlan = FactoryRecoveryPlanEngine.FromJson(root.RecoveryPlanJson);
        var hasDeadlock = FactoryDeadlockDetector.HasCycle(dependencies);
        var conflicts = FactoryConflictDetector.Detect(
            new FactoryDomainEvent(root.Source, root.Title, root.Description, root.Severity,
                root.SourceClassification, root.FacilityId ?? Guid.Empty, "case", root.Id.ToString(),
                null, null, null, null, root.MetadataJson),
            root.MetadataJson);

        return new FactoryCaseDetailSnapshot(
            root,
            children,
            dependencies,
            [blocker],
            recoveryPlan,
            root.Activities.OrderByDescending(a => a.CreatedAtUtc).ToList(),
            children.FirstOrDefault(c => c.Status != DigitalWorkCaseStatuses.Closed)?.Title ?? "Doğrulama",
            FactoryBlockerEngine.BuildBlockerExplanation(blocker, root),
            hasDeadlock,
            conflicts.Count > 0);
    }

    private async Task<DigitalWorkCase> VerifyOutcomeCoreAsync(
        Guid caseId, VerifyFactoryOutcomeRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var root = await dbContext.DigitalWorkCases.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            ?? throw new FactoryOrchestrationException(FactoryOrchestrationFailureCodes.NotFound, "Case not found.");

        if (root.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new FactoryOrchestrationException(FactoryOrchestrationFailureCodes.ConcurrencyConflict, "Concurrency conflict.");
        }

        var children = await repository.ListChildCasesAsync(caseId, cancellationToken);
        if (children.Any(c => c.Status != DigitalWorkCaseStatuses.Closed))
        {
            throw new FactoryOrchestrationException(
                FactoryOrchestrationFailureCodes.OutcomeNotVerified,
                "Alt vakalar kapanmadan fabrika sonucu doğrulanamaz.");
        }

        if (!request.MachineOperational || !request.MaintenanceComplete ||
            !request.QualityAcceptable || !request.ProductionResumed)
        {
            root.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingMachineVerification;
            root.Status = DigitalWorkCaseStatuses.Executing;
            root.UpdatedAtUtc = DateTimeOffset.UtcNow;
            root.ConcurrencyToken++;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new FactoryOrchestrationException(
                FactoryOrchestrationFailureCodes.OutcomeNotVerified,
                "Doğrulanmış fabrika sonucu kriterleri karşılanmadı.");
        }

        root.OutcomeVerificationJson = JsonSerializer.Serialize(request);
        root.VerificationState = DigitalWorkVerificationResults.Passed;
        root.VerifiedAtUtc = DateTimeOffset.UtcNow;
        root.CurrentBlockerCode = FactoryCaseBlockerCodes.None;
        root.Status = DigitalWorkCaseStatuses.Closed;
        root.ClosedAtUtc = DateTimeOffset.UtcNow;
        root.UpdatedAtUtc = DateTimeOffset.UtcNow;
        root.ConcurrencyToken++;

        await runtime.RecordVerificationAsync(
            caseId,
            new RecordVerificationRequest(
                "factory-outcome-verified",
                "orchestration",
                "verified",
                "verified",
                root.SourceClassification,
                1m,
                DigitalWorkVerificationResults.Passed),
            subjectId,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return root;
    }

    private async Task<DigitalWorkCase> ReopenCaseCoreAsync(
        Guid caseId, ReopenFactoryCaseRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var root = await dbContext.DigitalWorkCases.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            ?? throw new FactoryOrchestrationException(FactoryOrchestrationFailureCodes.NotFound, "Case not found.");

        if (root.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new FactoryOrchestrationException(FactoryOrchestrationFailureCodes.ConcurrencyConflict, "Concurrency conflict.");
        }

        root.Status = DigitalWorkCaseStatuses.Executing;
        root.ClosedAtUtc = null;
        root.VerifiedAtUtc = null;
        root.VerificationState = DigitalWorkVerificationResults.Pending;
        root.ReopenCount++;
        root.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingHuman;
        root.BlockerSinceUtc = DateTimeOffset.UtcNow;
        root.Description = $"{root.Description}\n[reopen] {request.Reason}";
        root.UpdatedAtUtc = DateTimeOffset.UtcNow;
        root.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return root;
    }

    private async Task<DigitalWorkCase> EscalateCaseCoreAsync(
        Guid caseId, string reason, string subjectId, CancellationToken cancellationToken)
    {
        var root = await dbContext.DigitalWorkCases.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            ?? throw new FactoryOrchestrationException(FactoryOrchestrationFailureCodes.NotFound, "Case not found.");

        root.Severity = "critical";
        root.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingHuman;
        root.Description = $"{root.Description}\n[escalated] {reason}";
        root.UpdatedAtUtc = DateTimeOffset.UtcNow;
        root.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return root;
    }
}
