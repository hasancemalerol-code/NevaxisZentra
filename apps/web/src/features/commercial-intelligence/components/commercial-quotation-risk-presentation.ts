import type { CommercialIntelligenceKpiCard, CommercialIntelligenceTemporalChartPoint } from "../types/commercial-intelligence-types";
import type {
  CommercialQuotationRiskAssessmentSummary,
  CommercialQuotationRiskComparisonSummary,
  CommercialQuotationRiskCustomerResult,
  CommercialQuotationRiskRiskDistributionSlice,
} from "../types/commercial-quotation-risk-types";
import {
  formatCount,
  formatIncidenceDeltaPoints,
  formatIncidenceFraction,
} from "../domain/commercial-intelligence-formatters";
import {
  COMMERCIAL_QUOTATION_RISK_KPI_ORDER,
} from "../domain/commercial-quotation-risk-labels";
import {
  getCommercialIntelligenceEvidenceStatusLabel,
} from "../domain/commercial-intelligence-labels";
import {
  getCommercialQuotationRiskComparisonTypeLabel,
  getCommercialQuotationRiskOutcomeLabel,
  getCommercialQuotationRiskPositionStateLabel,
  inferQuotationRiskEvidenceStatus,
} from "../domain/commercial-quotation-risk-labels";
import { localizeCommercialIntelligenceCustomerLabel, localizeCommercialIntelligenceMaterialLabel, resolveCommercialIntelligenceKpiToneClass } from "./commercial-intelligence-presentation";

export const COMMERCIAL_QUOTATION_RISK_TEMPORAL_TYPE = "temporal-risk-trend";
export const COMMERCIAL_QUOTATION_RISK_CUSTOMER_POSITION_TYPE = "customer-risk-position";

export function filterQuotationRiskTemporalComparisons(
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
): readonly CommercialQuotationRiskComparisonSummary[] {
  return comparisons.filter((item) => item.comparisonType === COMMERCIAL_QUOTATION_RISK_TEMPORAL_TYPE);
}

export function filterQuotationRiskCustomerComparisons(
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
): readonly CommercialQuotationRiskComparisonSummary[] {
  return comparisons.filter((item) => item.comparisonType === COMMERCIAL_QUOTATION_RISK_CUSTOMER_POSITION_TYPE);
}

export function selectLatestQuotationRiskTemporalComparison(
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
): CommercialQuotationRiskComparisonSummary | null {
  const temporal = filterQuotationRiskTemporalComparisons(comparisons);
  if (temporal.length === 0) return null;
  return [...temporal].sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc))[0] ?? null;
}

export function selectLatestQuotationRiskCustomerComparison(
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
): CommercialQuotationRiskComparisonSummary | null {
  const customer = filterQuotationRiskCustomerComparisons(comparisons);
  if (customer.length === 0) return null;
  return [...customer].sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc))[0] ?? null;
}

export function buildQuotationRiskComparisonSecondaryLabel(
  comparison: CommercialQuotationRiskComparisonSummary,
): string {
  const parts = [
    comparison.comparisonReference,
    comparison.materialLabel !== "—" ? comparison.materialLabel : null,
    comparison.currentWindowLabel !== "—" ? comparison.currentWindowLabel : null,
  ].filter(Boolean);
  return parts.join(" · ");
}

