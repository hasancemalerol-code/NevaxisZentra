namespace NevaxisZentra.Persistence.SaasIdentity;

public sealed class DevelopmentSaasIdentityOptions
{
    public const string SectionName = "DevelopmentSaasIdentity";

#if ZENTRA_PRODUCTION_BUILD
    // Customer PRODUCTION builds carry no demo usernames; the development identity seed is unreachable there.
    private const string DefaultOwnerEmail = "";
    private const string DefaultActiveEmail = "";
    private const string DefaultDeactivatedEmail = "";
    private const string DefaultNoMembershipEmail = "";
    private const string DefaultPendingInvitationEmail = "";
#else
    private const string DefaultOwnerEmail = "admin@demo.nevaxiszentra.local";
    private const string DefaultActiveEmail = "active-user@dev.local";
    private const string DefaultDeactivatedEmail = "deactivated-user@dev.local";
    private const string DefaultNoMembershipEmail = "no-membership@dev.local";
    private const string DefaultPendingInvitationEmail = "invited-user@dev.local";
#endif

    public bool Enabled { get; set; }

    public string TenantOwnerEmail { get; set; } = DefaultOwnerEmail;

    public string TenantOwnerPassword { get; set; } = string.Empty;

    public string ActiveUserEmail { get; set; } = DefaultActiveEmail;

    public string ActiveUserPassword { get; set; } = string.Empty;

    public string DeactivatedUserEmail { get; set; } = DefaultDeactivatedEmail;

    public string DeactivatedUserPassword { get; set; } = string.Empty;

    public string NoMembershipUserEmail { get; set; } = DefaultNoMembershipEmail;

    public string NoMembershipUserPassword { get; set; } = string.Empty;

    public string PendingInvitationEmail { get; set; } = DefaultPendingInvitationEmail;

    public string MfaSharedSecret { get; set; } = string.Empty;
}
