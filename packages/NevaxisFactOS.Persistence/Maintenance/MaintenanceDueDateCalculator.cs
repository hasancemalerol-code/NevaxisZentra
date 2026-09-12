namespace NevaxisFactOS.Persistence.Maintenance;

public static class MaintenanceDueDateCalculator
{
    public static DateTimeOffset CalculateNextDueAtUtc(
        PreventiveIntervalType intervalType,
        int intervalValue,
        DateTimeOffset fromUtc)
    {
        if (intervalValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalValue), "Interval value must be positive.");
        }

        return intervalType switch
        {
            PreventiveIntervalType.Days => fromUtc.AddDays(intervalValue),
            PreventiveIntervalType.Weeks => fromUtc.AddDays(intervalValue * 7),
            PreventiveIntervalType.Months => fromUtc.AddMonths(intervalValue),
            _ => fromUtc.AddMonths(intervalValue),
        };
    }
}
