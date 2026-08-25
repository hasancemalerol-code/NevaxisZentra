namespace CrevaFactOS.Persistence.CommercialIntelligence;

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
