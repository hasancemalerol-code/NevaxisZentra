using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence;

namespace NevaxisZentra.Persistence.Authorization;

public static class AcceptanceRoleGrantService
{
    public static async Task EnsureAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var definitions = await dbContext.PermissionDefinitions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var activeByKey = definitions
            .Where(definition => definition.Status == AuthorizationStatus.Active)
            .GroupBy(definition => definition.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var definitionById = definitions.ToDictionary(definition => definition.Id);

        var grants = await dbContext.TenantRolePermissions
            .IgnoreQueryFilters()
            .Where(grant => grant.TenantId == tenantId && grant.TenantRoleId == roleId)
            .ToListAsync(cancellationToken);
        var grantedKeys = grants
            .Select(grant => definitionById.TryGetValue(grant.PermissionDefinitionId, out var definition)
                ? definition.Key
                : null)
            .Where(key => key is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        var plan = AcceptanceRoleContract.Reconcile(
            activeByKey.Keys.ToHashSet(StringComparer.Ordinal),
            grantedKeys);
        if (plan.MissingDefinitions.Count > 0)
        {
            throw new AcceptanceRoleContractException(
                "Acceptance role contract failed. Active permission catalog is missing: "
                + string.Join(", ", plan.MissingDefinitions)
                + ". Do not hide the module. Apply the permission migration, then rerun role bootstrap.");
        }

        var changed = false;
        foreach (var key in plan.GrantsToAdd)
        {
            dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
            {
                TenantId = tenantId,
                TenantRoleId = roleId,
                PermissionDefinitionId = activeByKey[key].Id,
            });
            changed = true;
        }

        if (plan.ForbiddenGrantsToRemove.Count > 0)
        {
            var forbiddenIds = plan.ForbiddenGrantsToRemove
                .Select(key => activeByKey.TryGetValue(key, out var definition) ? definition.Id : (Guid?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();
            foreach (var grant in grants.Where(grant => forbiddenIds.Contains(grant.PermissionDefinitionId)))
            {
                dbContext.TenantRolePermissions.Remove(grant);
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        var membershipIds = await dbContext.TenantMembershipRoles
            .IgnoreQueryFilters()
            .Where(assignment =>
                assignment.TenantId == tenantId
                && assignment.TenantRoleId == roleId
                && assignment.Status == AuthorizationStatus.Active)
            .Select(assignment => assignment.TenantMembershipId)
            .ToListAsync(cancellationToken);
        var memberships = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .Where(membership => membershipIds.Contains(membership.Id))
            .ToListAsync(cancellationToken);
        foreach (var membership in memberships)
        {
            membership.AuthorizationVersion++;
        }
    }
}
