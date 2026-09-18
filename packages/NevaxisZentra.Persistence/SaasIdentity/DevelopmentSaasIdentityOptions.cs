namespace NevaxisZentra.Persistence.SaasIdentity;

public sealed class DevelopmentSaasIdentityOptions
{
    public const string SectionName = "DevelopmentSaasIdentity";

    public bool Enabled { get; set; }

    public string TenantOwnerEmail { get; set; } = "admin@demo.nevaxiszentra.local";

    public string TenantOwnerPassword { get; set; } = string.Empty;

    public string ActiveUserEmail { get; set; } = "active-user@dev.local";

    public string ActiveUserPassword { get; set; } = string.Empty;

    public string DeactivatedUserEmail { get; set; } = "deactivated-user@dev.local";

    public string DeactivatedUserPassword { get; set; } = string.Empty;

    public string NoMembershipUserEmail { get; set; } = "no-membership@dev.local";

    public string NoMembershipUserPassword { get; set; } = string.Empty;

    public string PendingInvitationEmail { get; set; } = "invited-user@dev.local";

    public string MfaSharedSecret { get; set; } = string.Empty;
}
