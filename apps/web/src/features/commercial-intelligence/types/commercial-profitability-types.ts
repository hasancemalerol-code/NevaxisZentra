export type CommercialProfitabilityTabId =
  | "overview"
  | "temporal"
  | "cost-mix"
  | "positions";

export type CommercialProfitabilityComparisonSummary = Readonly<{
  id: string;
  comparisonReference: string;
  profileId: string;
  profileCode: string;
  comparisonType: string;
  comparisonTypeLabel: string;
  materialId: string | null;
  materialLabel: string;
  customerReference: string | null;
  customerLabel: string;
  comparisonOutcome: string;
  outcomeLabel: string;
  evidenceStatus: string;
  evidenceLabel: string;
  aggregationMethod: string;
  candidateAssessmentCount: number;
  comparableAssessmentCount: number;
  excludedAssessmentCount: number;
  referenceMedianContributionMarginRatio: number | null;
  currentMedianContributionMarginRatio: number | null;
  referenceMarginLabel: string;
  currentMarginLabel: string;
  contributionMarginDelta: number | null;
  marginDeltaLabel: string;
  referenceWindowStartUtc: string | null;
  referenceWindowEndUtc: string | null;
  referenceWindowLabel: string;
  currentWindowStartUtc: string;
  currentWindowEndUtc: string;
  currentWindowLabel: string;
  leadingDriverCode: string | null;
  leadingDriverLabel: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  evaluatedLabel: string;
  isPilotData: boolean;
  realProfitabilityMappingComplete: boolean;
}>;

export type CommercialProfitabilityComparisonResultRow = Readonly<{
  id: string;
  resultDimension: string;
  canonicalResultReference: string;
  customerReference: string | null;
  customerLabel: string;
  materialId: string | null;
  materialLabel: string;
  componentKind: string | null;
  componentKindLabel: string;
  comparableAssessmentCount: number;
  excludedAssessmentCount: number;
  referenceMarginLabel: string;
  currentMarginLabel: string;
  marginDeltaLabel: string;
  referenceUnitContributionLabel: string;
  currentUnitContributionLabel: string;
  referenceUnitCostLabel: string;
  currentUnitCostLabel: string;
  unitCostChangeRatioLabel: string;
  referenceCostShareLabel: string;
  currentCostShareLabel: string;
  costShareDeltaLabel: string;
  positionState: string | null;
  positionStateLabel: string;
  contributionRank: number | null;
  isTied: boolean;
  evidenceStatus: string;
  evidenceLabel: string;
}>;

export type CommercialProfitabilityComparisonDriver = Readonly<{
  id: string;
  comparisonResultId: string | null;
  reasonCode: string;
  reasonLabel: string;
  severity: string;
  severityLabel: string;
  actualValue: number | null;
  referenceValue: number | null;
  thresholdValue: number | null;
  unit: string | null;
  normalizedVariance: number | null;
  normalizedVarianceLabel: string;
  affectedAssessmentCount: number;
  supportingText: string;
  sortOrder: number;
  isLeadingDriver: boolean;
}>;

export type CommercialProfitabilityComparisonDetail = Readonly<{
  summary: CommercialProfitabilityComparisonSummary;
  profileVersion: string;
  completeCoverageCount: number;
  partialCoverageCount: number;
  insufficientDataCount: number;
  sourceConflictCount: number;
  negativeContributionCount: number;
  belowThresholdCount: number;
  referenceMedianUnitContributionLabel: string;
  currentMedianUnitContributionLabel: string;
  unitContributionDeltaLabel: string;
  sourceProvenance: string;
  results: readonly CommercialProfitabilityComparisonResultRow[];
  drivers: readonly CommercialProfitabilityComparisonDriver[];
}>;

export type CommercialProfitabilityProfileSummary = Readonly<{
  id: string;
  profileCode: string;
  displayName: string;
  status: string;
  statusLabel: string;
  minimumContributionMarginRatio: number;
  minimumContributionMarginLabel: string;
  minimumComparableAssessmentCountPerWindow: number;
  maximumAdverseContributionMarginDelta: number;
  minimumFavorableContributionMarginDelta: number;
  maximumCostShareIncrease: number;
  ruleSetVersion: string;
  version: number;
  isPilotData: boolean;
}>;

export type CommercialProfitabilityKpiCard = Readonly<{
  id: string;
  label: string;
  value: string;
  hint: string;
  tone: "neutral" | "positive" | "negative" | "warning";
}>;

export type CommercialProfitabilityTemporalChartPoint = Readonly<{
  id: string;
  label: string;
  referenceMarginPercent: number | null;
  currentMarginPercent: number | null;
}>;

export type CommercialProfitabilityPositionRow = Readonly<{
  id: string;
  scopeLabel: string;
  marginLabel: string;
  unitContributionLabel: string;
  comparableCountLabel: string;
  positionStateLabel: string;
  isTied: boolean;
  rank: number | null;
}>;

export type CommercialProfitabilityWorkspacePayload = Readonly<{
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
  selectedTemporalComparisonId: string | null;
  selectedCostMixComparisonId: string | null;
  selectedPositionComparisonId: string | null;
  kpiCards: readonly CommercialProfitabilityKpiCard[];
  temporalChart: readonly CommercialProfitabilityTemporalChartPoint[];
  positionPreviewRows: readonly CommercialProfitabilityPositionRow[];
}>;