export function buildQuotationRiskKpiCards(input: Readonly<{
  assessments: readonly CommercialQuotationRiskAssessmentSummary[];
  temporalComparison: CommercialQuotationRiskComparisonSummary | null;
}>): readonly CommercialIntelligenceKpiCard[] {
  const { assessments, temporalComparison } = input;
  const scopeLabel = temporalComparison
    ? "Son zamansal teklif risk karşılaştırması"
    : "Teklif risk değerlendirme kümesi";
  const evidenceLabel = "Teklif risk kanıtı";
  const sourceDataset = temporalComparison
    ? "commercial_quotation_risk_comparison_runs"
    : "commercial_quotation_risk_assessments";

  const candidateTotal = assessments.reduce((sum, item) => sum + item.candidateObservationCount, 0);
  const attentionCount = assessments.filter((item) => item.outcome === "attention").length;
  const withinCount = assessments.filter((item) => item.outcome === "within-threshold").length;
  const insufficientOrConflict = assessments.filter(
    (item) => item.outcome === "insufficient-data" || item.outcome === "source-conflict",
  ).length;
  const currentIncidence = temporalComparison?.currentRiskIncidence ?? null;

  const cards: Record<string, CommercialIntelligenceKpiCard> = {
    "candidate-assessments": {
      id: "candidate-assessments",
      label: "İncelenen teklif",
      value: formatCount(assessments.length),
      detail: `Aday gözlem toplamı: ${formatCount(candidateTotal)}`,
      tone: "neutral",
      scopeLabel,
      evidenceLabel,
      stateBadge: assessments.length > 0 ? "Aktif" : "Eksik",
      sourceDataset,
      accessibleSummary: `${scopeLabel}: ${formatCount(assessments.length)} değerlendirme`,
    },
    "attention-assessments": {
      id: "attention-assessments",
      label: "Dikkat gerektiren",
      value: formatCount(attentionCount),
      detail: `Kapsam: ${formatCount(assessments.length)} değerlendirme`,
      tone: attentionCount > 0 ? "attention" : "neutral",
      scopeLabel,
      evidenceLabel,
      stateBadge: attentionCount > 0 ? "Dikkat" : "Normal",
      sourceDataset,
      accessibleSummary: `${attentionCount} dikkat gerektiren teklif`,
    },
    "within-threshold-assessments": {
      id: "within-threshold-assessments",
      label: "Eşik içinde kalan",
      value: formatCount(withinCount),
      detail: `Kapsam: ${formatCount(assessments.length)} değerlendirme`,
      tone: withinCount > 0 ? "controlled" : "neutral",
      scopeLabel,
      evidenceLabel,
      stateBadge: "Eşik içinde",
      sourceDataset,
      accessibleSummary: `${withinCount} eşik içinde teklif`,
    },
    "insufficient-or-conflict": {
      id: "insufficient-or-conflict",
      label: "Yetersiz / çakışma",
      value: formatCount(insufficientOrConflict),
      detail: "Değerlendirme sonucu bazında",
      tone: insufficientOrConflict > 0 ? "conflict" : "neutral",
      scopeLabel,
      evidenceLabel,
      stateBadge: insufficientOrConflict > 0 ? "Uyarı" : "Normal",
      sourceDataset,
      accessibleSummary: `${insufficientOrConflict} yetersiz veya çakışan değerlendirme`,
    },
    "current-risk-incidence": {
      id: "current-risk-incidence",
      label: "Güncel risk sıklığı",
      value: formatIncidenceFraction(currentIncidence),
      detail: temporalComparison
        ? `Güncel pencere: ${formatCount(temporalComparison.comparableAssessmentCount)} karşılaştırılabilir`
        : "Zamansal karşılaştırma yok",
      tone: currentIncidence === null ? "insufficient" : "informational",
      scopeLabel,
      evidenceLabel,
      stateBadge: currentIncidence === null ? "Hesaplanamadı" : "Gözlenen",
      sourceDataset,
      accessibleSummary: currentIncidence === null
        ? "Risk sıklığı hesaplanamadı"
        : `Güncel risk sıklığı ${formatIncidenceFraction(currentIncidence)}`,
      unavailableReason: currentIncidence === null ? "Hesaplanamadı" : undefined,
    },
  };

  return COMMERCIAL_QUOTATION_RISK_KPI_ORDER.map((id) => cards[id]);
}

export function buildQuotationRiskDistribution(
  assessments: readonly CommercialQuotationRiskAssessmentSummary[],
): readonly CommercialQuotationRiskRiskDistributionSlice[] {
  let priceRisk = 0;
  let expiryRisk = 0;
  let paymentRisk = 0;
  let within = 0;
  let insufficient = 0;
  let conflict = 0;

  for (const item of assessments) {
    const driver = item.leadingDriverCode ?? "";
    if (item.outcome === "source-conflict") {
      conflict += 1;
    } else if (item.outcome === "insufficient-data") {
      insufficient += 1;
    } else if (driver.includes("price") || driver.includes("quoted")) {
      priceRisk += 1;
    } else if (driver.includes("expir")) {
      expiryRisk += 1;
    } else if (driver.includes("payment")) {
      paymentRisk += 1;
    } else if (item.outcome === "within-threshold") {
      within += 1;
    }
  }

  return [
    { key: "price-risk", label: "Fiyat riski", count: priceRisk },
    { key: "expiry-risk", label: "Geçerlilik riski", count: expiryRisk },
    { key: "payment-risk", label: "Ödeme vadesi riski", count: paymentRisk },
    { key: "within-threshold", label: "Eşik içinde", count: within },
    { key: "insufficient-data", label: "Yetersiz veri", count: insufficient },
    { key: "source-conflict", label: "Kaynak çakışması", count: conflict },
  ];
}

