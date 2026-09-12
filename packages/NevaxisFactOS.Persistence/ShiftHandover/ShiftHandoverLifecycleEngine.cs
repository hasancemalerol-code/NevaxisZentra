namespace NevaxisFactOS.Persistence.ShiftHandover;

public static class ShiftHandoverLifecycleEngine
{
    private static readonly HashSet<(string From, string To)> ValidTransitions =
    [
        (ShiftHandoverStatuses.Draft, ShiftHandoverStatuses.ReadyForHandover),
        (ShiftHandoverStatuses.Draft, ShiftHandoverStatuses.Cancelled),
        (ShiftHandoverStatuses.ReadyForHandover, ShiftHandoverStatuses.Draft),
        (ShiftHandoverStatuses.ReadyForHandover, ShiftHandoverStatuses.HandedOver),
        (ShiftHandoverStatuses.ReadyForHandover, ShiftHandoverStatuses.Cancelled),
        (ShiftHandoverStatuses.HandedOver, ShiftHandoverStatuses.Acknowledged),
        (ShiftHandoverStatuses.Acknowledged, ShiftHandoverStatuses.Completed),
    ];

    public static bool CanTransition(string from, string to) =>
        ValidTransitions.Contains((from, to));

    public static bool IsTerminal(string status) =>
        ShiftHandoverStatuses.Terminal.Contains(status);

    public static bool IsPostAcknowledgment(string status) =>
        ShiftHandoverStatuses.PostAcknowledgment.Contains(status);

    public static bool CanEdit(string status) =>
        status is ShiftHandoverStatuses.Draft or ShiftHandoverStatuses.ReadyForHandover;

    public static bool CanAddItems(string status) =>
        status is ShiftHandoverStatuses.Draft or ShiftHandoverStatuses.ReadyForHandover;

    public static bool CanEditItems(string status) =>
        status is ShiftHandoverStatuses.Draft or ShiftHandoverStatuses.ReadyForHandover;

    public static bool CanMarkReady(string status) =>
        status == ShiftHandoverStatuses.Draft;

    public static bool CanRevertToDraft(string status) =>
        status == ShiftHandoverStatuses.ReadyForHandover;

    public static bool CanHandOver(string status) =>
        status == ShiftHandoverStatuses.ReadyForHandover;

    public static bool CanAcknowledge(string status) =>
        status == ShiftHandoverStatuses.HandedOver;

    public static bool CanComplete(string status) =>
        status == ShiftHandoverStatuses.Acknowledged;
}
