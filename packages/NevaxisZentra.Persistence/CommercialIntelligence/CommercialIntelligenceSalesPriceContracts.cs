namespace NevaxisZentra.Persistence.CommercialIntelligence;

public static class CommercialSalesPriceEvidenceKinds
{
    public const string SalesOrderLine = "sales-order-line";
    public const string InvoiceLine = "invoice-line";
    public const string QuotationLine = "quotation-line";
    public const string ManualDevelopmentSimulator = "manual-development-simulator";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        SalesOrderLine, InvoiceLine, QuotationLine, ManualDevelopmentSimulator,
    };
}

public static class CommercialSalesPriceGrossNetBasis
{
    public const string Gross = "gross";
    public const string Net = "net";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Gross, Net,
    };
}

public static class CommercialSalesPriceComparisonDimensions
{
    public const string Material = "material";
    public const string MaterialCustomer = "material-customer";
    public const string SalesOrderLine = "sales-order-line";
    public const string Facility = "facility";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Material, MaterialCustomer, SalesOrderLine, Facility,
    };
}

public static class CommercialSalesPriceDriverReasonCodes
{
    public const string WithinReferenceRange = "within-reference-range";
    public const string SalesPriceIncrease = "sales-price-increase";
    public const string FavorablePriceVariance = "favorable-price-variance";
    public const string InsufficientComparableObservations = "insufficient-comparable-observations";
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string IncompatibleEvidenceKind = "incompatible-evidence-kind";
    public const string IncompatibleGrossNetBasis = "incompatible-gross-net-basis";
    public const string CustomerMismatch = "customer-mismatch";
    public const string CancelledOrReturned = "cancelled-or-returned";
    public const string MissingReferencePrice = "missing-reference-price";
    public const string DuplicateSourceEvidence = "duplicate-source-evidence";
    public const string SourceConflict = "source-conflict";
}

public static class CommercialSalesPriceComparisonTypes
{
    public const string TemporalSalesPriceDeviation = "temporal-sales-price-deviation";
    public const string CustomerPricePosition = "customer-price-position";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        TemporalSalesPriceDeviation, CustomerPricePosition,
    };
}

public static class CommercialSalesPriceComparisonOutcomes
{
    public const string WithinReferenceRange = "within-reference-range";
    public const string Attention = "attention";
    public const string PositivePriceVariance = "positive-price-variance";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        WithinReferenceRange, Attention, PositivePriceVariance, InsufficientData, SourceConflict,
    };
}

public static class CommercialSalesPriceCustomerPositionStates
{
    public const string LowestComparablePrice = "lowest-comparable-price";
    public const string WithinSpreadThreshold = "within-spread-threshold";
    public const string HigherComparablePrice = "higher-comparable-price";
    public const string TiedComparablePrice = "tied-comparable-price";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        LowestComparablePrice, WithinSpreadThreshold, HigherComparablePrice,
        TiedComparablePrice, InsufficientData, SourceConflict,
    };
}

public static class CommercialSalesPriceComparisonDriverReasonCodes
{
    public const string WithinReferenceRange = "within-reference-range";
    public const string PositivePriceVariance = "positive-price-variance";
    public const string SalesPriceDecrease = "sales-price-decrease";
    public const string CurrentWindowPriceIncrease = "current-window-price-increase";
    public const string CurrentWindowFavorableVariance = "current-window-favorable-variance";
    public const string CustomerPriceSpread = "customer-price-spread";
    public const string CustomerLowestComparableMedian = "customer-lowest-comparable-median";
    public const string CustomerHigherComparableMedian = "customer-higher-comparable-median";
    public const string CustomerMedianTie = "customer-median-tie";
    public const string InsufficientCurrentWindow = "insufficient-current-window";
    public const string InsufficientReferenceWindow = "insufficient-reference-window";
    public const string InsufficientCustomerObservations = "insufficient-customer-observations";
    public const string SingleComparableCustomer = "single-comparable-customer";
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string IncompatibleGrossNetBasis = "incompatible-gross-net-basis";
    public const string IncompatibleEvidenceKind = "incompatible-evidence-kind";
    public const string DuplicateSourceEvidence = "duplicate-source-evidence";
    public const string MissingReferenceMedian = "missing-reference-median";
    public const string SourceConflict = "source-conflict";
}

public static class CommercialSalesPriceExclusionReasonCodes
{
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string IncompatibleEvidenceKind = "incompatible-evidence-kind";
    public const string IncompatibleGrossNetBasis = "incompatible-gross-net-basis";
    public const string CustomerMismatch = "customer-mismatch";
    public const string CancelledOrReturned = "cancelled-or-returned";
    public const string DuplicateSourceEvidence = "duplicate-source-evidence";
    public const string OutsideWindow = "outside-window";
    public const string DisallowedEvidenceStatus = "disallowed-evidence-status";
    public const string InvalidQuantityOrPrice = "invalid-quantity-or-price";
}
