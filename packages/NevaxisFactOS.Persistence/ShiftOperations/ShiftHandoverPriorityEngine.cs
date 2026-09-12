namespace NevaxisFactOS.Persistence.ShiftOperations;

public static class ShiftHandoverPriorityEngine
{
    public static string ResolvePriority(
        string category,
        string severity,
        bool verificationRequired,
        int carryoverCount,
        DateTimeOffset? deadlineUtc)
    {
        if (severity is "critical" or ShiftHandoverPriorities.Critical)
        {
            return ShiftHandoverPriorities.Critical;
        }

        if (category is ShiftOpenItemCategories.Machine or ShiftOpenItemCategories.Maintenance &&
            verificationRequired)
        {
            return ShiftHandoverPriorities.Critical;
        }

        if (carryoverCount >= 3)
        {
            return ShiftHandoverPriorities.Critical;
        }

        if (deadlineUtc.HasValue && deadlineUtc.Value <= DateTimeOffset.UtcNow.AddHours(4))
        {
            return ShiftHandoverPriorities.High;
        }

        if (severity is "high" or ShiftHandoverPriorities.High || carryoverCount >= 2)
        {
            return ShiftHandoverPriorities.High;
        }

        if (category is ShiftOpenItemCategories.Approval or ShiftOpenItemCategories.DigitalWorkforce)
        {
            return ShiftHandoverPriorities.High;
        }

        return severity is "low" or ShiftHandoverPriorities.Info
            ? ShiftHandoverPriorities.Info
            : ShiftHandoverPriorities.Normal;
    }
}
