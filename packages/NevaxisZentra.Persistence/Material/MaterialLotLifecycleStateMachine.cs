namespace NevaxisZentra.Persistence.Material;

public static class MaterialLotLifecycleStateMachine
{
    private static readonly Dictionary<MaterialLotStatus, HashSet<MaterialLotStatus>> AllowedTransitions =
        new()
        {
            [MaterialLotStatus.Draft] =
            [
                MaterialLotStatus.Received,
                MaterialLotStatus.Blocked,
            ],
            [MaterialLotStatus.Received] =
            [
                MaterialLotStatus.QualityHold,
                MaterialLotStatus.Available,
                MaterialLotStatus.Blocked,
            ],
            [MaterialLotStatus.QualityHold] =
            [
                MaterialLotStatus.Available,
                MaterialLotStatus.Blocked,
            ],
            [MaterialLotStatus.Available] =
            [
                MaterialLotStatus.QualityHold,
                MaterialLotStatus.Blocked,
                MaterialLotStatus.Closed,
            ],
            [MaterialLotStatus.Blocked] =
            [
                MaterialLotStatus.QualityHold,
                MaterialLotStatus.Closed,
            ],
            [MaterialLotStatus.Closed] = [],
        };

    public static bool CanTransition(MaterialLotStatus from, MaterialLotStatus to) =>
        AllowedTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsTerminal(MaterialLotStatus status) =>
        status == MaterialLotStatus.Closed;
}
