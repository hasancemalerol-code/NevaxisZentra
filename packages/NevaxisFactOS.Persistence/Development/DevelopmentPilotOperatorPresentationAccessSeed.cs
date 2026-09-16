using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.Energy;
using NevaxisFactOS.Persistence.Export;
using NevaxisFactOS.Persistence.FactoryIntelligence;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.IotPlc;
using NevaxisFactOS.Persistence.OperationalRisk;
using NevaxisFactOS.Persistence.ShiftWorkforce;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Development;

/// <summary>
/// Idempotent repair seed for production-pilot-operator presentation/demo module access.
/// Runs outside the main bootstrap transaction so permissions survive re-seed rollbacks.
/// </summary>
internal static class DevelopmentPilotOperatorPresentationAccessSeed
{
    internal static readonly string[] PresentationPermissionKeys =
    [
        PresentationModePermissionKeys.Access,
        ProductionExecutionPermissionKeys.Read,
        ProductionPlanningPermissionKeys.Read,
        EdgePermissionKeys.Read,
        MoldPermissionKeys.Read,
        MaterialPermissionKeys.Read,
        MaintenancePermissionKeys.Read,
        QualityPermissionKeys.Read,
        EnergyPermissionKeys.Read,
        IotPlcPermissionKeys.Read,
        ProcessOeePermissionKeys.Read,
        ExportPermissionKeys.Read,
        AiTaskPermissionKeys.Read,
        AiTaskPermissionKeys.Create,
        AiTaskPermissionKeys.Review,
        AiTaskPermissionKeys.Acknowledge,
        AiTaskPermissionKeys.Assign,
        DigitalWorkforcePermissionKeys.Read,
        FactoryOrchestrationPermissionKeys.Read,
        FactoryOrchestrationPermissionKeys.Manage,
        FactoryOrchestrationPermissionKeys.Verify,
        DigitalWorkforcePermissionKeys.ApprovalDecide,
        GoldenDemoP1PermissionKeys.Read,
        GoldenDemoP1PermissionKeys.Manage,
        GoldenDemoP1PermissionKeys.Approve,
        GoldenDemoP1PermissionKeys.Execute,
        GoldenDemoP1PermissionKeys.Verify,
        GoldenDemoP2PermissionKeys.Read,
        GoldenDemoP2PermissionKeys.Reset,
        GoldenDemoCustomsFilePermissionKeys.Read,
        GoldenDemoCustomsFilePermissionKeys.Manage,
        GoldenDemoCustomsFilePermissionKeys.Execute,
        GoldenDemoCustomsFilePermissionKeys.Approve,
        GoldenDemoCustomsFilePermissionKeys.Verify,
        GoldenDemoCustomsFilePermissionKeys.Reset,
        OperationalRiskPermissionKeys.Read,
        FactoryIntelligencePermissionKeys.Read,
        MixedProductionPermissionKeys.Read,
        MixedProductionPermissionKeys.Manage,
        MixedProductionPermissionKeys.Operate,
        ShiftWorkforcePermissionKeys.Read,
        ShiftWorkforcePermissionKeys.Manage,
        ShiftOperationsPermissionKeys.Read,
        ShiftOperationsPermissionKeys.Manage,
    ];

