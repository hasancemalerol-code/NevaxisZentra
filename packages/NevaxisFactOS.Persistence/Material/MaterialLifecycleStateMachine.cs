namespace NevaxisFactOS.Persistence.Material;

public static class MaterialLifecycleStateMachine
{
    private static readonly Dictionary<MaterialLifecycleStatus, HashSet<MaterialLifecycleStatus>> AllowedTransitions =
        new()
        {
            [MaterialLifecycleStatus.Draft] =
            [
                MaterialLifecycleStatus.Active,
                MaterialLifecycleStatus.Blocked,
            ],
            [MaterialLifecycleStatus.Active] =
            [
                MaterialLifecycleStatus.Blocked,
                MaterialLifecycleStatus.Obsolete,
            ],
            [MaterialLifecycleStatus.Blocked] =
            [
                MaterialLifecycleStatus.Active,
                MaterialLifecycleStatus.Obsolete,
            ],
            [MaterialLifecycleStatus.Obsolete] = [],
        };

    public static bool CanTransition(MaterialLifecycleStatus from, MaterialLifecycleStatus to) =>
        AllowedTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsTerminal(MaterialLifecycleStatus status) =>
        status == MaterialLifecycleStatus.Obsolete;
}
