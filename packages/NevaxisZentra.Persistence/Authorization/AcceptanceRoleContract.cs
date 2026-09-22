using System.Text.Json;

namespace NevaxisZentra.Persistence.Authorization;

public sealed class AcceptanceRoleContractException : Exception
{
    public AcceptanceRoleContractException(string message)
        : base(message)
    {
    }
}

public sealed record AcceptanceRoleGrantPlan(
    IReadOnlyList<string> MissingDefinitions,
    IReadOnlyList<string> GrantsToAdd,
    IReadOnlyList<string> ForbiddenGrantsToRemove)
{
    public bool IsSatisfied =>
        MissingDefinitions.Count == 0
        && GrantsToAdd.Count == 0
        && ForbiddenGrantsToRemove.Count == 0;
}

public static class AcceptanceRoleContract
{
    public const string ResourceName = "NevaxisZentra.Persistence.Authorization.acceptance-role-contract.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly AcceptanceRoleContractDocument Document = Load();

    public static string CatalogVersion => Document.CatalogVersion;

    public static string RoleKey => Document.RoleKey;

    public static string Subject => Document.Subject;

    public static string RestrictedRoleKey => Document.RestrictedRole.RoleKey;

    public static string RestrictedSubject => Document.RestrictedRole.Subject;

    public static IReadOnlyList<string> RequiredPermissionKeys { get; } =
        Document.RequiredPermissionKeys;

    public static IReadOnlyList<string> ForbiddenPermissionKeys { get; } =
        Document.ForbiddenPermissionKeys;

    public static IReadOnlyList<string> SubscriptionExemptPaths { get; } =
        Document.SubscriptionExemptPaths;

    public static IReadOnlyList<AcceptanceRoleRoute> Routes { get; } = Document.Routes;

    public static AcceptanceRoleRoute Route(string id)
    {
        var route = Routes.SingleOrDefault(candidate => candidate.Id == id);
        if (route is null)
        {
            throw new AcceptanceRoleContractException($"Acceptance role contract has no route '{id}'.");
        }

        return route;
    }

    public static AcceptanceRoleGrantPlan Reconcile(
        IReadOnlySet<string> activeCatalogKeys,
        IReadOnlySet<string> grantedKeys)
    {
        var missing = RequiredPermissionKeys
            .Where(key => !activeCatalogKeys.Contains(key))
            .ToArray();
        var toAdd = RequiredPermissionKeys
            .Where(key => activeCatalogKeys.Contains(key) && !grantedKeys.Contains(key))
            .ToArray();
        var toRemove = ForbiddenPermissionKeys
            .Where(grantedKeys.Contains)
            .ToArray();
        return new AcceptanceRoleGrantPlan(missing, toAdd, toRemove);
    }

    private static AcceptanceRoleContractDocument Load()
    {
        var assembly = typeof(AcceptanceRoleContract).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new AcceptanceRoleContractException(
                $"Acceptance role contract resource '{ResourceName}' is not embedded.");
        var document = JsonSerializer.Deserialize<AcceptanceRoleContractDocument>(stream, JsonOptions)
            ?? throw new AcceptanceRoleContractException("Acceptance role contract JSON is empty.");
        Validate(document);
        return document;
    }

    private static void Validate(AcceptanceRoleContractDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.CatalogVersion)
            || string.IsNullOrWhiteSpace(document.RoleKey)
            || string.IsNullOrWhiteSpace(document.Subject))
        {
            throw new AcceptanceRoleContractException("Acceptance role contract is missing catalog version, role, or subject.");
        }

        var required = document.RequiredPermissionKeys ?? [];
        var forbidden = document.ForbiddenPermissionKeys ?? [];
        var duplicates = required
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new AcceptanceRoleContractException(
                "Acceptance role contract repeats permission keys: " + string.Join(", ", duplicates));
        }

        var overlap = required.Intersect(forbidden, StringComparer.Ordinal).ToArray();
        if (overlap.Length > 0)
        {
            throw new AcceptanceRoleContractException(
                "Acceptance role contract lists keys as both required and forbidden: " + string.Join(", ", overlap));
        }

        var requiredSet = required.ToHashSet(StringComparer.Ordinal);
        var forbiddenSet = forbidden.ToHashSet(StringComparer.Ordinal);
        foreach (var route in document.Routes ?? [])
        {
            RequireListed(route.PermissionKey, requiredSet, route.Id, "permissionKey");
            RequireListed(route.QueryPermissionKey, requiredSet, route.Id, "queryPermissionKey");
            RequireListed(route.SimulatePermissionKey, requiredSet, route.Id, "simulatePermissionKey");
            if (!string.IsNullOrWhiteSpace(route.ForbiddenPermissionKey)
                && !forbiddenSet.Contains(route.ForbiddenPermissionKey))
            {
                throw new AcceptanceRoleContractException(
                    $"Route '{route.Id}' forbids '{route.ForbiddenPermissionKey}', which is not in forbiddenPermissionKeys.");
            }

            foreach (var key in route.PreparePermissionKeys ?? [])
            {
                RequireListed(key, requiredSet, route.Id, "preparePermissionKeys");
            }
        }
    }

    private static void RequireListed(string? key, HashSet<string> required, string routeId, string field)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!required.Contains(key))
        {
            throw new AcceptanceRoleContractException(
                $"Route '{routeId}' {field} '{key}' is not a required acceptance permission.");
        }
    }

    private sealed class AcceptanceRoleContractDocument
    {
        public string CatalogVersion { get; set; } = string.Empty;

        public string RoleKey { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public AcceptanceRoleRestrictedRole RestrictedRole { get; set; } = new();

        public string[] RequiredPermissionKeys { get; set; } = [];

        public string[] ForbiddenPermissionKeys { get; set; } = [];

        public string[] SubscriptionExemptPaths { get; set; } = [];

        public AcceptanceRoleRoute[] Routes { get; set; } = [];
    }

    private sealed class AcceptanceRoleRestrictedRole
    {
        public string RoleKey { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;
    }
}

public sealed class AcceptanceRoleRoute
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Path { get; set; }

    public string? PermissionKey { get; set; }

    public string? QueryPermissionKey { get; set; }

    public string? SimulatePermissionKey { get; set; }

    public string? ForbiddenPermissionKey { get; set; }

    public string[]? PreparePermissionKeys { get; set; }

    public string Nav { get; set; } = string.Empty;

    public string Acceptance { get; set; } = string.Empty;

    public string? Api { get; set; }
}