    internal static async Task EnsureOperatorPresentationAccessAsync(
        NevaxisFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var operatorIdentity = await EnsureOperatorIdentitySubjectAsync(
            dbContext,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        var operatorMembership = await EnsureOperatorTenantMembershipAsync(
            dbContext,
            tenantId,
            operatorIdentity.Id,
            seedTimestampUtc,
            cancellationToken);
        var operatorRole = await EnsureOperatorTenantRoleAsync(
            dbContext,
            tenantId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureMembershipRoleAsync(
            dbContext,
            tenantId,
            operatorMembership.Id,
            operatorRole.Id,
            seedTimestampUtc,
            cancellationToken);

        var facility = await dbContext.Facilities
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenantId
                    && (candidate.Id == DevelopmentPilotIds.FacilityId
                        || candidate.Code == pilotOptions.FacilityCode.Trim()),
                cancellationToken);
        if (facility is not null)
        {
            await EnsureOperatorFacilityMembershipAsync(
                dbContext,
                tenantId,
                facility.Id,
                operatorMembership.Id,
                seedTimestampUtc,
                cancellationToken);
        }

        foreach (var permissionKey in PresentationPermissionKeys)
        {
            await EnsureRolePermissionAssignmentAsync(
                dbContext,
                tenantId,
                operatorRole.Id,
                permissionKey,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRolePermissionAssignmentAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        string permissionKey,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(p => p.Key == permissionKey, cancellationToken);
        if (permissionDefinition is null)
        {
            return;
        }

        var exists = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment => assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
        {
            TenantId = tenantId,
            TenantRoleId = tenantRoleId,
            PermissionDefinitionId = permissionDefinition.Id,
        });
    }

    private static async Task<IdentitySubject> EnsureOperatorIdentitySubjectAsync(
        NevaxisFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string operatorSubject = "production-pilot-operator";
        var issuer = pilotOptions.DevelopmentIssuer.Trim();

        var existing = await dbContext.IdentitySubjects
            .SingleOrDefaultAsync(
                identity => identity.ExternalIssuer == issuer && identity.ExternalSubject == operatorSubject,
                cancellationToken)
            ?? await dbContext.IdentitySubjects
                .SingleOrDefaultAsync(
                    identity => identity.Id == DevelopmentPilotIds.OperatorIdentitySubjectId,
                    cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var identity = new IdentitySubject
        {
            Id = DevelopmentPilotIds.OperatorIdentitySubjectId,
            ExternalIssuer = issuer,
            ExternalSubject = operatorSubject,
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.IdentitySubjects.Add(identity);
        return identity;
    }

    private static async Task<TenantMembership> EnsureOperatorTenantMembershipAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership => membership.TenantId == tenantId && membership.IdentitySubjectId == identitySubjectId,
                cancellationToken)
            ?? await dbContext.TenantMemberships
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    membership => membership.TenantId == tenantId
                        && membership.Id == DevelopmentPilotIds.OperatorTenantMembershipId,
                    cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var membership = new TenantMembership
        {
            Id = DevelopmentPilotIds.OperatorTenantMembershipId,
            TenantId = tenantId,
            IdentitySubjectId = identitySubjectId,
            MembershipReference = TenantMembership.CreateReference(DevelopmentPilotIds.OperatorTenantMembershipId),
            Status = TenantMembershipStatus.Active,
            AuthorizationVersion = 1,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureOperatorTenantRoleAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string operatorRoleKey = "production-execution-operator";

        var existing = await dbContext.TenantRoles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                role => role.TenantId == tenantId
                    && (role.Key == operatorRoleKey || role.Id == DevelopmentPilotIds.OperatorTenantRoleId),
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var role = new TenantRole
        {
            Id = DevelopmentPilotIds.OperatorTenantRoleId,
            TenantId = tenantId,
            Key = operatorRoleKey,
            DisplayName = "Production Execution Operator",
            Status = AuthorizationStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantRoles.Add(role);
        return role;
    }

    private static async Task EnsureMembershipRoleAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantMembershipId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.TenantMembershipRoles
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment => assignment.TenantId == tenantId
                    && assignment.TenantMembershipId == tenantMembershipId
                    && assignment.TenantRoleId == tenantRoleId,
                cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.TenantMembershipRoles.Add(new TenantMembershipRole
        {
            TenantId = tenantId,
            TenantMembershipId = tenantMembershipId,
            TenantRoleId = tenantRoleId,
            Status = AuthorizationStatus.Active,
            AssignedAtUtc = seedTimestampUtc,
        });
    }

    private static async Task EnsureOperatorFacilityMembershipAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.FacilityMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership => membership.TenantId == tenantId
                    && membership.FacilityId == facilityId
                    && membership.TenantMembershipId == tenantMembershipId,
                cancellationToken);

        if (existing is not null)
        {
            return;
        }

        dbContext.FacilityMemberships.Add(new FacilityMembership
        {
            Id = DevelopmentPilotIds.OperatorFacilityMembershipId,
            TenantId = tenantId,
            FacilityId = facilityId,
            TenantMembershipId = tenantMembershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }
}
