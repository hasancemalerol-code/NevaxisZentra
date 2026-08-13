using CrevaFactOS.Persistence.Edge;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotEdgeSeed
{
    private const string InjectionAssetClass = "PlasticInjection";
    private const string LocalPilotProvenance =
        "Deterministic local pilot — plastic injection molding machine (simulated). Not real customer equipment.";

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

        await EnsureInjectionPilotMachineAsync(
            dbContext,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            "INJ-001",
            "Plastik Enjeksiyon Makinesi 001",
            "Pilot local injection machine #1 (Arburg Allrounder 470 S — simulated, 120 t, Ø45 mm screw).",
            "ASSET-PRESS-001",
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureInjectionPilotMachineAsync(
            dbContext,
            DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
            "INJ-002",
            "Plastik Enjeksiyon Makinesi 002",
            "Pilot local injection machine #2 (Engel e-victory 310 — simulated, 160 t, Ø50 mm screw).",
            "ASSET-INJ-002",
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureInjectionPilotMachineAsync(
            dbContext,
            DevelopmentPilotIds.EdgePilotInjectionMachine3Id,
            "INJ-003",
            "Plastik Enjeksiyon Makinesi 003",
            "Pilot local injection machine #3 (Haitian Mars III 250 — simulated, 250 t, Ø55 mm screw).",
            "ASSET-INJ-003",
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);

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

        await EnsureGatewayAssignmentAsync(
            dbContext,
            DevelopmentPilotIds.EdgePilotPressAssignmentId,
            DevelopmentPilotIds.EdgePilotPressMachineId,
            "INJ-001",
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureGatewayAssignmentAsync(
            dbContext,
            DevelopmentPilotIds.EdgePilotInjection2AssignmentId,
            DevelopmentPilotIds.EdgePilotInjectionMachine2Id,
            "INJ-002",
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureGatewayAssignmentAsync(
            dbContext,
            DevelopmentPilotIds.EdgePilotInjection3AssignmentId,
            DevelopmentPilotIds.EdgePilotInjectionMachine3Id,
            "INJ-003",
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureInjectionPilotMachineAsync(
        CrevaFactOSDbContext dbContext,
        Guid machineId,
        string machineCode,
        string displayName,
        string modelDescription,
        string assetReference,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var machine = await dbContext.MachineRegistries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == machineId, cancellationToken);

        if (machine is null)
        {
            dbContext.MachineRegistries.Add(new MachineRegistry
            {
                Id = machineId,
                TenantId = tenantId,
                FacilityId = facilityId,
                MachineCode = machineCode,
                Name = displayName,
                Description = $"{LocalPilotProvenance} {modelDescription}",
                AssetClass = InjectionAssetClass,
                AssetReference = assetReference,
                Status = MachineStatuses.Unknown,
                IsReadyForProduction = false,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
            });
            return;
        }

        var changed = false;
        if (!string.Equals(machine.MachineCode, machineCode, StringComparison.Ordinal))
        {
            machine.MachineCode = machineCode;
            changed = true;
        }

        if (!string.Equals(machine.Name, displayName, StringComparison.Ordinal))
        {
            machine.Name = displayName;
            changed = true;
        }

        var expectedDescription = $"{LocalPilotProvenance} {modelDescription}";
        if (!string.Equals(machine.Description, expectedDescription, StringComparison.Ordinal))
        {
            machine.Description = expectedDescription;
            changed = true;
        }

        if (!string.Equals(machine.AssetClass, InjectionAssetClass, StringComparison.Ordinal))
        {
            machine.AssetClass = InjectionAssetClass;
            changed = true;
        }

        if (machine.AssetReference is null)
        {
            machine.AssetReference = assetReference;
            changed = true;
        }

        if (changed)
        {
            machine.UpdatedAtUtc = seedTimestampUtc;
            machine.Version++;
        }
    }

    private static async Task EnsureGatewayAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid assignmentId,
        Guid machineId,
        string adapterKey,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.MachineGatewayAssignments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken);

        if (assignment is null)
        {
            dbContext.MachineGatewayAssignments.Add(new MachineGatewayAssignment
            {
                Id = assignmentId,
                TenantId = tenantId,
                FacilityId = facilityId,
                MachineId = machineId,
                GatewayId = DevelopmentPilotIds.EdgePilotSimulatorGatewayId,
                AdapterKey = adapterKey,
                Status = MachineAssignmentStatuses.Active,
                AssignedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
            });
            return;
        }

        if (!string.Equals(assignment.AdapterKey, adapterKey, StringComparison.Ordinal))
        {
            assignment.AdapterKey = adapterKey;
            assignment.UpdatedAtUtc = seedTimestampUtc;
        }
    }
}
