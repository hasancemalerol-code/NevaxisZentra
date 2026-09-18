namespace NevaxisZentra.Persistence.SaasIdentity;

public sealed record SaasIdentitySessionContext(
    Guid AccountId,
    string AccountReference,
    Guid SessionId,
    string SessionReference,
    Guid TenantId,
    string TenantKey,
    string TenantDisplayName,
    Guid TenantMembershipId,
    Guid IdentitySubjectId,
    Guid SecurityStampSnapshot,
    bool MfaSatisfied,
    bool PendingMfa);

public sealed record SaasIdentitySignInCommand(
    string OrganizationContext,
    string Email,
    string Password,
    string? DeviceLabel,
    Guid? CorrelationId);

public sealed record SaasIdentitySignInResult(
    bool Succeeded,
    bool RequiresMfa,
    string? SessionToken,
    string? SessionReference,
    string? FailureCode)
{
    public static SaasIdentitySignInResult GenericFailure() =>
        new(false, false, null, null, SaasIdentityFailureCodes.InvalidCredentials);

    public static SaasIdentitySignInResult Success(string sessionToken, string sessionReference, bool requiresMfa) =>
        new(true, requiresMfa, sessionToken, sessionReference, null);
}

public sealed record SaasIdentityMfaVerifyCommand(
    string SessionToken,
    string TotpCode,
    Guid? CorrelationId);

public sealed record SaasIdentityRecoveryCodeCommand(
    string SessionToken,
    string RecoveryCode,
    Guid? CorrelationId);

public sealed record SaasIdentityAuthenticationResult(
    bool Succeeded,
    string? SessionToken,
    string? SessionReference,
    string? FailureCode)
{
    public static SaasIdentityAuthenticationResult Success(string sessionToken, string sessionReference) =>
        new(true, sessionToken, sessionReference, null);

    public static SaasIdentityAuthenticationResult Failure(string failureCode) =>
        new(false, null, null, failureCode);
}

public sealed record SaasIdentitySignOutCommand(
    string SessionToken,
    Guid? CorrelationId);

public sealed record SaasIdentitySignOutAllCommand(
    Guid AccountId,
    Guid TenantId,
    Guid? CorrelationId);

public sealed record SaasIdentityRevokeSessionCommand(
    Guid AccountId,
    string SessionReference,
    Guid? CorrelationId);

public sealed record SaasIdentityForgotPasswordCommand(
    string Email,
    Guid? CorrelationId);

public sealed record SaasIdentityForgotPasswordResult(bool Accepted);

public sealed record SaasIdentityResetPasswordCommand(
    string RawToken,
    string NewPassword,
    Guid? CorrelationId);

public sealed record SaasIdentityVerifyEmailCommand(
    string RawToken,
    Guid? CorrelationId);

public sealed record SaasIdentityAcceptInvitationCommand(
    string RawToken,
    string DisplayName,
    string Password,
    Guid? CorrelationId);

public sealed record SaasIdentityInvitationAcceptResult(
    bool Succeeded,
    string? AccountReference,
    string? FailureCode,
    string? FailureMessage)
{
    public static SaasIdentityInvitationAcceptResult Ok(string accountReference) =>
        new(true, accountReference, null, null);

    public static SaasIdentityInvitationAcceptResult Fail(string code, string message) =>
        new(false, null, code, message);
}

public sealed record SaasIdentityMfaEnrollCommand(
    Guid AccountId,
    Guid? CorrelationId);

public sealed record SaasIdentityMfaEnrollBeginResult(
    bool Succeeded,
    string? SharedSecret,
    string? ProvisioningUri,
    IReadOnlyList<string>? RecoveryCodes,
    string? FailureCode);

public sealed record SaasIdentityMfaEnrollConfirmCommand(
    Guid AccountId,
    string SharedSecret,
    string TotpCode,
    bool RequireMfa,
    Guid? CorrelationId);

public sealed record SaasIdentityMeReadModel(
    string AccountReference,
    string Email,
    string DisplayName,
    string LifecycleStatus,
    bool EmailVerified,
    bool MfaRequired,
    bool MfaEnrolled,
    string TenantKey,
    string TenantDisplayName);

public sealed record SaasIdentitySessionReadModel(
    string SessionReference,
    string? DeviceLabel,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset IdleExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc,
    bool IsCurrent,
    bool MfaSatisfied,
    bool PendingMfa);

public sealed record SaasIdentityWriteResult(bool Succeeded, string? FailureCode, string? FailureMessage)
{
    public static SaasIdentityWriteResult Ok() => new(true, null, null);

    public static SaasIdentityWriteResult Fail(string code, string message) =>
        new(false, code, message);
}

public static class SaasIdentityFailureCodes
{
    public const string InvalidCredentials = "saas-identity.invalid-credentials";
    public const string AccountLocked = "saas-identity.account-locked";
    public const string SessionInvalid = "saas-identity.session-invalid";
    public const string MfaRequired = "saas-identity.mfa-required";
    public const string MfaInvalid = "saas-identity.mfa-invalid";
    public const string ValidationFailed = "saas-identity.validation-failed";
    public const string NotFound = "saas-identity.not-found";
    public const string TokenInvalid = "saas-identity.token-invalid";
    public const string TokenExpired = "saas-identity.token-expired";
    public const string TokenConsumed = "saas-identity.token-consumed";
    public const string Conflict = "saas-identity.conflict";
    public const string Unavailable = "saas-identity.unavailable";
}

public static class SaasIdentityTokenDeliveryKinds
{
    public const string PasswordReset = "password-reset";
    public const string EmailVerification = "email-verification";
    public const string Invitation = "invitation";
}
