namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public static class CommercialProfitabilityEvidenceStatuses
{
    public const string Simulated = "simulated";
    public const string Measured = "measured";
    public const string Calculated = "calculated";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Simulated, Measured, Calculated, InsufficientData, SourceConflict,
    };
}

public static class CommercialProfitabilityObservationKinds
{
    public const string Sales = "sales";
    public const string CostComponent = "cost-component";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Sales, CostComponent,
    };
}

public static class CommercialProfitabilitySourceKinds
{
    public const string ManualDevelopmentSimulator = "manual-development-simulator";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ManualDevelopmentSimulator,
    };
}

public static class CommercialProfitabilityCostComponentKinds
{
    public const string DirectMaterial = "direct-material";
    public const string DirectLabor = "direct-labor";
    public const string Machine = "machine";
    public const string Energy = "energy";
    public const string Tooling = "tooling";
    public const string Quality = "quality";
    public const string ScrapRework = "scrap-rework";
    public const string Packaging = "packaging";
    public const string Logistics = "logistics";
    public const string AllocatedOverhead = "allocated-overhead";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        DirectMaterial, DirectLabor, Machine, Energy, Tooling, Quality, ScrapRework,
        Packaging, Logistics, AllocatedOverhead, Other,
    };

    public static readonly IReadOnlyList<string> DefaultRequiredKinds =
    [
        DirectMaterial, DirectLabor, Machine, Energy,
    ];
}

public static class CommercialProfitabilityCostCoverageStatuses
{
    public const string CompleteRequiredCostCoverage = "complete-required-cost-coverage";
    public const string PartialCostCoverage = "partial-cost-coverage";
    public const string InsufficientCostEvidence = "insufficient-cost-evidence";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        CompleteRequiredCostCoverage, PartialCostCoverage, InsufficientCostEvidence, SourceConflict,
    };
}

public static class CommercialProfitabilityAssessmentOutcomes
{
    public const string WithinThreshold = "within-threshold";
    public const string BelowThreshold = "below-threshold";
    public const string NegativeContribution = "negative-contribution";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        WithinThreshold, BelowThreshold, NegativeContribution, InsufficientData, SourceConflict,
    };
}

public static class CommercialProfitabilityComponentInclusionStatuses
{
    public const string Included = "included";
    public const string Excluded = "excluded";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Included, Excluded,
    };
}

public static class CommercialProfitabilityExclusionReasonCodes
{
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string IncompatibleGrossNetBasis = "incompatible-gross-net-basis";
    public const string IncompatibleEvidenceKind = "incompatible-evidence-kind";
    public const string CancelledOrReturned = "cancelled-or-returned";
    public const string DisallowedEvidenceStatus = "disallowed-evidence-status";
    public const string InvalidQuantityOrPrice = "invalid-quantity-or-price";
    public const string DuplicateSourceEvidence = "duplicate-source-evidence";
    public const string StaleEvidence = "stale-evidence";
    public const string SourceConflict = "source-conflict";
    public const string InvalidAllocationBasis = "invalid-allocation-basis";
    public const string MissingAllocationBasis = "missing-allocation-basis";
}

public static class CommercialProfitabilityDriverReasonCodes
{
    public const string NegativeUnitContribution = "negative-unit-contribution";
    public const string ContributionMarginBelowThreshold = "contribution-margin-below-threshold";
    public const string IncompleteCostCoverage = "incomplete-cost-coverage";
    public const string MissingDirectMaterialCost = "missing-direct-material-cost";
    public const string MissingDirectLaborCost = "missing-direct-labor-cost";
    public const string MissingMachineCost = "missing-machine-cost";
    public const string MissingEnergyCost = "missing-energy-cost";
    public const string MissingOtherRequiredCost = "missing-other-required-cost";
    public const string StaleSalesEvidence = "stale-sales-evidence";
    public const string StaleCostEvidence = "stale-cost-evidence";
    public const string SourceConflict = "source-conflict";
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string InvalidAllocationBasis = "invalid-allocation-basis";
    public const string WithinProfile = "within-profile";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        NegativeUnitContribution, ContributionMarginBelowThreshold, IncompleteCostCoverage,
        MissingDirectMaterialCost, MissingDirectLaborCost, MissingMachineCost, MissingEnergyCost,
        MissingOtherRequiredCost, StaleSalesEvidence, StaleCostEvidence, SourceConflict,
        IncompatibleCurrency, IncompatibleUnit, IncompatiblePriceBasis, InvalidAllocationBasis,
        WithinProfile,
    };
}
