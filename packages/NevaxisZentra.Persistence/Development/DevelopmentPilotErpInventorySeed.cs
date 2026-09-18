using NevaxisZentra.Persistence.Erp;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotErpInventorySeed
{
    internal static async Task EnsureErpInventoryPilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var ppMaterial = await dbContext.Materials
            .FirstOrDefaultAsync(
                m => m.Id == DevelopmentPilotIds.MaterialPilotPpId
                     && m.TenantId == tenantId
                     && m.FacilityId == facilityId,
                cancellationToken);

        if (ppMaterial is not null && string.IsNullOrWhiteSpace(ppMaterial.ExternalMaterialCode))
        {
            ppMaterial.ExternalMaterialCode = DevelopmentPilotIds.MaterialPilotPpErpProductCode;
            ppMaterial.UpdatedAtUtc = seedTimestampUtc;
        }

        if (await dbContext.CanonicalErpInventoryBalances.AsNoTracking()
                .AnyAsync(b => b.Id == DevelopmentPilotIds.ErpPilotPpInventoryBalanceId, cancellationToken))
        {
            return;
        }

        dbContext.CanonicalErpInventoryBalances.Add(new CanonicalErpInventoryBalance
        {
            Id = DevelopmentPilotIds.ErpPilotPpInventoryBalanceId,
            TenantId = tenantId,
            FacilityId = facilityId,
            ConnectionProfileId = DevelopmentPilotIds.ErpPilotSimulatorConnectionId,
            Provider = ErpProviderKeys.Simulator,
            ExternalProductId = DevelopmentPilotIds.MaterialPilotPpErpProductCode,
            ExternalProductCode = DevelopmentPilotIds.MaterialPilotPpErpProductCode,
            ExternalWarehouseId = "WH-MAIN",
            ExternalWarehouseCode = "WH-MAIN",
            QuantityOnHand = 400m,
            QuantityAvailable = 400m,
            QuantityReserved = 50m,
            QuantityOnOrder = 0m,
            Unit = "kg",
            SourceVersion = "pilot-seed-v1",
            SourceUpdatedAtUtc = seedTimestampUtc,
            ImportedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });
    }
}
