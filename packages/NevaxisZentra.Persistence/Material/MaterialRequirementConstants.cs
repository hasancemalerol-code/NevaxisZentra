namespace NevaxisZentra.Persistence.Material;

public static class MaterialRequirementConstants
{
    public const string RuleSetVersion = "MATERIAL-REQUIREMENT-V1";

    /// <summary>
    /// Default variance tolerance percentage for consumption comparison.
    /// </summary>
    public const decimal DefaultVarianceTolerancePercent = 5m;

    public const string TheoreticalRequirementDisclaimer =
        "Teorik malzeme ihtiyacıdır; ERP stok rezervasyonu veya gerçek tüketim değildir.";

    public const string ConsumptionObservationDisclaimer =
        "Operasyonel tüketim gözlemidir; ERP stok hareketi değildir.";
}
