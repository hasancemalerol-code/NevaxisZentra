namespace NevaxisZentra.Persistence.Mold;

public static class MoldHistoryValueConversions
{
    public static string ToStorage(MoldMaintenanceType value) => value switch
    {
        MoldMaintenanceType.Preventive => "preventive",
        MoldMaintenanceType.Corrective => "corrective",
        MoldMaintenanceType.Cleaning => "cleaning",
        MoldMaintenanceType.Inspection => "inspection",
        MoldMaintenanceType.Repair => "repair",
        MoldMaintenanceType.Overhaul => "overhaul",
        _ => "other",
    };

    public static MoldMaintenanceType MaintenanceTypeFromStorage(string value) => value switch
    {
        "preventive" => MoldMaintenanceType.Preventive,
        "corrective" => MoldMaintenanceType.Corrective,
        "cleaning" => MoldMaintenanceType.Cleaning,
        "inspection" => MoldMaintenanceType.Inspection,
        "repair" => MoldMaintenanceType.Repair,
        "overhaul" => MoldMaintenanceType.Overhaul,
        _ => MoldMaintenanceType.Other,
    };

    public static string ToStorage(MoldMaintenanceStatus value) => value switch
    {
        MoldMaintenanceStatus.Planned => "planned",
        MoldMaintenanceStatus.InProgress => "in-progress",
        MoldMaintenanceStatus.Completed => "completed",
        MoldMaintenanceStatus.Cancelled => "cancelled",
        _ => "planned",
    };

    public static MoldMaintenanceStatus MaintenanceStatusFromStorage(string value) => value switch
    {
        "planned" => MoldMaintenanceStatus.Planned,
        "in-progress" => MoldMaintenanceStatus.InProgress,
        "completed" => MoldMaintenanceStatus.Completed,
        "cancelled" => MoldMaintenanceStatus.Cancelled,
        _ => MoldMaintenanceStatus.Planned,
    };

    public static string ToStorage(MoldRevisionType value) => value switch
    {
        MoldRevisionType.DesignChange => "design-change",
        MoldRevisionType.CavityChange => "cavity-change",
        MoldRevisionType.MaterialChange => "material-change",
        MoldRevisionType.CoolingChange => "cooling-change",
        MoldRevisionType.RunnerChange => "runner-change",
        MoldRevisionType.RepairImprovement => "repair-improvement",
        MoldRevisionType.CustomerChange => "customer-change",
        _ => "other",
    };

    public static MoldRevisionType RevisionTypeFromStorage(string value) => value switch
    {
        "design-change" => MoldRevisionType.DesignChange,
        "cavity-change" => MoldRevisionType.CavityChange,
        "material-change" => MoldRevisionType.MaterialChange,
        "cooling-change" => MoldRevisionType.CoolingChange,
        "runner-change" => MoldRevisionType.RunnerChange,
        "repair-improvement" => MoldRevisionType.RepairImprovement,
        "customer-change" => MoldRevisionType.CustomerChange,
        _ => MoldRevisionType.Other,
    };

    public static string ToStorage(MoldTrialResult value) => value switch
    {
        MoldTrialResult.Pending => "pending",
        MoldTrialResult.Successful => "successful",
        MoldTrialResult.ConditionallyApproved => "conditionally-approved",
        MoldTrialResult.Failed => "failed",
        MoldTrialResult.Cancelled => "cancelled",
        _ => "pending",
    };

    public static MoldTrialResult TrialResultFromStorage(string value) => value switch
    {
        "pending" => MoldTrialResult.Pending,
        "successful" => MoldTrialResult.Successful,
        "conditionally-approved" => MoldTrialResult.ConditionallyApproved,
        "failed" => MoldTrialResult.Failed,
        "cancelled" => MoldTrialResult.Cancelled,
        _ => MoldTrialResult.Pending,
    };

    public static string ToApi(MoldMaintenanceType value) => ToStorage(value);
    public static string ToApi(MoldMaintenanceStatus value) => ToStorage(value);
    public static string ToApi(MoldRevisionType value) => ToStorage(value);
    public static string ToApi(MoldTrialResult value) => ToStorage(value);
}
