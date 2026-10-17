namespace NevaxisZentra.Persistence.Maintenance;

/// <summary>
/// The one definition of "overdue preventive maintenance": an open due record (due or work-order-created) whose due
/// date has passed. The workspace summary, the reporting overview, asset reliability and risk projection all use it,
/// so the same fact always has the same value.
/// </summary>
internal static class MaintenanceOverdueRule
{
    public static bool IsOverdueOpenDue(PreventiveMaintenanceDue due, DateTimeOffset now) =>
        due.DueAtUtc < now &&
        due.Status is PreventiveMaintenanceDueStatus.Due or PreventiveMaintenanceDueStatus.WorkOrderCreated;
}
