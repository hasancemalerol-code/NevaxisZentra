namespace NevaxisZentra.Persistence.Mold;

public static class MoldHistoryValidator
{
    public const int MaxSummaryLength = 1000;
    public const int MaxFindingsLength = 2000;
    public const int MaxActionsLength = 2000;
    public const int MaxDisplayNameLength = 200;
    public const int MaxReferenceLength = 200;
    public const int MaxDecisionLength = 1000;
    public const int MaxTechnicalChangeLength = 2000;
    public const int MaxReasonLength = 1000;
    public const int MaxNumberLength = 50;

    public static bool TryNormalizeNumber(string? number, out string display, out string normalized, out string? failure)
    {
        display = string.Empty;
        normalized = string.Empty;
        failure = null;

        if (string.IsNullOrWhiteSpace(number))
        {
            failure = "Number is required.";
            return false;
        }

        display = number.Trim();
        if (display.Length > MaxNumberLength)
        {
            failure = "Number exceeds supported length.";
            return false;
        }

        normalized = display.ToUpperInvariant();
        return true;
    }

    public static string? ValidateBoundedText(string? value, int maxLength, string fieldName)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length > maxLength
            ? $"{fieldName} exceeds supported length."
            : null;
    }

    public static string? ValidateRequiredSummary(string? summary) =>
        string.IsNullOrWhiteSpace(summary)
            ? "Summary is required."
            : ValidateBoundedText(summary, MaxSummaryLength, "Summary");

    public static string? ValidateTrialCounts(int? shotCount, int? goodPartCount, int? scrapPartCount)
    {
        if (shotCount is < 0)
        {
            return "Shot count cannot be negative.";
        }

        if (goodPartCount is < 0)
        {
            return "Good part count cannot be negative.";
        }

        if (scrapPartCount is < 0)
        {
            return "Scrap part count cannot be negative.";
        }

        if (shotCount.HasValue && goodPartCount.HasValue && scrapPartCount.HasValue)
        {
            if (goodPartCount.Value + scrapPartCount.Value > shotCount.Value)
            {
                return "Good and scrap counts cannot exceed shot count.";
            }
        }
        else if (shotCount.HasValue)
        {
            var total = (goodPartCount ?? 0) + (scrapPartCount ?? 0);
            if (total > shotCount.Value)
            {
                return "Good and scrap counts cannot exceed shot count.";
            }
        }

        return null;
    }

    public static string? ValidateCycleTime(decimal? cycleTimeSeconds) =>
        cycleTimeSeconds is <= 0 ? "Cycle time must be positive when provided." : null;

    public static string? ValidateCavityCount(int? cavityCount) =>
        cavityCount is <= 0 ? "Cavity count must be positive when provided." : null;

    public static string? ValidateMaintenanceCompletion(
        MoldMaintenanceStatus status,
        DateTimeOffset openedAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        if (status == MoldMaintenanceStatus.Completed && completedAtUtc is null)
        {
            return "Completed maintenance requires a completion timestamp.";
        }

        if (completedAtUtc.HasValue && completedAtUtc.Value < openedAtUtc)
        {
            return "Completion cannot precede opening.";
        }

        return null;
    }

    public static bool IsTerminalMaintenanceStatus(MoldMaintenanceStatus status) =>
        status is MoldMaintenanceStatus.Completed or MoldMaintenanceStatus.Cancelled;
}
