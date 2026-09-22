using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.SaasIdentity;
using NevaxisZentra.Persistence.SaasSubscription;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.AdminAccess;

public interface IAdminAccessRepository
{
    Task<AdminAccessWorkspaceSummaryReadModel?> GetWorkspaceSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAccessMemberReadModel>> ListMembersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAccessInvitationReadModel>> ListInvitationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAccessFacilitySummaryReadModel>> ListFacilitiesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAccessTenantSessionReadModel>> ListTenantSessionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAccessPermissionReadModel>> ListPermissionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAccessRoleSummaryReadModel>> ListRolesAsync(
        CancellationToken cancellationToken = default);

    Task<AdminAccessEffectiveAccessReadModel?> GetEffectiveAccessAsync(
        string memberReference,
        CancellationToken cancellationToken = default);

    Task<AdminAccessEffectiveAccessReadModel?> GetCurrentEffectiveAccessAsync(
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAccessAuditEventReadModel>> ListAuditEventsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<CreateAdminAccessInvitationResult> CreateInvitationAsync(
        CreateAdminAccessInvitationCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> SuspendMemberAsync(
        SuspendAdminAccessMemberCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> ReactivateMemberAsync(
        ReactivateAdminAccessMemberCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> RevokeSessionAsync(
        RevokeAdminAccessSessionCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> RevokeAllMemberSessionsAsync(
        RevokeAllAdminAccessMemberSessionsCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> CreateRoleAsync(
        CreateAdminAccessRoleCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> PatchRoleAsync(
        PatchAdminAccessRoleCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> SetRolePermissionsAsync(
        SetAdminAccessRolePermissionsCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> PutMemberRolesAsync(
        PutAdminAccessMemberRolesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> PutMemberFacilitiesAsync(
        PutAdminAccessMemberFacilitiesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AdminAccessWriteResult> PutMemberOverridesAsync(
        PutAdminAccessMemberOverridesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default);
}

public sealed partial class AdminAccessRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IIdentityEmailDeliveryProvider emailDeliveryProvider,
    ITenantEntitlementEvaluator entitlementEvaluator,
    IDevelopmentTokenDeliverySink? developmentTokenDeliverySink = null) : IAdminAccessRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task<IReadOnlyList<AdminAccessPermissionReadModel>> ListPermissionsAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var permissions = await dbContext.PermissionDefinitions.AsNoTracking()
                    .OrderBy(p => p.NormalizedKey)
                    .ToListAsync(ct);
                return (IReadOnlyList<AdminAccessPermissionReadModel>)permissions.Select(MapPermission).ToList();
            },
            cancellationToken);

    public Task<IReadOnlyList<AdminAccessRoleSummaryReadModel>> ListRolesAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var roles = await dbContext.TenantRoles.AsNoTracking()
                    .OrderBy(r => r.Key)
                    .ToListAsync(ct);
                var grants = await dbContext.TenantRolePermissions.AsNoTracking().ToListAsync(ct);
                var permissions = await dbContext.PermissionDefinitions.AsNoTracking().ToListAsync(ct);
                var permissionById = permissions.ToDictionary(p => p.Id);
                return (IReadOnlyList<AdminAccessRoleSummaryReadModel>)roles.Select(role =>
                {
                    var keys = grants
                        .Where(g => g.TenantRoleId == role.Id)
                        .Select(g => permissionById.TryGetValue(g.PermissionDefinitionId, out var permission)
                            ? permission.Key
                            : null)
                        .Where(key => key is not null)
                        .Cast<string>()
                        .OrderBy(key => key, StringComparer.Ordinal)
                        .ToArray();
                    return MapRole(role, keys);
                }).ToList();
            },
            cancellationToken);

    public Task<AdminAccessEffectiveAccessReadModel?> GetEffectiveAccessAsync(
        string memberReference,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => LoadEffectiveAccessAsync(memberReference, ct),
            cancellationToken);

    public Task<AdminAccessEffectiveAccessReadModel?> GetCurrentEffectiveAccessAsync(
        Guid membershipId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var membership = await dbContext.TenantMemberships.AsNoTracking()
                    .SingleOrDefaultAsync(m => m.Id == membershipId, ct);
                return membership is null
                    ? null
                    : await LoadEffectiveAccessAsync(membership.MembershipReference, ct);
            },
            cancellationToken);

    public Task<IReadOnlyList<AdminAccessAuditEventReadModel>> ListAuditEventsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var boundedLimit = Math.Clamp(limit, 1, 200);
                var events = await dbContext.AuthorizationAuditEvents.AsNoTracking()
                    .OrderByDescending(e => e.CreatedAtUtc)
                    .ThenByDescending(e => e.EntityReference)
                    .Take(boundedLimit)
                    .ToListAsync(ct);
                return (IReadOnlyList<AdminAccessAuditEventReadModel>)events.Select(MapAudit).ToList();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> CreateRoleAsync(
        CreateAdminAccessRoleCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "role.create",
            command,
            actor,
            async ct =>
            {
                var key = AdminAccessValueConversions.NormalizeRoleKey(command.RoleKey);
                if (!AdminAccessValueConversions.IsValidRoleKey(key))
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ValidationFailed, "Role key is invalid.");
                }

                if (string.IsNullOrWhiteSpace(command.DisplayName))
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ValidationFailed, "Display name is required.");
                }

                if (await dbContext.TenantRoles.AnyAsync(r => r.Key == key, ct))
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ValidationFailed, "Role key already exists.");
                }

