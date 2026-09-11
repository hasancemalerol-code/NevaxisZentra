using System.Text.Json;
using CrevaFactOS.Persistence.DigitalWorkforce;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.FactoryOrchestration;

public interface IFactoryEventRouter
{
    Task<FactoryEventRouteResult> RouteAsync(FactoryDomainEvent eventData, string subjectId, CancellationToken cancellationToken);
}

public sealed class FactoryEventRouter : IFactoryEventRouter
{
    private readonly CrevaFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly IDigitalWorkforceRuntimeService runtime;
    private readonly IFactoryOrchestrationCoordinator coordinator;

    public FactoryEventRouter(
        CrevaFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        IDigitalWorkforceRuntimeService runtime,
        IFactoryOrchestrationCoordinator coordinator)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.runtime = runtime;
        this.coordinator = coordinator;
    }

    public Task<FactoryEventRouteResult> RouteAsync(
        FactoryDomainEvent eventData, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RouteCoreAsync(eventData, subjectId, ct),
            cancellationToken);

    private async Task<FactoryEventRouteResult> RouteCoreAsync(
        FactoryDomainEvent eventData, string subjectId, CancellationToken cancellationToken)
    {
        var correlationKey = FactoryCaseCorrelationEngine.BuildCorrelationKey(eventData);
        var existingCorrelation = await dbContext.FactoryEventCorrelations
            .FirstOrDefaultAsync(c => c.CorrelationKey == correlationKey, cancellationToken);

        if (existingCorrelation is not null)
        {
            var existingCase = await dbContext.DigitalWorkCases
                .FirstOrDefaultAsync(c => c.Id == existingCorrelation.RootCaseId, cancellationToken);
            if (existingCase is not null && existingCase.Status != DigitalWorkCaseStatuses.Closed)
            {
                existingCorrelation.LastSeenAtUtc = DateTimeOffset.UtcNow;
                existingCase.Description = $"{existingCase.Description}\n[enriched] {eventData.Description}";
                existingCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return new FactoryEventRouteResult(
                    existingCase,
                    false,
                    true,
                    correlationKey,
                    JsonSerializer.Deserialize<List<string>>(existingCase.ParticipatingDomainsJson) ?? []);
            }
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

        var impacts = FactoryDomainImpactEngine.Evaluate(eventData);
        var participatingDomains = FactoryCaseCorrelationEngine.ResolveParticipatingDomains(
            eventData.EventType, eventData.MetadataJson);

        workCase.OrchestrationRootCaseId = workCase.Id;
        workCase.CorrelationKey = correlationKey;
        workCase.PrimaryDomain = FactoryCaseCorrelationEngine.ResolvePrimaryDomain(eventData.EventType);
        workCase.ParticipatingDomainsJson = JsonSerializer.Serialize(participatingDomains);
        workCase.ImpactGraphJson = FactoryDomainImpactEngine.ToJson(impacts);
        workCase.RecoveryPlanJson = FactoryRecoveryPlanEngine.ToJson(
            FactoryRecoveryPlanEngine.Build(workCase, impacts, []));
        workCase.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingHuman;
        workCase.BlockerSinceUtc = DateTimeOffset.UtcNow;

        var dependencies = FactoryDependencyGraphBuilder.BuildInitialGraph(
            workCase.TenantId,
            eventData.FacilityId,
            workCase.Id,
            eventData,
            impacts,
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
        await dbContext.SaveChangesAsync(cancellationToken);

        return new FactoryEventRouteResult(workCase, true, false, correlationKey, participatingDomains);
    }
}
