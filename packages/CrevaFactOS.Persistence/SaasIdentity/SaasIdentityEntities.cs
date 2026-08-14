namespace CrevaFactOS.Persistence.SaasIdentity;

public sealed class SaasIdentityAccount
{
    public Guid Id { get; set; }
    public string AccountReference { get; set; } = string.Empty;
    public string EmailNormalized { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = SaasIdentityAccountLifecycleStatuses.PendingVerification;
    public DateTimeOffset? EmailVerifiedAtUtc { get; set; }
    public int FailedSignInCount { get; set; }
    public DateTimeOffset? LockoutUntilUtc { get; set; }
    public Guid SecurityStamp { get; set; }
    public Guid IdentitySubjectId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class SaasIdentityPasswordCredential
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public int PasswordHashVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class SaasIdentitySession
{
    public Guid Id { get; set; }
    public string SessionReference { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public Guid TenantId { get; set; }
    public Guid TenantMembershipId { get; set; }
    public Guid SecurityStampSnapshot { get; set; }
    public string? DeviceLabel { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset IdleExpiresAtUtc { get; set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
    public bool MfaSatisfied { get; set; }
    public bool PendingMfa { get; set; }
}

public sealed class SaasIdentityInvitation
{
    public Guid Id { get; set; }
    public string InvitationReference { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string IntendedEmailNormalized { get; set; } = string.Empty;
    public Guid? IntendedRoleId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SaasIdentityEmailVerificationToken
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SaasIdentityPasswordResetToken
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SaasIdentityMfaMethod
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string EncryptedSecret { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public DateTimeOffset EnrolledAtUtc { get; set; }
}

public sealed class SaasIdentityRecoveryCode
{
    public Guid Id { get; set; }
    public Guid MfaMethodId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SaasIdentitySecurityAuditEvent
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? AccountId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }
}
