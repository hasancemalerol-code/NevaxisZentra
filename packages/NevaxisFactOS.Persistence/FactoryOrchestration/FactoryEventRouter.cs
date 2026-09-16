using System.Text.Json;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;
using NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.FactoryOrchestration;

public interface IFactoryEventRouter
{
    Task<FactoryEventRouteResult> RouteAsync(FactoryDomainEvent eventData, string subjectId, CancellationToken cancellationToken);
}

public sealed class FactoryEventRouter : IFactoryEventRouter
{
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly IDigitalWorkforceRuntimeService runtime;
    private readonly IFactoryOrchestrationCoordinator coordinator;
    private readonly IFactoryOutcomeExecutionService outcomeExecution;
    private readonly IFactoryOutcomeAiTaskSyncService outcomeAiTaskSync;
    private readonly IFactoryRecoveryIntelligenceService recoveryIntelligence;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public FactoryEventRouter(
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        IDigitalWorkforceRuntimeService runtime,
        IFactoryOrchestrationCoordinator coordinator,
        IFactoryOutcomeExecutionService outcomeExecution,
        IFactoryOutcomeAiTaskSyncService outcomeAiTaskSync,
        IFactoryRecoveryIntelligenceService recoveryIntelligence,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.runtime = runtime;
        this.coordinator = coordinator;
        this.outcomeExecution = outcomeExecution;
        this.outcomeAiTaskSync = outcomeAiTaskSync;
        this.recoveryIntelligence = recoveryIntelligence;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public Task<FactoryEventRouteResult> RouteAsync(
        FactoryDomainEvent eventData, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RouteCoreAsync(eventData, subjectId, ct),
            cancellationToken);

    private async Task<FactoryEventRouteResult> RouteCoreAsync(
        FactoryDomainEvent eventData, string subjectId, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var existingReceipt = await dbContext.FactoryOutcomeEventReceipts
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.FacilityId == eventData.FacilityId &&
                     r.SourceType == eventData.SourceType &&
                     r.SourceId == eventData.SourceId,
                cancellationToken);
        if (existingReceipt is not null)
        {
            var receiptCase = await dbContext.DigitalWorkCases
                .FirstOrDefaultAsync(c => c.Id == existingReceipt.RootCaseId, cancellationToken)
                ?? throw new InvalidOperationException("Correlated case missing for duplicate event.");
            return new FactoryEventRouteResult(
                receiptCase,
                false,
                true,
                receiptCase.CorrelationKey ?? string.Empty,
                JsonSerializer.Deserialize<List<string>>(receiptCase.ParticipatingDomainsJson) ?? []);
        }

        var correlationKey = FactoryCaseCorrelationEngine.BuildCorrelationKey(eventData);
        var existingCorrelation = await dbContext.FactoryEventCorrelations
            .FirstOrDefaultAsync(c => c.CorrelationKey == correlationKey, cancellationToken);

        DigitalWorkCase? existingCase = null;
        if (existingCorrelation is not null)
        {
            existingCase = await dbContext.DigitalWorkCases
                .FirstOrDefaultAsync(c => c.Id == existingCorrelation.RootCaseId, cancellationToken);
        }

        var qualification = FactoryOutcomeEventQualificationEngine.Qualify(eventData, existingCase);
        if (!qualification.IsQualified)
        {
            throw new FactoryOrchestrationException(
                FactoryOrchestrationFailureCodes.EventNotQualified,
                qualification.RejectionReason ?? "Olay sonuç vakası için uygun değil.");
        }

        if (existingCase is not null && qualification.ShouldEnrichExisting)
        {
            existingCorrelation!.LastSeenAtUtc = DateTimeOffset.UtcNow;
            existingCase.Description = $"{existingCase.Description}\n[enriched] {eventData.Description}";
            existingCase.ImpactGraphJson = FactoryOutcomeImpactEngine.ToJson(FactoryOutcomeImpactEngine.Evaluate(eventData));
            existingCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await RecordEventReceiptAsync(tenantId, eventData, existingCase.Id, false, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new FactoryEventRouteResult(
                existingCase,
                false,
                true,
                correlationKey,
                JsonSerializer.Deserialize<List<string>>(existingCase.ParticipatingDomainsJson) ?? []);
        }

        var coordinatorEvent = eventData with
        {
            EventType = $"coordinator.{eventData.EventType}",
            Title = $"Fabrika vakası: {eventData.Title}",
        };

        var workCase = await runtime.TriggerCaseAsync(
            new TriggerWorkCaseRequest(
                coordinatorEvent.Title,
                coordinatorEvent.Description,
                coordinatorEvent.EventType,
                coordinatorEvent.SourceClassification,
                coordinatorEvent.Severity,
                DigitalWorkforceEventDispatcher.ResolveWorkerId(coordinatorEvent.EventType),
                coordinatorEvent.FacilityId,
                coordinatorEvent.MetadataJson),
            subjectId,
            cancellationToken);

        var impact = FactoryOutcomeImpactEngine.Evaluate(eventData);
        var participatingDomains = FactoryCaseCorrelationEngine.ResolveParticipatingDomains(
            eventData.EventType, eventData.MetadataJson);

        workCase.Source = FactoryOutcomeExecutionConstants.SourceModule;
        workCase.OrchestrationRootCaseId = workCase.Id;
        workCase.CorrelationKey = correlationKey;
        workCase.PrimaryDomain = FactoryCaseCorrelationEngine.ResolvePrimaryDomain(eventData.EventType);
        workCase.ParticipatingDomainsJson = JsonSerializer.Serialize(participatingDomains);
        workCase.ImpactGraphJson = FactoryOutcomeImpactEngine.ToJson(impact);
        workCase.BusinessImpact = impact.OutcomeAtRisk;
        workCase.RequiresApproval = true;
        workCase.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingHuman;
        workCase.BlockerSinceUtc = DateTimeOffset.UtcNow;
        workCase.StartedAtUtc = DateTimeOffset.UtcNow;

        var dependencies = FactoryDependencyGraphBuilder.BuildInitialGraph(
            workCase.TenantId,
            eventData.FacilityId,
            workCase.Id,
            eventData,
            impact.DomainImpacts,
            DigitalWorkforceEventDispatcher.ResolveWorkerId(
                FactoryCaseCorrelationEngine.ResolvePrimaryDomain(eventData.EventType) switch
                {
                    FactoryOrchestrationDomains.Maintenance => "maintenance.machine-fault",
                    FactoryOrchestrationDomains.Material => "material.shortage",
                    FactoryOrchestrationDomains.Quality => "quality.ncr",
                    FactoryOrchestrationDomains.Mold => "mold.lifecycle",
                    FactoryOrchestrationDomains.Supplier => "supplier.recovery",
                    _ => "production.recovery",
                }));

        if (FactoryDeadlockDetector.HasCycle(dependencies))
        {
            workCase.CurrentBlockerCode = FactoryCaseBlockerCodes.Deadlock;
        }

        dbContext.FactoryCaseDependencies.AddRange(dependencies);
        dbContext.FactoryEventCorrelations.Add(new FactoryEventCorrelation
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            FacilityId = eventData.FacilityId,
            CorrelationKey = correlationKey,
            RootCaseId = workCase.Id,
            SourceType = eventData.SourceType,
            SourceId = eventData.SourceId,
            EventCategory = eventData.EventType,
            FirstSeenAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow,
        });

        workCase.DependencyGraphJson = JsonSerializer.Serialize(dependencies.Select(d => new
        {
            from = d.FromNodeKey,
            to = d.ToNodeKey,
            edge = d.EdgeType,
        }));

        await coordinator.OrchestrateCoordinatorCaseAsync(workCase, eventData, subjectId, cancellationToken);

        var childCases = await dbContext.DigitalWorkCases
            .Include(c => c.PrimaryWorker)
            .Where(c => c.OrchestrationRootCaseId == workCase.Id && c.Id != workCase.Id)
            .ToListAsync(cancellationToken);

        var recoveryPlan = FactoryOutcomeRecoveryPlanEngine.Build(workCase, impact, childCases);
        var intelligence = await recoveryIntelligence.BuildForCaseAsync(workCase, childCases, cancellationToken);
        workCase.RecoveryPlanJson = FactoryOutcomeRecoveryPlanEngine.ToJson(recoveryPlan, intelligence);
        workCase.ClosureContractKey = DigitalWorkforceClosureContracts.Coordinator;

        await dbContext.SaveChangesAsync(cancellationToken);
        await outcomeExecution.InitializeActionsFromPlanAsync(workCase, cancellationToken);
        await outcomeAiTaskSync.EnsureActionTasksAsync(workCase, cancellationToken);
        await RecordEventReceiptAsync(tenantId, eventData, workCase.Id, true, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new FactoryEventRouteResult(workCase, true, false, correlationKey, participatingDomains);
    }

    private async Task RecordEventReceiptAsync(
        Guid tenantId,
        FactoryDomainEvent eventData,
        Guid rootCaseId,
        bool createdNewCase,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.FactoryOutcomeEventReceipts.AnyAsync(
            r => r.TenantId == tenantId &&
                 r.FacilityId == eventData.FacilityId &&
                 r.SourceType == eventData.SourceType &&
                 r.SourceId == eventData.SourceId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.FactoryOutcomeEventReceipts.Add(new FactoryOutcomeEventReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = eventData.FacilityId,
            SourceType = eventData.SourceType,
            SourceId = eventData.SourceId,
            RootCaseId = rootCaseId,
            CreatedNewCase = createdNewCase,
            ProcessedAtUtc = DateTimeOffset.UtcNow,
        });
    }
}
