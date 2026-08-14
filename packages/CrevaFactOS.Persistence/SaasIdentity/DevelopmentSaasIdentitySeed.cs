using CrevaFactOS.Persistence.Development;
using CrevaFactOS.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Persistence.SaasIdentity;

internal static class DevelopmentSaasIdentitySeed
{
    internal static async Task EnsureDevelopmentSaasIdentityFixturesAsync(
        CrevaFactOSDbContext dbContext,
        SaasIdentityMfaSecretProtector mfaSecretProtector,
        IOptions<DevelopmentSaasIdentityOptions> options,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var identityOptions = options.Value;
        if (!identityOptions.Enabled)
        {
            return;
        }

        var passwordHasher = new PasswordHasher<SaasIdentityAccount>();
        var tenantId = DevelopmentPilotIds.TenantId;

        await EnsureAccountAsync(
            dbContext,
            passwordHasher,
            mfaSecretProtector,
            DevelopmentSaasIdentityIds.TenantOwnerAccountId,
            DevelopmentSaasIdentityIds.TenantOwnerIdentitySubjectId,
            DevelopmentSaasIdentityIds.TenantOwnerMembershipId,
            DevelopmentSaasIdentityIds.TenantOwnerMfaMethodId,
            SaasIdentityEmailNormalizer.Normalize(identityOptions.TenantOwnerEmail),
            "Development Tenant Owner",
            SaasIdentityAccountLifecycleStatuses.Active,
            identityOptions.TenantOwnerPassword,
            tenantId,
            requireMfa: true,
            mfaSecret: identityOptions.MfaSharedSecret,
            seedTimestampUtc,
            cancellationToken);

        await EnsureAccountAsync(
            dbContext,
            passwordHasher,
            mfaSecretProtector,
            DevelopmentSaasIdentityIds.ActiveAccountId,
            DevelopmentSaasIdentityIds.ActiveIdentitySubjectId,
            DevelopmentSaasIdentityIds.ActiveMembershipId,
            mfaMethodId: null,
            SaasIdentityEmailNormalizer.Normalize(identityOptions.ActiveUserEmail),
            "Development Active User",
            SaasIdentityAccountLifecycleStatuses.Active,
            identityOptions.ActiveUserPassword,
            tenantId,
            requireMfa: false,
            mfaSecret: null,
            seedTimestampUtc,
            cancellationToken);

        await EnsureAccountAsync(
            dbContext,
            passwordHasher,
            mfaSecretProtector,
            DevelopmentSaasIdentityIds.DeactivatedAccountId,
            DevelopmentSaasIdentityIds.DeactivatedIdentitySubjectId,
            DevelopmentSaasIdentityIds.DeactivatedMembershipId,
            mfaMethodId: null,
            SaasIdentityEmailNormalizer.Normalize(identityOptions.DeactivatedUserEmail),
            "Development Deactivated User",
            SaasIdentityAccountLifecycleStatuses.Deactivated,
            identityOptions.DeactivatedUserPassword,
            tenantId,
            requireMfa: false,
            mfaSecret: null,
            seedTimestampUtc,
            cancellationToken);

        await EnsureAccountWithoutMembershipAsync(
            dbContext,
            passwordHasher,
            DevelopmentSaasIdentityIds.NoMembershipAccountId,
            DevelopmentSaasIdentityIds.NoMembershipIdentitySubjectId,
            SaasIdentityEmailNormalizer.Normalize(identityOptions.NoMembershipUserEmail),
            "Development No Membership User",
            identityOptions.NoMembershipUserPassword,
            seedTimestampUtc,
            cancellationToken);

        await EnsurePendingInvitationAsync(
            dbContext,
            tenantId,
            SaasIdentityEmailNormalizer.Normalize(identityOptions.PendingInvitationEmail),
            seedTimestampUtc,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAccountAsync(
        CrevaFactOSDbContext dbContext,
        PasswordHasher<SaasIdentityAccount> passwordHasher,
        SaasIdentityMfaSecretProtector mfaSecretProtector,
        Guid accountId,
        Guid identitySubjectId,
        Guid membershipId,
        Guid? mfaMethodId,
        string normalizedEmail,
        string displayName,
        string lifecycleStatus,
        string password,
        Guid tenantId,
        bool requireMfa,
        string? mfaSecret,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var account = await dbContext.SaasIdentityAccounts
            .SingleOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account is null)
        {
            await EnsureIdentitySubjectAsync(
                dbContext,
                identitySubjectId,
                accountId,
                seedTimestampUtc,
                cancellationToken);

            account = new SaasIdentityAccount
            {
                Id = accountId,
                AccountReference = $"acc_dev_{accountId:N}"[..Math.Min(100, $"acc_dev_{accountId:N}".Length)],
                EmailNormalized = normalizedEmail,
                DisplayName = displayName,
                LifecycleStatus = lifecycleStatus,
                EmailVerifiedAtUtc = lifecycleStatus == SaasIdentityAccountLifecycleStatuses.Active
                    ? seedTimestampUtc
                    : null,
                SecurityStamp = Guid.NewGuid(),
                IdentitySubjectId = identitySubjectId,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                Version = 1,
            };
            dbContext.SaasIdentityAccounts.Add(account);
        }

        await EnsurePasswordCredentialAsync(
            dbContext,
            passwordHasher,
            account,
            password,
            seedTimestampUtc,
            cancellationToken);

        await EnsureMembershipAsync(
            dbContext,
            membershipId,
            tenantId,
            identitySubjectId,
            seedTimestampUtc,
            cancellationToken);

        if (requireMfa && mfaMethodId.HasValue && !string.IsNullOrWhiteSpace(mfaSecret))
        {
            await EnsureMfaMethodAsync(
                dbContext,
                mfaSecretProtector,
                mfaMethodId.Value,
                accountId,
                mfaSecret,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task EnsureAccountWithoutMembershipAsync(
        CrevaFactOSDbContext dbContext,
        PasswordHasher<SaasIdentityAccount> passwordHasher,
        Guid accountId,
        Guid identitySubjectId,
        string normalizedEmail,
        string displayName,
        string password,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await dbContext.SaasIdentityAccounts.AsNoTracking()
                .AnyAsync(a => a.Id == accountId, cancellationToken))
        {
            return;
        }

        await EnsureIdentitySubjectAsync(
            dbContext,
            identitySubjectId,
            accountId,
            seedTimestampUtc,
            cancellationToken);

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

        await EnsurePasswordCredentialAsync(
            dbContext,
            passwordHasher,
            account,
            password,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureIdentitySubjectAsync(
        CrevaFactOSDbContext dbContext,
        Guid identitySubjectId,
        Guid accountId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.IdentitySubjects.AsNoTracking()
                .AnyAsync(s => s.Id == identitySubjectId, cancellationToken))
        {
            return;
        }

        dbContext.IdentitySubjects.Add(new IdentitySubject
        {
            Id = identitySubjectId,
            ExternalIssuer = SaasIdentityConstants.Issuer,
            ExternalSubject = accountId.ToString(),
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = seedTimestampUtc,
        });
    }

    private static async Task EnsurePasswordCredentialAsync(
        CrevaFactOSDbContext dbContext,
        PasswordHasher<SaasIdentityAccount> passwordHasher,
        SaasIdentityAccount account,
        string password,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SaasIdentityPasswordCredentials.AsNoTracking()
                .AnyAsync(c => c.AccountId == account.Id, cancellationToken))
        {
            return;
        }

        dbContext.SaasIdentityPasswordCredentials.Add(new SaasIdentityPasswordCredential
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PasswordHash = passwordHasher.HashPassword(account, password),
            PasswordHashVersion = 1,
            CreatedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        });
    }

    private static async Task EnsureMembershipAsync(
        CrevaFactOSDbContext dbContext,
        Guid membershipId,
        Guid tenantId,
        Guid identitySubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.TenantMemberships.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(m => m.Id == membershipId, cancellationToken))
        {
            return;
        }

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

    private static async Task EnsureMfaMethodAsync(
        CrevaFactOSDbContext dbContext,
        SaasIdentityMfaSecretProtector mfaSecretProtector,
        Guid mfaMethodId,
        Guid accountId,
        string mfaSecret,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SaasIdentityMfaMethods.AsNoTracking()
                .AnyAsync(m => m.Id == mfaMethodId, cancellationToken))
        {
            return;
        }

        dbContext.SaasIdentityMfaMethods.Add(new SaasIdentityMfaMethod
        {
            Id = mfaMethodId,
            AccountId = accountId,
            EncryptedSecret = mfaSecretProtector.Protect(mfaSecret.Trim()),
            IsRequired = true,
            EnrolledAtUtc = seedTimestampUtc,
        });
    }

    private static async Task EnsurePendingInvitationAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        string intendedEmailNormalized,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SaasIdentityInvitations.AsNoTracking()
                .AnyAsync(i => i.Id == DevelopmentSaasIdentityIds.PendingInvitationId, cancellationToken))
        {
            return;
        }

        const string developmentInvitationToken = "dev-invitation-token-do-not-use-in-production";
        dbContext.SaasIdentityInvitations.Add(new SaasIdentityInvitation
        {
            Id = DevelopmentSaasIdentityIds.PendingInvitationId,
            InvitationReference = "inv_dev_pending_fixture",
            TokenHash = SaasIdentityTokenUtilities.HashToken(developmentInvitationToken),
            TenantId = tenantId,
            IntendedEmailNormalized = intendedEmailNormalized,
            ExpiresAtUtc = seedTimestampUtc.Add(SaasIdentityConstants.InvitationTokenLifetime),
            CreatedAtUtc = seedTimestampUtc,
        });
    }
}
