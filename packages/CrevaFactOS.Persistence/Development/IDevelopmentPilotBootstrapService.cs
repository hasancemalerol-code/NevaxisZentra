namespace CrevaFactOS.Persistence.Development;

public interface IDevelopmentPilotBootstrapService
{
    Task<DevelopmentPilotBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken = default);
}

public sealed record DevelopmentPilotBootstrapResult(
    bool Executed,
    bool AlreadyInitialized,
    Guid TenantId,
    Guid FacilityId,
    Guid TenantMembershipId,
    int ProductionPlanCount);
