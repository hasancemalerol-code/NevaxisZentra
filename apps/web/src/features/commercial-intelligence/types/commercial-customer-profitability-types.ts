import type {
  CommercialProfitabilityComparisonSummary,
  CommercialProfitabilityKpiCard,
  CommercialProfitabilityProfileSummary,
} from "./commercial-profitability-types";

export type CommercialCustomerProfitabilityTableRow = Readonly<{
  id: string;
  customerLabel: string;
  customerReference: string | null;
  referenceMarginLabel: string;
  currentMarginLabel: string;
  marginDeltaLabel: string;
  unitContributionLabel: string;
  positionStateLabel: string;
  outcomeLabel: string;
  evidenceLabel: string;
  coverageLabel: string;
  evaluatedLabel: string;
  comparisonOutcome: string;
  evidenceStatus: string;
}>;

export type CommercialCustomerProfitabilityWorkspacePayload = Readonly<{
  facilityLabel: string;
  evidenceLabel: string;
  hasDevelopmentPilotData: boolean;
  ruleSetVersion: string;
  comparisonRuleSetVersion: string;
  provenanceDisclaimer: string;
  realProfitabilityMappingComplete: boolean;
  reportingPeriodLabel: string;
  freshnessLabel: string;
  profiles: readonly CommercialProfitabilityProfileSummary[];
  comparisons: readonly CommercialProfitabilityComparisonSummary[];
  selectedComparisonId: string | null;
  kpiCards: readonly CommercialProfitabilityKpiCard[];
  tableRows: readonly CommercialCustomerProfitabilityTableRow[];
  page: number;
  pageSize: number;
  returnedCount: number;
}>;