                var now = DateTimeOffset.UtcNow;
                dbContext.TenantRoles.Add(new TenantRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = actor.TenantId,
                    Key = key,
                    DisplayName = command.DisplayName.Trim(),
                    Description = command.Description?.Trim(),
                    IsProtected = false,
                    Status = AuthorizationStatus.Active,
                    Version = 1,
                    CreatedByActorReference = actor.ActorReference,
                    UpdatedByActorReference = actor.ActorReference,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                });
                AppendAudit(actor, AdminAccessAuditEventTypes.RoleCreated, AdminAccessAuditEntityTypes.Role, key, null, new { key, command.DisplayName });
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> PatchRoleAsync(
        PatchAdminAccessRoleCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "role.patch",
            command,
            actor,
            async ct =>
            {
                var role = await FindRoleAsync(command.RoleReference, ct);
                if (role is null)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.NotFound, "Role was not found.");
                }

                if (role.Version != command.ExpectedVersion)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ConcurrencyConflict, "Role version is stale.");
                }

                var before = new { role.DisplayName, role.Description, status = role.Status.ToString(), role.Version };
                if (!string.IsNullOrWhiteSpace(command.DisplayName))
                {
                    role.DisplayName = command.DisplayName.Trim();
                }

                if (command.Description is not null)
                {
                    role.Description = command.Description.Trim();
                }

                if (!string.IsNullOrWhiteSpace(command.Status))
                {
                    var status = ParseStatus(command.Status);
                    if (status is null)
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ValidationFailed, "Role status is invalid.");
                    }

                    if (status == AuthorizationStatus.Retired || status == AuthorizationStatus.Inactive)
                    {
                        if (role.IsProtected || role.Key == AdminAccessRoleCodes.TenantOwner)
                        {
                            return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ProtectedRole, "Protected role cannot be retired.");
                        }

                        if (role.Key == AdminAccessRoleCodes.TenantOwner
                            && await CountActiveOwnersAsync(excludeMembershipId: null, ct) <= 1)
                        {
                            return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.FinalOwnerProtected, "Final tenant owner cannot be removed.");
                        }
                    }

                    role.Status = status.Value;
                }

                role.UpdatedByActorReference = actor.ActorReference;
                role.UpdatedAtUtc = DateTimeOffset.UtcNow;
                role.Version++;
                AppendAudit(
                    actor,
                    role.Status == AuthorizationStatus.Retired
                        ? AdminAccessAuditEventTypes.RoleRetired
                        : AdminAccessAuditEventTypes.RoleUpdated,
                    AdminAccessAuditEntityTypes.Role,
                    role.Key,
                    before,
                    new { role.DisplayName, role.Description, status = role.Status.ToString(), role.Version });
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> SetRolePermissionsAsync(
        SetAdminAccessRolePermissionsCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "role.permissions",
            command,
            actor,
            async ct =>
            {
                var role = await FindRoleAsync(command.RoleReference, ct);
                if (role is null)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.NotFound, "Role was not found.");
                }

                if (role.Version != command.ExpectedVersion)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ConcurrencyConflict, "Role version is stale.");
                }

                if (role.Status != AuthorizationStatus.Active)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.RetiredRole, "Retired roles cannot receive new grants.");
                }

                var actorKeys = await LoadGrantedKeysAsync(actor.MembershipId, ct);
                var before = await LoadRolePermissionKeysAsync(role.Id, ct);
                var existing = await dbContext.TenantRolePermissions
                    .Where(g => g.TenantRoleId == role.Id)
                    .ToListAsync(ct);

                foreach (var grant in command.Grants.OrderBy(g => g.PermissionKey, StringComparer.Ordinal))
                {
                    var key = AdminAccessValueConversions.NormalizePermissionKey(grant.PermissionKey);
                    var permission = await dbContext.PermissionDefinitions.SingleOrDefaultAsync(p => p.NormalizedKey == key || p.Key == key, ct);
                    if (permission is null)
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.InvalidGrant, "Permission key is invalid.");
                    }

                    if (AdminAccessValueConversions.IsPlatformPermission(permission.Key))
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.PlatformGrantBlocked, "Platform-admin capability cannot be granted.");
                    }

                    if (!AdminAccessValueConversions.IsCurrentlySupportedRecordScope(grant.RecordScope)
                        || !ScopeSupported(permission, grant.RecordScope))
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.InvalidScope, "Record scope is not supported.");
                    }

                    var effect = AdminAccessValueConversions.NormalizePermissionKey(grant.Effect);
                    if (effect is "revoke" or "deny" or "remove")
                    {
                        var match = existing.FirstOrDefault(g => g.PermissionDefinitionId == permission.Id);
                        if (match is not null)
                        {
                            dbContext.TenantRolePermissions.Remove(match);
                            existing.Remove(match);
                        }

                        continue;
                    }

                    if (permission.Status != AuthorizationStatus.Active)
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.RetiredPermission, "Inactive permissions cannot be newly granted.");
                    }

                    if (!actorKeys.Contains(permission.Key))
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.UnownedPermission, "Actor cannot grant a permission they do not possess.");
                    }

                    var current = existing.FirstOrDefault(g => g.PermissionDefinitionId == permission.Id);
                    if (current is null)
                    {
                        var assignment = new TenantRolePermissionAssignment
                        {
                            TenantId = actor.TenantId,
                            TenantRoleId = role.Id,
                            PermissionDefinitionId = permission.Id,
                            RecordScope = grant.RecordScope,
                        };
                        dbContext.TenantRolePermissions.Add(assignment);
                        existing.Add(assignment);
                    }
                    else
                    {
                        current.RecordScope = grant.RecordScope;
                    }
                }

                role.UpdatedByActorReference = actor.ActorReference;
                role.UpdatedAtUtc = DateTimeOffset.UtcNow;
                role.Version++;
                var after = command.Grants.Select(g => AdminAccessValueConversions.NormalizePermissionKey(g.PermissionKey)).OrderBy(k => k).ToArray();
                AppendAudit(actor, AdminAccessAuditEventTypes.RolePermissionsChanged, AdminAccessAuditEntityTypes.Role, role.Key, before, after);
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> PutMemberRolesAsync(
        PutAdminAccessMemberRolesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "member.roles",
            command,
            actor,
            async ct =>
            {
                var membership = await FindMembershipAsync(command.MemberReference, ct);
                if (membership is null)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.NotFound, "Member was not found.");
                }

                if (membership.Id == actor.MembershipId)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.SelfMutation, "Users cannot modify their own roles.");
                }

                if (membership.AuthorizationVersion != command.ExpectedAuthorizationVersion)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ConcurrencyConflict, "Authorization version is stale.");
                }

                var requested = command.RoleReferences
                    .Select(AdminAccessValueConversions.NormalizeRoleKey)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(key => key, StringComparer.Ordinal)
                    .ToArray();
                var roles = await dbContext.TenantRoles.Where(r => requested.Contains(r.Key)).ToListAsync(ct);
                if (roles.Count != requested.Length)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.InvalidGrant, "One or more role references are invalid.");
                }

                if (roles.Any(r => r.Status != AuthorizationStatus.Active))
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.RetiredRole, "Retired roles cannot be newly assigned.");
                }

                var actorKeys = await LoadGrantedKeysAsync(actor.MembershipId, ct);
                var actorIsOwner = await HasActiveOwnerRoleAsync(actor.MembershipId, ct);
                if (roles.Any(r => r.Key == AdminAccessRoleCodes.TenantOwner)
                    && (!actorIsOwner || !actorKeys.Contains(AdminAccessPermissionKeys.GrantOwner)))
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Forbidden, "Tenant-owner can only be granted by a current tenant owner.");
                }

                var existing = await dbContext.TenantMembershipRoles
                    .Where(r => r.TenantMembershipId == membership.Id)
                    .ToListAsync(ct);
                var before = await LoadMemberRoleKeysAsync(membership.Id, ct);
                if (before.Contains(AdminAccessRoleCodes.TenantOwner)
                    && !requested.Contains(AdminAccessRoleCodes.TenantOwner)
                    && await CountActiveOwnersAsync(membership.Id, ct) <= 0)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.FinalOwnerProtected, "Final active tenant owner cannot be removed.");
                }

                foreach (var assignment in existing.Where(item => !roles.Any(role => role.Id == item.TenantRoleId)).ToList())
                {
                    dbContext.TenantMembershipRoles.Remove(assignment);
                }

                foreach (var role in roles)
                {
                    if (existing.Any(item => item.TenantRoleId == role.Id && item.Status == AuthorizationStatus.Active))
                    {
                        continue;
                    }

                    var current = existing.FirstOrDefault(item => item.TenantRoleId == role.Id);
                    if (current is null)
                    {
                        dbContext.TenantMembershipRoles.Add(new TenantMembershipRole
                        {
                            TenantId = actor.TenantId,
                            TenantMembershipId = membership.Id,
                            TenantRoleId = role.Id,
                            Status = AuthorizationStatus.Active,
                            AssignedAtUtc = DateTimeOffset.UtcNow,
                        });
                    }
                    else
                    {
                        current.Status = AuthorizationStatus.Active;
                        current.AssignedAtUtc = DateTimeOffset.UtcNow;
                    }
                }

                membership.AuthorizationVersion++;
                AppendAudit(actor, AdminAccessAuditEventTypes.MemberRolesChanged, AdminAccessAuditEntityTypes.Membership, membership.MembershipReference, before, requested);
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> PutMemberFacilitiesAsync(
        PutAdminAccessMemberFacilitiesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "member.facilities",
            command,
            actor,
            async ct =>
            {
                var membership = await FindMembershipAsync(command.MemberReference, ct);
                if (membership is null)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.NotFound, "Member was not found.");
                }

                if (membership.Id == actor.MembershipId)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.SelfMutation, "Users cannot modify their own facility assignments.");
                }

                if (membership.AuthorizationVersion != command.ExpectedAuthorizationVersion)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ConcurrencyConflict, "Authorization version is stale.");
                }

                var requested = command.FacilityCodes
                    .Select(code => code.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(code => code, StringComparer.Ordinal)
                    .ToArray();
                var facilities = (await dbContext.Facilities.ToListAsync(ct))
                    .Where(f => requested.Contains(f.Code.Trim().ToLowerInvariant()))
                    .ToList();
                if (facilities.Count != requested.Length)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.InvalidGrant, "One or more facility codes are invalid.");
                }

                if (facilities.Any(f => f.Id == DevelopmentPilotIds.UnauthorizedFacilityId))
                {
                    return AdminAccessWriteResult.Fail(
                        AdminAccessFailureCodes.CrossFacility,
                        "One or more facilities cannot be assigned through admin access.");
                }

                var existing = await dbContext.FacilityMemberships
                    .Where(f => f.TenantMembershipId == membership.Id)
                    .ToListAsync(ct);
                var before = await LoadMemberFacilityCodesAsync(membership.Id, ct);
                var now = DateTimeOffset.UtcNow;
                foreach (var assignment in existing)
                {
                    assignment.Status = facilities.Any(f => f.Id == assignment.FacilityId)
                        ? FacilityMembershipStatus.Active
                        : FacilityMembershipStatus.Inactive;
                    assignment.UpdatedAtUtc = now;
                    assignment.Version++;
                }

                foreach (var facility in facilities.Where(f => existing.All(e => e.FacilityId != f.Id)))
                {
                    dbContext.FacilityMemberships.Add(new FacilityMembership
                    {
                        Id = Guid.NewGuid(),
                        TenantId = actor.TenantId,
                        FacilityId = facility.Id,
                        TenantMembershipId = membership.Id,
                        Status = FacilityMembershipStatus.Active,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                        Version = 1,
                    });
                }

                membership.AuthorizationVersion++;
                AppendAudit(actor, AdminAccessAuditEventTypes.MemberFacilitiesChanged, AdminAccessAuditEntityTypes.Membership, membership.MembershipReference, before, requested);
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> PutMemberOverridesAsync(
        PutAdminAccessMemberOverridesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "member.overrides",
            command,
            actor,
            async ct =>
            {
                var membership = await FindMembershipAsync(command.MemberReference, ct);
                if (membership is null)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.NotFound, "Member was not found.");
                }

                if (membership.Id == actor.MembershipId)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.SelfMutation, "Users cannot modify their own permission overrides.");
                }

                if (membership.AuthorizationVersion != command.ExpectedAuthorizationVersion)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ConcurrencyConflict, "Authorization version is stale.");
                }

                var actorKeys = await LoadGrantedKeysAsync(actor.MembershipId, ct);
                var existing = await dbContext.TenantMembershipPermissionOverrides
                    .Where(o => o.TenantMembershipId == membership.Id)
                    .ToListAsync(ct);
                var before = existing
                    .Where(o => o.Status == "Active")
                    .Select(o => o.Effect + ":" + o.PermissionDefinitionId)
                    .OrderBy(v => v)
                    .ToArray();
                var now = DateTimeOffset.UtcNow;
                var desiredIds = new HashSet<Guid>();

                foreach (var input in command.Overrides.OrderBy(o => o.PermissionKey, StringComparer.Ordinal))
                {
                    var key = AdminAccessValueConversions.NormalizePermissionKey(input.PermissionKey);
                    var permission = await dbContext.PermissionDefinitions.SingleOrDefaultAsync(p => p.NormalizedKey == key || p.Key == key, ct);
                    if (permission is null)
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.InvalidGrant, "Permission key is invalid.");
                    }

                    if (AdminAccessValueConversions.IsPlatformPermission(permission.Key))
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.PlatformGrantBlocked, "Platform-admin capability cannot be granted.");
                    }

                    var effect = AdminAccessValueConversions.NormalizePermissionKey(input.Effect);
                    if (effect is not AdminAccessOverrideEffects.Allow and not AdminAccessOverrideEffects.Deny)
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ValidationFailed, "Override effect must be allow or deny.");
                    }

                    if (!AdminAccessValueConversions.IsCurrentlySupportedRecordScope(input.RecordScope)
                        || !ScopeSupported(permission, input.RecordScope))
                    {
                        return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.InvalidScope, "Record scope is not supported.");
                    }

                    if (effect == AdminAccessOverrideEffects.Allow)
                    {
                        if (permission.Status != AuthorizationStatus.Active)
                        {
                            return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.RetiredPermission, "Inactive permissions cannot be newly granted.");
                        }

                        if (!actorKeys.Contains(permission.Key))
                        {
                            return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.UnownedPermission, "Actor cannot grant a permission they do not possess.");
                        }
                    }

                    desiredIds.Add(permission.Id);
                    var current = existing.FirstOrDefault(o => o.PermissionDefinitionId == permission.Id);
                    if (current is null)
                    {
                        dbContext.TenantMembershipPermissionOverrides.Add(new TenantMembershipPermissionOverride
                        {
                            Id = Guid.NewGuid(),
                            TenantId = actor.TenantId,
                            TenantMembershipId = membership.Id,
                            PermissionDefinitionId = permission.Id,
                            Effect = effect,
                            RecordScope = input.RecordScope,
                            Status = "Active",
                            CreatedAtUtc = now,
                            UpdatedAtUtc = now,
                            Version = 1,
                        });
                    }
                    else
                    {
                        current.Effect = effect;
                        current.RecordScope = input.RecordScope;
                        current.Status = "Active";
                        current.UpdatedAtUtc = now;
                        current.Version++;
                    }
                }

                foreach (var leftover in existing.Where(o => !desiredIds.Contains(o.PermissionDefinitionId)))
                {
                    leftover.Status = "Inactive";
                    leftover.UpdatedAtUtc = now;
                    leftover.Version++;
                }

                membership.AuthorizationVersion++;
                var after = command.Overrides
                    .Select(o => AdminAccessValueConversions.NormalizePermissionKey(o.Effect) + ":" + AdminAccessValueConversions.NormalizePermissionKey(o.PermissionKey))
                    .OrderBy(v => v)
                    .ToArray();
                AppendAudit(actor, AdminAccessAuditEventTypes.MemberOverridesChanged, AdminAccessAuditEntityTypes.Membership, membership.MembershipReference, before, after);
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    private async Task<AdminAccessWriteResult> ExecuteMutationAsync<TCommand>(
        string? idempotencyKey,
        string operation,
        TCommand command,
        AdminAccessActorContext actor,
        Func<CancellationToken, Task<AdminAccessWriteResult>> action,
        CancellationToken cancellationToken)
    {
        return await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    var hash = HashCommand(command);
                    var existing = await dbContext.AdminAccessIdempotencyRecords
                        .SingleOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, ct);
                    if (existing is not null)
                    {
                        return existing.RequestHash == hash
                            ? AdminAccessWriteResult.Ok(idempotentReplay: true)
                            : AdminAccessWriteResult.Fail(AdminAccessFailureCodes.IdempotencyConflict, "Idempotent retry conflicts with the original request.");
                    }

                    dbContext.AdminAccessIdempotencyRecords.Add(new AdminAccessIdempotencyRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = actor.TenantId,
                        IdempotencyKey = idempotencyKey,
                        RequestHash = hash,
                        Operation = operation,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                    });
                }

                var result = await action(ct);
                if (!result.Succeeded)
                {
                    dbContext.ChangeTracker.Clear();
                }

                return result;
            },
            cancellationToken);
    }

    private async Task<AdminAccessEffectiveAccessReadModel?> LoadEffectiveAccessAsync(
        string memberReference,
        CancellationToken cancellationToken)
    {
        var membership = await FindMembershipAsync(memberReference, cancellationToken);
        if (membership is null || membership.Status != TenantMembershipStatus.Active)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var roleAssignments = await dbContext.TenantMembershipRoles.AsNoTracking()
            .Where(r => r.TenantMembershipId == membership.Id && r.Status == AuthorizationStatus.Active)
            .ToListAsync(cancellationToken);
        var roles = await dbContext.TenantRoles.AsNoTracking()
            .Where(r => roleAssignments.Select(a => a.TenantRoleId).Contains(r.Id) && r.Status == AuthorizationStatus.Active)
            .OrderBy(r => r.Key)
            .ToListAsync(cancellationToken);
        var roleGrants = await dbContext.TenantRolePermissions.AsNoTracking()
            .Where(g => roles.Select(r => r.Id).Contains(g.TenantRoleId))
            .ToListAsync(cancellationToken);
        var overrides = await dbContext.TenantMembershipPermissionOverrides.AsNoTracking()
            .Where(o => o.TenantMembershipId == membership.Id && o.Status == "Active")
            .ToListAsync(cancellationToken);
        var permissions = await dbContext.PermissionDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        var permissionById = permissions.ToDictionary(p => p.Id);
        var facilities = await (
            from assignment in dbContext.FacilityMemberships.AsNoTracking()
            join facility in dbContext.Facilities.AsNoTracking() on assignment.FacilityId equals facility.Id
            where assignment.TenantMembershipId == membership.Id
                && assignment.Status == FacilityMembershipStatus.Active
                && facility.Status == FacilityStatus.Active
            orderby facility.Code
            select facility.Code).ToListAsync(cancellationToken);

        var denied = new HashSet<string>(StringComparer.Ordinal);
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in overrides.Where(o => IsEffective(o.EffectiveFromUtc, o.EffectiveUntilUtc, now)))
        {
            if (!permissionById.TryGetValue(item.PermissionDefinitionId, out var permission)
                || permission.Status != AuthorizationStatus.Active)
            {
                continue;
            }

            if (item.Effect == AdminAccessOverrideEffects.Deny)
            {
                denied.Add(permission.Key);
            }
        }

        foreach (var grant in roleGrants.Where(g => IsEffective(g.EffectiveFromUtc, g.EffectiveUntilUtc, now)))
        {
            if (!permissionById.TryGetValue(grant.PermissionDefinitionId, out var permission)
                || permission.Status != AuthorizationStatus.Active
                || denied.Contains(permission.Key))
            {
                continue;
            }

            allowed.Add(permission.Key);
        }

        foreach (var item in overrides.Where(o => o.Effect == AdminAccessOverrideEffects.Allow && IsEffective(o.EffectiveFromUtc, o.EffectiveUntilUtc, now)))
        {
            if (!permissionById.TryGetValue(item.PermissionDefinitionId, out var permission)
                || permission.Status != AuthorizationStatus.Active
                || denied.Contains(permission.Key))
            {
                continue;
            }

            allowed.Add(permission.Key);
        }

        return new AdminAccessEffectiveAccessReadModel(
            membership.MembershipReference,
            membership.AuthorizationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            roles.Select(r => r.Key).ToArray(),
            facilities,
            allowed.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            denied.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            new AdminAccessNavigationVisibilityReadModel(
                allowed.Contains(EdgePermissionKeys.Read),
                allowed.Contains(MaterialPermissionKeys.Read),
                allowed.Contains(MaintenancePermissionKeys.Read)),
            AcceptanceRoleContract.CatalogVersion);
    }

    private async Task<HashSet<string>> LoadGrantedKeysAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        var membership = await dbContext.TenantMemberships.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return [];
        }

        var access = await LoadEffectiveAccessAsync(membership.MembershipReference, cancellationToken);
        return access?.GrantedPermissionKeys.ToHashSet(StringComparer.Ordinal) ?? [];
    }

    private Task<string[]> LoadRolePermissionKeysAsync(Guid roleId, CancellationToken cancellationToken) =>
        (
            from grant in dbContext.TenantRolePermissions.AsNoTracking()
            join permission in dbContext.PermissionDefinitions.AsNoTracking() on grant.PermissionDefinitionId equals permission.Id
            where grant.TenantRoleId == roleId
            orderby permission.Key
            select permission.Key).ToArrayAsync(cancellationToken);

    private Task<string[]> LoadMemberRoleKeysAsync(Guid membershipId, CancellationToken cancellationToken) =>
        (
            from assignment in dbContext.TenantMembershipRoles.AsNoTracking()
            join role in dbContext.TenantRoles.AsNoTracking() on assignment.TenantRoleId equals role.Id
            where assignment.TenantMembershipId == membershipId && assignment.Status == AuthorizationStatus.Active
            orderby role.Key
            select role.Key).ToArrayAsync(cancellationToken);

    private Task<string[]> LoadMemberFacilityCodesAsync(Guid membershipId, CancellationToken cancellationToken) =>
        (
            from assignment in dbContext.FacilityMemberships.AsNoTracking()
            join facility in dbContext.Facilities.AsNoTracking() on assignment.FacilityId equals facility.Id
            where assignment.TenantMembershipId == membershipId && assignment.Status == FacilityMembershipStatus.Active
            orderby facility.Code
            select facility.Code).ToArrayAsync(cancellationToken);

    private Task<TenantRole?> FindRoleAsync(string roleReference, CancellationToken cancellationToken)
    {
        var key = AdminAccessValueConversions.NormalizeRoleKey(roleReference);
        return dbContext.TenantRoles.SingleOrDefaultAsync(r => r.Key == key, cancellationToken);
    }

    private Task<TenantMembership?> FindMembershipAsync(string memberReference, CancellationToken cancellationToken)
    {
        var reference = memberReference.Trim();
        return dbContext.TenantMemberships.SingleOrDefaultAsync(m => m.MembershipReference == reference, cancellationToken);
    }

    private Task<int> CountActiveOwnersAsync(Guid? excludeMembershipId, CancellationToken cancellationToken) =>
        (
            from assignment in dbContext.TenantMembershipRoles
            join role in dbContext.TenantRoles on assignment.TenantRoleId equals role.Id
            join membership in dbContext.TenantMemberships on assignment.TenantMembershipId equals membership.Id
            where role.Key == AdminAccessRoleCodes.TenantOwner
                && role.Status == AuthorizationStatus.Active
                && assignment.Status == AuthorizationStatus.Active
                && membership.Status == TenantMembershipStatus.Active
                && (excludeMembershipId == null || membership.Id != excludeMembershipId)
            select membership.Id).Distinct().CountAsync(cancellationToken);

    private Task<bool> HasActiveOwnerRoleAsync(Guid membershipId, CancellationToken cancellationToken) =>
        (
            from assignment in dbContext.TenantMembershipRoles
            join role in dbContext.TenantRoles on assignment.TenantRoleId equals role.Id
            where assignment.TenantMembershipId == membershipId
                && assignment.Status == AuthorizationStatus.Active
                && role.Key == AdminAccessRoleCodes.TenantOwner
                && role.Status == AuthorizationStatus.Active
            select role.Id).AnyAsync(cancellationToken);

    private void AppendAudit(
        AdminAccessActorContext actor,
        string eventType,
        string entityType,
        string entityReference,
        object? before,
        object? after)
    {
        dbContext.AuthorizationAuditEvents.Add(new AuthorizationAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = actor.TenantId,
            EventType = eventType,
            EntityType = entityType,
            EntityReference = entityReference,
            ActorReference = actor.ActorReference,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            CorrelationId = actor.CorrelationId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private static bool ScopeSupported(PermissionDefinition permission, string? scope)
    {
        if (scope is null)
        {
            return true;
        }

        var supported = AdminAccessValueConversions.ParseRecordScopes(permission.SupportedRecordScopes);
        return supported.Contains(scope);
    }

    private static bool IsEffective(DateTimeOffset? from, DateTimeOffset? until, DateTimeOffset now) =>
        (from is null || from <= now) && (until is null || until > now);

    private static AuthorizationStatus? ParseStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "active" => AuthorizationStatus.Active,
            "inactive" => AuthorizationStatus.Inactive,
            "retired" => AuthorizationStatus.Retired,
            _ => null,
        };

    private static string HashCommand<T>(T command)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(command, JsonOptions));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.NameEquals("expectedAuthorizationVersion")
                    || property.NameEquals("idempotencyKey"))
                {
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static AdminAccessPermissionReadModel MapPermission(PermissionDefinition permission) =>
        new(
            permission.Key,
            string.IsNullOrWhiteSpace(permission.NormalizedKey) ? permission.Key : permission.NormalizedKey,
            string.IsNullOrWhiteSpace(permission.TurkishLabel) ? permission.Description : permission.TurkishLabel,
            permission.Description,
            string.IsNullOrWhiteSpace(permission.ModuleResourceCode)
                ? AdminAccessValueConversions.MapModuleResource(permission.Key)
                : permission.ModuleResourceCode,
            string.IsNullOrWhiteSpace(permission.ActionCode)
                ? AdminAccessValueConversions.MapHonestAction(permission.Key)
                : permission.ActionCode,
            string.IsNullOrWhiteSpace(permission.SensitivityClassification)
                ? AdminAccessSensitivity.Standard
                : permission.SensitivityClassification,
            permission.RequiresRecentAuth,
            AdminAccessValueConversions.ParseRecordScopes(permission.SupportedRecordScopes),
            permission.Status.ToString());

    private static AdminAccessRoleSummaryReadModel MapRole(TenantRole role, IReadOnlyList<string> keys) =>
        new(role.Key, role.DisplayName, role.Description, role.SystemTemplateOrigin, role.IsProtected, role.Status.ToString(), role.Version, keys);

    private static AdminAccessAuditEventReadModel MapAudit(AuthorizationAuditEvent item) =>
        new(
            item.EventType,
            item.EntityType,
            item.EntityReference,
            item.ActorReference,
            item.CreatedAtUtc,
            Deserialize(item.BeforeJson),
            Deserialize(item.AfterJson));

    private static object? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<object>(json, JsonOptions);
    }
}

