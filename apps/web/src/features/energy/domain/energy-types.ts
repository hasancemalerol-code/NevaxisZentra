export type EnergyCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type EnergyWorkspaceMeta = Readonly<{
  activeMeasurementPointCount: number;
  recentWindowCount: number;
  baselineCount: number;
  activeTargetCount: number;
  recentAssessmentCount: number;
  ruleSetVersion: string;
  intelligenceRuleSetVersion: string;
  provenanceDisclaimer: string;
  intelligenceProvenanceDisclaimer: string;
  hasDevelopmentPilotData: boolean;
  capabilities: EnergyCapabilities;
  facilityLabel: string;
  reportingPeriodStartUtc: string | null;
  reportingPeriodEndUtc: string | null;
  latestFreshnessUtc: string | null;
  evidenceLabel: string;
}>;

export type EnergyKpiCard = Readonly<{
  id:
    | "total-consumption"
    | "peak-power"
    | "energy-intensity"
    | "target-variance"
    | "idle-consumption"
    | "estimated-cost";
  label: string;
  value: string;
  unit: string | null;
  meta: string;
  evidenceLabel: string;
  state: "available" | "unavailable" | "insufficient-data";
  unavailableReason?: string;
}>;

export type EnergyTrendPoint = Readonly<{
  label: string;
  consumedKwh: number | null;
  averagePowerKw: number | null;
  maximumDemandKw: number | null;
  targetKwh: number | null;
  baselineKwh: number | null;
  windowStartUtc: string;
  windowEndUtc: string;
}>;

export type EnergyAllocationSlice = Readonly<{
  id: string;
  label: string;
  kwh: number;
  percent: number;
  tone: "machine" | "auxiliary" | "residual";
}>;

export type EnergyIntelligenceRow = Readonly<{
  id: string;
  assessmentReference: string;
  outcome: string;
  outcomeLabel: string;
  scopeLabel: string;
  evaluationPeriodLabel: string;
  leadingDriverLabel: string;
  comparisonLabel: string | null;
  freshnessLabel: string;
  evaluatedAtUtc: string;
}>;

export type EnergyOpportunitySummary = Readonly<{
  reducibleKwhLabel: string | null;
  costImpactLabel: string | null;
  hasApprovedTarget: boolean;
  disclaimer: string;
}>;

export type EnergyMeasurementPointSummary = Readonly<{
  id: string;
  pointCode: string;
  displayName: string;
  measurementScope: string;
  measurementScopeLabel: string;
  equipmentCategory: string;
  equipmentCategoryLabel: string;
  machineAssetLabel: string | null;
  status: string;
  statusLabel: string;
  evidenceLabel: string;
  freshnessLabel: string;
  latestReadingLabel: string | null;
  isPilotData: boolean;
}>;

export type EnergyMeasurementPointDetail = Readonly<{
  point: EnergyMeasurementPointSummary;
  sourceProvenance: string;
  measurementUnit: string;
  ruleSetVersion: string;
  currentWindowLabel: string | null;
  powerLabel: string | null;
  demandLabel: string | null;
  allocationLabels: readonly string[];
  dataQualityLabel: string;
  calculationMethodLabel: string | null;
}>;

export type EnergyBaselineSummary = Readonly<{
  id: string;
  baselineReference: string;
  comparisonDimension: string;
  comparisonDimensionLabel: string;
  measurementPointCode: string | null;
  sampleCount: number;
  medianCenter: number | null;
  madDispersion: number | null;
  relativeTolerance: number | null;
  upperThresholdConsumedKwh: number | null;
  evidenceLabel: string;
  provenance: string;
  ruleSetVersion: string;
  isPilotData: boolean;
  isCalculated: true;
}>;

export type EnergyTargetSummary = Readonly<{
  id: string;
  targetReference: string;
  targetScope: string;
  targetScopeLabel: string;
  targetMetric: string;
  targetMetricLabel: string;
  targetValue: number;
  unit: string;
  tolerance: number;
  effectiveFromUtc: string;
  effectiveToUtc: string;
  sourceProvenance: string;
  status: string;
  statusLabel: string;
  version: number;
  isPilotData: boolean;
  isApprovedTarget: boolean;
}>;

export type EnergyAssessmentSummary = Readonly<{
  id: string;
  assessmentReference: string;
  assessmentTitle: string;
  comparisonDimension: string;
  comparisonDimensionLabel: string;
  measurementPointCode: string | null;
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  evaluationPeriodLabel: string;
  actualConsumedKwh: number | null;
  actualMaximumDemandKw: number | null;
  varianceConsumedKwh: number | null;
  variancePercent: number | null;
  outcome: string;
  outcomeLabel: string;
  leadingDriverCode: string;
  leadingDriverLabel: string;
  evidenceLabel: string;
  evaluatedAtUtc: string;
  isPilotData: boolean;
}>;

export type EnergyAssessmentDriver = Readonly<{
  id: string;
  reasonCode: string;
  labelTr: string;
  severity: string;
  severityLabel: string;
  category: string;
  categoryLabel: string;
  actualValue: number | null;
  actualUnit: string | null;
  comparisonValue: number | null;
  comparisonUnit: string | null;
  normalizedVariance: number | null;
  explanation: string | null;
  evidenceSource: string | null;
  freshnessState: string;
  freshnessLabel: string;
  isLeadingDriver: boolean;
}>;

export type EnergyAssessmentDetail = Readonly<{
  summary: EnergyAssessmentSummary;
  baselineReference: string | null;
  targetReference: string | null;
  baselineLabel: string | null;
  targetLabel: string | null;
  varianceLabel: string | null;
  drivers: readonly EnergyAssessmentDriver[];
  ruleSetVersion: string;
  provenanceLabel: string | null;
}>;

export type EnergyWorkspacePayload = Readonly<{
  meta: EnergyWorkspaceMeta;
  kpis: readonly EnergyKpiCard[];
  trend: readonly EnergyTrendPoint[];
  trendScope: string;
  trendInsufficientData: boolean;
  trendBucketDurationMs: number | null;
  trendReportingTotalKwh: number;
  trendTargetComparisonNote: string | null;
  trendBaselineComparisonNote: string | null;
  trendTargetCompatible: boolean;
  trendBaselineCompatible: boolean;
  allocations: readonly EnergyAllocationSlice[];
  allocationTotalKwh: number;
  windowConsumptionKwh: number;
  reportingTotalKwh: number;
  allocatedConsumptionKwh: number;
  residualConsumptionKwh: number;
  allocationCoveragePercent: number;
  intelligenceRows: readonly EnergyIntelligenceRow[];
  opportunity: EnergyOpportunitySummary | null;
  measurementPoints: readonly EnergyMeasurementPointSummary[];
  baselines: readonly EnergyBaselineSummary[];
  targets: readonly EnergyTargetSummary[];
  assessments: readonly EnergyAssessmentSummary[];
  partialErrors: readonly string[];
}>;

export type EnergyWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;
