namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialQuotationRiskEvidenceStatuses
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

public static class CommercialQuotationRiskSourceKinds
{
    public const string ManualDevelopmentSimulator = "manual-development-simulator";
    public const string ErpQuotationLine = "erp-quotation-line";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ManualDevelopmentSimulator, ErpQuotationLine,
    };
}

public static class CommercialQuotationRiskAssessmentOutcomes
{
    public const string WithinThreshold = "within-threshold";
    public const string Attention = "attention";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        WithinThreshold, Attention, InsufficientData, SourceConflict,
    };
}

public static class CommercialQuotationRiskDriverReasonCodes
{
    public const string QuotedPriceBelowReference = "quoted-price-below-reference";
    public const string QuotedPriceAboveReference = "quoted-price-above-reference";
    public const string QuotationExpired = "quotation-expired";
    public const string QuotationExpiringSoon = "quotation-expiring-soon";
    public const string PaymentTermExceeded = "payment-term-exceeded";
    public const string InsufficientComparableSalesEvidence = "insufficient-comparable-sales-evidence";
    public const string StaleReferenceEvidence = "stale-reference-evidence";
    public const string SourceConflict = "source-conflict";
    public const string IncompatibleCurrency = "incompatible-currency";
    public const string IncompatibleUnit = "incompatible-unit";
    public const string IncompatiblePriceBasis = "incompatible-price-basis";
    public const string WithinProfile = "within-profile";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        QuotedPriceBelowReference, QuotedPriceAboveReference, QuotationExpired,
        QuotationExpiringSoon, PaymentTermExceeded, InsufficientComparableSalesEvidence,
        StaleReferenceEvidence, SourceConflict, IncompatibleCurrency, IncompatibleUnit,
        IncompatiblePriceBasis, WithinProfile,
    };
}

public static class CommercialQuotationRiskExclusionReasonCodes
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
    public const string OutsideWindow = "outside-window";
    public const string StaleEvidence = "stale-evidence";
}
