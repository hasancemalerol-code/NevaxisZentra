namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialIntelligenceConstants
{
    public const string RuleSetVersion = "COMMERCIAL-INTELLIGENCE-1.0";
    public const string ComparisonRuleSetVersion = "COMMERCIAL-INTELLIGENCE-2.0";
    public const string SalesRuleSetVersion = "COMMERCIAL-SALES-PRICE-1.0";
    public const string SalesComparisonRuleSetVersion = "COMMERCIAL-SALES-PRICE-2.0";
    public const string QuotationRiskRuleSetVersion = "COMMERCIAL-QUOTATION-RISK-1.0";
    public const string QuotationRiskComparisonRuleSetVersion = "COMMERCIAL-QUOTATION-RISK-2.0";
    public const string ProfitabilityRuleSetVersion = "COMMERCIAL-PROFITABILITY-1.0";
    public const string ProfitabilityComparisonRuleSetVersion = "COMMERCIAL-PROFITABILITY-2.0";
    public const string ProfitabilityDevelopmentPilotProvenance =
        "Deterministic local Development contribution evidence — simulated, non-production, not real customer, quotation, invoice, order, production, cost, accounting, or market evidence.";
    public const string ProfitabilityComparisonDevelopmentPilotProvenance =
        "Deterministic local Development profitability comparison evidence — simulated, non-production, not real customer, quotation, invoice, order, production, cost, accounting, profit, or market evidence.";
    public const decimal DefaultProfitabilityMinimumContributionMarginRatio = 0.15m;
    public const int DefaultProfitabilityMaximumEvidenceAgeDays = 180;
    public const int DefaultProfitabilityMinimumComparableAssessmentCountPerWindow = 3;
    public const decimal DefaultProfitabilityMaximumAdverseContributionMarginDelta = 0.05m;
    public const decimal DefaultProfitabilityMinimumFavorableContributionMarginDelta = 0.05m;
    public const decimal DefaultProfitabilityMaximumCostShareIncrease = 0.10m;
    public const decimal DefaultQuotationRiskMaximumRiskIncidenceDelta = 0.10m;
    public const string QuotationRiskDevelopmentPilotProvenance =
        "Deterministic local Development quotation-risk evidence — simulated, non-production, not real customer, quotation, invoice, order, or market evidence.";
    public const int DefaultQuotationRiskExpiryWarningDays = 7;
    public const int DefaultQuotationRiskMaximumPaymentTermDays = 90;
    public const int DefaultQuotationRiskMaximumEvidenceAgeDays = 365;
    public const decimal DefaultQuotationRiskMaximumNegativeVarianceRatio = 0.10m;
    public const decimal DefaultQuotationRiskMaximumPositiveVarianceRatio = 0.15m;
    public const decimal DefaultCustomerSpreadThreshold = 0.10m;
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development commercial-intelligence data — simulated, non-production, not real customer evidence, market prices, or guaranteed savings.";
    public const string SalesDevelopmentPilotProvenance =
        "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence.";
    public const int DefaultMinimumComparableObservations = 6;
    public const decimal DefaultMaximumPositiveVarianceRatio = 0.10m;
    public const decimal DefaultSupplierSpreadThreshold = 0.10m;
    public const string ReferenceCenterMedian = "median";
}
