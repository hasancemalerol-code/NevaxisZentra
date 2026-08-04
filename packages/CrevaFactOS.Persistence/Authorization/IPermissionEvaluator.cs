namespace CrevaFactOS.Persistence.Authorization;

public interface IPermissionEvaluator
{
    Task<PermissionCheckResult> EvaluateAsync(
        Guid identitySubjectId,
        Guid tenantId,
        Guid membershipId,
        string permissionKey,
        CancellationToken cancellationToken = default);
}
