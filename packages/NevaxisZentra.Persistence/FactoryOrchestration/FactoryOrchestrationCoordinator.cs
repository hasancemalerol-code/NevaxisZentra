using System.Text.Json;
using NevaxisZentra.Persistence.DigitalWorkforce;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.FactoryOrchestration;

public interface IFactoryOrchestrationCoordinator
{
    Task<DigitalWorkCase> OrchestrateCoordinatorCaseAsync(
        DigitalWorkCase rootCase,
        FactoryDomainEvent triggerEvent,
        string subjectId,
        CancellationToken cancellationToken);
}

public sealed class FactoryOrchestrationCoordinator : IFactoryOrchestrationCoordinator
{
    private readonly IDigitalWorkforceRuntimeService runtime;
    private readonly IDigitalWorkforceRepository repository;
    private readonly NevaxisZentraDbContext dbContext;

    public FactoryOrchestrationCoordinator(
        IDigitalWorkforceRuntimeService runtime,
        IDigitalWorkforceRepository repository,
        NevaxisZentraDbContext dbContext)
    {
        this.runtime = runtime;
        this.repository = repository;
        this.dbContext = dbContext;
    }

    public async Task<DigitalWorkCase> OrchestrateCoordinatorCaseAsync(
        DigitalWorkCase rootCase,
        FactoryDomainEvent triggerEvent,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var specialistEvents = ResolveSpecialistEvents(triggerEvent);
        var childCases = new List<DigitalWorkCase>();

        var newDependencies = new List<FactoryCaseDependency>();

        var existingChildren = await dbContext.DigitalWorkCases
            .Include(c => c.PrimaryWorker)
            .Where(c => c.OrchestrationRootCaseId == rootCase.Id && c.Id != rootCase.Id)
            .ToListAsync(cancellationToken);

        foreach (var specialist in specialistEvents)
        {
            var workerId = DigitalWorkforceEventDispatcher.ResolveWorkerId(specialist.EventType);
            if (existingChildren.Any(c => c.PrimaryWorkerId == workerId) ||
                childCases.Any(c => c.PrimaryWorkerId == workerId))
            {
                continue;
            }

            var child = await runtime.TriggerCaseAsync(
                new TriggerWorkCaseRequest(
                    specialist.Title,
                    specialist.Description,
                    specialist.EventType,
                    triggerEvent.SourceClassification,
                    triggerEvent.Severity,
                    workerId,
                    triggerEvent.FacilityId,
                    specialist.MetadataJson),
                subjectId,
                cancellationToken);

            child.ParentCaseId = rootCase.Id;
            child.OrchestrationRootCaseId = rootCase.Id;
            child.CorrelationKey = rootCase.CorrelationKey;
            child.PrimaryDomain = FactoryCaseCorrelationEngine.ResolvePrimaryDomain(specialist.EventType);
            child.ParticipatingDomainsJson = JsonSerializer.Serialize(
                FactoryCaseCorrelationEngine.ResolveParticipatingDomains(specialist.EventType, specialist.MetadataJson));
            child.UpdatedAtUtc = DateTimeOffset.UtcNow;
            childCases.Add(child);

            FactoryDependencyGraphBuilder.AddChildCaseEdge(
                newDependencies,
                rootCase.TenantId,
                triggerEvent.FacilityId,
                rootCase.Id,
                child.PrimaryDomain,
                child.Id,
                child.PrimaryWorkerId);
        }

        if (newDependencies.Count > 0)
        {
            dbContext.FactoryCaseDependencies.AddRange(newDependencies);
        }

        rootCase.MetadataJson = JsonSerializer.Serialize(new
        {
            allSubcasesResolved = childCases.All(c => c.Status == DigitalWorkCaseStatuses.Closed),
            childCaseIds = childCases.Select(c => c.Id).ToList(),
            orchestrated = true,
        });
        rootCase.CurrentBlockerCode = childCases.Any(c => c.Status != DigitalWorkCaseStatuses.Closed)
            ? FactoryCaseBlockerCodes.WaitingHuman
            : FactoryCaseBlockerCodes.None;
        rootCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return rootCase;
    }

    private static List<FactoryDomainEvent> ResolveSpecialistEvents(FactoryDomainEvent trigger)
    {
        var events = new List<FactoryDomainEvent>();
        var primary = FactoryCaseCorrelationEngine.ResolvePrimaryDomain(trigger.EventType);

        if (primary is FactoryOrchestrationDomains.Maintenance or FactoryOrchestrationDomains.Iot)
        {
            events.Add(trigger with { EventType = "maintenance.machine-fault", Title = $"Bakım: {trigger.Title}" });
            events.Add(trigger with
            {
                EventType = "production.recovery",
                Title = $"Üretim kurtarma: {trigger.Title}",
                MetadataJson = """{"impactedDomains":["production","planning"]}""",
            });
        }
        else if (primary is FactoryOrchestrationDomains.Material)
        {
            events.Add(trigger with { EventType = "material.shortage", Title = $"Malzeme: {trigger.Title}" });
            events.Add(trigger with { EventType = "supplier.recovery", Title = $"Tedarik: {trigger.Title}" });
        }
        else if (primary is FactoryOrchestrationDomains.Quality)
        {
            events.Add(trigger with { EventType = "quality.ncr", Title = $"Kalite: {trigger.Title}" });
            events.Add(trigger with { EventType = "production.hold", Title = $"Üretim hold: {trigger.Title}" });
        }
        else if (primary is FactoryOrchestrationDomains.Mold)
        {
            events.Add(trigger with { EventType = "mold.lifecycle", Title = $"Kalıp: {trigger.Title}" });
            events.Add(trigger with { EventType = "quality.verification", Title = $"Kalite doğrulama: {trigger.Title}" });
        }
        else
        {
            events.Add(trigger);
        }

        return events;
    }
}
