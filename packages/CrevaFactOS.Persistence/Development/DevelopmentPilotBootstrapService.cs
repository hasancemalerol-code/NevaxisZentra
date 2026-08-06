using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Persistence.Development;

internal sealed class DevelopmentPilotBootstrapService(
    IOptions<DevelopmentPilotOptions> options)
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
            DevelopmentPilotBootstrapDbContextFactory.Create();
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
