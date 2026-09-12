using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Development;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.SaasIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Persistence.AdminAccess;

internal static class DevelopmentAdminAccessSeed
{
    internal static async Task EnsureDevelopmentAdminAccessFoundationAsync(
        NevaxisFactOSDbContext dbContext,
        IOptions<DevelopmentSaasIdentityOptions> identityOptions,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await BackfillPermissionMetadataAsync(dbContext, cancellationToken);
        await EnsureAdminAccessPermissionsAsync(dbContext, seedTimestampUtc, cancellationToken);
        await BackfillMembershipReferencesAsync(dbContext, cancellationToken);

        foreach (var template in AdminAccessRoleTemplates.DevelopmentTemplates)
        {
            await EnsureTemplateRoleAsync(dbContext, template, seedTimestampUtc, cancellationToken);
        }

        await EnsureMembershipRoleAsync(
            dbContext,
            DevelopmentSaasIdentityIds.TenantOwnerMembershipId,
            DevelopmentAdminAccessIds.TenantOwnerRoleId,
            seedTimestampUtc,
            cancellationToken);
        await EnsureTenantOwnerFacilityMembershipAsync(
            dbContext,
            seedTimestampUtc,
            cancellationToken);

        if (!identityOptions.Value.Enabled || string.IsNullOrWhiteSpace(identityOptions.Value.ActiveUserPassword))
        {
            return;
        }

        var passwordHasher = new PasswordHasher<SaasIdentityAccount>();
        await EnsurePersonaAccountAsync(
            dbContext,
            passwordHasher,
            DevelopmentAdminAccessIds.MachineResponsibleAccountId,
            DevelopmentAdminAccessIds.MachineResponsibleIdentitySubjectId,
            DevelopmentAdminAccessIds.MachineResponsibleMembershipId,
            DevelopmentAdminAccessIds.MachineResponsibleRoleId,
            "machine-responsible@dev.local",
            "Development Machine Responsible",
            identityOptions.Value.ActiveUserPassword,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePersonaAccountAsync(
            dbContext,
            passwordHasher,
            DevelopmentAdminAccessIds.MaterialsSalesAccountId,
            DevelopmentAdminAccessIds.MaterialsSalesIdentitySubjectId,
            DevelopmentAdminAccessIds.MaterialsSalesMembershipId,
            DevelopmentAdminAccessIds.SalesOrderRoleId,
            "materials-sales@dev.local",
            "Development Materials Sales",
            identityOptions.Value.ActiveUserPassword,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePersonaAccountAsync(
            dbContext,
            passwordHasher,
            DevelopmentAdminAccessIds.MaintenanceTeamAccountId,
            DevelopmentAdminAccessIds.MaintenanceTeamIdentitySubjectId,
            DevelopmentAdminAccessIds.MaintenanceTeamMembershipId,
            DevelopmentAdminAccessIds.MaintenanceTeamRoleId,
            "maintenance-team@dev.local",
            "Development Maintenance Team",
            identityOptions.Value.ActiveUserPassword,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task BackfillPermissionMetadataAsync(
        NevaxisFactOSDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var permissions = await dbContext.PermissionDefinitions.ToListAsync(cancellationToken);
        foreach (var permission in permissions)
        {
            if (string.IsNullOrWhiteSpace(permission.NormalizedKey))
            {
                permission.NormalizedKey = AdminAccessValueConversions.NormalizePermissionKey(permission.Key);
            }

            if (string.IsNullOrWhiteSpace(permission.TurkishLabel))
            {
                permission.TurkishLabel = permission.Description;
            }

            if (string.IsNullOrWhiteSpace(permission.ModuleResourceCode))
            {
                permission.ModuleResourceCode = AdminAccessValueConversions.MapModuleResource(permission.Key);
            }

            if (string.IsNullOrWhiteSpace(permission.ActionCode))
            {
                permission.ActionCode = AdminAccessValueConversions.MapHonestAction(permission.Key);
            }

            if (string.IsNullOrWhiteSpace(permission.SensitivityClassification))
            {
                permission.SensitivityClassification = permission.Key.Contains("writeback", StringComparison.Ordinal)
                    || AdminAccessValueConversions.IsPlatformPermission(permission.Key)
                    || permission.Key == AdminAccessPermissionKeys.GrantOwner
                        ? AdminAccessSensitivity.Sensitive
                        : AdminAccessSensitivity.Standard;
            }

            permission.RequiresRecentAuth = permission.SensitivityClassification == AdminAccessSensitivity.Sensitive;
            if (string.IsNullOrWhiteSpace(permission.SupportedRecordScopes))
            {
                permission.SupportedRecordScopes = AdminAccessValueConversions.IsPlatformPermission(permission.Key)
                    || permission.Key.StartsWith("admin-access.", StringComparison.Ordinal)
                        ? AdminAccessRecordScopes.Tenant
                        : "facility,tenant";
            }
        }
    }

    private static async Task EnsureAdminAccessPermissionsAsync(
        NevaxisFactOSDbContext dbContext,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(
            dbContext,
            DevelopmentAdminAccessIds.AdminAccessReadPermissionId,
            AdminAccessPermissionKeys.Read,
            "Yönetim erişimini görüntüle",
            "admin-access",
            AdminAccessActions.View,
            AdminAccessSensitivity.Standard,
            false,
            AdminAccessRecordScopes.Tenant,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePermissionAsync(
            dbContext,
            DevelopmentAdminAccessIds.AdminAccessManagePermissionId,
            AdminAccessPermissionKeys.Manage,
            "Yönetim erişimini düzenle",
            "admin-access",
            AdminAccessActions.ManageSettings,
            AdminAccessSensitivity.Sensitive,
            true,
            AdminAccessRecordScopes.Tenant,
            seedTimestampUtc,
            cancellationToken);
        await EnsurePermissionAsync(
            dbContext,
            DevelopmentAdminAccessIds.AdminAccessGrantOwnerPermissionId,
            AdminAccessPermissionKeys.GrantOwner,
            "Kiracı sahibi ata",
            "admin-access",
            AdminAccessActions.ManageSettings,
            AdminAccessSensitivity.Sensitive,
            true,
            AdminAccessRecordScopes.Tenant,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsurePermissionAsync(
        NevaxisFactOSDbContext dbContext,
        Guid id,
        string key,
        string turkishLabel,
        string module,
        string action,
        string sensitivity,
        bool requiresRecentAuth,
        string scopes,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.PermissionDefinitions
            .SingleOrDefaultAsync(p => p.Key == key || p.Id == id, cancellationToken);
        if (existing is not null)
        {
            existing.NormalizedKey = key;
            existing.TurkishLabel = turkishLabel;
            existing.ModuleResourceCode = module;
            existing.ActionCode = action;
            existing.SensitivityClassification = sensitivity;
            existing.RequiresRecentAuth = requiresRecentAuth;
            existing.SupportedRecordScopes = scopes;
            return;
        }

        dbContext.PermissionDefinitions.Add(new PermissionDefinition
        {
            Id = id,
            Key = key,
            NormalizedKey = key,
            Description = turkishLabel,
            TurkishLabel = turkishLabel,
            ModuleResourceCode = module,
            ActionCode = action,
            SensitivityClassification = sensitivity,
            RequiresRecentAuth = requiresRecentAuth,
            SupportedRecordScopes = scopes,
            Status = AuthorizationStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        });
    }

    private static async Task BackfillMembershipReferencesAsync(
        NevaxisFactOSDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var memberships = await dbContext.TenantMemberships.IgnoreQueryFilters()
            .Where(m => m.MembershipReference == null || m.MembershipReference == "")
            .ToListAsync(cancellationToken);
        foreach (var membership in memberships)
        {
            membership.MembershipReference = TenantMembership.CreateReference(membership.Id);
            if (membership.AuthorizationVersion == 0)
            {
                membership.AuthorizationVersion = 1;
            }
        }
    }

    private static async Task EnsureTemplateRoleAsync(
        NevaxisFactOSDbContext dbContext,
        AdminAccessRoleTemplate template,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var tenantId = DevelopmentPilotIds.TenantId;
        var role = await dbContext.TenantRoles.IgnoreQueryFilters()
            .SingleOrDefaultAsync(r => r.TenantId == tenantId && (r.Id == template.RoleId || r.Key == template.RoleCode), cancellationToken);
        if (role is null)
        {
            role = new TenantRole
            {
                Id = template.RoleId,
                TenantId = tenantId,
                Key = template.RoleCode,
                DisplayName = template.DisplayNameTr,
                Description = template.DescriptionTr,
                SystemTemplateOrigin = "development-template",
                IsProtected = template.IsProtected,
                Status = AuthorizationStatus.Active,
                Version = 1,
                CreatedByActorReference = "system",
                UpdatedByActorReference = "system",
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
            };
            dbContext.TenantRoles.Add(role);
        }
        else
        {
            role.DisplayName = template.DisplayNameTr;
            role.Description = template.DescriptionTr;
            role.SystemTemplateOrigin = "development-template";
            role.IsProtected = template.IsProtected;
            if (role.Version == 0)
            {
                role.Version = 1;
            }
        }

        foreach (var permissionKey in template.PermissionKeys)
        {
            var permission = await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(p => p.Key == permissionKey, cancellationToken);
            if (permission is null)
            {
                continue;
            }

            var exists = await dbContext.TenantRolePermissions.IgnoreQueryFilters()
                .AnyAsync(
                    g => g.TenantId == tenantId && g.TenantRoleId == role.Id && g.PermissionDefinitionId == permission.Id,
                    cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.TenantRolePermissions.Add(new TenantRolePermissionAssignment
            {
                TenantId = tenantId,
                TenantRoleId = role.Id,
                PermissionDefinitionId = permission.Id,
            });
        }
    }

    private static async Task EnsureTenantOwnerFacilityMembershipAsync(
        NevaxisFactOSDbContext dbContext,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var tenantId = DevelopmentPilotIds.TenantId;
        var membershipId = DevelopmentSaasIdentityIds.TenantOwnerMembershipId;
        var existing = await dbContext.FacilityMemberships.IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId
                    && membership.FacilityId == DevelopmentPilotIds.FacilityId
                    && membership.TenantMembershipId == membershipId,
                cancellationToken);

        if (existing is not null)
        {
            existing.Status = FacilityMembershipStatus.Active;
            existing.UpdatedAtUtc = seedTimestampUtc;
            return;
        }

        dbContext.FacilityMemberships.Add(new FacilityMembership
        {
            Id = DevelopmentAdminAccessIds.TenantOwnerFacilityMembershipId,
            TenantId = tenantId,
            FacilityId = DevelopmentPilotIds.FacilityId,
            TenantMembershipId = membershipId,
            Status = FacilityMembershipStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
            Version = 1,
        });
    }

    private static async Task EnsureMembershipRoleAsync(
        NevaxisFactOSDbContext dbContext,
        Guid membershipId,
        Guid roleId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var tenantId = DevelopmentPilotIds.TenantId;
        var exists = await dbContext.TenantMembershipRoles.IgnoreQueryFilters()
            .AnyAsync(r => r.TenantMembershipId == membershipId && r.TenantRoleId == roleId, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.TenantMembershipRoles.Add(new TenantMembershipRole
        {
            TenantId = tenantId,
            TenantMembershipId = membershipId,
            TenantRoleId = roleId,
            Status = AuthorizationStatus.Active,
            AssignedAtUtc = seedTimestampUtc,
        });
    }

    private static async Task EnsurePersonaAccountAsync(
        NevaxisFactOSDbContext dbContext,
        PasswordHasher<SaasIdentityAccount> passwordHasher,
        Guid accountId,
        Guid identitySubjectId,
        Guid membershipId,
        Guid roleId,
        string email,
        string displayName,
        string password,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var tenantId = DevelopmentPilotIds.TenantId;
        var normalizedEmail = SaasIdentityEmailNormalizer.Normalize(email);
        if (!await dbContext.IdentitySubjects.AnyAsync(s => s.Id == identitySubjectId, cancellationToken))
        {
            dbContext.IdentitySubjects.Add(new IdentitySubject
            {
                Id = identitySubjectId,
                ExternalIssuer = SaasIdentityConstants.Issuer,
                ExternalSubject = accountId.ToString(),
                Status = IdentitySubjectStatus.Active,
                CreatedAtUtc = seedTimestampUtc,
            });
        }

        if (!await dbContext.SaasIdentityAccounts.AnyAsync(a => a.Id == accountId, cancellationToken))
        {
            var account = new SaasIdentityAccount
            {
                Id = accountId,
                AccountReference = $"acc_dev_{accountId:N}"[..Math.Min(100, $"acc_dev_{accountId:N}".Length)],
                EmailNormalized = normalizedEmail,
                DisplayName = displayName,
                LifecycleStatus = SaasIdentityAccountLifecycleStatuses.Active,
                EmailVerifiedAtUtc = seedTimestampUtc,
                SecurityStamp = Guid.NewGuid(),
                IdentitySubjectId = identitySubjectId,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
            };
            dbContext.SaasIdentityAccounts.Add(account);
            dbContext.SaasIdentityPasswordCredentials.Add(new SaasIdentityPasswordCredential
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PasswordHash = passwordHasher.HashPassword(account, password),
                PasswordHashVersion = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
            });
        }

        if (!await dbContext.TenantMemberships.IgnoreQueryFilters().AnyAsync(m => m.Id == membershipId, cancellationToken))
        {
            dbContext.TenantMemberships.Add(new TenantMembership
            {
                Id = membershipId,
                TenantId = tenantId,
                IdentitySubjectId = identitySubjectId,
                MembershipReference = TenantMembership.CreateReference(membershipId),
                Status = TenantMembershipStatus.Active,
                AuthorizationVersion = 1,
                CreatedAtUtc = seedTimestampUtc,
            });
        }

        await EnsureMembershipRoleAsync(dbContext, membershipId, roleId, seedTimestampUtc, cancellationToken);

        if (!await dbContext.FacilityMemberships.IgnoreQueryFilters().AnyAsync(
                f => f.TenantMembershipId == membershipId && f.FacilityId == DevelopmentPilotIds.FacilityId,
                cancellationToken))
        {
            dbContext.FacilityMemberships.Add(new FacilityMembership
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = DevelopmentPilotIds.FacilityId,
                TenantMembershipId = membershipId,
                Status = FacilityMembershipStatus.Active,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
            });
        }
    }
}
