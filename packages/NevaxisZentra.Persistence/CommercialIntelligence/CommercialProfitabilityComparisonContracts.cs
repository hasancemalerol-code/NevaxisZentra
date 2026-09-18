namespace NevaxisZentra.Persistence.CommercialIntelligence;

public static class CommercialProfitabilityComparisonTypes
{
    public const string TemporalContributionTrend = "temporal-contribution-trend";
    public const string CostComponentMixChange = "cost-component-mix-change";
    public const string CustomerContributionPosition = "customer-contribution-position";
    public const string MaterialContributionPosition = "material-contribution-position";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        TemporalContributionTrend,
        CostComponentMixChange,
        CustomerContributionPosition,
        MaterialContributionPosition,
    };
}

public static class CommercialProfitabilityComparisonOutcomes
{
    public const string WithinComparisonBand = "within-comparison-band";
    public const string ContributionDeterioration = "contribution-deterioration";
    public const string ContributionImprovement = "contribution-improvement";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        WithinComparisonBand,
        ContributionDeterioration,
        ContributionImprovement,
        InsufficientData,
        SourceConflict,
    };
}

public static class CommercialProfitabilityComparisonResultDimensions
{
    public const string Summary = "summary";
    public const string Customer = "customer";
    public const string Material = "material";
    public const string CostComponent = "cost-component";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Summary, Customer, Material, CostComponent,
    };
}

public static class CommercialProfitabilityContributionPositionStates
{
    public const string HigherObservedContributionPosition = "higher-observed-contribution-position";
    public const string WithinObservedContributionBand = "within-observed-contribution-band";
    public const string LowerObservedContributionPosition = "lower-observed-contribution-position";
    public const string TiedContributionPosition = "tied-contribution-position";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        HigherObservedContributionPosition,
        WithinObservedContributionBand,
        LowerObservedContributionPosition,
        TiedContributionPosition,
        InsufficientData,
        SourceConflict,
    };
}

public static class CommercialProfitabilityComparisonAggregationMethods
{
    public const string UnweightedMedian = "unweighted-median";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        UnweightedMedian,
    };
}

public static class CommercialProfitabilityComparisonDriverReasonCodes
{
    public const string ContributionMarginDeterioration = "contribution-margin-deterioration";
    public const string ContributionMarginImprovement = "contribution-margin-improvement";
    public const string NegativeContributionConcentration = "negative-contribution-concentration";
    public const string BelowThresholdConcentration = "below-threshold-concentration";
    public const string DirectMaterialCostIncrease = "direct-material-cost-increase";
    public const string DirectLaborCostIncrease = "direct-labor-cost-increase";
    public const string MachineCostIncrease = "machine-cost-increase";
    public const string EnergyCostIncrease = "energy-cost-increase";
    public const string ToolingCostIncrease = "tooling-cost-increase";
    public const string QualityCostIncrease = "quality-cost-increase";
    public const string ScrapReworkCostIncrease = "scrap-rework-cost-increase";
    public const string PackagingCostIncrease = "packaging-cost-increase";
    public const string LogisticsCostIncrease = "logistics-cost-increase";
    public const string AllocatedOverheadCostIncrease = "allocated-overhead-cost-increase";
    public const string OtherCostIncrease = "other-cost-increase";
    public const string CostShareIncrease = "cost-share-increase";
    public const string CustomerContributionConcentration = "customer-contribution-concentration";
    public const string MaterialContributionConcentration = "material-contribution-concentration";
    public const string ContributionPositionTie = "contribution-position-tie";
    public const string InsufficientReferenceWindow = "insufficient-reference-window";
    public const string InsufficientCurrentWindow = "insufficient-current-window";
    public const string InsufficientScopeEvidence = "insufficient-scope-evidence";
    public const string IncompleteCostCoverage = "incomplete-cost-coverage";
    public const string SourceConflict = "source-conflict";
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string WithinComparisonBand = "within-comparison-band";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ContributionMarginDeterioration, ContributionMarginImprovement,
        NegativeContributionConcentration, BelowThresholdConcentration,
        DirectMaterialCostIncrease, DirectLaborCostIncrease, MachineCostIncrease, EnergyCostIncrease,
        ToolingCostIncrease, QualityCostIncrease, ScrapReworkCostIncrease, PackagingCostIncrease,
        LogisticsCostIncrease, AllocatedOverheadCostIncrease, OtherCostIncrease, CostShareIncrease,
        CustomerContributionConcentration, MaterialContributionConcentration, ContributionPositionTie,
        InsufficientReferenceWindow, InsufficientCurrentWindow, InsufficientScopeEvidence,
        IncompleteCostCoverage, SourceConflict, IncompatibleCurrency, IncompatibleUnit,
        IncompatiblePriceBasis, WithinComparisonBand,
    };
}
