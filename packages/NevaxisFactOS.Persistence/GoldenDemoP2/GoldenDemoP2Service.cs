using NevaxisFactOS.Persistence.GoldenDemoP1;

namespace NevaxisFactOS.Persistence.GoldenDemoP2;

public interface IGoldenDemoP2Service
{
    Task<GoldenDemoResetResult> ResetPilotEventAsync(
        Guid tenantId,
        Guid facilityId,
        Guid productionEventId,
        string subjectId,
        CancellationToken cancellationToken);

    Task<GoldenDemoPresentationStatus> GetPresentationStatusAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken);
}

public sealed class GoldenDemoP2Service : IGoldenDemoP2Service
{
    private readonly IGoldenDemoP1Repository p1Repository;
    private readonly IGoldenDemoP2Repository p2Repository;

    public GoldenDemoP2Service(IGoldenDemoP1Repository p1Repository, IGoldenDemoP2Repository p2Repository)
    {
        this.p1Repository = p1Repository;
        this.p2Repository = p2Repository;
    }

    public async Task<GoldenDemoResetResult> ResetPilotEventAsync(
        Guid tenantId,
        Guid facilityId,
        Guid productionEventId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var evt = await p1Repository.GetEventAsync(productionEventId, cancellationToken)
            ?? throw new GoldenDemoP2Exception(
                GoldenDemoP2FailureCodes.EventNotFound,
                "Production event not found.");
        if (evt.TenantId != tenantId || evt.FacilityId != facilityId)
        {
            throw new GoldenDemoP2Exception(
                GoldenDemoP2FailureCodes.EventNotFound,
                "Production event not found.");
        }

        var previousStatus = evt.Status;
        await p2Repository.ResetPilotEventAsync(productionEventId, cancellationToken);
        return new GoldenDemoResetResult(
            productionEventId,
            evt.MachineCode,
            previousStatus,
            GoldenDemoP1EventStatuses.Active,
            DateTimeOffset.UtcNow);
    }

    public async Task<GoldenDemoPresentationStatus> GetPresentationStatusAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken)
    {
        var evt = await p1Repository.GetActiveEventAsync(tenantId, facilityId, machineAssetId, cancellationToken)
            ?? throw new GoldenDemoP2Exception(
                GoldenDemoP2FailureCodes.EventNotFound,
                "No active golden demo production event found for this facility.");

        var baseline = await p1Repository.GetBaselineAsync(evt.Id, cancellationToken);
        var current = await p1Repository.GetObservationAsync(evt.Id, "current", cancellationToken)
            ?? throw new GoldenDemoP2Exception(
                GoldenDemoP2FailureCodes.EventNotFound,
                "Current observation missing.");
        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, current);
        var recovery = ProductionRecoveryEngine.Calculate(decomposition, current, evt, DateTimeOffset.UtcNow);
        return GoldenDemoP2PresentationEngine.Build(evt, recovery.ProductionRecoveryGoodParts);
    }
}
