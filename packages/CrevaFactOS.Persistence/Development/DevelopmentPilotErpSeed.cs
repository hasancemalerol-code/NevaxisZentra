using CrevaFactOS.Persistence.Erp;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotErpSeed
{
    public static async Task EnsureErpSimulatorConnectionAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ErpConnectionProfiles
            .AsNoTracking()
            .AnyAsync(c => c.Id == DevelopmentPilotIds.ErpPilotSimulatorConnectionId, cancellationToken);

        if (existing)
        {
            return;
        }

        dbContext.ErpConnectionProfiles.Add(new ErpConnectionProfile
        {
            Id = DevelopmentPilotIds.ErpPilotSimulatorConnectionId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Provider = ErpProviderKeys.Simulator,
            Name = "Development ERP Simulator",
            ExternalCompanyId = "SIM-FACTORY-01",
            ExternalCompanyCode = "SIM-FACTORY-01",
            BaseEndpoint = null,
            ConnectionMode = ErpConnectionModes.Simulator,
            CredentialReference = null,
            Status = ErpConnectionStatuses.Configured,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
