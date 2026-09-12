using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Production;

namespace NevaxisFactOS.Persistence.ShiftAssignment;

public static class ShiftAssignmentShiftWindowCalculator
{
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) ComputeShiftWindowUtc(
        Facility facility,
        ProductionShift shift,
        DateOnly shiftDate)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(facility.TimeZoneId);
            var localDate = shiftDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var shiftStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDate + shift.StartLocalTime, tz);
            var isOvernight = shift.EndLocalTime <= shift.StartLocalTime;
            var endBase = isOvernight ? localDate.AddDays(1) : localDate;
            var shiftEndUtc = TimeZoneInfo.ConvertTimeToUtc(endBase + shift.EndLocalTime, tz);
            return (shiftStartUtc, shiftEndUtc);
        }
        catch
        {
            var localDate = shiftDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var shiftStartUtc = new DateTimeOffset(localDate + shift.StartLocalTime, TimeSpan.Zero);
            var isOvernight = shift.EndLocalTime <= shift.StartLocalTime;
            var endBase = isOvernight ? localDate.AddDays(1) : localDate;
            var shiftEndUtc = new DateTimeOffset(endBase + shift.EndLocalTime, TimeSpan.Zero);
            return (shiftStartUtc, shiftEndUtc);
        }
    }
}
