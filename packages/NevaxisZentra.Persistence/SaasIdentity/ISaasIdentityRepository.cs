namespace NevaxisZentra.Persistence.SaasIdentity;

public interface ISaasIdentityRepository
{
    Task<SaasIdentitySignInResult> SignInAsync(
        SaasIdentitySignInCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityAuthenticationResult> VerifyMfaAsync(
        SaasIdentityMfaVerifyCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityAuthenticationResult> ConsumeRecoveryCodeAsync(
        SaasIdentityRecoveryCodeCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityWriteResult> SignOutAsync(
        SaasIdentitySignOutCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityWriteResult> SignOutAllAsync(
        SaasIdentitySignOutAllCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentitySessionContext?> ResolveSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityMeReadModel?> GetMeAsync(
        SaasIdentitySessionContext sessionContext,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaasIdentitySessionReadModel>> ListSessionsAsync(
        SaasIdentitySessionContext sessionContext,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityWriteResult> RevokeSessionAsync(
        SaasIdentityRevokeSessionCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityForgotPasswordResult> RequestPasswordResetAsync(
        SaasIdentityForgotPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityWriteResult> ResetPasswordAsync(
        SaasIdentityResetPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityWriteResult> VerifyEmailAsync(
        SaasIdentityVerifyEmailCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityInvitationAcceptResult> AcceptInvitationAsync(
        SaasIdentityAcceptInvitationCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityMfaEnrollBeginResult> BeginMfaEnrollmentAsync(
        SaasIdentityMfaEnrollCommand command,
        CancellationToken cancellationToken = default);

    Task<SaasIdentityWriteResult> ConfirmMfaEnrollmentAsync(
        SaasIdentityMfaEnrollConfirmCommand command,
        CancellationToken cancellationToken = default);
}
