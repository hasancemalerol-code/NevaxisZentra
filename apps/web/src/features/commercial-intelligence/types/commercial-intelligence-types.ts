export type CommercialIntelligenceTabId =
  | "overview"
  | "analyses"
  | "suppliers"
  | "profiles";

export type CommercialIntelligenceKpiTone =
  | "critical"
  | "attention"
  | "informational"
  | "controlled"
  | "insufficient"
  | "conflict"
  | "neutral";

export type CommercialIntelligenceKpiCard = Readonly<{
  id: string;
  label: string;
  value: string;
  detail: string;
  tone: CommercialIntelligenceKpiTone;
  scopeLabel: string;
  evidenceLabel: string;
  stateBadge: string;
  sourceDataset: string;
  accessibleSummary: string;
  unavailableReason?: string;
}>;

export type CommercialIntelligenceAssessmentSummary = Readonly<{
  id: string;
  assessmentReference: string;
  profileId: string;
  profileCode: string;
  materialId: string;
  materialLabel: string;
  supplierReference: string | null;
  supplierLabel: string;
  comparisonDimension: string;
  comparisonDimensionLabel: string;
  assessmentOutcome: string;
  outcomeLabel: string;
  currentUnitPrice: number;
  referenceMedianUnitPrice: number | null;
  varianceRatio: number | null;
  varianceLabel: string;
  comparableObservationCount: number;
  excludedObservationCount: number;
  currencyCode: string;
  normalizedUnit: string;
  windowStartUtc: string;
  windowEndUtc: string;
  windowLabel: string;
  leadingDriverCode: string | null;
  leadingDriverLabel: string;
  evidenceStatus: string;
  evidenceLabel: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  evaluatedLabel: string;
  isPilotData: boolean;
}>;

export type CommercialIntelligenceAssessmentDriver = Readonly<{
  id: string;
  reasonCode: string;
  reasonLabel: string;
  severity: string;
  severityLabel: string;
  actualValue: number | null;
  thresholdValue: number | null;
  unit: string | null;
  normalizedVariance: number | null;
  normalizedVarianceLabel: string;
  affectedObservationCount: number;
  supportingText: string;
  sortOrder: number;
  isLeadingDriver: boolean;
}>;

export type CommercialIntelligenceAssessmentDetail = Readonly<{
  summary: CommercialIntelligenceAssessmentSummary;
  absoluteVariance: number | null;
  priceBasisTaxInclusion: string;
  priceBasisTaxLabel: string;
  priceBasisDiscountInclusion: string;
  priceBasisDiscountLabel: string;
  priceBasisFreightInclusion: string;
  priceBasisFreightLabel: string;
  sourceProvenance: string;
  drivers: readonly CommercialIntelligenceAssessmentDriver[];
  exclusionReasons: readonly string[];
}>;

export type CommercialIntelligenceComparisonSummary = Readonly<{
  id: string;
  comparisonReference: string;
  profileId: string;
  profileCode: string;
  comparisonType: string;
  comparisonTypeLabel: string;
  materialId: string;
  materialLabel: string;
  comparisonOutcome: string;
  outcomeLabel: string;
  currentMedianUnitPrice: number | null;
  referenceMedianUnitPrice: number | null;
  signedVarianceRatio: number | null;
  signedVarianceLabel: string;
  supplierSpreadRatio: number | null;
  supplierSpreadLabel: string;
  comparableObservationCount: number;
  excludedObservationCount: number;
  currencyCode: string;
  normalizedUnit: string;
  currentWindowStartUtc: string;
  currentWindowEndUtc: string;
  currentWindowLabel: string;
  referenceWindowStartUtc: string | null;
  referenceWindowEndUtc: string | null;
  referenceWindowLabel: string;
  leadingDriverCode: string | null;
  leadingDriverLabel: string;
  evidenceStatus: string;
  evidenceLabel: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  evaluatedLabel: string;
  isPilotData: boolean;
}>;

export type CommercialIntelligenceSupplierResult = Readonly<{
  id: string;
  supplierReference: string | null;
  supplierLabel: string;
  comparableObservationCount: number;
  supplierMedianUnitPrice: number | null;
  supplierMedianLabel: string;
  lowestComparableMedianUnitPrice: number | null;
  relativePricePositionRatio: number | null;
  relativePositionLabel: string;
  priceRank: number;
  isTied: boolean;
  positionState: string;
  positionStateLabel: string;
  evidenceStatus: string;
  evidenceLabel: string;
}>;

