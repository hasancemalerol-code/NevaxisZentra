namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public static class DigitalWorkforceExecutionLeaseEngine
{
    public static readonly TimeSpan DefaultLeaseDuration = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan HeartbeatExtension = TimeSpan.FromMinutes(1);

    public static string CreateClaimToken() => Guid.NewGuid().ToString("N");

    public static bool IsLeaseExpired(DigitalWorkExecution execution, DateTimeOffset now) =>
        execution.LeaseExpiresAtUtc.HasValue && execution.LeaseExpiresAtUtc <= now;

    public static bool CanReclaim(DigitalWorkExecution execution, DateTimeOffset now) =>
        execution.Status is DigitalWorkExecutionStatuses.Claimed or DigitalWorkExecutionStatuses.Running &&
        IsLeaseExpired(execution, now);

    public static void ApplyClaim(DigitalWorkExecution execution, string owner, DateTimeOffset now)
    {
        execution.ClaimOwner = owner;
        execution.ClaimToken = CreateClaimToken();
        execution.ClaimedAtUtc = now;
        execution.LeaseExpiresAtUtc = now.Add(DefaultLeaseDuration);
        execution.LastHeartbeatUtc = now;
        execution.Status = DigitalWorkExecutionStatuses.Claimed;
        execution.UpdatedAtUtc = now;
    }

    public static void RefreshHeartbeat(DigitalWorkExecution execution, string owner, string claimToken, DateTimeOffset now)
    {
        if (!string.Equals(execution.ClaimOwner, owner, StringComparison.Ordinal) ||
            !string.Equals(execution.ClaimToken, claimToken, StringComparison.Ordinal))
        {
            throw new DigitalWorkforceException(
                DigitalWorkforceFailureCodes.ExecutionLeaseLost,
                "Execution lease is owned by another runtime.");
        }

        execution.LastHeartbeatUtc = now;
        execution.LeaseExpiresAtUtc = now.Add(HeartbeatExtension);
        execution.UpdatedAtUtc = now;
    }

    public static bool ValidateOwnership(DigitalWorkExecution execution, string owner, string? claimToken) =>
        string.Equals(execution.ClaimOwner, owner, StringComparison.Ordinal) &&
        (claimToken is null || string.Equals(execution.ClaimToken, claimToken, StringComparison.Ordinal));
}
