using System.Security.Cryptography;
using System.Text;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Development;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasIdentity;
using CrevaFactOS.Persistence.SaasSubscription;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.AdminAccess;

public sealed partial class AdminAccessRepository
{
    public Task<AdminAccessWorkspaceSummaryReadModel?> GetWorkspaceSummaryAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var tenant = await dbContext.Tenants.AsNoTracking().SingleOrDefaultAsync(ct);
                if (tenant is null)
                {
                    return null;
                }

                var now = DateTimeOffset.UtcNow;
                var activeMembers = await dbContext.TenantMemberships.AsNoTracking()
                    .CountAsync(m => m.Status == TenantMembershipStatus.Active, ct);
                var suspendedMembers = await dbContext.TenantMemberships.AsNoTracking()
                    .CountAsync(m => m.Status == TenantMembershipStatus.Inactive, ct);
                var pendingInvitations = await dbContext.SaasIdentityInvitations.AsNoTracking()
                    .CountAsync(i => i.ConsumedAtUtc == null && i.ExpiresAtUtc > now, ct);
                var activeSessions = await (
                    from session in dbContext.SaasIdentitySessions.AsNoTracking()
                    where session.TenantId == tenant.Id
                        && session.RevokedAtUtc == null
                        && session.IdleExpiresAtUtc > now
                        && session.AbsoluteExpiresAtUtc > now
                    select session.Id).CountAsync(ct);

