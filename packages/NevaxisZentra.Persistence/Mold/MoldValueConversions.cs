namespace NevaxisZentra.Persistence.Mold;

public static class MoldValueConversions
{
    public static string ToStorage(MoldOwnershipType value) => value switch
    {
        MoldOwnershipType.CompanyOwned => "company-owned",
        MoldOwnershipType.CustomerOwned => "customer-owned",
        MoldOwnershipType.SupplierOwned => "supplier-owned",
        MoldOwnershipType.Leased => "leased",
        _ => "unknown",
    };

    public static MoldOwnershipType OwnershipFromStorage(string value) => value switch
    {
        "company-owned" => MoldOwnershipType.CompanyOwned,
        "customer-owned" => MoldOwnershipType.CustomerOwned,
        "supplier-owned" => MoldOwnershipType.SupplierOwned,
        "leased" => MoldOwnershipType.Leased,
        _ => MoldOwnershipType.Unknown,
    };

    public static string ToStorage(MoldLocationType value) => value switch
    {
        MoldLocationType.Warehouse => "warehouse",
        MoldLocationType.ProductionArea => "production-area",
        MoldLocationType.MaintenanceArea => "maintenance-area",
        MoldLocationType.TrialArea => "trial-area",
        MoldLocationType.ExternalCustomer => "external-customer",
        MoldLocationType.ExternalSupplier => "external-supplier",
        MoldLocationType.Transit => "transit",
        _ => "unknown",
    };

    public static MoldLocationType LocationFromStorage(string value) => value switch
    {
        "warehouse" => MoldLocationType.Warehouse,
        "production-area" => MoldLocationType.ProductionArea,
        "maintenance-area" => MoldLocationType.MaintenanceArea,
        "trial-area" => MoldLocationType.TrialArea,
        "external-customer" => MoldLocationType.ExternalCustomer,
        "external-supplier" => MoldLocationType.ExternalSupplier,
        "transit" => MoldLocationType.Transit,
        _ => MoldLocationType.Unknown,
    };

    public static string ToStorage(MoldSourceType value) => value switch
    {
        MoldSourceType.Manual => "manual",
        MoldSourceType.Imported => "imported",
        MoldSourceType.Erp => "erp",
        _ => "unknown",
    };

    public static MoldSourceType SourceFromStorage(string value) => value switch
    {
        "manual" => MoldSourceType.Manual,
        "imported" => MoldSourceType.Imported,
        "erp" => MoldSourceType.Erp,
        _ => MoldSourceType.Unknown,
    };

    public static string ToStorage(MoldCounterSourceType value) => value switch
    {
        MoldCounterSourceType.Manual => "manual",
        MoldCounterSourceType.Production => "production",
        MoldCounterSourceType.Edge => "edge",
        MoldCounterSourceType.Erp => "erp",
        MoldCounterSourceType.Imported => "imported",
        _ => "unknown",
    };

    public static MoldCounterSourceType CounterSourceFromStorage(string value) => value switch
    {
        "manual" => MoldCounterSourceType.Manual,
        "production" => MoldCounterSourceType.Production,
        "edge" => MoldCounterSourceType.Edge,
        "erp" => MoldCounterSourceType.Erp,
        "imported" => MoldCounterSourceType.Imported,
        _ => MoldCounterSourceType.Unknown,
    };

    public static string ToStorage(MoldLifecycleState value) => value switch
    {
        MoldLifecycleState.Draft => "draft",
        MoldLifecycleState.Active => "active",
        MoldLifecycleState.Maintenance => "maintenance",
        MoldLifecycleState.Trial => "trial",
        MoldLifecycleState.Quarantined => "quarantined",
        MoldLifecycleState.Retired => "retired",
        MoldLifecycleState.Scrapped => "scrapped",
        _ => "draft",
    };

    public static MoldLifecycleState LifecycleFromStorage(string value) => value switch
    {
        "draft" => MoldLifecycleState.Draft,
        "active" => MoldLifecycleState.Active,
        "maintenance" => MoldLifecycleState.Maintenance,
        "trial" => MoldLifecycleState.Trial,
        "quarantined" => MoldLifecycleState.Quarantined,
        "retired" => MoldLifecycleState.Retired,
        "scrapped" => MoldLifecycleState.Scrapped,
        _ => MoldLifecycleState.Draft,
    };
}
