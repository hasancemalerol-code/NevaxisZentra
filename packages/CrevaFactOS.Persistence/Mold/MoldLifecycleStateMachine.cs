namespace CrevaFactOS.Persistence.Mold;

public static class MoldLifecycleStateMachine
{
    private static readonly Dictionary<MoldLifecycleState, HashSet<MoldLifecycleState>> AllowedTransitions =
        new Dictionary<MoldLifecycleState, HashSet<MoldLifecycleState>>
        {
            [MoldLifecycleState.Draft] =
            [
                MoldLifecycleState.Active,
                MoldLifecycleState.Quarantined,
                MoldLifecycleState.Retired,
            ],
            [MoldLifecycleState.Active] =
            [
                MoldLifecycleState.Maintenance,
                MoldLifecycleState.Trial,
                MoldLifecycleState.Quarantined,
                MoldLifecycleState.Retired,
            ],
            [MoldLifecycleState.Maintenance] =
            [
                MoldLifecycleState.Active,
                MoldLifecycleState.Trial,
                MoldLifecycleState.Quarantined,
                MoldLifecycleState.Retired,
            ],
            [MoldLifecycleState.Trial] =
            [
                MoldLifecycleState.Active,
                MoldLifecycleState.Maintenance,
                MoldLifecycleState.Quarantined,
                MoldLifecycleState.Retired,
            ],
            [MoldLifecycleState.Quarantined] =
            [
                MoldLifecycleState.Active,
                MoldLifecycleState.Maintenance,
                MoldLifecycleState.Retired,
            ],
            [MoldLifecycleState.Retired] =
            [
                MoldLifecycleState.Active,
                MoldLifecycleState.Scrapped,
            ],
            [MoldLifecycleState.Scrapped] = [],
        };

    public static bool CanTransition(MoldLifecycleState from, MoldLifecycleState to) =>
        AllowedTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsTerminal(MoldLifecycleState state) =>
        state == MoldLifecycleState.Scrapped;
}
