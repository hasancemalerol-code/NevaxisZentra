namespace NevaxisFactOS.Persistence.Authorization;

public sealed record PermissionCheckResult(bool Granted, string InternalReason)
{
    public static PermissionCheckResult Grant() => new(true, "Exact active tenant permission assignment found.");

    public static PermissionCheckResult Deny(string reason) => new(false, reason);
}
