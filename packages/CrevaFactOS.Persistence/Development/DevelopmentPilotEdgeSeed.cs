using CrevaFactOS.Persistence.Edge;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotEdgeSeed
{
    internal static async Task EnsureEdgeSimulatorGatewayAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var gatewayExists = await dbContext.EdgeGateways
            .IgnoreQueryFilters()
            .AnyAsync(g => g.Id == DevelopmentPilotIds.EdgePilotSimulatorGatewayId, cancellationToken);

        if (!gatewayExists)
        {
            dbContext.EdgeGateways.Add(new EdgeGateway
            {
                Id = DevelopmentPilotIds.EdgePilotSimulatorGatewayId,
                TenantId = tenantId,
                FacilityId = facilityId,
                GatewayCode = "SIM-GW-001",
                Name = "Machine Simulator Gateway",
                Description = "Development-only deterministic simulator. Not a real PLC gateway.",
                VendorKey = EdgeGatewayVendorKeys.Simulator,
                Status = EdgeGatewayStatuses.Unknown,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
            });
        }

        var cncExists = await dbContext.MachineRegistries
            .IgnoreQueryFilters()
            .AnyAsync(m => m.Id == DevelopmentPilotIds.EdgePilotCncMachineId, cancellationToken);

        if (!cncExists)
        {
            dbContext.MachineRegistries.Add(new MachineRegistry
            {
                Id = DevelopmentPilotIds.EdgePilotCncMachineId,
                TenantId = tenantId,
                FacilityId = facilityId,
                MachineCode = "CNC-001",
                Name = "CNC Tezgâh 001",
                Description = "Pilot CNC machining center (simulated).",
                AssetClass = "CNC",
                AssetReference = "ASSET-CNC-001",
                Status = MachineStatuses.Unknown,
                IsReadyForProduction = false,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
            });
        }

        var pressExists = await dbContext.MachineRegistries
            .IgnoreQueryFilters()
            .AnyAsync(m => m.Id == DevelopmentPilotIds.EdgePilotPressMachineId, cancellationToken);

        if (!pressExists)
        {
            dbContext.MachineRegistries.Add(new MachineRegistry
            {
                Id = DevelopmentPilotIds.EdgePilotPressMachineId,
                TenantId = tenantId,
                FacilityId = facilityId,
                MachineCode = "PRESS-001",
                Name = "Pres Makinesi 001",
                Description = "Pilot hydraulic press (simulated).",
                AssetClass = "Press",
                AssetReference = "ASSET-PRESS-001",
                Status = MachineStatuses.Unknown,
                IsReadyForProduction = false,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
            });
        }

        var cncAssignmentExists = await dbContext.MachineGatewayAssignments
            .IgnoreQueryFilters()
            .AnyAsync(a => a.Id == DevelopmentPilotIds.EdgePilotCncAssignmentId, cancellationToken);

        if (!cncAssignmentExists)
        {
            dbContext.MachineGatewayAssignments.Add(new MachineGatewayAssignment
            {
                Id = DevelopmentPilotIds.EdgePilotCncAssignmentId,
                TenantId = tenantId,
                FacilityId = facilityId,
                MachineId = DevelopmentPilotIds.EdgePilotCncMachineId,
                GatewayId = DevelopmentPilotIds.EdgePilotSimulatorGatewayId,
                AdapterKey = "CNC-001",
                Status = MachineAssignmentStatuses.Active,
                AssignedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
            });
        }

        var pressAssignmentExists = await dbContext.MachineGatewayAssignments
            .IgnoreQueryFilters()
            .AnyAsync(a => a.Id == DevelopmentPilotIds.EdgePilotPressAssignmentId, cancellationToken);

        if (!pressAssignmentExists)
        {
            dbContext.MachineGatewayAssignments.Add(new MachineGatewayAssignment
            {
                Id = DevelopmentPilotIds.EdgePilotPressAssignmentId,
                TenantId = tenantId,
                FacilityId = facilityId,
                MachineId = DevelopmentPilotIds.EdgePilotPressMachineId,
                GatewayId = DevelopmentPilotIds.EdgePilotSimulatorGatewayId,
                AdapterKey = "PRESS-001",
                Status = MachineAssignmentStatuses.Active,
                AssignedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
            });
        }
    }
}
