using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Erp.Logo;
using CrevaFactOS.Persistence.Erp.Mikro;
using CrevaFactOS.Persistence.Erp.Netsis;
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

    public static async Task EnsureErpLogoTestConnectionAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ErpConnectionProfiles
            .AsNoTracking()
            .AnyAsync(c => c.Id == DevelopmentPilotIds.ErpPilotLogoTestConnectionId, cancellationToken);

        if (existing)
        {
            return;
        }

        dbContext.ErpConnectionProfiles.Add(new ErpConnectionProfile
        {
            Id = DevelopmentPilotIds.ErpPilotLogoTestConnectionId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Provider = ErpProviderKeys.Logo,
            Name = "Development Logo Test Transport",
            ExternalCompanyId = LogoReadTransportTestFixture.FixtureCompanyId,
            ExternalCompanyCode = LogoReadTransportTestFixture.FixtureCompanyCode,
            BaseEndpoint = null,
            ConnectionMode = ErpConnectionModes.Api,
            CredentialReference = LogoCredentialReferences.TestFixture,
            Status = ErpConnectionStatuses.Configured,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureErpMikroTestConnectionAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ErpConnectionProfiles
            .AsNoTracking()
            .AnyAsync(c => c.Id == DevelopmentPilotIds.ErpPilotMikroTestConnectionId, cancellationToken);

        if (existing)
        {
            return;
        }

        dbContext.ErpConnectionProfiles.Add(new ErpConnectionProfile
        {
            Id = DevelopmentPilotIds.ErpPilotMikroTestConnectionId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Provider = ErpProviderKeys.Mikro,
            Name = "Development Mikro Test Transport",
            ExternalCompanyId = MikroReadTransportTestFixture.FixtureCompanyId,
            ExternalCompanyCode = MikroReadTransportTestFixture.FixtureCompanyCode,
            BaseEndpoint = null,
            ConnectionMode = ErpConnectionModes.Api,
            CredentialReference = MikroCredentialReferences.TestFixture,
            Status = ErpConnectionStatuses.Configured,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureErpNetsisTestConnectionAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ErpConnectionProfiles
            .AsNoTracking()
            .AnyAsync(c => c.Id == DevelopmentPilotIds.ErpPilotNetsisTestConnectionId, cancellationToken);

        if (existing)
        {
            return;
        }

        dbContext.ErpConnectionProfiles.Add(new ErpConnectionProfile
        {
            Id = DevelopmentPilotIds.ErpPilotNetsisTestConnectionId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Provider = ErpProviderKeys.Netsis,
            Name = "Development Netsis Test Transport",
            ExternalCompanyId = NetsisReadTransportTestFixture.FixtureCompanyId,
            ExternalCompanyCode = NetsisReadTransportTestFixture.FixtureCompanyCode,
            BaseEndpoint = null,
            ConnectionMode = ErpConnectionModes.Api,
            CredentialReference = NetsisCredentialReferences.TestFixture,
            Status = ErpConnectionStatuses.Configured,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
