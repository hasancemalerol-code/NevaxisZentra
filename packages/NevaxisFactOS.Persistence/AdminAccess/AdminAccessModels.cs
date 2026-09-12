namespace NevaxisFactOS.Persistence.AdminAccess;

public sealed record AdminAccessActorContext(
    Guid TenantId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string MembershipReference,
    string ActorReference,
    bool MfaSatisfied,
    Guid? CorrelationId);

public sealed record AdminAccessPermissionReadModel(
    string PermissionKey,
    string NormalizedKey,
    string TurkishLabel,
    string Description,
    string ModuleResourceCode,
    string ActionCode,
    string SensitivityClassification,
    bool RequiresRecentAuth,
    IReadOnlyList<string> SupportedRecordScopes,
    string Status);

public sealed record AdminAccessRoleSummaryReadModel(
    string RoleReference,
    string DisplayName,
    string? Description,
    string? SystemTemplateOrigin,
    bool IsProtected,
    string Status,
    uint Version,
    IReadOnlyList<string> PermissionKeys);

public sealed record AdminAccessEffectiveAccessReadModel(
    string MemberReference,
    string AuthorizationVersion,
    IReadOnlyList<string> RoleReferences,
    IReadOnlyList<string> FacilityCodes,
    IReadOnlyList<string> GrantedPermissionKeys,
    IReadOnlyList<string> DeniedPermissionKeys,
    AdminAccessNavigationVisibilityReadModel Navigation);

public sealed record AdminAccessNavigationVisibilityReadModel(
    bool Machines,
    bool Materials,
    bool Maintenance);

public sealed record AdminAccessAuditEventReadModel(
    string EventType,
    string EntityType,
    string EntityReference,
    string ActorReference,
    DateTimeOffset CreatedAtUtc,
    object? Before,
    object? After);

public sealed record AdminAccessWriteResult(
    bool Succeeded,
    string? FailureCode,
    string? FailureMessage,
    bool IdempotentReplay = false)
{
    public static AdminAccessWriteResult Ok(bool idempotentReplay = false) =>
        new(true, null, null, idempotentReplay);

    public static AdminAccessWriteResult Fail(string code, string message) =>
        new(false, code, message);
}

public sealed record CreateAdminAccessRoleCommand(
    string RoleKey,
    string DisplayName,
    string? Description,
    string? IdempotencyKey);

public sealed record PatchAdminAccessRoleCommand(
    string RoleReference,
    string? DisplayName,
    string? Description,
    string? Status,
    uint ExpectedVersion,
    string? IdempotencyKey);

public sealed record SetAdminAccessRolePermissionsCommand(
    string RoleReference,
    IReadOnlyList<AdminAccessPermissionGrantInput> Grants,
    uint ExpectedVersion,
    string? IdempotencyKey);

public sealed record AdminAccessPermissionGrantInput(
    string PermissionKey,
    string? RecordScope,
    string Effect);

public sealed record PutAdminAccessMemberRolesCommand(
    string MemberReference,
    IReadOnlyList<string> RoleReferences,
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record PutAdminAccessMemberFacilitiesCommand(
    string MemberReference,
    IReadOnlyList<string> FacilityCodes,
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record PutAdminAccessMemberOverridesCommand(
    string MemberReference,
    IReadOnlyList<AdminAccessPermissionOverrideInput> Overrides,
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record AdminAccessPermissionOverrideInput(
    string PermissionKey,
    string Effect,
    string? RecordScope);

public sealed record AdminAccessWorkspaceSummaryReadModel(
    int ActiveMembers,
    int PendingInvitations,
    int SuspendedMembers,
    int ActiveSessions,
    string TenantDisplayName,
    string TenantKey);

public sealed record AdminAccessMemberReadModel(
    string MemberReference,
    string DisplayName,
    string EmailMasked,
    string MembershipStatus,
    IReadOnlyList<string> RoleReferences,
    IReadOnlyList<string> FacilityCodes,
    bool MfaEnrolled,
    bool MfaRequired,
    DateTimeOffset? LastSessionActivityUtc,
    string AuthorizationVersion);

public sealed record AdminAccessInvitationReadModel(
    string InvitationReference,
    string EmailMasked,
    string DeliveryStatus,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset CreatedAtUtc,
    string? IntendedRoleReference);

public sealed record AdminAccessTenantSessionReadModel(
    string MemberReference,
    string AccountDisplayName,
    string EmailMasked,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset IdleExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc,
    bool MfaSatisfied,
    bool IsRevoked,
    string? DeviceLabel,
    string RevokeToken);

public sealed record AdminAccessFacilitySummaryReadModel(
    string FacilityCode,
    string DisplayName,
    string Status);

public sealed record CreateAdminAccessInvitationCommand(
    string Email,
    string? IntendedRoleReference,
    string? IdempotencyKey);

public sealed record CreateAdminAccessInvitationResult(
    bool Succeeded,
    string? InvitationReference,
    string? DeliveryStatus,
    string? FailureCode,
    string? FailureMessage,
    bool IdempotentReplay = false)
{
    public static CreateAdminAccessInvitationResult Ok(
        string invitationReference,
        string deliveryStatus,
        bool idempotentReplay = false) =>
        new(true, invitationReference, deliveryStatus, null, null, idempotentReplay);

    public static CreateAdminAccessInvitationResult Fail(string code, string message) =>
        new(false, null, null, code, message);
}

public sealed record SuspendAdminAccessMemberCommand(
    string MemberReference,
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record ReactivateAdminAccessMemberCommand(
    string MemberReference,
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record RevokeAdminAccessSessionCommand(
    string SessionReference,
    string? IdempotencyKey);

public sealed record RevokeAllAdminAccessMemberSessionsCommand(
    string MemberReference,
    string? IdempotencyKey);
