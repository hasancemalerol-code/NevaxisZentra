namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialIntelligenceConstants
{
    public const string RuleSetVersion = "COMMERCIAL-INTELLIGENCE-1.0";
    public const string ComparisonRuleSetVersion = "COMMERCIAL-INTELLIGENCE-2.0";
    public const string SalesRuleSetVersion = "COMMERCIAL-SALES-PRICE-1.0";
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development commercial-intelligence data — simulated, non-production, not real customer evidence, market prices, or guaranteed savings.";
    public const string SalesDevelopmentPilotProvenance =
        "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence.";
    public const int DefaultMinimumComparableObservations = 6;
    public const decimal DefaultMaximumPositiveVarianceRatio = 0.10m;
    public const decimal DefaultSupplierSpreadThreshold = 0.10m;
    public const string ReferenceCenterMedian = "median";
}
