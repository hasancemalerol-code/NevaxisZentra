namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public static class DigitalWorkforceApprovalGuard
{
    public static readonly TimeSpan DefaultApprovalTtl = TimeSpan.FromHours(8);

    public static bool IsExpired(DigitalWorkApproval approval, DateTimeOffset now) =>
        approval.ExpiresAtUtc.HasValue && approval.ExpiresAtUtc <= now;

    public static bool IsStale(DigitalWorkApproval approval, DigitalWorkCase workCase) =>
        approval.CaseConcurrencyTokenAtRequest != workCase.ConcurrencyToken;

    public static bool CanResumeExecution(DigitalWorkApproval approval, DigitalWorkCase workCase, DateTimeOffset now)
    {
        if (approval.Status != DigitalWorkApprovalStatuses.Approved)
        {
            return false;
        }

        if (IsExpired(approval, now))
        {
            return false;
        }

        if (IsStale(approval, workCase))
        {
            return false;
        }

        return true;
    }

    public static DateTimeOffset ResolveExpiresAt(DateTimeOffset createdAtUtc) =>
        createdAtUtc.Add(DefaultApprovalTtl);
}
