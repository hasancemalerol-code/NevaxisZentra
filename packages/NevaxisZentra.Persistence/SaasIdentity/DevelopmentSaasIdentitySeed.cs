using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace NevaxisZentra.Persistence.SaasIdentity;

internal static class DevelopmentSaasIdentitySeed
{
    internal static async Task EnsureDevelopmentSaasIdentityFixturesAsync(
        NevaxisZentraDbContext dbContext,
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
        NevaxisZentraDbContext dbContext,
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
        else if (!string.Equals(account.EmailNormalized, normalizedEmail, StringComparison.Ordinal))
        {
            account.EmailNormalized = normalizedEmail;
            account.UpdatedAtUtc = seedTimestampUtc;
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
        NevaxisZentraDbContext dbContext,
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
        NevaxisZentraDbContext dbContext,
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
        NevaxisZentraDbContext dbContext,
        PasswordHasher<SaasIdentityAccount> passwordHasher,
        SaasIdentityAccount account,
        string password,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existingCredential = await dbContext.SaasIdentityPasswordCredentials
            .SingleOrDefaultAsync(c => c.AccountId == account.Id, cancellationToken);
        if (existingCredential is not null)
        {
            // Development/acceptance bootstrap only: this seed is reachable solely through the
            // development pilot bootstrap (guarded against non-Development hosts).
            TrySyncExistingCredential(passwordHasher, account, existingCredential, password, seedTimestampUtc);
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

    /// <summary>
    /// Re-aligns an already-seeded development credential with the configured development password.
    /// Verifies through the canonical <see cref="PasswordHasher{TUser}"/> (never compares hashes); a password
    /// that already verifies causes no mutation. On mismatch the hash is replaced and the account security
    /// stamp is rotated so sessions issued against the old credential stop resolving.
    /// </summary>
    /// <returns><c>true</c> when the credential was rewritten.</returns>
    internal static bool TrySyncExistingCredential(
        PasswordHasher<SaasIdentityAccount> passwordHasher,
        SaasIdentityAccount account,
        SaasIdentityPasswordCredential credential,
        string password,
        DateTimeOffset seedTimestampUtc)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var verification = passwordHasher.VerifyHashedPassword(account, credential.PasswordHash, password);
        if (verification != PasswordVerificationResult.Failed)
        {
            return false;
        }

        credential.PasswordHash = passwordHasher.HashPassword(account, password);
        credential.UpdatedAtUtc = seedTimestampUtc;
        account.SecurityStamp = Guid.NewGuid();
        account.UpdatedAtUtc = seedTimestampUtc;
        return true;
    }

    private static async Task EnsureMembershipAsync(
        NevaxisZentraDbContext dbContext,
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
        NevaxisZentraDbContext dbContext,
        SaasIdentityMfaSecretProtector mfaSecretProtector,
        Guid mfaMethodId,
        Guid accountId,
        string mfaSecret,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var normalizedSecret = mfaSecret.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSecret))
        {
            return;
        }

        var existing = await dbContext.SaasIdentityMfaMethods
            .SingleOrDefaultAsync(m => m.Id == mfaMethodId, cancellationToken);
        if (existing is null)
        {
            dbContext.SaasIdentityMfaMethods.Add(new SaasIdentityMfaMethod
            {
                Id = mfaMethodId,
                AccountId = accountId,
                EncryptedSecret = mfaSecretProtector.Protect(normalizedSecret),
                IsRequired = true,
                EnrolledAtUtc = seedTimestampUtc,
            });
            return;
        }

        existing.EncryptedSecret = mfaSecretProtector.Protect(normalizedSecret);
        existing.IsRequired = true;
    }

    private static async Task EnsurePendingInvitationAsync(
        NevaxisZentraDbContext dbContext,
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
