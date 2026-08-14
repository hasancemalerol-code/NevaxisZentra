namespace CrevaFactOS.Persistence.AdminAccess;

public static class AdminAccessActions
{
    public const string View = "view";
    public const string Create = "create";
    public const string Update = "update";
    public const string Submit = "submit";
    public const string Approve = "approve";
    public const string Reject = "reject";
    public const string Cancel = "cancel";
    public const string Export = "export";
    public const string ExternalWriteback = "external-writeback";
    public const string ManageSettings = "manage-settings";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        View, Create, Update, Submit, Approve, Reject, Cancel, Export, ExternalWriteback, ManageSettings,
    };
}

public static class AdminAccessRecordScopes
{
    public const string Own = "own";
    public const string Assigned = "assigned";
    public const string Facility = "facility";
    public const string Tenant = "tenant";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Own, Assigned, Facility, Tenant,
    };

    public static readonly IReadOnlySet<string> CurrentlySupported = new HashSet<string>(StringComparer.Ordinal)
    {
        Facility, Tenant,
    };
}

public static class AdminAccessSensitivity
{
    public const string Standard = "standard";
    public const string Sensitive = "sensitive";
}

public static class AdminAccessOverrideEffects
{
    public const string Allow = "allow";
    public const string Deny = "deny";
}

public static class AdminAccessRoleCodes
{
    public const string TenantOwner = "tenant-owner";
    public const string TenantAdmin = "tenant-admin";
    public const string FacilityManager = "facility-manager";
    public const string ProductionManager = "production-manager";
    public const string Planner = "planner";
    public const string ShiftSupervisor = "shift-supervisor";
    public const string MachineResponsible = "machine-responsible";
    public const string Operator = "operator";
    public const string MaintenanceTeam = "maintenance-team";
    public const string QualityTeam = "quality-team";
    public const string MaterialWarehouse = "material-warehouse";
    public const string SalesOrder = "sales-order";
    public const string Purchasing = "purchasing";
    public const string BillingManager = "billing-manager";
    public const string ReadOnlyAuditor = "read-only-auditor";

    public static readonly IReadOnlyList<string> TemplateCodes =
    [
        TenantOwner, TenantAdmin, FacilityManager, ProductionManager, Planner, ShiftSupervisor,
        MachineResponsible, Operator, MaintenanceTeam, QualityTeam, MaterialWarehouse, SalesOrder,
        Purchasing, BillingManager, ReadOnlyAuditor,
    ];
}

public static class AdminAccessFailureCodes
{
    public const string Unauthenticated = "admin-access.unauthenticated";
    public const string Forbidden = "admin-access.forbidden";
    public const string ValidationFailed = "admin-access.validation-failed";
    public const string NotFound = "admin-access.not-found";
    public const string ConcurrencyConflict = "admin-access.concurrency-conflict";
    public const string IdempotencyConflict = "admin-access.idempotency-conflict";
    public const string SelfMutation = "admin-access.self-mutation";
    public const string SelfElevation = "admin-access.self-elevation";
    public const string UnownedPermission = "admin-access.unowned-permission";
    public const string PlatformGrantBlocked = "admin-access.platform-grant-blocked";
    public const string FinalOwnerProtected = "admin-access.final-owner-protected";
    public const string ProtectedRole = "admin-access.protected-role";
    public const string MfaRequired = "admin-access.mfa-required";
    public const string InvalidScope = "admin-access.invalid-scope";
    public const string InvalidGrant = "admin-access.invalid-grant";
    public const string CrossTenant = "admin-access.cross-tenant";
    public const string CrossFacility = "admin-access.cross-facility";
    public const string RetiredPermission = "admin-access.retired-permission";
    public const string RetiredRole = "admin-access.retired-role";
    public const string Unavailable = "admin-access.unavailable";
}

public static class AdminAccessAuditEventTypes
{
    public const string RoleCreated = "admin-access.role.created";
    public const string RoleUpdated = "admin-access.role.updated";
    public const string RoleRetired = "admin-access.role.retired";
    public const string RolePermissionsChanged = "admin-access.role.permissions-changed";
    public const string MemberRolesChanged = "admin-access.member.roles-changed";
    public const string MemberFacilitiesChanged = "admin-access.member.facilities-changed";
    public const string MemberOverridesChanged = "admin-access.member.overrides-changed";
}

public static class AdminAccessAuditEntityTypes
{
    public const string Role = "tenant-role";
    public const string Membership = "tenant-membership";
}

public static class AdminAccessValueConversions
{
    public static string NormalizePermissionKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            '.',
            value.Trim().ToLowerInvariant().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(segment => string.Concat(segment.Where(static ch => char.IsAsciiLetterOrDigit(ch) || ch == '-'))));
    }

    public static string NormalizeRoleKey(string? value) =>
        NormalizePermissionKey(value).Replace('.', '-');

    public static bool IsValidPermissionKey(string key) =>
        !string.IsNullOrWhiteSpace(key)
        && System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-z][a-z0-9-]*(\\.[a-z][a-z0-9-]*)+$");

    public static bool IsValidRoleKey(string key) =>
        !string.IsNullOrWhiteSpace(key)
        && System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-z][a-z0-9-]*$");

    public static bool IsValidRecordScope(string? scope) =>
        scope is null || AdminAccessRecordScopes.All.Contains(scope);

    public static bool IsCurrentlySupportedRecordScope(string? scope) =>
        scope is null || AdminAccessRecordScopes.CurrentlySupported.Contains(scope);

    public static bool IsPlatformPermission(string permissionKey) =>
        permissionKey.StartsWith("system.", StringComparison.Ordinal)
        || permissionKey is "authorization.diagnostics.read" or "tenant.context.read";

    public static IReadOnlyList<string> ParseRecordScopes(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizePermissionKey)
            .Where(static item => item.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static item => item, StringComparer.Ordinal)
            .ToArray();
    }

    public static string JoinRecordScopes(IEnumerable<string> scopes) =>
        string.Join(',', scopes.Select(NormalizePermissionKey).Where(static item => item.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(static item => item, StringComparer.Ordinal));

    public static string CreateMembershipReference(Guid membershipId) =>
        $"mem_{membershipId:N}";

    public static string MapHonestAction(string permissionKey)
    {
        var action = permissionKey.Split('.').LastOrDefault() ?? string.Empty;
        return action switch
        {
            "read" => AdminAccessActions.View,
            "create" => AdminAccessActions.Create,
            "write" or "update" => AdminAccessActions.Update,
            "submit" or "review" or "schedule" or "transfer" or "handoff" or "start" or "pause"
                or "complete" or "record" or "downtime" or "setup" or "exception" or "assign"
                or "handover" or "inspect" or "execute" => AdminAccessActions.Submit,
            "approve" or "release" or "disposition" => AdminAccessActions.Approve,
            "reject" => AdminAccessActions.Reject,
            "cancel" => AdminAccessActions.Cancel,
            "export" => AdminAccessActions.Export,
            "writeback" or "sync" or "ingest" => AdminAccessActions.ExternalWriteback,
            "manage" or "ncr" => AdminAccessActions.ManageSettings,
            _ => action.Length > 0 ? action : AdminAccessActions.View,
        };
    }

    public static string MapModuleResource(string permissionKey)
    {
        var parts = permissionKey.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 1 ? permissionKey : string.Join('.', parts.Take(parts.Length - 1));
    }
}
