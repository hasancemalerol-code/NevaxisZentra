using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Development;

/// <summary>
/// Idempotent repair seed for the canonical production-pilot-operator acceptance role.
/// Permission keys come only from AcceptanceRoleContract.
/// Runs outside the main bootstrap transaction so grants survive re-seed rollbacks.
/// </summary>
internal static class DevelopmentPilotOperatorPresentationAccessSeed
{
    internal static IReadOnlyList<string> PresentationPermissionKeys =>
        AcceptanceRoleContract.RequiredPermissionKeys;

    internal static async Task EnsureOperatorPresentationAccessAsync(
        NevaxisZentraDbContext dbContext,
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

        await AcceptanceRoleGrantService.EnsureAsync(
            dbContext,
            tenantId,
            operatorRole.Id,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IdentitySubject> EnsureOperatorIdentitySubjectAsync(
        NevaxisZentraDbContext dbContext,
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
        NevaxisZentraDbContext dbContext,
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
        NevaxisZentraDbContext dbContext,
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
        NevaxisZentraDbContext dbContext,
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
        NevaxisZentraDbContext dbContext,
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
