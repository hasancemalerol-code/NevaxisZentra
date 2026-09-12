namespace NevaxisFactOS.Persistence.SaasIdentity;

public static class SaasIdentityConstants
{
    public const string Issuer = "NevaxisFactOS-saas";

    public const string SessionCookieName = "NevaxisFactOS.session";

    public const string CsrfCookieName = "NevaxisFactOS.csrf";

    public const string CsrfHeaderName = "X-NevaxisFactOS-Csrf";

    public static readonly TimeSpan SessionIdleLifetime = TimeSpan.FromHours(8);

    public static readonly TimeSpan SessionAbsoluteLifetime = TimeSpan.FromDays(14);

    public static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    public static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);

    public static readonly TimeSpan InvitationTokenLifetime = TimeSpan.FromDays(7);

    public static readonly TimeSpan MfaChallengeLifetime = TimeSpan.FromMinutes(10);

    public const int MaxFailedSignInAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public const int MinimumPasswordLength = 12;
}

public static class SaasIdentityAccountLifecycleStatuses
{
    public const string PendingVerification = "PendingVerification";
    public const string Active = "Active";
    public const string Locked = "Locked";
    public const string Deactivated = "Deactivated";
}

public static class SaasIdentitySecurityEventTypes
{
    public const string SignInSuccess = "sign-in.success";
    public const string SignInFailure = "sign-in.failure";
    public const string TemporaryLockout = "sign-in.lockout";
    public const string SignOut = "sign-out";
    public const string SessionRevoked = "session.revoked";
    public const string PasswordResetRequested = "password-reset.requested";
    public const string PasswordResetCompleted = "password-reset.completed";
    public const string EmailVerificationCompleted = "email-verification.completed";
    public const string InvitationAccepted = "invitation.accepted";
    public const string MfaEnrolled = "mfa.enrolled";
    public const string MfaChallengeSucceeded = "mfa.challenge.succeeded";
    public const string MfaChallengeFailed = "mfa.challenge.failed";
    public const string SecurityStampChanged = "security-stamp.changed";
}
