using NevaxisZentra.Persistence.GoldenDemoP1;

namespace NevaxisZentra.Persistence.GoldenDemoP2B;

public interface IGoldenDemoP2BService
{
    Task<DigitalFactoryTeamSnapshot> GetDigitalFactoryTeamSnapshotAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken);
}

public sealed class GoldenDemoP2BService : IGoldenDemoP2BService
{
    private readonly IGoldenDemoP1Service p1Service;

    public GoldenDemoP2BService(IGoldenDemoP1Service p1Service) => this.p1Service = p1Service;

    public async Task<DigitalFactoryTeamSnapshot> GetDigitalFactoryTeamSnapshotAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken)
    {
        var workspace = await p1Service.GetWorkspaceAsync(tenantId, facilityId, machineAssetId, cancellationToken);
        var plan = workspace.Plan;
        var orchestratorFinding = workspace.AgentFindings.FirstOrDefault(f => f.AgentCode == GoldenDemoAgentCodes.Orchestrator);

        return new DigitalFactoryTeamSnapshot(
            workspace.Event.Id,
            workspace.Event.MachineCode,
            GoldenDemoP2BAgentEvidenceEngine.Build(workspace, plan),
            GoldenDemoP2BOrchestratorEngine.Build(workspace, plan),
            GoldenDemoP2BActivityProjector.Build(workspace, plan),
            GoldenDemoP2BApprovalSheetEngine.Build(workspace, plan),
            GoldenDemoP2BPlanTraceabilityEngine.Build(workspace, plan),
            GoldenDemoP2BInterventionTimelineEngine.Build(workspace, plan, workspace.Verification));
    }
}
