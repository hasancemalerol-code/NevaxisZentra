namespace CrevaFactOS.Persistence.ProductionPlanning;

public static class PlanningLogisticsValueConversions
{
    private static readonly Dictionary<PlanningLogisticsConstraintType, string> ConstraintTypeToStorage =
        new()
        {
            [PlanningLogisticsConstraintType.ShipmentPickup] = "shipment-pickup",
            [PlanningLogisticsConstraintType.ShipmentDeparture] = "shipment-departure",
            [PlanningLogisticsConstraintType.PackingReady] = "packing-ready",
            [PlanningLogisticsConstraintType.QualityRelease] = "quality-release",
            [PlanningLogisticsConstraintType.MaterialInbound] = "material-inbound",
            [PlanningLogisticsConstraintType.CustomerDelivery] = "customer-delivery",
            [PlanningLogisticsConstraintType.CustomsCutoff] = "customs-cutoff",
        };

    private static readonly Dictionary<PlanningLogisticsConstraintStatus, string> ConstraintStatusToStorage =
        new()
        {
            [PlanningLogisticsConstraintStatus.Active] = "active",
            [PlanningLogisticsConstraintStatus.Satisfied] = "satisfied",
            [PlanningLogisticsConstraintStatus.AtRisk] = "at-risk",
            [PlanningLogisticsConstraintStatus.Breached] = "breached",
            [PlanningLogisticsConstraintStatus.NotApplicable] = "not-applicable",
        };

    public static string ToStorage(PlanningLogisticsConstraintType type) =>
        ConstraintTypeToStorage.TryGetValue(type, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown constraint type: {type}");

    public static string ToStorage(PlanningLogisticsConstraintStatus status) =>
        ConstraintStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown constraint status: {status}");

    public static PlanningLogisticsConstraintType ConstraintTypeFromStorage(string value) =>
        ConstraintTypeToStorage.First(pair => pair.Value == value).Key;

    public static PlanningLogisticsConstraintStatus ConstraintStatusFromStorage(string value) =>
        ConstraintStatusToStorage.First(pair => pair.Value == value).Key;
}
