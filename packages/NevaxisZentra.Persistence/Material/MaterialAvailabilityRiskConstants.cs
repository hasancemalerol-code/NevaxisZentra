namespace NevaxisZentra.Persistence.Material;

/// <summary>
/// Authoritative material availability risk boundary.
/// Source: <see cref="MaterialRequirementAssessment"/> with <see cref="MaterialRequirementAssessmentType.AvailabilityRisk"/>.
/// Inputs: ERP inventory balances + material requirement profiles (no invented thresholds).
/// </summary>
public static class MaterialAvailabilityRiskConstants
{
    public const string RuleSetVersion = "INVENTORY-RISK-V1";

    public const string WorkspaceDisclaimer =
        "Bu ekran yalnızca malzeme kullanılabilirliği riskini gösterir; otomatik stok, satın alma veya üretim işlemi yapmaz.";
}

public static class MaterialAvailabilityRiskStatuses
{
    public const string Sufficient = "sufficient";
    public const string Shortage = "shortage";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Sufficient,
        Shortage,
        InsufficientData,
        SourceConflict,
    };
}

public static class MaterialAvailabilityRiskDriverCodes
{
    public const string RequirementMissing = "requirement-missing";
    public const string RequirementIncomplete = "requirement-incomplete";
    public const string ErpReadDenied = "erp-read-denied";
    public const string MaterialErpLinkMissing = "material-erp-link-missing";
    public const string ErpBalanceMissing = "erp-balance-missing";
    public const string ErpSourceStale = "erp-source-stale";
    public const string ErpQuantityUnknown = "erp-quantity-unknown";
    public const string UnitConflict = "unit-conflict";
    public const string ShortageDetected = "shortage-detected";
    public const string SufficientCoverage = "sufficient-coverage";
}
