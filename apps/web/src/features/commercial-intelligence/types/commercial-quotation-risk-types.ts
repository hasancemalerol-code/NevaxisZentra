import type { CommercialIntelligenceKpiCard, CommercialIntelligenceTemporalChartPoint } from "./commercial-intelligence-types";

export type CommercialQuotationRiskTabId =
  | "overview"
  | "assessments"
  | "trends"
  | "customers"
  | "profile";

export type CommercialQuotationRiskAssessmentSummary = Readonly<{
  id: string;
  assessmentReference: string;
  profileId: string;
  profileCode: string;
  evidenceObservationId: string;
  quotationReference: string;
  customerReference: string;
  customerLabel: string;
  materialId: string;
  materialLabel: string;
  outcome: string;
  outcomeLabel: string;
  quotedComparableUnitPrice: number;
  quotedPriceLabel: string;
  referenceMedianUnitPrice: number | null;
  referencePriceLabel: string;
  signedPriceVarianceRatio: number | null;
  varianceLabel: string;
  candidateObservationCount: number;
  comparableObservationCount: number;
  excludedObservationCount: number;
  paymentTermDays: number;
  remainingValidityDays: number | null;
  remainingValidityLabel: string;
  currencyCode: string;
  unitOfMeasure: string;
  referenceWindowStartUtc: string;
  referenceWindowEndUtc: string;
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

export type CommercialQuotationRiskAssessmentDriver = Readonly<{
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

export type CommercialQuotationRiskAssessmentDetail = Readonly<{
  summary: CommercialQuotationRiskAssessmentSummary;
  profileVersion: string;
  grossNetBasis: string;
  grossNetBasisLabel: string;
  taxBasis: string;
  taxBasisLabel: string;
  discountBasis: string;
  discountBasisLabel: string;
  freightBasis: string;
  freightBasisLabel: string;
  sourceProvenance: string;
  drivers: readonly CommercialQuotationRiskAssessmentDriver[];
}>;

export type CommercialQuotationRiskComparisonSummary = Readonly<{
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
  candidateAssessmentCount: number;
  comparableAssessmentCount: number;
  excludedAssessmentCount: number;
  referenceRiskIncidence: number | null;
  referenceIncidenceLabel: string;
  currentRiskIncidence: number | null;
  currentIncidenceLabel: string;
  riskIncidenceDelta: number | null;
  incidenceDeltaLabel: string;
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
  realQuotationMappingComplete: boolean;
}>;

export type CommercialQuotationRiskComparisonDriver = Readonly<{
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

export type CommercialQuotationRiskCustomerResult = Readonly<{
  id: string;
  customerReference: string;
  customerLabel: string;
  candidateAssessmentCount: number;
  comparableAssessmentCount: number;
  excludedAssessmentCount: number;
  riskBearingAssessmentCount: number;
  riskIncidenceFraction: number | null;
  riskIncidenceLabel: string;
  priceRiskCount: number;
  expiryRiskCount: number;
  paymentTermRiskCount: number;
  positionState: string;
  positionStateLabel: string;
  riskRank: number | null;
  isTied: boolean;
  tieLabel: string | null;
  evidenceStatus: string;
  evidenceLabel: string;
}>;

export type CommercialQuotationRiskComparisonDetail = Readonly<{
  summary: CommercialQuotationRiskComparisonSummary;
  profileVersion: string;
  riskBearingAssessmentCount: number;
  insufficientDataCount: number;
  sourceConflictCount: number;
  priceRiskCount: number;
  expiryRiskCount: number;
  paymentTermRiskCount: number;
  referenceCandidateAssessmentCount: number | null;
  referenceComparableAssessmentCount: number | null;
  referenceExcludedAssessmentCount: number | null;
  referenceRiskBearingAssessmentCount: number | null;
  currentCandidateAssessmentCount: number | null;
  currentComparableAssessmentCount: number | null;
  currentExcludedAssessmentCount: number | null;
  currentRiskBearingAssessmentCount: number | null;
  sourceProvenance: string;
  customerResults: readonly CommercialQuotationRiskCustomerResult[];
  drivers: readonly CommercialQuotationRiskComparisonDriver[];
}>;

export type CommercialQuotationRiskProfileSummary = Readonly<{
  id: string;
  profileCode: string;
  displayName: string;
  status: string;
  statusLabel: string;
  referenceWindowDurationDays: number;
  minimumComparableObservations: number;
  maximumNegativePriceVarianceRatio: number;
  maximumNegativeVarianceLabel: string;
  maximumPositivePriceVarianceRatio: number;
  maximumPositiveVarianceLabel: string;
  maximumPaymentTermDays: number;
  expiryWarningDays: number;
  maximumEvidenceAgeDays: number;
  maximumRiskIncidenceDelta: number;
  maximumRiskIncidenceDeltaLabel: string;
  allowedEvidenceStatuses: string;
  allowedEvidenceLabels: readonly string[];
  allowedSalesEvidenceKinds: string;
  allowedSourceEvidenceLabels: readonly string[];
  requiredTaxInclusion: string;
  requiredTaxLabel: string;
  requiredDiscountInclusion: string;
  requiredDiscountLabel: string;
  requiredFreightInclusion: string;
  requiredFreightLabel: string;
  requiredGrossNetBasis: string;
  requiredGrossNetLabel: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  version: number;
  isExecutable: boolean;
  isPilotData: boolean;
  compatibilitySummary: string;
}>;

export type CommercialQuotationRiskRiskDistributionSlice = Readonly<{
  key: string;
  label: string;
  count: number;
}>;

export type CommercialQuotationRiskWorkspacePayload = Readonly<{
  facilityLabel: string;
  reportingPeriodLabel: string;
  freshnessLabel: string;
  evidenceLabel: string;
  provenanceDisclaimer: string;
  ruleSetVersion: string;
  comparisonRuleSetVersion: string;
  defaultMinimumComparableObservations: number;
  hasDevelopmentPilotData: boolean;
  realQuotationMappingComplete: boolean;
  customerSegmentMappingComplete: boolean;
  selectedTemporalComparisonId: string | null;
  selectedCustomerComparisonId: string | null;
  kpis: readonly CommercialIntelligenceKpiCard[];
  riskDistribution: readonly CommercialQuotationRiskRiskDistributionSlice[];
  temporalChart: Readonly<{
    points: readonly CommercialIntelligenceTemporalChartPoint[];
    unavailableReason: string | null;
  }>;
  temporalTrend: Readonly<{
    comparisonId: string | null;
    comparisonReference: string | null;
    referenceWindowLabel: string;
    currentWindowLabel: string;
    referenceIncidenceLabel: string;
    currentIncidenceLabel: string;
    incidenceDeltaLabel: string;
    outcomeLabel: string | null;
    leadingDriverLabel: string | null;
    referenceComparableCount: number | null;
    referenceExcludedCount: number | null;
    currentComparableCount: number | null;
    currentExcludedCount: number | null;
    priceRiskCount: number | null;
    expiryRiskCount: number | null;
    paymentTermRiskCount: number | null;
    unavailableReason: string | null;
    evidenceLabel: string | null;
  }>;
  customerComparison: Readonly<{
    rows: readonly CommercialQuotationRiskCustomerResult[];
    comparisonId: string | null;
    comparisonReference: string | null;
    outcomeLabel: string | null;
    leadingDriverLabel: string | null;
    unavailableReason: string | null;
  }>;
  assessments: readonly CommercialQuotationRiskAssessmentSummary[];
  comparisons: readonly CommercialQuotationRiskComparisonSummary[];
  profiles: readonly CommercialQuotationRiskProfileSummary[];
}>;
