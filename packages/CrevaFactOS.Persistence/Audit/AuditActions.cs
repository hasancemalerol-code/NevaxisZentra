namespace CrevaFactOS.Persistence.Audit;

public static class AuditActions
{
    public const string Authenticate = "authenticate";
    public const string AccessTenant = "access.tenant";
    public const string Authorize = "authorize";
    public const string WriteAudit = "write.audit";
    public const string ValidateConfiguration = "validate.configuration";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Authenticate,
        AccessTenant,
        Authorize,
        WriteAudit,
        ValidateConfiguration,
    };
}
