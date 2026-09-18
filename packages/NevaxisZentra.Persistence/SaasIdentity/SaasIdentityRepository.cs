using System.Text.Json;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.SaasIdentity;

public sealed class SaasIdentityRepository(
    NevaxisZentraDbContext dbContext,
    SaasIdentityMfaSecretProtector mfaSecretProtector,
    IIdentityEmailDeliveryProvider emailDeliveryProvider,
    TenantContextAccessor tenantContextAccessor,
    IDevelopmentTokenDeliverySink? developmentTokenDeliverySink = null) : ISaasIdentityRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly PasswordHasher<SaasIdentityAccount> passwordHasher = new();

    public async Task<SaasIdentitySignInResult> SignInAsync(
        SaasIdentitySignInCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedEmail = SaasIdentityEmailNormalizer.Normalize(command.Email);
        var normalizedTenantKey = SaasIdentityEmailNormalizer.NormalizeOrganizationContext(command.OrganizationContext);

        FormattableString tenantLookupSql =
            $"SELECT tenant_id AS \"TenantId\", tenant_key AS \"TenantKey\" FROM resolve_active_tenant({normalizedTenantKey})";
        var tenantMatch = await dbContext.Database
            .SqlQuery<TenantSignInContext>(tenantLookupSql)
            .SingleOrDefaultAsync(cancellationToken);

        if (tenantMatch is null)
        {
            await AppendAuditAsync(
                null,
                null,
                SaasIdentitySecurityEventTypes.SignInFailure,
                new { reason = "tenant-context" },
                command.CorrelationId,
                now,
                cancellationToken);
            return SaasIdentitySignInResult.GenericFailure();
        }

        var tenantId = tenantMatch.TenantId;

        var account = await dbContext.SaasIdentityAccounts
            .SingleOrDefaultAsync(a => a.EmailNormalized == normalizedEmail, cancellationToken);

        if (account is null)
        {
            await AppendAuditAsync(
                tenantId,
                null,
                SaasIdentitySecurityEventTypes.SignInFailure,
                new { reason = "credentials" },
                command.CorrelationId,
                now,
                cancellationToken);
            return SaasIdentitySignInResult.GenericFailure();
        }

        if (account.LifecycleStatus == SaasIdentityAccountLifecycleStatuses.Deactivated)
        {
            await AppendAuditAsync(
                tenantId,
                account.Id,
                SaasIdentitySecurityEventTypes.SignInFailure,
                new { reason = "deactivated" },
                command.CorrelationId,
                now,
                cancellationToken);
            return SaasIdentitySignInResult.GenericFailure();
        }

        if (account.LockoutUntilUtc is not null && account.LockoutUntilUtc > now)
        {
            await AppendAuditAsync(
                tenantId,
                account.Id,
                SaasIdentitySecurityEventTypes.TemporaryLockout,
                new { untilUtc = account.LockoutUntilUtc },
                command.CorrelationId,
                now,
                cancellationToken);
            return SaasIdentitySignInResult.GenericFailure();
        }

        if (account.LockoutUntilUtc is not null && account.LockoutUntilUtc <= now)
        {
            account.FailedSignInCount = 0;
            account.LockoutUntilUtc = null;
            if (account.LifecycleStatus == SaasIdentityAccountLifecycleStatuses.Locked)
            {
                account.LifecycleStatus = SaasIdentityAccountLifecycleStatuses.Active;
            }
        }

        var credential = await dbContext.SaasIdentityPasswordCredentials
            .SingleOrDefaultAsync(c => c.AccountId == account.Id, cancellationToken);

        if (credential is null
            || passwordHasher.VerifyHashedPassword(account, credential.PasswordHash, command.Password)
                == PasswordVerificationResult.Failed)
        {
            account.FailedSignInCount++;
            account.UpdatedAtUtc = now;

            if (account.FailedSignInCount >= SaasIdentityConstants.MaxFailedSignInAttempts)
            {
                account.LockoutUntilUtc = now.Add(SaasIdentityConstants.LockoutDuration);
                account.LifecycleStatus = SaasIdentityAccountLifecycleStatuses.Locked;
                await AppendAuditAsync(
                    tenantId,
                    account.Id,
                    SaasIdentitySecurityEventTypes.TemporaryLockout,
                    new { untilUtc = account.LockoutUntilUtc },
                    command.CorrelationId,
                    now,
                    cancellationToken);
            }

            await AppendAuditAsync(
                tenantId,
                account.Id,
                SaasIdentitySecurityEventTypes.SignInFailure,
                new { reason = "credentials" },
                command.CorrelationId,
                now,
                cancellationToken);
            await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
            {
                await dbContext.SetTenantSessionContextAsync(tenantId, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            return SaasIdentitySignInResult.GenericFailure();
        }

        FormattableString membershipLookupSql =
            $"SELECT tenant_id AS \"TenantId\", tenant_key AS \"TenantKey\", membership_id AS \"MembershipId\" FROM resolve_active_tenant_membership({account.IdentitySubjectId}, {normalizedTenantKey})";
        var membershipMatch = await dbContext.Database
            .SqlQuery<MembershipSignInContext>(membershipLookupSql)
            .SingleOrDefaultAsync(cancellationToken);

        if (membershipMatch is null)
        {
            await AppendAuditAsync(
                tenantId,
                account.Id,
                SaasIdentitySecurityEventTypes.SignInFailure,
                new { reason = "membership" },
                command.CorrelationId,
                now,
                cancellationToken);
            return SaasIdentitySignInResult.GenericFailure();
        }

        account.FailedSignInCount = 0;
        account.LockoutUntilUtc = null;
        account.UpdatedAtUtc = now;

        var mfaMethod = await dbContext.SaasIdentityMfaMethods.AsNoTracking()
            .SingleOrDefaultAsync(m => m.AccountId == account.Id, cancellationToken);
        var requiresMfa = mfaMethod is not null && mfaMethod.IsRequired;

        var (session, rawToken) = CreateSession(
            account,
            tenantId,
            membershipMatch.MembershipId,
            command.DeviceLabel,
            now,
            requiresMfa);

        dbContext.SaasIdentitySessions.Add(session);

        await AppendAuditAsync(
            tenantId,
            account.Id,
            SaasIdentitySecurityEventTypes.SignInSuccess,
            new { sessionReference = session.SessionReference, pendingMfa = requiresMfa },
            command.CorrelationId,
            now,
            cancellationToken);

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await dbContext.SetTenantSessionContextAsync(tenantId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        return SaasIdentitySignInResult.Success(rawToken, session.SessionReference, requiresMfa);
    }

    public async Task<SaasIdentityAuthenticationResult> VerifyMfaAsync(
        SaasIdentityMfaVerifyCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var (session, account) = await LoadPendingMfaSessionAsync(command.SessionToken, now, cancellationToken);
        if (session is null || account is null)
        {
            return SaasIdentityAuthenticationResult.Failure(SaasIdentityFailureCodes.SessionInvalid);
        }

        var mfaMethod = await dbContext.SaasIdentityMfaMethods
            .SingleOrDefaultAsync(m => m.AccountId == account.Id, cancellationToken);
        if (mfaMethod is null)
        {
            return SaasIdentityAuthenticationResult.Failure(SaasIdentityFailureCodes.MfaInvalid);
        }

        var secret = mfaSecretProtector.Unprotect(mfaMethod.EncryptedSecret);
        if (!SaasIdentityTotpService.VerifyCode(secret, command.TotpCode, now))
        {
            await AppendAuditAsync(
                session.TenantId,
                account.Id,
                SaasIdentitySecurityEventTypes.MfaChallengeFailed,
                new { sessionReference = session.SessionReference },
                command.CorrelationId,
                now,
                cancellationToken);
            await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
            {
                await dbContext.SetTenantSessionContextAsync(session.TenantId, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return SaasIdentityAuthenticationResult.Failure(SaasIdentityFailureCodes.MfaInvalid);
        }

        var rotatedToken = SaasIdentityTokenUtilities.GenerateOpaqueToken();
        session.TokenHash = SaasIdentityTokenUtilities.HashToken(rotatedToken);
        session.PendingMfa = false;
        session.MfaSatisfied = true;
        session.LastSeenAtUtc = now;
        session.IdleExpiresAtUtc = now.Add(SaasIdentityConstants.SessionIdleLifetime);

        await AppendAuditAsync(
            session.TenantId,
            account.Id,
            SaasIdentitySecurityEventTypes.MfaChallengeSucceeded,
            new { sessionReference = session.SessionReference },
            command.CorrelationId,
            now,
            cancellationToken);

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await dbContext.SetTenantSessionContextAsync(session.TenantId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return SaasIdentityAuthenticationResult.Success(rotatedToken, session.SessionReference);
    }

    public async Task<SaasIdentityAuthenticationResult> ConsumeRecoveryCodeAsync(
        SaasIdentityRecoveryCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var (session, account) = await LoadPendingMfaSessionAsync(command.SessionToken, now, cancellationToken);
        if (session is null || account is null)
        {
            return SaasIdentityAuthenticationResult.Failure(SaasIdentityFailureCodes.SessionInvalid);
        }

        var mfaMethod = await dbContext.SaasIdentityMfaMethods
            .SingleOrDefaultAsync(m => m.AccountId == account.Id, cancellationToken);
        if (mfaMethod is null)
        {
            return SaasIdentityAuthenticationResult.Failure(SaasIdentityFailureCodes.MfaInvalid);
        }

        var codeHash = SaasIdentityTokenUtilities.HashToken(command.RecoveryCode.Trim().ToLowerInvariant());
        var recoveryCode = await dbContext.SaasIdentityRecoveryCodes
            .SingleOrDefaultAsync(
                c => c.MfaMethodId == mfaMethod.Id
                    && c.CodeHash == codeHash
                    && c.ConsumedAtUtc == null,
                cancellationToken);

        if (recoveryCode is null)
        {
            await AppendAuditAsync(
                session.TenantId,
                account.Id,
                SaasIdentitySecurityEventTypes.MfaChallengeFailed,
                new { sessionReference = session.SessionReference, method = "recovery" },
                command.CorrelationId,
                now,
                cancellationToken);
            await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
            {
                await dbContext.SetTenantSessionContextAsync(session.TenantId, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return SaasIdentityAuthenticationResult.Failure(SaasIdentityFailureCodes.MfaInvalid);
        }

        recoveryCode.ConsumedAtUtc = now;
        var rotatedToken = SaasIdentityTokenUtilities.GenerateOpaqueToken();
        session.TokenHash = SaasIdentityTokenUtilities.HashToken(rotatedToken);
        session.PendingMfa = false;
        session.MfaSatisfied = true;
        session.LastSeenAtUtc = now;
        session.IdleExpiresAtUtc = now.Add(SaasIdentityConstants.SessionIdleLifetime);

        await AppendAuditAsync(
            session.TenantId,
            account.Id,
            SaasIdentitySecurityEventTypes.MfaChallengeSucceeded,
            new { sessionReference = session.SessionReference, method = "recovery" },
            command.CorrelationId,
            now,
            cancellationToken);

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await dbContext.SetTenantSessionContextAsync(session.TenantId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return SaasIdentityAuthenticationResult.Success(rotatedToken, session.SessionReference);
    }

    public async Task<SaasIdentityWriteResult> SignOutAsync(
        SaasIdentitySignOutCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var tokenHash = SaasIdentityTokenUtilities.HashToken(command.SessionToken);
        var resolution = await ResolveSessionByTokenHashAsync(tokenHash, cancellationToken);

        if (resolution is null || resolution.RevokedAtUtc is not null)
        {
            return SaasIdentityWriteResult.Ok();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SetTenantSessionContextAsync(resolution.TenantId, cancellationToken);
        var session = await dbContext.SaasIdentitySessions
            .SingleOrDefaultAsync(s => s.Id == resolution.SessionId && s.RevokedAtUtc == null, cancellationToken);

        if (session is null)
        {
            return SaasIdentityWriteResult.Ok();
        }

        session.RevokedAtUtc = now;
        session.RevokedReason = "sign-out";

        await AppendAuditAsync(
            session.TenantId,
            session.AccountId,
            SaasIdentitySecurityEventTypes.SignOut,
            new { sessionReference = session.SessionReference },
            command.CorrelationId,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SaasIdentityWriteResult.Ok();
    }

    public async Task<SaasIdentityWriteResult> SignOutAllAsync(
        SaasIdentitySignOutAllCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await RevokeAllSessionsAsync(
            command.AccountId,
            command.TenantId,
            "sign-out-all",
            command.CorrelationId,
            now,
            cancellationToken);
        return SaasIdentityWriteResult.Ok();
    }

    public async Task<SaasIdentitySessionContext?> ResolveSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var tokenHash = SaasIdentityTokenUtilities.HashToken(sessionToken);
        var resolution = await ResolveSessionByTokenHashAsync(tokenHash, cancellationToken);

        if (resolution is null || resolution.RevokedAtUtc is not null)
        {
            return null;
        }

        if (resolution.IdleExpiresAtUtc <= now || resolution.AbsoluteExpiresAtUtc <= now)
        {
            return null;
        }

        if (resolution.LifecycleStatus == SaasIdentityAccountLifecycleStatuses.Deactivated
            || resolution.AccountSecurityStamp != resolution.SecurityStampSnapshot)
        {
            return null;
        }

        if (resolution.PendingMfa || !resolution.MfaSatisfied)
        {
            if (resolution.MfaRequired
                && resolution.CreatedAtUtc.Add(SaasIdentityConstants.MfaChallengeLifetime) <= now)
            {
                return null;
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SetTenantSessionContextAsync(resolution.TenantId, cancellationToken);
        var session = await dbContext.SaasIdentitySessions
            .SingleOrDefaultAsync(s => s.Id == resolution.SessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        session.LastSeenAtUtc = now;
        session.IdleExpiresAtUtc = now.Add(SaasIdentityConstants.SessionIdleLifetime);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SaasIdentitySessionContext(
            resolution.AccountId,
            resolution.AccountReference,
            resolution.SessionId,
            resolution.SessionReference,
            resolution.TenantId,
            resolution.TenantKey,
            resolution.TenantDisplayName,
            resolution.TenantMembershipId,
            resolution.IdentitySubjectId,
            resolution.SecurityStampSnapshot,
            resolution.MfaSatisfied,
            resolution.PendingMfa);
    }

    public async Task<SaasIdentityMeReadModel?> GetMeAsync(
        SaasIdentitySessionContext sessionContext,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.SaasIdentityAccounts.AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == sessionContext.AccountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var mfaMethod = await dbContext.SaasIdentityMfaMethods.AsNoTracking()
            .SingleOrDefaultAsync(m => m.AccountId == account.Id, cancellationToken);

        return new SaasIdentityMeReadModel(
            account.AccountReference,
            account.EmailNormalized,
            account.DisplayName,
            account.LifecycleStatus,
            account.EmailVerifiedAtUtc is not null,
            mfaMethod?.IsRequired ?? false,
            mfaMethod is not null,
            sessionContext.TenantKey,
            sessionContext.TenantDisplayName);
    }

    public async Task<IReadOnlyList<SaasIdentitySessionReadModel>> ListSessionsAsync(
        SaasIdentitySessionContext sessionContext,
        CancellationToken cancellationToken = default)
    {
        var sessions = await dbContext.SaasIdentitySessions.AsNoTracking()
            .Where(s => s.AccountId == sessionContext.AccountId && s.RevokedAtUtc == null)
            .OrderByDescending(s => s.LastSeenAtUtc)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return sessions
            .Where(s => s.IdleExpiresAtUtc > now && s.AbsoluteExpiresAtUtc > now)
            .Select(s => new SaasIdentitySessionReadModel(
                s.SessionReference,
                s.DeviceLabel,
                s.CreatedAtUtc,
                s.LastSeenAtUtc,
                s.IdleExpiresAtUtc,
                s.AbsoluteExpiresAtUtc,
                s.Id == sessionContext.SessionId,
                s.MfaSatisfied,
                s.PendingMfa))
            .ToList();
    }

    public async Task<SaasIdentityWriteResult> RevokeSessionAsync(
        SaasIdentityRevokeSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = await dbContext.SaasIdentitySessions
            .SingleOrDefaultAsync(
                s => s.AccountId == command.AccountId
                    && s.SessionReference == command.SessionReference
                    && s.RevokedAtUtc == null,
                cancellationToken);

        if (session is null)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.NotFound,
                "Session was not found.");
        }

        session.RevokedAtUtc = now;
        session.RevokedReason = "revoked";

        await AppendAuditAsync(
            session.TenantId,
            session.AccountId,
            SaasIdentitySecurityEventTypes.SessionRevoked,
            new { sessionReference = session.SessionReference },
            command.CorrelationId,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaasIdentityWriteResult.Ok();
    }

    public async Task<SaasIdentityForgotPasswordResult> RequestPasswordResetAsync(
        SaasIdentityForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedEmail = SaasIdentityEmailNormalizer.Normalize(command.Email);
        var account = await dbContext.SaasIdentityAccounts
            .SingleOrDefaultAsync(a => a.EmailNormalized == normalizedEmail, cancellationToken);

        if (account is not null
            && account.LifecycleStatus != SaasIdentityAccountLifecycleStatuses.Deactivated)
        {
            var rawToken = SaasIdentityTokenUtilities.GenerateOpaqueToken();
            dbContext.SaasIdentityPasswordResetTokens.Add(new SaasIdentityPasswordResetToken
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                TokenHash = SaasIdentityTokenUtilities.HashToken(rawToken),
                ExpiresAtUtc = now.Add(SaasIdentityConstants.PasswordResetTokenLifetime),
                CreatedAtUtc = now,
            });

            await DeliverTokenAsync(
                SaasIdentityTokenDeliveryKinds.PasswordReset,
                account.EmailNormalized,
                rawToken,
                cancellationToken);

            await AppendAuditAsync(
                null,
                account.Id,
                SaasIdentitySecurityEventTypes.PasswordResetRequested,
                new { accountReference = account.AccountReference },
                command.CorrelationId,
                now,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SaasIdentityForgotPasswordResult(true);
    }

    public async Task<SaasIdentityWriteResult> ResetPasswordAsync(
        SaasIdentityResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = SaasIdentityPasswordPolicy.Validate(command.NewPassword);
        if (!validation.IsValid)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.ValidationFailed,
                validation.FailureMessage ?? "Password validation failed.");
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = SaasIdentityTokenUtilities.HashToken(command.RawToken);
        var resetToken = await dbContext.SaasIdentityPasswordResetTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.TokenInvalid,
                "Reset token is invalid.");
        }

        if (resetToken.ConsumedAtUtc is not null)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.TokenConsumed,
                "Reset token has already been used.");
        }

        if (resetToken.ExpiresAtUtc <= now)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.TokenExpired,
                "Reset token has expired.");
        }

        var account = await dbContext.SaasIdentityAccounts
            .SingleOrDefaultAsync(a => a.Id == resetToken.AccountId, cancellationToken);
        if (account is null)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.NotFound,
                "Account was not found.");
        }

        resetToken.ConsumedAtUtc = now;
        await SetPasswordAsync(account, command.NewPassword, now, cancellationToken);
        await RotateSecurityStampAndRevokeSessionsAsync(
            account,
            "password-reset",
            command.CorrelationId,
            now,
            cancellationToken);

        await AppendAuditAsync(
            null,
            account.Id,
            SaasIdentitySecurityEventTypes.PasswordResetCompleted,
            new { accountReference = account.AccountReference },
            command.CorrelationId,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaasIdentityWriteResult.Ok();
    }

    public async Task<SaasIdentityWriteResult> VerifyEmailAsync(
        SaasIdentityVerifyEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var tokenHash = SaasIdentityTokenUtilities.HashToken(command.RawToken);
        var verificationToken = await dbContext.SaasIdentityEmailVerificationTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (verificationToken is null)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.TokenInvalid,
                "Verification token is invalid.");
        }

        if (verificationToken.ConsumedAtUtc is not null)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.TokenConsumed,
                "Verification token has already been used.");
        }

        if (verificationToken.ExpiresAtUtc <= now)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.TokenExpired,
                "Verification token has expired.");
        }

        var account = await dbContext.SaasIdentityAccounts
            .SingleOrDefaultAsync(a => a.Id == verificationToken.AccountId, cancellationToken);
        if (account is null)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.NotFound,
                "Account was not found.");
        }

        verificationToken.ConsumedAtUtc = now;
        account.EmailVerifiedAtUtc = now;
        if (account.LifecycleStatus == SaasIdentityAccountLifecycleStatuses.PendingVerification)
        {
            account.LifecycleStatus = SaasIdentityAccountLifecycleStatuses.Active;
        }

        account.UpdatedAtUtc = now;

        await AppendAuditAsync(
            null,
            account.Id,
            SaasIdentitySecurityEventTypes.EmailVerificationCompleted,
            new { accountReference = account.AccountReference },
            command.CorrelationId,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaasIdentityWriteResult.Ok();
    }

    public async Task<SaasIdentityInvitationAcceptResult> AcceptInvitationAsync(
        SaasIdentityAcceptInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        var passwordValidation = SaasIdentityPasswordPolicy.Validate(command.Password);
        if (!passwordValidation.IsValid)
        {
            return SaasIdentityInvitationAcceptResult.Fail(
                SaasIdentityFailureCodes.ValidationFailed,
                passwordValidation.FailureMessage ?? "Password validation failed.");
        }

        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return SaasIdentityInvitationAcceptResult.Fail(
                SaasIdentityFailureCodes.ValidationFailed,
                "Display name is required.");
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = SaasIdentityTokenUtilities.HashToken(command.RawToken);
        FormattableString invitationLookupSql =
            $"""
             SELECT
                 invitation_id AS "InvitationId",
                 tenant_id AS "TenantId",
                 intended_email_normalized AS "IntendedEmailNormalized",
                 expires_at_utc AS "ExpiresAtUtc",
                 consumed_at_utc AS "ConsumedAtUtc",
                 invitation_reference AS "InvitationReference"
             FROM resolve_saas_identity_invitation_by_token_hash({tokenHash})
             """;
        var invitationMatch = await dbContext.Database
            .SqlQuery<InvitationTokenResolution>(invitationLookupSql)
            .SingleOrDefaultAsync(cancellationToken);

        if (invitationMatch is null)
        {
            return SaasIdentityInvitationAcceptResult.Fail(
                SaasIdentityFailureCodes.TokenInvalid,
                "Invitation token is invalid.");
        }

        if (invitationMatch.ConsumedAtUtc is not null)
        {
            return SaasIdentityInvitationAcceptResult.Fail(
                SaasIdentityFailureCodes.TokenConsumed,
                "Invitation has already been accepted.");
        }

        if (invitationMatch.ExpiresAtUtc <= now)
        {
            return SaasIdentityInvitationAcceptResult.Fail(
                SaasIdentityFailureCodes.TokenExpired,
                "Invitation has expired.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SetTenantSessionContextAsync(invitationMatch.TenantId, cancellationToken);

        var invitation = await dbContext.SaasIdentityInvitations
            .SingleAsync(i => i.Id == invitationMatch.InvitationId, cancellationToken);

        var account = await dbContext.SaasIdentityAccounts
            .SingleOrDefaultAsync(
                a => a.EmailNormalized == invitation.IntendedEmailNormalized,
                cancellationToken);

        if (account is null)
        {
            account = await CreateAccountAsync(
                invitation.IntendedEmailNormalized,
                command.DisplayName.Trim(),
                now,
                cancellationToken);
        }
        else if (account.EmailNormalized != invitation.IntendedEmailNormalized)
        {
            return SaasIdentityInvitationAcceptResult.Fail(
                SaasIdentityFailureCodes.Conflict,
                "Invitation email does not match the existing account.");
        }
        else
        {
            account.DisplayName = command.DisplayName.Trim();
            account.UpdatedAtUtc = now;
        }

        await SetPasswordAsync(account, command.Password, now, cancellationToken);

        var existingMembership = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                m => m.TenantId == invitation.TenantId
                    && m.IdentitySubjectId == account.IdentitySubjectId,
                cancellationToken);

        Guid membershipId;
        if (existingMembership is null)
        {
            membershipId = Guid.NewGuid();
            dbContext.TenantMemberships.Add(new TenantMembership
            {
                Id = membershipId,
                TenantId = invitation.TenantId,
                IdentitySubjectId = account.IdentitySubjectId,
                MembershipReference = TenantMembership.CreateReference(membershipId),
                Status = TenantMembershipStatus.Active,
                AuthorizationVersion = 1,
                CreatedAtUtc = now,
            });
        }
        else
        {
            membershipId = existingMembership.Id;
            existingMembership.Status = TenantMembershipStatus.Active;
        }

        invitation.ConsumedAtUtc = now;

        await AppendAuditAsync(
            invitation.TenantId,
            account.Id,
            SaasIdentitySecurityEventTypes.InvitationAccepted,
            new
            {
                accountReference = account.AccountReference,
                invitationReference = invitation.InvitationReference,
            },
            command.CorrelationId,
            now,
            cancellationToken);

        if (!tenantContextAccessor.IsAvailable)
        {
            tenantContextAccessor.Set(new TenantContext(invitation.TenantId, invitation.TenantId.ToString("D"), membershipId));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return SaasIdentityInvitationAcceptResult.Ok(account.AccountReference);
    }

    public async Task<SaasIdentityMfaEnrollBeginResult> BeginMfaEnrollmentAsync(
        SaasIdentityMfaEnrollCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.SaasIdentityAccounts.AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);
        if (account is null)
        {
            return new SaasIdentityMfaEnrollBeginResult(
                false,
                null,
                null,
                null,
                SaasIdentityFailureCodes.NotFound);
        }

        var secret = SaasIdentityTotpService.GenerateSecret();
        var provisioningUri = SaasIdentityTotpService.BuildProvisioningUri(
            secret,
            account.EmailNormalized,
            SaasIdentityConstants.Issuer);
        var recoveryCodes = SaasIdentityTotpService.GenerateRecoveryCodes();

        return new SaasIdentityMfaEnrollBeginResult(
            true,
            secret,
            provisioningUri,
            recoveryCodes,
            null);
    }

    public async Task<SaasIdentityWriteResult> ConfirmMfaEnrollmentAsync(
        SaasIdentityMfaEnrollConfirmCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!SaasIdentityTotpService.VerifyCode(command.SharedSecret, command.TotpCode))
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.MfaInvalid,
                "TOTP verification failed.");
        }

        var now = DateTimeOffset.UtcNow;
        var account = await dbContext.SaasIdentityAccounts
            .SingleOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);
        if (account is null)
        {
            return SaasIdentityWriteResult.Fail(
                SaasIdentityFailureCodes.NotFound,
                "Account was not found.");
        }

        var existingMethod = await dbContext.SaasIdentityMfaMethods
            .SingleOrDefaultAsync(m => m.AccountId == account.Id, cancellationToken);

        if (existingMethod is not null)
        {
            var existingCodes = await dbContext.SaasIdentityRecoveryCodes
                .Where(c => c.MfaMethodId == existingMethod.Id)
                .ToListAsync(cancellationToken);
            dbContext.SaasIdentityRecoveryCodes.RemoveRange(existingCodes);
            dbContext.SaasIdentityMfaMethods.Remove(existingMethod);
        }

        var mfaMethod = new SaasIdentityMfaMethod
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            EncryptedSecret = mfaSecretProtector.Protect(command.SharedSecret),
            IsRequired = command.RequireMfa,
            EnrolledAtUtc = now,
        };
        dbContext.SaasIdentityMfaMethods.Add(mfaMethod);

        var recoveryCodes = SaasIdentityTotpService.GenerateRecoveryCodes();
        foreach (var code in recoveryCodes)
        {
            dbContext.SaasIdentityRecoveryCodes.Add(new SaasIdentityRecoveryCode
            {
                Id = Guid.NewGuid(),
                MfaMethodId = mfaMethod.Id,
                CodeHash = SaasIdentityTokenUtilities.HashToken(code.ToLowerInvariant()),
                CreatedAtUtc = now,
            });
        }

        await AppendAuditAsync(
            null,
            account.Id,
            SaasIdentitySecurityEventTypes.MfaEnrolled,
            new { accountReference = account.AccountReference, required = command.RequireMfa },
            command.CorrelationId,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return SaasIdentityWriteResult.Ok();
    }

    private async Task<(SaasIdentitySession? Session, SaasIdentityAccount? Account)> LoadPendingMfaSessionAsync(
        string sessionToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tokenHash = SaasIdentityTokenUtilities.HashToken(sessionToken);
        var resolution = await ResolveSessionByTokenHashAsync(tokenHash, cancellationToken);

        if (resolution is null
            || resolution.RevokedAtUtc is not null
            || !resolution.PendingMfa
            || resolution.IdleExpiresAtUtc <= now
            || resolution.AbsoluteExpiresAtUtc <= now
            || resolution.CreatedAtUtc.Add(SaasIdentityConstants.MfaChallengeLifetime) <= now
            || resolution.LifecycleStatus == SaasIdentityAccountLifecycleStatuses.Deactivated
            || resolution.AccountSecurityStamp != resolution.SecurityStampSnapshot)
        {
            return (null, null);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SetTenantSessionContextAsync(resolution.TenantId, cancellationToken);
        var session = await dbContext.SaasIdentitySessions
            .SingleOrDefaultAsync(s => s.Id == resolution.SessionId, cancellationToken);
        var account = await dbContext.SaasIdentityAccounts
            .SingleOrDefaultAsync(a => a.Id == resolution.AccountId, cancellationToken);

        if (session is null || account is null)
        {
            return (null, null);
        }

        await transaction.CommitAsync(cancellationToken);
        return (session, account);
    }

    private static (SaasIdentitySession Session, string RawToken) CreateSession(
        SaasIdentityAccount account,
        Guid tenantId,
        Guid tenantMembershipId,
        string? deviceLabel,
        DateTimeOffset now,
        bool requiresMfa)
    {
        var rawToken = SaasIdentityTokenUtilities.GenerateOpaqueToken();
        var session = new SaasIdentitySession
        {
            Id = Guid.NewGuid(),
            SessionReference = SaasIdentityTokenUtilities.GenerateReference("ses"),
            TokenHash = SaasIdentityTokenUtilities.HashToken(rawToken),
            AccountId = account.Id,
            TenantId = tenantId,
            TenantMembershipId = tenantMembershipId,
            SecurityStampSnapshot = account.SecurityStamp,
            DeviceLabel = string.IsNullOrWhiteSpace(deviceLabel) ? null : deviceLabel.Trim(),
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            IdleExpiresAtUtc = now.Add(SaasIdentityConstants.SessionIdleLifetime),
            AbsoluteExpiresAtUtc = now.Add(SaasIdentityConstants.SessionAbsoluteLifetime),
            MfaSatisfied = !requiresMfa,
            PendingMfa = requiresMfa,
        };

        return (session, rawToken);
    }

    private async Task<SaasIdentityAccount> CreateAccountAsync(
        string normalizedEmail,
        string displayName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accountId = Guid.NewGuid();
        var identitySubject = new IdentitySubject
        {
            Id = Guid.NewGuid(),
            ExternalIssuer = SaasIdentityConstants.Issuer,
            ExternalSubject = accountId.ToString(),
            Status = IdentitySubjectStatus.Active,
            CreatedAtUtc = now,
        };

        dbContext.IdentitySubjects.Add(identitySubject);

        var account = new SaasIdentityAccount
        {
            Id = accountId,
            AccountReference = SaasIdentityTokenUtilities.GenerateReference("acc"),
            EmailNormalized = normalizedEmail,
            DisplayName = displayName,
            LifecycleStatus = SaasIdentityAccountLifecycleStatuses.PendingVerification,
            FailedSignInCount = 0,
            SecurityStamp = Guid.NewGuid(),
            IdentitySubjectId = identitySubject.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };

        dbContext.SaasIdentityAccounts.Add(account);
        await Task.CompletedTask;
        return account;
    }

    private async Task SetPasswordAsync(
        SaasIdentityAccount account,
        string password,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var credential = await dbContext.SaasIdentityPasswordCredentials
            .SingleOrDefaultAsync(c => c.AccountId == account.Id, cancellationToken);

        var hash = passwordHasher.HashPassword(account, password);
        if (credential is null)
        {
            dbContext.SaasIdentityPasswordCredentials.Add(new SaasIdentityPasswordCredential
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                PasswordHash = hash,
                PasswordHashVersion = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }
        else
        {
            credential.PasswordHash = hash;
            credential.PasswordHashVersion = 1;
            credential.UpdatedAtUtc = now;
        }
    }

    private async Task RotateSecurityStampAndRevokeSessionsAsync(
        SaasIdentityAccount account,
        string reason,
        Guid? correlationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        account.SecurityStamp = Guid.NewGuid();
        account.UpdatedAtUtc = now;

        var tenantIds = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.IdentitySubjectId == account.IdentitySubjectId
                && m.Status == TenantMembershipStatus.Active)
            .Select(m => m.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            await RevokeAllSessionsAsync(
                account.Id,
                tenantId,
                reason,
                correlationId,
                now,
                cancellationToken);
        }

        await AppendAuditAsync(
            null,
            account.Id,
            SaasIdentitySecurityEventTypes.SecurityStampChanged,
            new { accountReference = account.AccountReference, reason },
            correlationId,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeAllSessionsAsync(
        Guid accountId,
        Guid tenantId,
        string reason,
        Guid? correlationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SetTenantSessionContextAsync(tenantId, cancellationToken);

        var sessions = await dbContext.SaasIdentitySessions
            .Where(s => s.AccountId == accountId && s.TenantId == tenantId && s.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.RevokedAtUtc = now;
            session.RevokedReason = reason;

            await AppendAuditAsync(
                session.TenantId,
                accountId,
                SaasIdentitySecurityEventTypes.SessionRevoked,
                new { sessionReference = session.SessionReference, reason },
                correlationId,
                now,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task DeliverTokenAsync(
        string deliveryKind,
        string recipientEmail,
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (developmentTokenDeliverySink is not null)
        {
            developmentTokenDeliverySink.Capture(deliveryKind, recipientEmail, rawToken);
            return;
        }

        switch (deliveryKind)
        {
            case SaasIdentityTokenDeliveryKinds.PasswordReset:
                await emailDeliveryProvider.DeliverPasswordResetAsync(
                    recipientEmail,
                    rawToken,
                    cancellationToken);
                break;
            case SaasIdentityTokenDeliveryKinds.EmailVerification:
                await emailDeliveryProvider.DeliverEmailVerificationAsync(
                    recipientEmail,
                    rawToken,
                    cancellationToken);
                break;
            case SaasIdentityTokenDeliveryKinds.Invitation:
                await emailDeliveryProvider.DeliverInvitationAsync(
                    recipientEmail,
                    rawToken,
                    cancellationToken);
                break;
        }
    }

    private async Task AppendAuditAsync(
        Guid? tenantId,
        Guid? accountId,
        string eventType,
        object payload,
        Guid? correlationId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.SaasIdentitySecurityAuditEvents.Add(new SaasIdentitySecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountId = accountId,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = correlationId,
        });
        await Task.CompletedTask;
    }

    private sealed record TenantSignInContext(Guid TenantId, string TenantKey);

    private sealed record MembershipSignInContext(Guid TenantId, string TenantKey, Guid MembershipId);

    private sealed record InvitationTokenResolution(
        Guid InvitationId,
        Guid TenantId,
        string IntendedEmailNormalized,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset? ConsumedAtUtc,
        string InvitationReference);

    private sealed record SessionTokenResolution(
        Guid SessionId,
        string SessionReference,
        Guid AccountId,
        string AccountReference,
        Guid TenantId,
        string TenantKey,
        string TenantDisplayName,
        Guid TenantMembershipId,
        Guid IdentitySubjectId,
        Guid SecurityStampSnapshot,
        bool MfaSatisfied,
        bool PendingMfa,
        DateTimeOffset IdleExpiresAtUtc,
        DateTimeOffset AbsoluteExpiresAtUtc,
        DateTimeOffset? RevokedAtUtc,
        string LifecycleStatus,
        Guid AccountSecurityStamp,
        DateTimeOffset CreatedAtUtc,
        bool MfaRequired);

    private async Task<SessionTokenResolution?> ResolveSessionByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        FormattableString sql =
            $"""
             SELECT
                 session_id AS "SessionId",
                 session_reference AS "SessionReference",
                 account_id AS "AccountId",
                 account_reference AS "AccountReference",
                 tenant_id AS "TenantId",
                 tenant_key AS "TenantKey",
                 tenant_display_name AS "TenantDisplayName",
                 tenant_membership_id AS "TenantMembershipId",
                 identity_subject_id AS "IdentitySubjectId",
                 security_stamp_snapshot AS "SecurityStampSnapshot",
                 mfa_satisfied AS "MfaSatisfied",
                 pending_mfa AS "PendingMfa",
                 idle_expires_at_utc AS "IdleExpiresAtUtc",
                 absolute_expires_at_utc AS "AbsoluteExpiresAtUtc",
                 revoked_at_utc AS "RevokedAtUtc",
                 lifecycle_status AS "LifecycleStatus",
                 account_security_stamp AS "AccountSecurityStamp",
                 created_at_utc AS "CreatedAtUtc",
                 mfa_required AS "MfaRequired"
             FROM resolve_saas_identity_session_by_token_hash({tokenHash})
             """;

        return await dbContext.Database
            .SqlQuery<SessionTokenResolution>(sql)
            .SingleOrDefaultAsync(cancellationToken);
    }
}

public sealed class UnavailableSaasIdentityRepository : ISaasIdentityRepository
{
    private static SaasIdentityWriteResult Unavailable() =>
        SaasIdentityWriteResult.Fail(SaasIdentityFailureCodes.Unavailable, "SaaS identity persistence is unavailable.");

    public Task<SaasIdentitySignInResult> SignInAsync(
        SaasIdentitySignInCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SaasIdentitySignInResult.GenericFailure());

    public Task<SaasIdentityAuthenticationResult> VerifyMfaAsync(
        SaasIdentityMfaVerifyCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SaasIdentityAuthenticationResult.Failure(SaasIdentityFailureCodes.Unavailable));

    public Task<SaasIdentityAuthenticationResult> ConsumeRecoveryCodeAsync(
        SaasIdentityRecoveryCodeCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SaasIdentityAuthenticationResult.Failure(SaasIdentityFailureCodes.Unavailable));

    public Task<SaasIdentityWriteResult> SignOutAsync(
        SaasIdentitySignOutCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasIdentityWriteResult> SignOutAllAsync(
        SaasIdentitySignOutAllCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasIdentitySessionContext?> ResolveSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasIdentitySessionContext?>(null);

    public Task<SaasIdentityMeReadModel?> GetMeAsync(
        SaasIdentitySessionContext sessionContext,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasIdentityMeReadModel?>(null);

    public Task<IReadOnlyList<SaasIdentitySessionReadModel>> ListSessionsAsync(
        SaasIdentitySessionContext sessionContext,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SaasIdentitySessionReadModel>>([]);

    public Task<SaasIdentityWriteResult> RevokeSessionAsync(
        SaasIdentityRevokeSessionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasIdentityForgotPasswordResult> RequestPasswordResetAsync(
        SaasIdentityForgotPasswordCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SaasIdentityForgotPasswordResult(true));

    public Task<SaasIdentityWriteResult> ResetPasswordAsync(
        SaasIdentityResetPasswordCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasIdentityWriteResult> VerifyEmailAsync(
        SaasIdentityVerifyEmailCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasIdentityInvitationAcceptResult> AcceptInvitationAsync(
        SaasIdentityAcceptInvitationCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SaasIdentityInvitationAcceptResult.Fail(
            SaasIdentityFailureCodes.Unavailable,
            "SaaS identity persistence is unavailable."));

    public Task<SaasIdentityMfaEnrollBeginResult> BeginMfaEnrollmentAsync(
        SaasIdentityMfaEnrollCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SaasIdentityMfaEnrollBeginResult(
            false,
            null,
            null,
            null,
            SaasIdentityFailureCodes.Unavailable));

    public Task<SaasIdentityWriteResult> ConfirmMfaEnrollmentAsync(
        SaasIdentityMfaEnrollConfirmCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