public sealed class UnavailableAdminAccessRepository : IAdminAccessRepository
{
    public Task<AdminAccessWorkspaceSummaryReadModel?> GetWorkspaceSummaryAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AdminAccessWorkspaceSummaryReadModel?>(null);

    public Task<IReadOnlyList<AdminAccessMemberReadModel>> ListMembersAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminAccessMemberReadModel>>([]);

    public Task<IReadOnlyList<AdminAccessInvitationReadModel>> ListInvitationsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminAccessInvitationReadModel>>([]);

    public Task<IReadOnlyList<AdminAccessFacilitySummaryReadModel>> ListFacilitiesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminAccessFacilitySummaryReadModel>>([]);

    public Task<IReadOnlyList<AdminAccessTenantSessionReadModel>> ListTenantSessionsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminAccessTenantSessionReadModel>>([]);

    public Task<IReadOnlyList<AdminAccessPermissionReadModel>> ListPermissionsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminAccessPermissionReadModel>>([]);

    public Task<IReadOnlyList<AdminAccessRoleSummaryReadModel>> ListRolesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminAccessRoleSummaryReadModel>>([]);

    public Task<AdminAccessEffectiveAccessReadModel?> GetEffectiveAccessAsync(
        string memberReference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AdminAccessEffectiveAccessReadModel?>(null);

    public Task<AdminAccessEffectiveAccessReadModel?> GetCurrentEffectiveAccessAsync(
        Guid membershipId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AdminAccessEffectiveAccessReadModel?>(null);

    public Task<IReadOnlyList<AdminAccessAuditEventReadModel>> ListAuditEventsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminAccessAuditEventReadModel>>([]);

    public Task<CreateAdminAccessInvitationResult> CreateInvitationAsync(
        CreateAdminAccessInvitationCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateAdminAccessInvitationResult.Fail(
            AdminAccessFailureCodes.Unavailable,
            "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> SuspendMemberAsync(
        SuspendAdminAccessMemberCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> ReactivateMemberAsync(
        ReactivateAdminAccessMemberCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> RevokeSessionAsync(
        RevokeAdminAccessSessionCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> RevokeAllMemberSessionsAsync(
        RevokeAllAdminAccessMemberSessionsCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> CreateRoleAsync(
        CreateAdminAccessRoleCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> PatchRoleAsync(
        PatchAdminAccessRoleCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> SetRolePermissionsAsync(
        SetAdminAccessRolePermissionsCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> PutMemberRolesAsync(
        PutAdminAccessMemberRolesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> PutMemberFacilitiesAsync(
        PutAdminAccessMemberFacilitiesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));

    public Task<AdminAccessWriteResult> PutMemberOverridesAsync(
        PutAdminAccessMemberOverridesCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AdminAccessWriteResult.Fail(AdminAccessFailureCodes.Unavailable, "Admin access persistence is unavailable."));
}