                return new AdminAccessWorkspaceSummaryReadModel(
                    activeMembers,
                    pendingInvitations,
                    suspendedMembers,
                    activeSessions,
                    tenant.DisplayName,
                    tenant.TenantKey);
            },
            cancellationToken);

    public Task<IReadOnlyList<AdminAccessMemberReadModel>> ListMembersAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var memberships = await dbContext.TenantMemberships.AsNoTracking()
                    .OrderBy(m => m.MembershipReference)
                    .ToListAsync(ct);
                var accounts = await dbContext.SaasIdentityAccounts.AsNoTracking().ToListAsync(ct);
                var mfaMethods = await dbContext.SaasIdentityMfaMethods.AsNoTracking().ToListAsync(ct);
                var now = DateTimeOffset.UtcNow;

                var members = new List<AdminAccessMemberReadModel>();
                foreach (var membership in memberships)
                {
                    var account = accounts.SingleOrDefault(a => a.IdentitySubjectId == membership.IdentitySubjectId);
                    var mfa = account is null
                        ? null
                        : mfaMethods.SingleOrDefault(m => m.AccountId == account.Id);
                    var roleReferences = await LoadMemberRoleKeysAsync(membership.Id, ct);
                    var facilityCodes = await LoadMemberFacilityCodesAsync(membership.Id, ct);
                    var lastSession = account is null
                        ? null
                        : await dbContext.SaasIdentitySessions.AsNoTracking()
                            .Where(s => s.AccountId == account.Id && s.TenantId == membership.TenantId)
                            .OrderByDescending(s => s.LastSeenAtUtc)
                            .Select(s => (DateTimeOffset?)s.LastSeenAtUtc)
                            .FirstOrDefaultAsync(ct);

                    members.Add(new AdminAccessMemberReadModel(
                        membership.MembershipReference,
                        account?.DisplayName ?? "Bilinmeyen hesap",
                        MaskEmail(account?.EmailNormalized),
                        membership.Status == TenantMembershipStatus.Active
                            ? AdminAccessMembershipDisplayStatuses.Active
                            : AdminAccessMembershipDisplayStatuses.Suspended,
                        roleReferences,
                        facilityCodes,
                        mfa is not null,
                        mfa?.IsRequired == true,
                        lastSession,
                        membership.AuthorizationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }

                return (IReadOnlyList<AdminAccessMemberReadModel>)members;
            },
            cancellationToken);

    public Task<IReadOnlyList<AdminAccessInvitationReadModel>> ListInvitationsAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var now = DateTimeOffset.UtcNow;
                var invitations = await dbContext.SaasIdentityInvitations.AsNoTracking()
                    .Where(i => i.ConsumedAtUtc == null)
                    .OrderByDescending(i => i.CreatedAtUtc)
                    .ToListAsync(ct);
                var roles = await dbContext.TenantRoles.AsNoTracking().ToListAsync(ct);
                var deliveryByReference = await LoadInvitationDeliveryStatusesAsync(
                    invitations.Select(i => i.InvitationReference).ToArray(),
                    ct);

                return (IReadOnlyList<AdminAccessInvitationReadModel>)invitations
                    .Where(i => i.ExpiresAtUtc > now)
                    .Select(invitation =>
                    {
                        var role = invitation.IntendedRoleId is null
                            ? null
                            : roles.SingleOrDefault(r => r.Id == invitation.IntendedRoleId);
                        deliveryByReference.TryGetValue(
                            invitation.InvitationReference,
                            out var deliveryStatus);
                        return new AdminAccessInvitationReadModel(
                            invitation.InvitationReference,
                            MaskEmail(invitation.IntendedEmailNormalized),
                            deliveryStatus ?? AdminAccessInvitationDeliveryStatuses.CreatedNotDelivered,
                            invitation.ExpiresAtUtc,
                            invitation.CreatedAtUtc,
                            role?.Key);
                    })
                    .ToList();
            },
            cancellationToken);

    public Task<IReadOnlyList<AdminAccessFacilitySummaryReadModel>> ListFacilitiesAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var facilities = await dbContext.Facilities.AsNoTracking()
                    .Where(f => f.Id != DevelopmentPilotIds.UnauthorizedFacilityId)
                    .OrderBy(f => f.Code)
                    .ToListAsync(ct);
                return (IReadOnlyList<AdminAccessFacilitySummaryReadModel>)facilities
                    .Select(f => new AdminAccessFacilitySummaryReadModel(
                        f.Code,
                        f.Name,
                        f.Status.ToString()))
                    .ToList();
            },
            cancellationToken);

    public Task<IReadOnlyList<AdminAccessTenantSessionReadModel>> ListTenantSessionsAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var now = DateTimeOffset.UtcNow;
                var sessions = await dbContext.SaasIdentitySessions.AsNoTracking()
                    .OrderByDescending(s => s.LastSeenAtUtc)
                    .ToListAsync(ct);
                var memberships = await dbContext.TenantMemberships.AsNoTracking().ToListAsync(ct);
                var accounts = await dbContext.SaasIdentityAccounts.AsNoTracking().ToListAsync(ct);

                return (IReadOnlyList<AdminAccessTenantSessionReadModel>)sessions
                    .Select(session =>
                    {
                        var membership = memberships.SingleOrDefault(m => m.Id == session.TenantMembershipId);
                        var account = accounts.SingleOrDefault(a => a.Id == session.AccountId);
                        var isActive = session.RevokedAtUtc is null
                            && session.IdleExpiresAtUtc > now
                            && session.AbsoluteExpiresAtUtc > now;
                        return new AdminAccessTenantSessionReadModel(
                            membership?.MembershipReference ?? "unknown",
                            account?.DisplayName ?? "Bilinmeyen hesap",
                            MaskEmail(account?.EmailNormalized),
                            session.CreatedAtUtc,
                            session.LastSeenAtUtc,
                            session.IdleExpiresAtUtc,
                            session.AbsoluteExpiresAtUtc,
                            session.MfaSatisfied,
                            !isActive,
                            session.DeviceLabel,
                            session.SessionReference);
                    })
                    .ToList();
            },
            cancellationToken);

    public Task<CreateAdminAccessInvitationResult> CreateInvitationAsync(
        CreateAdminAccessInvitationCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteInvitationMutationAsync(
            command.IdempotencyKey,
            command,
            actor,
            async ct =>
            {
                var normalizedEmail = SaasIdentityEmailNormalizer.Normalize(command.Email);
                if (string.IsNullOrWhiteSpace(normalizedEmail) || !normalizedEmail.Contains('@'))
                {
                    return CreateAdminAccessInvitationResult.Fail(
                        AdminAccessFailureCodes.ValidationFailed,
                        "Email is invalid.");
                }

                var existingMembership = await (
                    from membership in dbContext.TenantMemberships.AsNoTracking()
                    join account in dbContext.SaasIdentityAccounts.AsNoTracking()
                        on membership.IdentitySubjectId equals account.IdentitySubjectId
                    where account.EmailNormalized == normalizedEmail
                        && membership.Status == TenantMembershipStatus.Active
                    select membership.Id).AnyAsync(ct);
                if (existingMembership)
                {
                    return CreateAdminAccessInvitationResult.Fail(
                        AdminAccessFailureCodes.ValidationFailed,
                        "An active membership already exists for this email.");
                }

                TenantRole? intendedRole = null;
                if (!string.IsNullOrWhiteSpace(command.IntendedRoleReference))
                {
                    intendedRole = await FindRoleAsync(command.IntendedRoleReference, ct);
                    if (intendedRole is null || intendedRole.Status != AuthorizationStatus.Active)
                    {
                        return CreateAdminAccessInvitationResult.Fail(
                            AdminAccessFailureCodes.InvalidGrant,
                            "Intended role is invalid.");
                    }

                    if (intendedRole.Key == AdminAccessRoleCodes.TenantOwner)
                    {
                        var actorIsOwner = await HasActiveOwnerRoleAsync(actor.MembershipId, ct);
                        var actorKeys = await LoadGrantedKeysAsync(actor.MembershipId, ct);
                        if (!actorIsOwner || !actorKeys.Contains(AdminAccessPermissionKeys.GrantOwner))
                        {
                            return CreateAdminAccessInvitationResult.Fail(
                                AdminAccessFailureCodes.Forbidden,
                                "Tenant-owner can only be granted by a current tenant owner.");
                        }
                    }
                }

                var now = DateTimeOffset.UtcNow;
                var pendingInvitation = await dbContext.SaasIdentityInvitations
                    .SingleOrDefaultAsync(
                        i => i.IntendedEmailNormalized == normalizedEmail && i.ConsumedAtUtc == null && i.ExpiresAtUtc > now,
                        ct);
                if (pendingInvitation is not null)
                {
                    var existingDelivery = await LoadInvitationDeliveryStatusAsync(pendingInvitation.InvitationReference, ct);
                    return CreateAdminAccessInvitationResult.Ok(
                        pendingInvitation.InvitationReference,
                        existingDelivery ?? AdminAccessInvitationDeliveryStatuses.CreatedNotDelivered,
                        idempotentReplay: true);
                }

                var seatEvaluation = await entitlementEvaluator.EvaluateUserLimitForAdditionalSeatsAsync(1, ct);
                if (!seatEvaluation.Allowed)
                {
                    return CreateAdminAccessInvitationResult.Fail(
                        AdminAccessFailureCodes.SubscriptionLimitExceeded,
                        "Tenant user seat limit has been reached.");
                }

                var invitationId = Guid.NewGuid();
                var invitationReference = $"inv_{invitationId:N}";
                var rawToken = SaasIdentityTokenUtilities.GenerateOpaqueToken();
                var deliveryStatus = await TryDeliverInvitationAsync(normalizedEmail, rawToken, ct);

                dbContext.SaasIdentityInvitations.Add(new SaasIdentityInvitation
                {
                    Id = invitationId,
                    InvitationReference = invitationReference,
                    TokenHash = SaasIdentityTokenUtilities.HashToken(rawToken),
                    TenantId = actor.TenantId,
                    IntendedEmailNormalized = normalizedEmail,
                    IntendedRoleId = intendedRole?.Id,
                    ExpiresAtUtc = now.Add(SaasIdentityConstants.InvitationTokenLifetime),
                    CreatedAtUtc = now,
                });

                AppendAudit(
                    actor,
                    AdminAccessAuditEventTypes.InvitationCreated,
                    AdminAccessAuditEntityTypes.Membership,
                    invitationId.ToString(),
                    null,
                    new
                    {
                        emailMasked = MaskEmail(normalizedEmail),
                        deliveryStatus,
                        intendedRole = intendedRole?.Key,
                    });

                await dbContext.SaveChangesAsync(ct);
                return CreateAdminAccessInvitationResult.Ok(invitationReference, deliveryStatus);
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> SuspendMemberAsync(
        SuspendAdminAccessMemberCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "member.suspend",
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
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.SelfMutation, "Users cannot suspend their own membership.");
                }

                if (membership.AuthorizationVersion != command.ExpectedAuthorizationVersion)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ConcurrencyConflict, "Authorization version is stale.");
                }

                if (membership.Status == TenantMembershipStatus.Inactive)
                {
                    return AdminAccessWriteResult.Ok(idempotentReplay: true);
                }

                if (await HasActiveOwnerRoleAsync(membership.Id, ct)
                    && await CountActiveOwnersAsync(membership.Id, ct) <= 0)
                {
                    return AdminAccessWriteResult.Fail(
                        AdminAccessFailureCodes.FinalOwnerProtected,
                        "Final active tenant owner cannot be suspended.");
                }

                var before = new { status = membership.Status.ToString(), membership.AuthorizationVersion };
                membership.Status = TenantMembershipStatus.Inactive;
                membership.AuthorizationVersion++;
                await RevokeSessionsForMembershipAsync(membership, ct);
                AppendAudit(
                    actor,
                    AdminAccessAuditEventTypes.MembershipSuspended,
                    AdminAccessAuditEntityTypes.Membership,
                    membership.MembershipReference,
                    before,
                    new { status = membership.Status.ToString(), membership.AuthorizationVersion });
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> ReactivateMemberAsync(
        ReactivateAdminAccessMemberCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "member.reactivate",
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
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.SelfMutation, "Users cannot reactivate their own membership.");
                }

                if (membership.AuthorizationVersion != command.ExpectedAuthorizationVersion)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.ConcurrencyConflict, "Authorization version is stale.");
                }

                if (membership.Status == TenantMembershipStatus.Active)
                {
                    return AdminAccessWriteResult.Ok(idempotentReplay: true);
                }

                var before = new { status = membership.Status.ToString(), membership.AuthorizationVersion };
                membership.Status = TenantMembershipStatus.Active;
                membership.AuthorizationVersion++;
                AppendAudit(
                    actor,
                    AdminAccessAuditEventTypes.MembershipReactivated,
                    AdminAccessAuditEntityTypes.Membership,
                    membership.MembershipReference,
                    before,
                    new { status = membership.Status.ToString(), membership.AuthorizationVersion });
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> RevokeSessionAsync(
        RevokeAdminAccessSessionCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "session.revoke",
            command,
            actor,
            async ct =>
            {
                var session = await dbContext.SaasIdentitySessions
                    .SingleOrDefaultAsync(s => s.SessionReference == command.SessionReference.Trim(), ct);
                if (session is null || session.TenantId != actor.TenantId)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.NotFound, "Session was not found.");
                }

                if (session.RevokedAtUtc is not null)
                {
                    return AdminAccessWriteResult.Ok(idempotentReplay: true);
                }

                var now = DateTimeOffset.UtcNow;
                session.RevokedAtUtc = now;
                session.RevokedReason = "admin-revoked";
                AppendAudit(
                    actor,
                    AdminAccessAuditEventTypes.SessionRevoked,
                    AdminAccessAuditEntityTypes.Membership,
                    TenantMembership.CreateReference(session.TenantMembershipId),
                    null,
                    new { sessionReference = session.SessionReference });
                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    public Task<AdminAccessWriteResult> RevokeAllMemberSessionsAsync(
        RevokeAllAdminAccessMemberSessionsCommand command,
        AdminAccessActorContext actor,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "member.sessions.revoke-all",
            command,
            actor,
            async ct =>
            {
                var membership = await FindMembershipAsync(command.MemberReference, ct);
                if (membership is null)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.NotFound, "Member was not found.");
                }

                var account = await dbContext.SaasIdentityAccounts.AsNoTracking()
                    .SingleOrDefaultAsync(a => a.IdentitySubjectId == membership.IdentitySubjectId, ct);
                if (account is null)
                {
                    return AdminAccessWriteResult.Fail(AdminAccessFailureCodes.NotFound, "Account was not found.");
                }

                var now = DateTimeOffset.UtcNow;
                var sessions = await dbContext.SaasIdentitySessions
                    .Where(s => s.AccountId == account.Id
                        && s.TenantId == membership.TenantId
                        && s.RevokedAtUtc == null)
                    .ToListAsync(ct);

                foreach (var session in sessions)
                {
                    session.RevokedAtUtc = now;
                    session.RevokedReason = "admin-revoked-all";
                    AppendAudit(
                        actor,
                        AdminAccessAuditEventTypes.SessionRevoked,
                        AdminAccessAuditEntityTypes.Membership,
                        membership.MembershipReference,
                        null,
                        new { sessionReference = session.SessionReference, bulk = true });
                }

                await dbContext.SaveChangesAsync(ct);
                return AdminAccessWriteResult.Ok();
            },
            cancellationToken);

    private async Task<CreateAdminAccessInvitationResult> ExecuteInvitationMutationAsync(
        string? idempotencyKey,
        CreateAdminAccessInvitationCommand command,
        AdminAccessActorContext actor,
        Func<CancellationToken, Task<CreateAdminAccessInvitationResult>> action,
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
                        if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
                        {
                            return CreateAdminAccessInvitationResult.Fail(
                                AdminAccessFailureCodes.IdempotencyConflict,
                                "Idempotent retry conflicts with the original request.");
                        }

                        var normalizedEmail = SaasIdentityEmailNormalizer.Normalize(command.Email);
                        var now = DateTimeOffset.UtcNow;
                        var replayInvitation = await dbContext.SaasIdentityInvitations.AsNoTracking()
                            .Where(i => i.IntendedEmailNormalized == normalizedEmail && i.ConsumedAtUtc == null && i.ExpiresAtUtc > now)
                            .OrderByDescending(i => i.CreatedAtUtc)
                            .FirstOrDefaultAsync(ct);
                        if (replayInvitation is null)
                        {
                            return CreateAdminAccessInvitationResult.Fail(
                                AdminAccessFailureCodes.NotFound,
                                "Invitation replay could not be resolved.");
                        }

                        var replayDelivery = await LoadInvitationDeliveryStatusAsync(replayInvitation.InvitationReference, ct);
                        return CreateAdminAccessInvitationResult.Ok(
                            replayInvitation.InvitationReference,
                            replayDelivery ?? AdminAccessInvitationDeliveryStatuses.CreatedNotDelivered,
                            idempotentReplay: true);
                    }

                    dbContext.AdminAccessIdempotencyRecords.Add(new AdminAccessIdempotencyRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = actor.TenantId,
                        IdempotencyKey = idempotencyKey,
                        RequestHash = hash,
                        Operation = "invitation.create",
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

    private async Task<string> TryDeliverInvitationAsync(
        string recipientEmail,
        string rawToken,
        CancellationToken cancellationToken)
    {
        try
        {
            if (developmentTokenDeliverySink is not null)
            {
                developmentTokenDeliverySink.Capture(
                    SaasIdentityTokenDeliveryKinds.Invitation,
                    recipientEmail,
                    rawToken);
                return AdminAccessInvitationDeliveryStatuses.CreatedDelivered;
            }

            await emailDeliveryProvider.DeliverInvitationAsync(recipientEmail, rawToken, cancellationToken);
            return AdminAccessInvitationDeliveryStatuses.CreatedDelivered;
        }
        catch (InvalidOperationException)
        {
            return AdminAccessInvitationDeliveryStatuses.CreatedNotDelivered;
        }
    }

    private async Task RevokeSessionsForMembershipAsync(
        TenantMembership membership,
        CancellationToken cancellationToken)
    {
        var accountId = await dbContext.SaasIdentityAccounts.AsNoTracking()
            .Where(a => a.IdentitySubjectId == membership.IdentitySubjectId)
            .Select(a => a.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var sessions = await dbContext.SaasIdentitySessions
            .Where(s => s.AccountId == accountId
                && s.TenantId == membership.TenantId
                && s.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAtUtc = now;
            session.RevokedReason = "membership-suspended";
        }
    }

    private async Task<Dictionary<string, string>> LoadInvitationDeliveryStatusesAsync(
        string[] invitationReferences,
        CancellationToken cancellationToken)
    {
        if (invitationReferences.Length == 0)
        {
            return [];
        }

        var invitations = await dbContext.SaasIdentityInvitations.AsNoTracking()
            .Where(i => invitationReferences.Contains(i.InvitationReference))
            .ToListAsync(cancellationToken);
        if (invitations.Count == 0)
        {
            return [];
        }

        var invitationIds = invitations
            .Select(i => i.Id.ToString())
            .ToArray();
        var events = await dbContext.AuthorizationAuditEvents.AsNoTracking()
            .Where(e => e.EventType == AdminAccessAuditEventTypes.InvitationCreated
                && invitationIds.Contains(e.EntityReference))
            .ToListAsync(cancellationToken);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var invitation in invitations)
        {
            var auditEvent = events.SingleOrDefault(e => e.EntityReference == invitation.Id.ToString());
            var status = ExtractDeliveryStatus(auditEvent?.AfterJson);
            if (status is not null)
            {
                result[invitation.InvitationReference] = status;
            }
        }

        return result;
    }

    private async Task<string?> LoadInvitationDeliveryStatusAsync(
        string invitationReference,
        CancellationToken cancellationToken)
    {
        var invitation = await dbContext.SaasIdentityInvitations.AsNoTracking()
            .SingleOrDefaultAsync(i => i.InvitationReference == invitationReference, cancellationToken);
        if (invitation is null)
        {
            return null;
        }

        var afterJson = await dbContext.AuthorizationAuditEvents.AsNoTracking()
            .Where(e => e.EventType == AdminAccessAuditEventTypes.InvitationCreated
                && e.EntityReference == invitation.Id.ToString())
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => e.AfterJson)
            .FirstOrDefaultAsync(cancellationToken);

        return ExtractDeliveryStatus(afterJson);
    }

    private static string? ExtractDeliveryStatus(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = System.Text.Json.JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("deliveryStatus", out var deliveryStatus))
        {
            return deliveryStatus.GetString();
        }

        return null;
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "—";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return email;
        }

        var localPart = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var visible = localPart.Length > 0 ? localPart[..1] : string.Empty;
        return $"{visible}***@{domain}";
    }
}