export function buildQuotationRiskTemporalChartData(
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
): Readonly<{
  points: readonly CommercialIntelligenceTemporalChartPoint[];
  unavailableReason: string | null;
}> {
  const temporal = filterQuotationRiskTemporalComparisons(comparisons).slice(0, 6);
  if (temporal.length === 0) {
    return {
      points: [],
      unavailableReason: "Karşılaştırılabilir zamansal risk eğilimi çalışması yok",
    };
  }

  const points = temporal.map((item) => ({
    id: item.id,
    label: buildQuotationRiskComparisonSecondaryLabel(item),
    primaryLabel: getCommercialQuotationRiskComparisonTypeLabel(item.comparisonType),
    secondaryLabel: buildQuotationRiskComparisonSecondaryLabel(item),
    traceReference: item.comparisonReference,
    currentMedian: item.currentRiskIncidence,
    referenceMedian: item.referenceRiskIncidence,
    signedVarianceRatio: item.riskIncidenceDelta,
    signedVarianceLabel: formatIncidenceDeltaPoints(item.riskIncidenceDelta),
    outcome: item.comparisonOutcome,
    outcomeLabel: item.outcomeLabel,
    currencyCode: "",
    normalizedUnit: "",
  }));

  const hasValidDenominators = temporal.some(
    (item) => item.referenceRiskIncidence !== null && item.currentRiskIncidence !== null,
  );
  if (!hasValidDenominators) {
    return {
      points: [],
      unavailableReason: "Karşılaştırma için yeterli veri yok",
    };
  }

  return { points, unavailableReason: null };
}

export function mapQuotationRiskCustomerResult(input: Readonly<{
  id: string;
  customerReference: string;
  candidateAssessmentCount: number;
  comparableAssessmentCount: number;
  excludedAssessmentCount: number;
  riskBearingAssessmentCount: number;
  riskIncidenceFraction: number | null;
  priceRiskCount: number;
  expiryRiskCount: number;
  paymentTermRiskCount: number;
  positionState: string;
  riskRank: number | null;
  isTied: boolean;
  evidenceStatus: string;
}>): CommercialQuotationRiskCustomerResult {
  return {
    id: input.id,
    customerReference: input.customerReference,
    customerLabel: localizeCommercialIntelligenceCustomerLabel(input.customerReference),
    candidateAssessmentCount: input.candidateAssessmentCount,
    comparableAssessmentCount: input.comparableAssessmentCount,
    excludedAssessmentCount: input.excludedAssessmentCount,
    riskBearingAssessmentCount: input.riskBearingAssessmentCount,
    riskIncidenceFraction: input.riskIncidenceFraction,
    riskIncidenceLabel: formatIncidenceFraction(input.riskIncidenceFraction),
    priceRiskCount: input.priceRiskCount,
    expiryRiskCount: input.expiryRiskCount,
    paymentTermRiskCount: input.paymentTermRiskCount,
    positionState: input.positionState,
    positionStateLabel: getCommercialQuotationRiskPositionStateLabel(input.positionState),
    riskRank: input.riskRank,
    isTied: input.isTied,
    tieLabel: input.isTied ? "Eşit risk konumu" : null,
    evidenceStatus: input.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(input.evidenceStatus),
  };
}

export function buildQuotationRiskCustomerPositionRows(
  customerResults: readonly CommercialQuotationRiskCustomerResult[],
): readonly CommercialQuotationRiskCustomerResult[] {
  return [...customerResults].sort((left, right) => {
    const leftRank = left.riskRank ?? Number.MAX_SAFE_INTEGER;
    const rightRank = right.riskRank ?? Number.MAX_SAFE_INTEGER;
    if (leftRank !== rightRank) return leftRank - rightRank;
    return left.customerReference.localeCompare(right.customerReference, "tr");
  });
}

export function resolveQuotationRiskKpiToneClass(tone: CommercialIntelligenceKpiCard["tone"]): string {
  return resolveCommercialIntelligenceKpiToneClass(tone);
}

export function mapAssessmentEvidenceFromOutcome(outcome: string): string {
  return inferQuotationRiskEvidenceStatus(outcome);
}

export function localizeQuotationRiskMaterial(materialId: string | null | undefined): string {
  return localizeCommercialIntelligenceMaterialLabel(materialId ?? "");
}

export function buildQuotationRiskAssessmentPrimaryLabel(
  assessment: CommercialQuotationRiskAssessmentSummary,
): string {
  return `${assessment.quotationReference} · ${assessment.customerReference}`;
}

export function buildQuotationRiskAssessmentSecondaryLabel(
  assessment: CommercialQuotationRiskAssessmentSummary,
): string {
  return `${assessment.materialLabel} · ${assessment.outcomeLabel} · ${assessment.varianceLabel}`;
}

export function getQuotationRiskComparisonOutcomeLabel(value: string | null | undefined): string {
  return getCommercialQuotationRiskOutcomeLabel(value);
}
