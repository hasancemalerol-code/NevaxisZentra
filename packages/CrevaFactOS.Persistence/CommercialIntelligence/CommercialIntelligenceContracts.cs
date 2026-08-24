namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialIntelligenceProfileStatuses
{
    public const string Draft = "draft";
    public const string Approved = "approved";
    public const string Inactive = "inactive";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft, Approved, Inactive,
    };
}

public static class CommercialIntelligenceEvidenceStatuses
{
    public const string MeasuredTransaction = "measured-transaction";
    public const string Calculated = "calculated";
    public const string Simulated = "simulated";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        MeasuredTransaction, Calculated, Simulated, InsufficientData, SourceConflict,
    };
}

public static class CommercialIntelligenceAssessmentOutcomes
{
    public const string WithinReferenceRange = "within-reference-range";
    public const string Attention = "attention";
    public const string FavorableVariance = "favorable-variance";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        WithinReferenceRange, Attention, FavorableVariance, InsufficientData, SourceConflict,
    };
}

public static class CommercialIntelligenceDriverReasonCodes
{
    public const string WithinReferenceRange = "within-reference-range";
    public const string PurchasePriceIncrease = "purchase-price-increase";
    public const string FavorablePriceVariance = "favorable-price-variance";
    public const string SupplierPriceSpread = "supplier-price-spread";
    public const string InsufficientComparableObservations = "insufficient-comparable-observations";
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string MissingReferencePrice = "missing-reference-price";
    public const string DuplicateSourceEvidence = "duplicate-source-evidence";
    public const string SourceConflict = "source-conflict";
}

public static class CommercialIntelligenceComparisonDimensions
{
    public const string Material = "material";
    public const string MaterialSupplier = "material-supplier";
    public const string PurchaseOrderLine = "purchase-order-line";
    public const string Facility = "facility";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Material, MaterialSupplier, PurchaseOrderLine, Facility,
    };
}

public static class CommercialIntelligencePriceBasisInclusionStates
{
    public const string Excluded = "excluded";
    public const string Included = "included";
    public const string Unknown = "unknown";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Excluded, Included, Unknown,
    };
}

public static class CommercialIntelligenceFailureCodes
{
    public const string NotFound = "commercial-intelligence.not-found";
    public const string ValidationFailed = "commercial-intelligence.validation-failed";
    public const string ConcurrencyConflict = "commercial-intelligence.concurrency-conflict";
    public const string IdempotencyConflict = "commercial-intelligence.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "commercial-intelligence.idempotency-legacy-unverifiable";
    public const string Unavailable = "commercial-intelligence.unavailable";
}

public static class CommercialIntelligenceExclusionReasonCodes
{
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string DuplicateSourceEvidence = "duplicate-source-evidence";
    public const string OutsideWindow = "outside-window";
    public const string DisallowedEvidenceStatus = "disallowed-evidence-status";
    public const string InvalidQuantityOrPrice = "invalid-quantity-or-price";
    public const string SupplierMismatch = "supplier-mismatch";
}
