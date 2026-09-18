namespace NevaxisZentra.Persistence.Authorization;

internal sealed class DenyAllPermissionEvaluator : IPermissionEvaluator
{
    public Task<PermissionCheckResult> EvaluateAsync(
        Guid identitySubjectId,
        Guid tenantId,
        Guid membershipId,
        string permissionKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PermissionCheckResult.Deny("Persistence-backed permission evaluation is unavailable."));
}
