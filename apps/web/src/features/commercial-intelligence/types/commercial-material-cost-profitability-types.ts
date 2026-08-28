import type {
  CommercialProfitabilityComparisonSummary,
  CommercialProfitabilityKpiCard,
  CommercialProfitabilityProfileSummary,
} from "./commercial-profitability-types";
import { COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS } from "../domain/commercial-material-cost-profitability-labels";

export type CommercialMaterialCostProfitabilityLensId =
  (typeof COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS)[number]["id"];

export type CommercialMaterialProfitabilityTableRow = Readonly<{
  id: string;
  materialLabel: string;
  materialId: string | null;
  referenceMarginLabel: string;
  currentMarginLabel: string;
  marginDeltaLabel: string;
  positionStateLabel: string;
  outcomeLabel: string;
  evidenceLabel: string;
  coverageLabel: string;
  evaluatedLabel: string;
  comparisonOutcome: string;
  evidenceStatus: string;
  leadingDriverLabel: string;
}>;

export type CommercialCostDriverProfitabilityTableRow = Readonly<{
  id: string;
  comparisonReference: string;
  materialLabel: string;
  leadingDriverLabel: string;
  referenceCostShareLabel: string;
  currentCostShareLabel: string;
  costShareDeltaLabel: string;
  marginDeltaLabel: string;
  outcomeLabel: string;
  evidenceLabel: string;
  coverageLabel: string;
  evaluatedLabel: string;
  comparisonOutcome: string;
  evidenceStatus: string;
}>;

export type CommercialMaterialCostProfitabilityWorkspacePayload = Readonly<{
  facilityLabel: string;
  evidenceLabel: string;
  hasDevelopmentPilotData: boolean;
  ruleSetVersion: string;
  comparisonRuleSetVersion: string;
  provenanceDisclaimer: string;
  realProfitabilityMappingComplete: boolean;
  reportingPeriodLabel: string;
  freshnessLabel: string;
  filterScopeLabel: string;
  profiles: readonly CommercialProfitabilityProfileSummary[];
  materialComparisons: readonly CommercialProfitabilityComparisonSummary[];
  costMixComparisons: readonly CommercialProfitabilityComparisonSummary[];
  selectedMaterialComparisonId: string | null;
  selectedCostMixComparisonId: string | null;
  kpiCards: readonly CommercialProfitabilityKpiCard[];
  materialTableRows: readonly CommercialMaterialProfitabilityTableRow[];
  costMixTableRows: readonly CommercialCostDriverProfitabilityTableRow[];
  materialPage: number;
  materialPageSize: number;
  materialReturnedCount: number;
  costMixPage: number;
  costMixPageSize: number;
  costMixReturnedCount: number;
}>;
