using NevaxisZentra.Persistence.GoldenDemoP1;

namespace NevaxisZentra.Persistence.GoldenDemoP3;

public interface IGoldenDemoP3Service
{
    Task<OutcomeSnapshot> GetOutcomeAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken);
}

public sealed class GoldenDemoP3Service : IGoldenDemoP3Service
{
    private readonly IGoldenDemoP1Service p1Service;

    public GoldenDemoP3Service(IGoldenDemoP1Service p1Service) => this.p1Service = p1Service;

    public async Task<OutcomeSnapshot> GetOutcomeAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken)
    {
        var workspace = await p1Service.GetWorkspaceAsync(tenantId, facilityId, machineAssetId, cancellationToken);
        return GoldenDemoP3OutcomeProjectionEngine.Build(workspace);
    }
}
