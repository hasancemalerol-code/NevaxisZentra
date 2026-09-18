namespace NevaxisZentra.Persistence.DigitalWorkforce;

public sealed record DigitalWorkforceDomainEvent(
    string EventType,
    string Title,
    string Description,
    string Severity,
    string SourceClassification,
    Guid? FacilityId,
    string? MetadataJson);

public interface IDigitalWorkforceEventDispatcher
{
    Task<DigitalWorkCase> DispatchAsync(
        DigitalWorkforceDomainEvent domainEvent,
        string subjectId,
        CancellationToken cancellationToken);
}

public sealed class DigitalWorkforceEventDispatcher : IDigitalWorkforceEventDispatcher
{
    private readonly IDigitalWorkforceRuntimeService runtime;
    private readonly IDigitalWorkforceWorkerFlowEngine flowEngine;

    public DigitalWorkforceEventDispatcher(
        IDigitalWorkforceRuntimeService runtime,
        IDigitalWorkforceWorkerFlowEngine flowEngine)
    {
        this.runtime = runtime;
        this.flowEngine = flowEngine;
    }

    public async Task<DigitalWorkCase> DispatchAsync(
        DigitalWorkforceDomainEvent domainEvent,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var workerId = ResolveWorkerId(domainEvent.EventType);
        var workCase = await runtime.TriggerCaseAsync(
            new TriggerWorkCaseRequest(
                domainEvent.Title,
                domainEvent.Description,
                domainEvent.EventType,
                domainEvent.SourceClassification,
                domainEvent.Severity,
                workerId,
                domainEvent.FacilityId,
                domainEvent.MetadataJson),
            subjectId,
            cancellationToken);

        return await flowEngine.RunDeterministicFlowAsync(workCase.Id, domainEvent.EventType, subjectId, cancellationToken);
    }

    public static Guid ResolveWorkerId(string eventType) => eventType switch
    {
        var e when e.StartsWith("production.", StringComparison.OrdinalIgnoreCase) =>
            DevelopmentPilotDigitalWorkforceIds.WorkerProductionRecovery,
        var e when e.StartsWith("maintenance.", StringComparison.OrdinalIgnoreCase) =>
            DevelopmentPilotDigitalWorkforceIds.WorkerMaintenanceResolution,
        var e when e.StartsWith("material.", StringComparison.OrdinalIgnoreCase) =>
            DevelopmentPilotDigitalWorkforceIds.WorkerMaterialShortage,
        var e when e.StartsWith("quality.", StringComparison.OrdinalIgnoreCase) =>
            DevelopmentPilotDigitalWorkforceIds.WorkerQualityNcr,
        var e when e.StartsWith("supplier.", StringComparison.OrdinalIgnoreCase) =>
            DevelopmentPilotDigitalWorkforceIds.WorkerSupplierRecovery,
        var e when e.StartsWith("mold.", StringComparison.OrdinalIgnoreCase) =>
            DevelopmentPilotDigitalWorkforceIds.WorkerMoldLifecycle,
        var e when e.StartsWith("throughput.", StringComparison.OrdinalIgnoreCase) =>
            DevelopmentPilotDigitalWorkforceIds.WorkerThroughputOptimizer,
        var e when e.StartsWith("coordinator.", StringComparison.OrdinalIgnoreCase) =>
            DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator,
        _ => DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator,
    };
}