export type CommercialIntelligenceComparisonDriver = Readonly<{
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
  affectedObservationCount: number;
  supportingText: string;
  sortOrder: number;
  isLeadingDriver: boolean;
}>;

export type CommercialIntelligenceComparisonDetail = Readonly<{
  summary: CommercialIntelligenceComparisonSummary;
  profileVersion: string;
  priceBasisTaxInclusion: string;
  priceBasisTaxLabel: string;
  priceBasisDiscountInclusion: string;
  priceBasisDiscountLabel: string;
  priceBasisFreightInclusion: string;
  priceBasisFreightLabel: string;
  sourceProvenance: string;
  supplierResults: readonly CommercialIntelligenceSupplierResult[];
  drivers: readonly CommercialIntelligenceComparisonDriver[];
  unavailableDimensions: readonly string[];
  unavailableDimensionLabels: readonly string[];
}>;

export type CommercialIntelligenceProfileSummary = Readonly<{
  id: string;
  profileCode: string;
  displayName: string;
  status: string;
  statusLabel: string;
  comparisonDimension: string;
  comparisonDimensionLabel: string;
  referenceWindowDurationDays: number;
  minimumComparableObservations: number;
  maximumPositiveVarianceRatio: number;
  maximumPositiveVarianceLabel: string;
  supplierSpreadThreshold: number | null;
  supplierSpreadThresholdLabel: string;
  allowedEvidenceStatuses: string;
  allowedEvidenceLabels: readonly string[];
  requiredTaxInclusion: string;
  requiredTaxLabel: string;
  requiredDiscountInclusion: string;
  requiredDiscountLabel: string;
  requiredFreightInclusion: string;
  requiredFreightLabel: string;
  referenceCenter: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  version: number;
  isExecutable: boolean;
  isPilotData: boolean;
  compatibilitySummary: string;
}>;

export type CommercialIntelligenceTemporalChartPoint = Readonly<{
  id: string;
  label: string;
  primaryLabel: string;
  secondaryLabel: string;
  traceReference: string;
  currentMedian: number | null;
  referenceMedian: number | null;
  signedVarianceRatio: number | null;
  signedVarianceLabel: string;
  outcome: string;
  outcomeLabel: string;
  currencyCode: string;
  normalizedUnit: string;
}>;

export type CommercialIntelligencePriorityDeviation = Readonly<{
  id: string;
  kind: "assessment" | "comparison";
  reference: string;
  primaryLabel: string;
  secondaryLabel: string;
  traceReference: string;
  materialLabel: string;
  outcomeLabel: string;
  varianceLabel: string;
  evaluatedLabel: string;
}>;

export type CommercialIntelligenceEvidenceSummary = Readonly<{
  comparableTotal: number;
  excludedTotal: number;
  attentionCount: number;
  favorableCount: number;
  insufficientCount: number;
  conflictCount: number;
  spreadFindingCount: number;
  reconciliationLabel: string;
}>;

export type CommercialIntelligenceWorkspacePayload = Readonly<{
  facilityLabel: string;
  reportingPeriodLabel: string;
  freshnessLabel: string;
  evidenceLabel: string;
  provenanceDisclaimer: string;
  ruleSetVersion: string;
  referenceCenter: string;
  defaultMinimumComparableObservations: number;
  hasDevelopmentPilotData: boolean;
  unavailableComparisonDimensions: readonly string[];
  kpis: readonly CommercialIntelligenceKpiCard[];
  temporalChart: Readonly<{
    points: readonly CommercialIntelligenceTemporalChartPoint[];
    unavailableReason: string | null;
  }>;
  supplierPosition: Readonly<{
    rows: readonly CommercialIntelligenceSupplierResult[];
    spreadLabel: string;
    comparisonId: string | null;
    comparisonReference: string | null;
    primaryLabel: string | null;
    secondaryLabel: string | null;
    outcomeLabel: string | null;
    comparableObservationCount: number;
    excludedObservationCount: number;
    currencyCode: string | null;
    normalizedUnit: string | null;
    evaluatedLabel: string | null;
    unavailableReason: string | null;
  }>;
  priorityDeviations: readonly CommercialIntelligencePriorityDeviation[];
  evidenceSummary: CommercialIntelligenceEvidenceSummary;
  assessments: readonly CommercialIntelligenceAssessmentSummary[];
  comparisons: readonly CommercialIntelligenceComparisonSummary[];
  profiles: readonly CommercialIntelligenceProfileSummary[];
}>;
