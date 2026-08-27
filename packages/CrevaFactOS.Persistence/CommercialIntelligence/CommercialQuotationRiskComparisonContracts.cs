namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialQuotationRiskComparisonTypes
{
    public const string TemporalRiskTrend = "temporal-risk-trend";
    public const string CustomerRiskPosition = "customer-risk-position";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        TemporalRiskTrend, CustomerRiskPosition,
    };
}

public static class CommercialQuotationRiskComparisonOutcomes
{
    public const string WithinRiskBand = "within-risk-band";
    public const string RiskIncrease = "risk-increase";
    public const string RiskDecrease = "risk-decrease";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        WithinRiskBand, RiskIncrease, RiskDecrease, InsufficientData, SourceConflict,
    };
}

public static class CommercialQuotationRiskCustomerPositionStates
{
    public const string LowestObservedRiskPosition = "lowest-observed-risk-position";
    public const string WithinRiskBand = "within-risk-band";
    public const string HigherObservedRiskPosition = "higher-observed-risk-position";
    public const string TiedRiskPosition = "tied-risk-position";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        LowestObservedRiskPosition, WithinRiskBand, HigherObservedRiskPosition,
        TiedRiskPosition, InsufficientData, SourceConflict,
    };
}

public static class CommercialQuotationRiskComparisonDriverReasonCodes
{
    public const string RiskIncidenceIncrease = "risk-incidence-increase";
    public const string RiskIncidenceDecrease = "risk-incidence-decrease";
    public const string PriceRiskConcentration = "price-risk-concentration";
    public const string ExpiryRiskConcentration = "expiry-risk-concentration";
    public const string PaymentTermRiskConcentration = "payment-term-risk-concentration";
    public const string CustomerRiskConcentration = "customer-risk-concentration";
    public const string CustomerRiskTie = "customer-risk-tie";
    public const string InsufficientReferenceWindow = "insufficient-reference-window";
    public const string InsufficientCurrentWindow = "insufficient-current-window";
    public const string InsufficientCustomerEvidence = "insufficient-customer-evidence";
    public const string SourceConflict = "source-conflict";
    public const string WithinRiskBand = "within-risk-band";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        RiskIncidenceIncrease, RiskIncidenceDecrease, PriceRiskConcentration,
        ExpiryRiskConcentration, PaymentTermRiskConcentration, CustomerRiskConcentration,
        CustomerRiskTie, InsufficientReferenceWindow, InsufficientCurrentWindow,
        InsufficientCustomerEvidence, SourceConflict, WithinRiskBand,
    };
}
