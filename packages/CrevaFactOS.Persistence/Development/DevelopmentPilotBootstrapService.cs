using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Persistence.Development;

internal sealed class DevelopmentPilotBootstrapService(
    IOptions<DevelopmentPilotOptions> options,
    IOptions<PersistenceOptions> persistenceOptions)
    : IDevelopmentPilotBootstrapService
{
    public async Task<DevelopmentPilotBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken = default)
    {
        var pilotOptions = options.Value;

        if (!pilotOptions.Enabled)
        {
            return new DevelopmentPilotBootstrapResult(
                Executed: false,
                AlreadyInitialized: false,
                DevelopmentPilotIds.TenantId,
                DevelopmentPilotIds.FacilityId,
                DevelopmentPilotIds.TenantMembershipId,
                ProductionPlanCount: 0);
        }

        var bootstrapContext =
            DevelopmentPilotBootstrapDbContextFactory.Create(persistenceOptions.Value);
        await using var dbContext = bootstrapContext.DbContext;

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var seedTimestampUtc = DateTimeOffset.UtcNow;
        var tenant = await EnsureTenantAsync(
            dbContext,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);

        bootstrapContext.TenantContextAccessor.Set(
            new TenantContext(
                tenant.Id,
                tenant.TenantKey,
                DevelopmentPilotIds.TenantMembershipId));
        var identitySubject = await EnsureIdentitySubjectAsync(
            dbContext,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        var tenantMembership = await EnsureTenantMembershipAsync(
            dbContext,
            tenant.Id,
            identitySubject.Id,
            seedTimestampUtc,
            cancellationToken);
        var tenantRole = await EnsureTenantRoleAsync(
            dbContext,
            tenant.Id,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningReadPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningWritePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningReviewPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningApprovePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningSchedulePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningTransferPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningHandoffPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsureMembershipRoleAsync(
            dbContext,
            tenant.Id,
            tenantMembership.Id,
            tenantRole.Id,
            seedTimestampUtc,
            cancellationToken);
        var facility = await EnsureFacilityAsync(
            dbContext,
            tenant.Id,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        await EnsureFacilityMembershipAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            tenantMembership.Id,
            seedTimestampUtc,
            cancellationToken);

        // Second identity: planning-pilot-reviewer (approve/review role for maker-checker)
        var reviewerIdentity = await EnsureReviewerIdentitySubjectAsync(
            dbContext,
            pilotOptions,
            seedTimestampUtc,
            cancellationToken);
        var reviewerMembership = await EnsureReviewerTenantMembershipAsync(
            dbContext,
            tenant.Id,
            reviewerIdentity.Id,
            seedTimestampUtc,
            cancellationToken);
        var reviewerRole = await EnsureReviewerTenantRoleAsync(
            dbContext,
            tenant.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningReadPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningReviewPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningApprovePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningSchedulePermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningTransferPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePlanningHandoffPermissionAssignmentAsync(
            dbContext,
            tenant.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsureMembershipRoleAsync(
            dbContext,
            tenant.Id,
            reviewerMembership.Id,
            reviewerRole.Id,
            seedTimestampUtc,
            cancellationToken);
        await EnsureReviewerFacilityMembershipAsync(
            dbContext,
            tenant.Id,
            facility.Id,
            reviewerMembership.Id,
            seedTimestampUtc,
            cancellationToken);

        // Give planner and reviewer production execution read
        await EnsureExecutionReadPermissionAssignmentAsync(
            dbContext, tenant.Id, tenantRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionReadPermissionAssignmentAsync(
            dbContext, tenant.Id, reviewerRole.Id, seedTimestampUtc, cancellationToken);

        // Third identity: production-pilot-operator (full execution permissions)
        var operatorIdentity = await EnsureOperatorIdentitySubjectAsync(
            dbContext, pilotOptions, seedTimestampUtc, cancellationToken);
        var operatorMembership = await EnsureOperatorTenantMembershipAsync(
            dbContext, tenant.Id, operatorIdentity.Id, seedTimestampUtc, cancellationToken);
        var operatorRole = await EnsureOperatorTenantRoleAsync(
            dbContext, tenant.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionReadPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionWritePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionStartPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionPausePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionCompletePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionRecordPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionDowntimePermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionSetupPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionExceptionPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        // 5L: assign (supervisor-level), handover, exception.assign
        await EnsureExecutionAssignPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionHandoverPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsureExecutionExceptionAssignPermissionAssignmentAsync(
            dbContext, tenant.Id, operatorRole.Id, seedTimestampUtc, cancellationToken);
        await EnsurePilotProductionShiftsAsync(
            dbContext, tenant.Id, DevelopmentPilotIds.FacilityId, seedTimestampUtc, cancellationToken);
        await EnsureMembershipRoleAsync(
            dbContext, tenant.Id, operatorMembership.Id, operatorRole.Id,
            seedTimestampUtc, cancellationToken);
        await EnsureOperatorFacilityMembershipAsync(
            dbContext, tenant.Id, facility.Id, operatorMembership.Id, seedTimestampUtc, cancellationToken);

        var productionPlanCount = 0;
        if (pilotOptions.ProductionPlanSeedEnabled)
        {
            productionPlanCount = await EnsureProductionPlansAsync(
                dbContext,
                tenant.Id,
                facility.Id,
                pilotOptions.DevelopmentSubject,
                seedTimestampUtc,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DevelopmentPilotBootstrapResult(
            Executed: true,
            AlreadyInitialized: false,
            tenant.Id,
            facility.Id,
            tenantMembership.Id,
            productionPlanCount);
    }

    private static async Task<Tenant> EnsureTenantAsync(
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingTenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                tenant => tenant.Id == DevelopmentPilotIds.TenantId
                    || tenant.TenantKey == pilotOptions.TenantKey,
                cancellationToken);

        if (existingTenant is not null)
        {
            return existingTenant;
        }

        var tenant = new Tenant
        {
            Id = DevelopmentPilotIds.TenantId,
            TenantKey = pilotOptions.TenantKey.Trim(),
            DisplayName = pilotOptions.TenantName.Trim(),
            Status = TenantStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.Tenants.Add(tenant);
        return tenant;
    }

    private static async Task<IdentitySubject> EnsureIdentitySubjectAsync(
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var issuer = pilotOptions.DevelopmentIssuer.Trim();
        var subject = pilotOptions.DevelopmentSubject.Trim();

        var existingIdentity = await dbContext.IdentitySubjects
            .SingleOrDefaultAsync(
                identity =>
                    identity.ExternalIssuer == issuer
                    && identity.ExternalSubject == subject,
                cancellationToken);

        if (existingIdentity is not null)
        {
            return existingIdentity;
        }

        var identitySubject = new IdentitySubject
        {
            Id = DevelopmentPilotIds.IdentitySubjectId,
            ExternalIssuer = issuer,
            ExternalSubject = subject,
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.IdentitySubjects.Add(identitySubject);
        return identitySubject;
    }

    private static async Task<TenantMembership> EnsureTenantMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingMembership = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.IdentitySubjectId == identitySubjectId,
                cancellationToken);

        if (existingMembership is not null)
        {
            return existingMembership;
        }

        var membership = new TenantMembership
        {
            Id = DevelopmentPilotIds.TenantMembershipId,
            TenantId = tenantId,
            IdentitySubjectId = identitySubjectId,
            Status = TenantMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureTenantRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var roleKey = pilotOptions.RoleKey.Trim();

        var existingRole = await dbContext.TenantRoles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                role => role.TenantId == tenantId && role.Key == roleKey,
                cancellationToken);

        if (existingRole is not null)
        {
            return existingRole;
        }

        var tenantRole = new TenantRole
        {
            Id = DevelopmentPilotIds.TenantRoleId,
            TenantId = tenantId,
            Key = roleKey,
            DisplayName = pilotOptions.RoleDisplayName.Trim(),
            Status = AuthorizationStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantRoles.Add(tenantRole);
        return tenantRole;
    }

    private static async Task EnsurePlanningReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Read,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id
                        == DevelopmentPilotIds.PlanningReadPermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            throw new InvalidOperationException(
                "production.planning.read permission definition is missing.");
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
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

    private static async Task EnsurePlanningWritePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Write,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id
                        == DevelopmentPilotIds.PlanningWritePermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
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

    private static async Task EnsurePlanningReviewPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Review,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningReviewPermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
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

    private static async Task EnsurePlanningApprovePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Approve,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningApprovePermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
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

    private static async Task EnsurePlanningSchedulePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Schedule,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningSchedulePermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
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

    private static async Task EnsurePlanningTransferPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Transfer,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningTransferPermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
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

    private static async Task EnsureMembershipRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantMembershipId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingAssignment = await dbContext.TenantMembershipRoles
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantMembershipId == tenantMembershipId
                    && assignment.TenantRoleId == tenantRoleId,
                cancellationToken);

        if (existingAssignment)
        {
            return;
        }

        dbContext.TenantMembershipRoles.Add(new TenantMembershipRole
        {
            TenantId = tenantId,
            TenantMembershipId = tenantMembershipId,
            TenantRoleId = tenantRoleId,
        });
    }

    private static async Task<Facility> EnsureFacilityAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var facilityCode = pilotOptions.FacilityCode.Trim();

        var existingFacility = await dbContext.Facilities
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                facility =>
                    facility.TenantId == tenantId
                    && (facility.Id == DevelopmentPilotIds.FacilityId
                        || facility.Code == facilityCode),
                cancellationToken);

        if (existingFacility is not null)
        {
            return existingFacility;
        }

        var facility = new Facility
        {
            Id = DevelopmentPilotIds.FacilityId,
            TenantId = tenantId,
            Code = facilityCode,
            Name = pilotOptions.FacilityName.Trim(),
            Status = FacilityStatus.Active,
            TimeZoneId = pilotOptions.FacilityTimeZoneId.Trim(),
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        };

        dbContext.Facilities.Add(facility);
        return facility;
    }

    private static async Task EnsureFacilityMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingMembership = await dbContext.FacilityMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.FacilityId == facilityId
                    && membership.TenantMembershipId == tenantMembershipId,
                cancellationToken);

        if (existingMembership is not null)
        {
            return;
        }

        dbContext.FacilityMemberships.Add(new FacilityMembership
        {
            Id = DevelopmentPilotIds.FacilityMembershipId,
            TenantId = tenantId,
            FacilityId = facilityId,
            TenantMembershipId = tenantMembershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static async Task EnsurePlanningHandoffPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Key == ProductionPlanningPermissionKeys.Handoff,
                cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(
                    permission =>
                        permission.Id == DevelopmentPilotIds.PlanningHandoffPermissionDefinitionId,
                    cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var existingAssignment = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                assignment =>
                    assignment.TenantId == tenantId
                    && assignment.TenantRoleId == tenantRoleId
                    && assignment.PermissionDefinitionId == permissionDefinition.Id,
                cancellationToken);

        if (existingAssignment)
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

    private static async Task<IdentitySubject> EnsureReviewerIdentitySubjectAsync(
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string reviewerSubject = "planning-pilot-reviewer";
        var issuer = pilotOptions.DevelopmentIssuer.Trim();

        var existingIdentity = await dbContext.IdentitySubjects
            .SingleOrDefaultAsync(
                identity =>
                    identity.ExternalIssuer == issuer
                    && identity.ExternalSubject == reviewerSubject,
                cancellationToken)
            ?? await dbContext.IdentitySubjects
                .SingleOrDefaultAsync(
                    identity => identity.Id == DevelopmentPilotIds.ReviewerIdentitySubjectId,
                    cancellationToken);

        if (existingIdentity is not null)
        {
            return existingIdentity;
        }

        var identitySubject = new IdentitySubject
        {
            Id = DevelopmentPilotIds.ReviewerIdentitySubjectId,
            ExternalIssuer = issuer,
            ExternalSubject = reviewerSubject,
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.IdentitySubjects.Add(identitySubject);
        return identitySubject;
    }

    private static async Task<TenantMembership> EnsureReviewerTenantMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingMembership = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.IdentitySubjectId == identitySubjectId,
                cancellationToken)
            ?? await dbContext.TenantMemberships
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    membership =>
                        membership.TenantId == tenantId
                        && membership.Id == DevelopmentPilotIds.ReviewerTenantMembershipId,
                    cancellationToken);

        if (existingMembership is not null)
        {
            return existingMembership;
        }

        var membership = new TenantMembership
        {
            Id = DevelopmentPilotIds.ReviewerTenantMembershipId,
            TenantId = tenantId,
            IdentitySubjectId = identitySubjectId,
            Status = TenantMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureReviewerTenantRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string reviewerRoleKey = "production-planning-reviewer";

        var existingRole = await dbContext.TenantRoles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                role =>
                    role.TenantId == tenantId
                    && (role.Key == reviewerRoleKey
                        || role.Id == DevelopmentPilotIds.ReviewerTenantRoleId),
                cancellationToken);

        if (existingRole is not null)
        {
            return existingRole;
        }

        var tenantRole = new TenantRole
        {
            Id = DevelopmentPilotIds.ReviewerTenantRoleId,
            TenantId = tenantId,
            Key = reviewerRoleKey,
            DisplayName = "Production Planning Reviewer",
            Status = AuthorizationStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantRoles.Add(tenantRole);
        return tenantRole;
    }

    private static async Task EnsureReviewerFacilityMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingMembership = await dbContext.FacilityMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.FacilityId == facilityId
                    && membership.TenantMembershipId == tenantMembershipId,
                cancellationToken);

        if (existingMembership is not null)
        {
            return;
        }

        dbContext.FacilityMemberships.Add(new FacilityMembership
        {
            Id = DevelopmentPilotIds.ReviewerFacilityMembershipId,
            TenantId = tenantId,
            FacilityId = facilityId,
            TenantMembershipId = tenantMembershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static Task EnsureExecutionReadPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Read,
            DevelopmentPilotIds.ExecutionReadPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionWritePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Write,
            DevelopmentPilotIds.ExecutionWritePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionStartPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Start,
            DevelopmentPilotIds.ExecutionStartPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionPausePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Pause,
            DevelopmentPilotIds.ExecutionPausePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionCompletePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Complete,
            DevelopmentPilotIds.ExecutionCompletePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionRecordPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Record,
            DevelopmentPilotIds.ExecutionRecordPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionDowntimePermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Downtime,
            DevelopmentPilotIds.ExecutionDowntimePermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionSetupPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Setup,
            DevelopmentPilotIds.ExecutionSetupPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionExceptionPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Exception,
            DevelopmentPilotIds.ExecutionExceptionPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionAssignPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Assign,
            DevelopmentPilotIds.ExecutionAssignPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionHandoverPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.Handover,
            DevelopmentPilotIds.ExecutionHandoverPermissionDefinitionId,
            cancellationToken);

    private static Task EnsureExecutionExceptionAssignPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext, Guid tenantId, Guid tenantRoleId,
        DateTimeOffset seedTimestampUtc, CancellationToken cancellationToken) =>
        EnsureNamedPermissionAssignmentAsync(
            dbContext, tenantId, tenantRoleId,
            ProductionExecutionPermissionKeys.ExceptionAssign,
            DevelopmentPilotIds.ExecutionExceptionAssignPermissionDefinitionId,
            cancellationToken);

    private static async Task EnsurePilotProductionShiftsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var shiftDefinitions = new[]
        {
            (DevelopmentPilotIds.PilotShiftV1Id, "V1", "Gündüz Vardiyası", new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0), 1),
            (DevelopmentPilotIds.PilotShiftV2Id, "V2", "Akşam Vardiyası", new TimeSpan(16, 0, 0), new TimeSpan(0, 0, 0), 2),
            (DevelopmentPilotIds.PilotShiftV3Id, "V3", "Gece Vardiyası", new TimeSpan(0, 0, 0), new TimeSpan(8, 0, 0), 3),
        };

        foreach (var (id, code, name, start, end, sortOrder) in shiftDefinitions)
        {
            var exists = await dbContext.ProductionShifts
                .IgnoreQueryFilters()
                .AnyAsync(s => s.Id == id || (s.TenantId == tenantId && s.FacilityId == facilityId && s.Code == code), cancellationToken);

            if (!exists)
            {
                dbContext.ProductionShifts.Add(new Production.ProductionShift
                {
                    Id = id,
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    Code = code,
                    Name = name,
                    StartLocalTime = start,
                    EndLocalTime = end,
                    IsActive = true,
                    SortOrder = sortOrder,
                    Version = 1,
                    CreatedAtUtc = seedTimestampUtc,
                    UpdatedAtUtc = seedTimestampUtc,
                });
            }
        }
    }

    private static async Task EnsureNamedPermissionAssignmentAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid tenantRoleId,
        string permissionKey,
        Guid fallbackPermissionId,
        CancellationToken cancellationToken)
    {
        var permissionDefinition = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(p => p.Key == permissionKey, cancellationToken)
            ?? await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(p => p.Id == fallbackPermissionId, cancellationToken);

        if (permissionDefinition is null)
        {
            return;
        }

        var exists = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(
                a =>
                    a.TenantId == tenantId
                    && a.TenantRoleId == tenantRoleId
                    && a.PermissionDefinitionId == permissionDefinition.Id,
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
        CrevaFactOSDbContext dbContext,
        DevelopmentPilotOptions pilotOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string operatorSubject = "production-pilot-operator";
        var issuer = pilotOptions.DevelopmentIssuer.Trim();

        var existing = await dbContext.IdentitySubjects
            .SingleOrDefaultAsync(
                i => i.ExternalIssuer == issuer && i.ExternalSubject == operatorSubject,
                cancellationToken)
            ?? await dbContext.IdentitySubjects
                .SingleOrDefaultAsync(
                    i => i.Id == DevelopmentPilotIds.OperatorIdentitySubjectId,
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
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                m => m.TenantId == tenantId && m.IdentitySubjectId == identitySubjectId,
                cancellationToken)
            ?? await dbContext.TenantMemberships
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    m => m.TenantId == tenantId && m.Id == DevelopmentPilotIds.OperatorTenantMembershipId,
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
            Status = TenantMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        };

        dbContext.TenantMemberships.Add(membership);
        return membership;
    }

    private static async Task<TenantRole> EnsureOperatorTenantRoleAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        const string operatorRoleKey = "production-execution-operator";

        var existing = await dbContext.TenantRoles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId
                    && (r.Key == operatorRoleKey || r.Id == DevelopmentPilotIds.OperatorTenantRoleId),
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

    private static async Task EnsureOperatorFacilityMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.FacilityMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                m =>
                    m.TenantId == tenantId
                    && m.FacilityId == facilityId
                    && m.TenantMembershipId == tenantMembershipId,
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

    private static async Task<int> EnsureProductionPlansAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string createdBy,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var seededCount = 0;
        var seedPlans = DevelopmentPilotProductionPlanSeed.CreatePlans(
            tenantId,
            facilityId,
            createdBy.Trim(),
            seedTimestampUtc);

        foreach (var seedPlan in seedPlans)
        {
            var existingPlan = await dbContext.ProductionPlans
                .IgnoreQueryFilters()
                .AnyAsync(
                    plan =>
                        plan.TenantId == tenantId
                        && plan.FacilityId == facilityId
                        && plan.ReferenceCode == seedPlan.ReferenceCode,
                    cancellationToken);

            if (existingPlan)
            {
                continue;
            }

            dbContext.ProductionPlans.Add(seedPlan);
            seededCount++;
        }

        return seededCount;
    }
}
