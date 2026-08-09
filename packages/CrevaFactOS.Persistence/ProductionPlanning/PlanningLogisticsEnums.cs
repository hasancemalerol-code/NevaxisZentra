namespace CrevaFactOS.Persistence.ProductionPlanning;

public enum PlanningLogisticsConstraintType
{
    ShipmentPickup,
    ShipmentDeparture,
    PackingReady,
    QualityRelease,
    MaterialInbound,
    CustomerDelivery,
    CustomsCutoff,
}

public enum PlanningLogisticsConstraintStatus
{
    Active,
    Satisfied,
    AtRisk,
    Breached,
    NotApplicable,
}

public enum PlanningLogisticsRiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical,
}

public enum PlanningLogisticsSourceType
{
    ExportShipment,
    ExportCase,
}
