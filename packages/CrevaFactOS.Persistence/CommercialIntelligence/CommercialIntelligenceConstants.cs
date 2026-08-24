namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialIntelligenceConstants
{
    public const string RuleSetVersion = "COMMERCIAL-INTELLIGENCE-1.0";
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development commercial-intelligence data — simulated, non-production, not real customer evidence, market prices, or guaranteed savings.";
    public const int DefaultMinimumComparableObservations = 6;
    public const decimal DefaultMaximumPositiveVarianceRatio = 0.10m;
    public const decimal DefaultSupplierSpreadThreshold = 0.10m;
    public const string ReferenceCenterMedian = "median";
}
