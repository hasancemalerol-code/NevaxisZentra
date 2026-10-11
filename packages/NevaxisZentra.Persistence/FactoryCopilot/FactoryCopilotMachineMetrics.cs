using System.Globalization;

namespace NevaxisZentra.Persistence.FactoryCopilot;

/// <summary>
/// The factory's daily operating calendar. Every production KPI Copilot reports is computed over scheduled production
/// time only: a working day is 07:00-17:00 (600 min) minus three planned breaks (60 min) = 540 net minutes.
/// Planned breaks are planned downtime, never a machine loss.
/// </summary>
internal static class FactoryWorkingCalendar
{
    public const int GrossMinutesPerDay = 600;
    public const int PlannedBreakMinutesPerDay = 60;
    public const int NetScheduledMinutesPerDay = GrossMinutesPerDay - PlannedBreakMinutesPerDay;

    public const string Description =
        "Factory calendar: working day 07:00-17:00 (600 min); planned breaks 09:30-09:45, 12:00-12:30, 15:00-15:15 (60 min, planned downtime, not a loss); net scheduled production time 540 min per working day";
}

/// <summary>How a canonical event relates to the factory calendar. Carried in the event's operation code.</summary>
internal static class FactoryCalendarWindows
{
    public const string Scheduled = "SCHEDULED";
    public const string PlannedBreak = "PLANNED_BREAK";
    public const string OutsideShift = "OUTSIDE_SHIFT";

    public static bool IsClassified(string? window) =>
        window is Scheduled or PlannedBreak or OutsideShift;
}

internal sealed record FactoryCopilotProducedRow(Guid ResourceId, string? Window, decimal Units);

internal sealed record FactoryCopilotStopRow(Guid ResourceId, string? Window, string Reason, int Count, decimal Minutes);

internal sealed record FactoryCopilotMachineIdentity(Guid Id, string Code, string Name);

internal static class FactoryCopilotMachineMetrics
{
    /// <summary>Builds the calendar-aware per-machine summary from grouped event rows. Pure and deterministic.</summary>
    public static FactoryCopilotMachinePerformance Build(
        DateTimeOffset periodFrom,
        DateTimeOffset periodTo,
        int eventCount,
        int workingDays,
        IReadOnlyList<FactoryCopilotMachineIdentity> machines,
        IReadOnlyList<FactoryCopilotProducedRow> produced,
        IReadOnlyList<FactoryCopilotStopRow> stops)
    {
        var calendarClassified = produced.Any(p => FactoryCalendarWindows.IsClassified(p.Window))
            || stops.Any(s => FactoryCalendarWindows.IsClassified(s.Window));
        var scheduledMinutes = workingDays * FactoryWorkingCalendar.NetScheduledMinutesPerDay;

        var summaries = machines.Select(machine =>
        {
            decimal Units(string? window) => produced
                .Where(p => p.ResourceId == machine.Id && Matches(p.Window, window))
                .Sum(p => p.Units);
            decimal Minutes(string? window) => stops
                .Where(s => s.ResourceId == machine.Id && Matches(s.Window, window))
                .Sum(s => s.Minutes);
            int Count(string? window) => stops
                .Where(s => s.ResourceId == machine.Id && Matches(s.Window, window))
                .Sum(s => s.Count);

            // Without a calendar classification every event counts as "unclassified" and no calendar KPI is derived.
            var reasonWindow = calendarClassified ? FactoryCalendarWindows.Scheduled : null;
            var reasons = stops
                .Where(s => s.ResourceId == machine.Id && (calendarClassified ? s.Window == reasonWindow : true))
                .GroupBy(s => s.Reason)
                .Select(g => new FactoryCopilotMachineStopReason(g.Key, g.Sum(x => x.Count), g.Sum(x => x.Minutes)))
                .OrderByDescending(r => r.Minutes)
                .ThenBy(r => r.Reason, StringComparer.Ordinal)
                .Take(3)
                .ToList();

            var scheduledUnits = Units(FactoryCalendarWindows.Scheduled);
            var unplannedMinutes = Minutes(FactoryCalendarWindows.Scheduled);
            return new FactoryCopilotMachineSummary(
                machine.Code,
                machine.Name,
                scheduledUnits,
                Units(FactoryCalendarWindows.PlannedBreak),
                Units(FactoryCalendarWindows.OutsideShift),
                UnclassifiedUnits: calendarClassified ? 0m : Units(null),
                Count(FactoryCalendarWindows.Scheduled),
                unplannedMinutes,
                Minutes(FactoryCalendarWindows.PlannedBreak),
                Minutes(FactoryCalendarWindows.OutsideShift),
                UnclassifiedStopCount: calendarClassified ? 0 : Count(null),
                UnclassifiedStopMinutes: calendarClassified ? 0m : Minutes(null),
                reasons,
                AvailabilityPercent: calendarClassified && scheduledMinutes > 0
                    ? Math.Round(100m * (1m - unplannedMinutes / scheduledMinutes), 2)
                    : null,
                UnitsPerScheduledHour: calendarClassified && scheduledMinutes > 0
                    ? Math.Round(scheduledUnits / (scheduledMinutes / 60m), 2)
                    : null);
        })
        .OrderByDescending(m => calendarClassified ? m.UnplannedStopMinutes : m.UnclassifiedStopMinutes)
        .ThenBy(m => m.ResourceCode, StringComparer.Ordinal)
        .Take(10)
        .ToList();

        return new FactoryCopilotMachinePerformance(
            periodFrom,
            periodTo,
            eventCount,
            calendarClassified,
            workingDays,
            scheduledMinutes,
            summaries);

        bool Matches(string? actual, string? wanted) =>
            wanted is null
                ? !FactoryCalendarWindows.IsClassified(actual)
                : string.Equals(actual, wanted, StringComparison.Ordinal);
    }

    public static string Number(decimal? value) =>
        (value ?? 0m).ToString("0.##", CultureInfo.InvariantCulture);
}
