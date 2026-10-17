import {
  COMMERCIAL_INTELLIGENCE_KPI_ORDER,
  COMMERCIAL_SALES_PRICE_KPI_ORDER,
  COMMERCIAL_SUPPLIER_COMPARISON_KPI_ORDER,
  getCommercialIntelligenceComparisonTypeLabel,
  getCommercialIntelligenceEvidenceStatusLabel,
  getCommercialIntelligenceOutcomeLabel,
  getCommercialIntelligencePositionStateLabel,
} from "../domain/commercial-intelligence-labels";
import {
  formatCount,
  formatCurrency,
  formatRatioAsPercent,
} from "../domain/commercial-intelligence-formatters";
import type {
  CommercialIntelligenceAssessmentSummary,
  CommercialIntelligenceComparisonSummary,
  CommercialIntelligenceEvidenceSummary,
  CommercialIntelligenceKpiCard,
  CommercialIntelligenceKpiTone,
  CommercialIntelligencePriorityDeviation,
  CommercialIntelligenceSupplierResult,
  CommercialIntelligenceTemporalChartPoint,
  CommercialSalesPriceAssessmentSummary,
  CommercialSalesPriceComparisonSummary,
  CommercialSalesPriceCustomerResult,
} from "../types/commercial-intelligence-types";

const PILOT_MATERIAL_LABELS: Record<string, string> = {
  "bb000001-00a2-4000-8000-0000000000a2": "PILOT-PP-001 · PP-H110",
  "bb000001-00a3-4000-8000-0000000000a3": "PILOT-PP-002 · Sınır malzemesi",
  "bb000001-00b4-4000-8000-0000000000b4": "PILOT-SP-001 · Satış malzemesi",
};

export const COMMERCIAL_INTELLIGENCE_PILOT_MATERIAL_ID = "bb000001-00a2-4000-8000-0000000000a2";
export const COMMERCIAL_SALES_PRICE_PILOT_MATERIAL_ID = "bb000001-00b4-4000-8000-0000000000b4";

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const TECHNICAL_RUN_REFERENCE_PATTERN = /^CI-(CMP|ASSESS)-PILOT-/i;

export const COMMERCIAL_INTELLIGENCE_SUPPLIER_COMPARISON_TYPE = "supplier-price-position";
export const COMMERCIAL_INTELLIGENCE_TEMPORAL_COMPARISON_TYPE = "temporal-price-deviation";
export const COMMERCIAL_INTELLIGENCE_SUPPLIER_SPREAD_DRIVER = "supplier-price-spread";
export const COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE = "temporal-sales-price-deviation";
export const COMMERCIAL_SALES_PRICE_CUSTOMER_POSITION_TYPE = "customer-price-position";
export const COMMERCIAL_SALES_PRICE_CUSTOMER_SPREAD_DRIVER = "customer-price-spread";

export function localizeCommercialIntelligenceMaterialLabel(materialId: string): string {
  const normalized = materialId.trim().toLowerCase();
  return PILOT_MATERIAL_LABELS[normalized] ?? "Malzeme kaydı";
}

export function localizeCommercialIntelligenceSupplierLabel(
  supplierReference: string | null | undefined,
): string {
  if (!supplierReference?.trim()) return "Tedarikçi belirtilmedi";
  return supplierReference.trim();
}

export function localizeCommercialIntelligenceCustomerLabel(
  customerReference: string | null | undefined,
): string {
  if (!customerReference?.trim()) return "Müşteri belirtilmedi";
  return customerReference.trim();
}

export function isTechnicalRunReference(value: string | null | undefined): boolean {
  if (!value?.trim()) return false;
  return TECHNICAL_RUN_REFERENCE_PATTERN.test(value.trim());
}

export function buildCommercialIntelligenceComparisonPrimaryLabel(
  comparison: Pick<CommercialIntelligenceComparisonSummary, "comparisonType">,
): string {
  return getCommercialIntelligenceComparisonTypeLabel(comparison.comparisonType);
}

export function buildCommercialIntelligenceComparisonSecondaryLabel(
  comparison: Pick<
    CommercialIntelligenceComparisonSummary,
    | "comparisonType"
    | "materialLabel"
    | "currentWindowLabel"
    | "referenceWindowLabel"
    | "comparableObservationCount"
  >,
): string {
  if (comparison.comparisonType === COMMERCIAL_INTELLIGENCE_TEMPORAL_COMPARISON_TYPE) {
    return `${comparison.materialLabel} · ${comparison.referenceWindowLabel} / ${comparison.currentWindowLabel}`;
  }
  const supplierCount = Math.max(0, Math.floor(comparison.comparableObservationCount / 6));
  const supplierLabel = supplierCount > 0
    ? `${supplierCount} karşılaştırılabilir tedarikçi`
    : "Tedarikçi penceresi";
  return `${comparison.materialLabel} · ${supplierLabel} · ${comparison.currentWindowLabel}`;
}

export function buildCommercialIntelligenceAssessmentPrimaryLabel(
  assessment: Pick<CommercialIntelligenceAssessmentSummary, "materialLabel" | "comparisonDimensionLabel">,
): string {
  return `${assessment.comparisonDimensionLabel} fiyat analizi`;
}

export function buildCommercialIntelligenceAssessmentSecondaryLabel(
  assessment: Pick<
    CommercialIntelligenceAssessmentSummary,
    "materialLabel" | "supplierLabel" | "windowLabel" | "evaluatedLabel"
  >,
): string {
  return `${assessment.materialLabel} · ${assessment.supplierLabel} · ${assessment.windowLabel}`;
}

export function selectLatestTemporalComparison(
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): CommercialIntelligenceComparisonSummary | null {
  const temporal = comparisons
    .filter((item) => item.comparisonType === COMMERCIAL_INTELLIGENCE_TEMPORAL_COMPARISON_TYPE)
    .slice()
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc));
  return temporal[0] ?? null;
}

export function selectLatestCompatibleSupplierComparison(
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): CommercialIntelligenceComparisonSummary | null {
  const supplierRuns = comparisons
    .filter((item) => item.comparisonType === COMMERCIAL_INTELLIGENCE_SUPPLIER_COMPARISON_TYPE)
    .slice()
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc));

  const compatible = supplierRuns.filter(
    (item) => item.comparableObservationCount > 0
      && item.comparisonOutcome !== "source-conflict"
      && item.comparisonOutcome !== "insufficient-data",
  );

  return compatible[0] ?? supplierRuns[0] ?? null;
}

export function selectLatestAssessment(
  assessments: readonly CommercialIntelligenceAssessmentSummary[],
): CommercialIntelligenceAssessmentSummary | null {
  const ordered = assessments
    .slice()
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc));
  return ordered[0] ?? null;
}

export function resolveCommercialIntelligenceEvidenceContext(
  assessments: readonly CommercialIntelligenceAssessmentSummary[],
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): Readonly<{
  comparableTotal: number;
  excludedTotal: number;
  scopeLabel: string;
  sourceDataset: string;
  accessibleSummary: string;
}> {
  const latestTemporal = selectLatestTemporalComparison(comparisons);
  if (latestTemporal) {
    return {
      comparableTotal: latestTemporal.comparableObservationCount,
      excludedTotal: latestTemporal.excludedObservationCount,
      scopeLabel: "Son dönemsel karşılaştırma",
      sourceDataset: "commercial_purchase_price_comparison_runs",
      accessibleSummary: `${buildCommercialIntelligenceComparisonPrimaryLabel(latestTemporal)}: ${latestTemporal.comparableObservationCount} karşılaştırılabilir, ${latestTemporal.excludedObservationCount} dışlanan gözlem`,
    };
  }

  const latestAssessment = selectLatestAssessment(assessments);
  if (latestAssessment) {
    return {
      comparableTotal: latestAssessment.comparableObservationCount,
      excludedTotal: latestAssessment.excludedObservationCount,
      scopeLabel: "Son fiyat analizi",
      sourceDataset: "commercial_purchase_price_assessments",
      accessibleSummary: `${latestAssessment.materialLabel}: ${latestAssessment.comparableObservationCount} karşılaştırılabilir, ${latestAssessment.excludedObservationCount} dışlanan gözlem`,
    };
  }

  return {
    comparableTotal: 0,
    excludedTotal: 0,
    scopeLabel: "Kanıt bağlamı yok",
    sourceDataset: "—",
    accessibleSummary: "Karşılaştırılabilir gözlem bağlamı bulunamadı",
  };
}

export function isSupplierSpreadThresholdBreach(
  item: Readonly<{
    comparisonType?: string;
    leadingDriverCode: string | null;
    supplierSpreadRatio: number | null;
    comparisonOutcome?: string;
    assessmentOutcome?: string;
  }>,
  supplierSpreadThreshold: number | null,
): boolean {
  if (item.leadingDriverCode === COMMERCIAL_INTELLIGENCE_SUPPLIER_SPREAD_DRIVER) {
    return true;
  }
  if (
    item.comparisonType === COMMERCIAL_INTELLIGENCE_SUPPLIER_COMPARISON_TYPE
    && supplierSpreadThreshold !== null
    && item.supplierSpreadRatio !== null
    && item.supplierSpreadRatio > supplierSpreadThreshold
    && item.comparisonOutcome === "attention"
  ) {
    return true;
  }
  return false;
}

export function countSupplierSpreadFindings(
  assessments: readonly CommercialIntelligenceAssessmentSummary[],
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
  supplierSpreadThreshold: number | null,
): number {
  const assessmentBreaches = assessments.filter(
    (item) => item.leadingDriverCode === COMMERCIAL_INTELLIGENCE_SUPPLIER_SPREAD_DRIVER,
  ).length;
  const comparisonBreaches = comparisons.filter(
    (item) => isSupplierSpreadThresholdBreach(item, supplierSpreadThreshold),
  ).length;
  return assessmentBreaches + comparisonBreaches;
}

export function buildCommercialIntelligenceKpiCards(input: Readonly<{
  assessments: readonly CommercialIntelligenceAssessmentSummary[];
  comparisons: readonly CommercialIntelligenceComparisonSummary[];
  supplierSpreadThreshold: number | null;
}>): CommercialIntelligenceKpiCard[] {
  const { assessments, comparisons, supplierSpreadThreshold } = input;
  const evidenceContext = resolveCommercialIntelligenceEvidenceContext(assessments, comparisons);
  const recordScopeLabel = "Kayıtlı analizler";
  const evidenceLabel = "Satın alma fiyat kanıtı";

  const attentionCount = [
    ...assessments.filter((item) => item.assessmentOutcome === "attention"),
    ...comparisons.filter((item) => item.comparisonOutcome === "attention"),
  ].length;
  const favorableCount = [
    ...assessments.filter((item) => item.assessmentOutcome === "favorable-variance"),
    ...comparisons.filter((item) => item.comparisonOutcome === "favorable-variance"),
  ].length;
  const insufficientConflictCount = [
    ...assessments.filter(
      (item) => item.assessmentOutcome === "insufficient-data" || item.assessmentOutcome === "source-conflict",
    ),
    ...comparisons.filter(
      (item) => item.comparisonOutcome === "insufficient-data" || item.comparisonOutcome === "source-conflict",
    ),
  ].length;
  const spreadFindingCount = countSupplierSpreadFindings(assessments, comparisons, supplierSpreadThreshold);

  const cards: Record<string, CommercialIntelligenceKpiCard> = {
    "comparable-observations": {
      id: "comparable-observations",
      label: "Karşılaştırılabilir",
      value: formatCount(evidenceContext.comparableTotal),
      detail: "Seçili kanıt bağlamındaki dahil edilen gözlemler",
      tone: evidenceContext.comparableTotal > 0 ? "controlled" : "insufficient",
      scopeLabel: evidenceContext.scopeLabel,
      evidenceLabel,
      stateBadge: evidenceContext.comparableTotal > 0 ? "Aktif" : "Eksik",
      sourceDataset: evidenceContext.sourceDataset,
      accessibleSummary: evidenceContext.accessibleSummary,
    },
    "excluded-observations": {
      id: "excluded-observations",
      label: "Dışlanan gözlemler",
      value: formatCount(evidenceContext.excludedTotal),
      detail: "Aynı kanıt bağlamında dışlanan gözlemler",
      tone: evidenceContext.excludedTotal > 0 ? "attention" : "neutral",
      scopeLabel: evidenceContext.scopeLabel,
      evidenceLabel,
      stateBadge: evidenceContext.excludedTotal > 0 ? "Dışlanan" : "Yok",
      sourceDataset: evidenceContext.sourceDataset,
      accessibleSummary: `${evidenceContext.scopeLabel}: ${evidenceContext.excludedTotal} dışlanan gözlem`,
    },
    "attention-analyses": {
      id: "attention-analyses",
      label: "Dikkat gerektiren analizler",
      value: formatCount(attentionCount),
      detail: "Dikkat sonucu veren kayıtlı analiz ve karşılaştırmalar",
      tone: attentionCount > 0 ? "attention" : "controlled",
      scopeLabel: recordScopeLabel,
      evidenceLabel,
      stateBadge: attentionCount > 0 ? "Dikkat" : "Düşük",
      sourceDataset: "commercial_purchase_price_assessments, commercial_purchase_price_comparison_runs",
      accessibleSummary: `${attentionCount} kayıtlı analiz dikkat sonucu verdi`,
    },
    "favorable-variances": {
      id: "favorable-variances",
      label: "Olumlu sapmalar",
      value: formatCount(favorableCount),
      detail: "Olumlu sapma sonucu veren kayıtlı analizler",
      tone: favorableCount > 0 ? "informational" : "neutral",
      scopeLabel: recordScopeLabel,
      evidenceLabel,
      stateBadge: favorableCount > 0 ? "Olumlu" : "Yok",
      sourceDataset: "commercial_purchase_price_assessments, commercial_purchase_price_comparison_runs",
      accessibleSummary: `${favorableCount} kayıtlı analiz olumlu sapma sonucu verdi`,
    },
    "supplier-spread": {
      id: "supplier-spread",
      label: "Tedarikçi yayılım bulguları",
      value: formatCount(spreadFindingCount),
      detail: "Yayılım eşiğini aşan kayıtlı bulgular",
      tone: spreadFindingCount > 0 ? "informational" : "neutral",
      scopeLabel: recordScopeLabel,
      evidenceLabel,
      stateBadge: spreadFindingCount > 0 ? "Eşik aşımı" : "Yok",
      sourceDataset: "commercial_purchase_price_assessments, commercial_purchase_price_comparison_runs",
      accessibleSummary: `${spreadFindingCount} kayıtlı analiz tedarikçi yayılım eşiğini aştı`,
    },
    "insufficient-conflict": {
      id: "insufficient-conflict",
      label: "Yetersiz / çelişkili kanıt",
      value: formatCount(insufficientConflictCount),
      detail: "Veri yetersizliği veya kaynak uyuşmazlığı kayıtları",
      tone: insufficientConflictCount > 0 ? "conflict" : "neutral",
      scopeLabel: recordScopeLabel,
      evidenceLabel,
      stateBadge: insufficientConflictCount > 0 ? "Eksik/Çelişki" : "Yok",
      sourceDataset: "commercial_purchase_price_assessments, commercial_purchase_price_comparison_runs",
      accessibleSummary: `${insufficientConflictCount} kayıtlı analiz yetersiz veya çelişkili kanıt bildirdi`,
    },
  };

  return reconcileCommercialIntelligenceKpis(
    COMMERCIAL_INTELLIGENCE_KPI_ORDER.map((id) => cards[id]),
  );
}

export function isRawUuidPrimaryLabel(value: string | null | undefined): boolean {
  if (!value?.trim()) return false;
  return UUID_PATTERN.test(value.trim());
}

export function validateRatioPresentation(ratio: number | null | undefined): boolean {
  if (ratio === null || ratio === undefined || Number.isNaN(ratio)) return true;
  if (ratio < -1) return false;
  return true;
}

export function reconcileCommercialIntelligenceKpis(
  cards: readonly CommercialIntelligenceKpiCard[],
): CommercialIntelligenceKpiCard[] {
  return [...cards];
}

export function buildCommercialIntelligenceTemporalChartData(
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): Readonly<{
  points: readonly CommercialIntelligenceTemporalChartPoint[];
  unavailableReason: string | null;
}> {
  const temporal = comparisons
    .filter((item) => item.comparisonType === "temporal-price-deviation")
    .slice()
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc))
    .slice(0, 6);

  if (temporal.length === 0) {
    return {
      points: [],
      unavailableReason: "Karşılaştırılabilir dönemsel sapma çalışması yok",
    };
  }

  const points = temporal.map((item) => ({
    id: item.id,
    label: buildCommercialIntelligenceComparisonSecondaryLabel(item),
    primaryLabel: buildCommercialIntelligenceComparisonPrimaryLabel(item),
    secondaryLabel: buildCommercialIntelligenceComparisonSecondaryLabel(item),
    traceReference: item.comparisonReference,
    currentMedian: item.currentMedianUnitPrice,
    referenceMedian: item.referenceMedianUnitPrice,
    signedVarianceRatio: item.signedVarianceRatio,
    signedVarianceLabel: item.signedVarianceLabel,
    outcome: item.comparisonOutcome,
    outcomeLabel: item.outcomeLabel,
    currencyCode: item.currencyCode,
    normalizedUnit: item.normalizedUnit,
  }));

  return { points, unavailableReason: null };
}

export function buildCommercialIntelligenceSupplierPositionRows(
  supplierResults: readonly CommercialIntelligenceSupplierResult[],
): readonly CommercialIntelligenceSupplierResult[] {
  return [...supplierResults].sort((left, right) => {
    if (left.priceRank !== right.priceRank) return left.priceRank - right.priceRank;
    return left.supplierLabel.localeCompare(right.supplierLabel, "tr");
  });
}

export function reconcileCommercialIntelligenceSupplierSpread(
  rows: readonly CommercialIntelligenceSupplierResult[],
  spreadRatio: number | null | undefined,
): string {
  if (!rows.length) return "—";
  const medians = rows
    .map((row) => row.supplierMedianUnitPrice)
    .filter((value): value is number => value !== null && !Number.isNaN(value));
  if (medians.length < 2) {
    return spreadRatio === null || spreadRatio === undefined
      ? "Tek tedarikçi — yayılım hesaplanamaz"
      : formatRatioAsPercent(spreadRatio);
  }
  const min = Math.min(...medians);
  const max = Math.max(...medians);
  const computed = min > 0 ? (max - min) / min : null;
  if (spreadRatio !== null && spreadRatio !== undefined && computed !== null) {
    const delta = Math.abs(spreadRatio - computed);
    if (delta > 0.0001) {
      return `${formatRatioAsPercent(spreadRatio)} (medyan aralığı: ${formatRatioAsPercent(computed)})`;
    }
  }
  return formatRatioAsPercent(spreadRatio ?? computed);
}

export function buildCommercialIntelligencePriorityDeviations(
  assessments: readonly CommercialIntelligenceAssessmentSummary[],
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): CommercialIntelligencePriorityDeviation[] {
  const assessmentItems = assessments
    .filter((item) => item.assessmentOutcome === "attention" || item.assessmentOutcome === "source-conflict")
    .map((item) => ({
      id: item.id,
      kind: "assessment" as const,
      reference: item.assessmentReference,
      primaryLabel: buildCommercialIntelligenceAssessmentPrimaryLabel(item),
      secondaryLabel: `${item.outcomeLabel} · ${item.varianceLabel} · ${item.evaluatedLabel}`,
      traceReference: item.assessmentReference,
      materialLabel: item.materialLabel,
      outcomeLabel: item.outcomeLabel,
      varianceLabel: item.varianceLabel,
      evaluatedLabel: item.evaluatedLabel,
      sortKey: Date.parse(item.evaluatedAtUtc),
    }));

  const comparisonItems = comparisons
    .filter((item) => item.comparisonOutcome === "attention" || item.comparisonOutcome === "source-conflict")
    .map((item) => ({
      id: item.id,
      kind: "comparison" as const,
      reference: item.comparisonReference,
      primaryLabel: buildCommercialIntelligenceComparisonPrimaryLabel(item),
      secondaryLabel: `${item.materialLabel} · ${item.outcomeLabel} · ${item.signedVarianceLabel} · ${item.evaluatedLabel}`,
      traceReference: item.comparisonReference,
      materialLabel: item.materialLabel,
      outcomeLabel: item.outcomeLabel,
      varianceLabel: item.signedVarianceLabel,
      evaluatedLabel: item.evaluatedLabel,
      sortKey: Date.parse(item.evaluatedAtUtc),
    }));

  return [...assessmentItems, ...comparisonItems]
    .sort((left, right) => right.sortKey - left.sortKey)
    .slice(0, 5)
    .map((item) => ({
      id: item.id,
      kind: item.kind,
      reference: item.reference,
      primaryLabel: item.primaryLabel,
      secondaryLabel: item.secondaryLabel,
      traceReference: item.traceReference,
      materialLabel: item.materialLabel,
      outcomeLabel: item.outcomeLabel,
      varianceLabel: item.varianceLabel,
      evaluatedLabel: item.evaluatedLabel,
    }));
}

export function buildCommercialIntelligenceEvidenceSummary(
  assessments: readonly CommercialIntelligenceAssessmentSummary[],
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
  supplierSpreadThreshold: number | null,
): CommercialIntelligenceEvidenceSummary {
  const evidenceContext = resolveCommercialIntelligenceEvidenceContext(assessments, comparisons);

  const attentionCount = [
    ...assessments.filter((item) => item.assessmentOutcome === "attention"),
    ...comparisons.filter((item) => item.comparisonOutcome === "attention"),
  ].length;
  const favorableCount = [
    ...assessments.filter((item) => item.assessmentOutcome === "favorable-variance"),
    ...comparisons.filter((item) => item.comparisonOutcome === "favorable-variance"),
  ].length;
  const insufficientCount = [
    ...assessments.filter((item) => item.assessmentOutcome === "insufficient-data"),
    ...comparisons.filter((item) => item.comparisonOutcome === "insufficient-data"),
  ].length;
  const conflictCount = [
    ...assessments.filter((item) => item.assessmentOutcome === "source-conflict"),
    ...comparisons.filter((item) => item.comparisonOutcome === "source-conflict"),
  ].length;
  const spreadFindingCount = countSupplierSpreadFindings(assessments, comparisons, supplierSpreadThreshold);

  const denominator = evidenceContext.comparableTotal + evidenceContext.excludedTotal;
  const reconciliationLabel = denominator > 0
    ? `${evidenceContext.scopeLabel}: ${evidenceContext.comparableTotal} karşılaştırılabilir / ${evidenceContext.excludedTotal} dışlanan gözlem`
    : "Karşılaştırılabilir gözlem bağlamı yok";

  return {
    comparableTotal: evidenceContext.comparableTotal,
    excludedTotal: evidenceContext.excludedTotal,
    attentionCount,
    favorableCount,
    insufficientCount,
    conflictCount,
    spreadFindingCount,
    reconciliationLabel,
  };
}

export function resolveCommercialIntelligenceKpiToneClass(tone: CommercialIntelligenceKpiTone): string {
  return `commint-kpi-card__tone--${tone}`;
}

export function mapCommercialIntelligenceSupplierResult(input: Readonly<{
  id: string;
  supplierReference: string | null;
  comparableObservationCount: number;
  supplierMedianUnitPrice: number | null;
  lowestComparableMedianUnitPrice: number | null;
  relativePricePositionRatio: number | null;
  priceRank: number;
  isTied: boolean;
  positionState: string;
  evidenceStatus: string;
}>, currencyCode: string): CommercialIntelligenceSupplierResult {
  return {
    id: input.id,
    supplierReference: input.supplierReference,
    supplierLabel: localizeCommercialIntelligenceSupplierLabel(input.supplierReference),
    comparableObservationCount: input.comparableObservationCount,
    supplierMedianUnitPrice: input.supplierMedianUnitPrice,
    supplierMedianLabel: formatCurrency(input.supplierMedianUnitPrice, currencyCode),
    lowestComparableMedianUnitPrice: input.lowestComparableMedianUnitPrice,
    relativePricePositionRatio: input.relativePricePositionRatio,
    relativePositionLabel: formatRatioAsPercent(input.relativePricePositionRatio),
    priceRank: input.priceRank,
    isTied: input.isTied,
    positionState: input.positionState,
    positionStateLabel: getCommercialIntelligencePositionStateLabel(input.positionState),
    evidenceStatus: input.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(input.evidenceStatus),
  };
}

export function formatCommercialIntelligenceOutcomeWithVariance(
  outcome: string,
  varianceRatio: number | null | undefined,
): string {
  const outcomeLabel = getCommercialIntelligenceOutcomeLabel(outcome);
  const varianceLabel = formatRatioAsPercent(varianceRatio);
  if (varianceLabel === "—") return outcomeLabel;
  return `${outcomeLabel} (${varianceLabel})`;
}

export const COMMERCIAL_INTELLIGENCE_FORBIDDEN_UI_TERMS = [
  "recommended supplier",
  "guaranteed savings",
  "automatic procurement",
  "best supplier",
] as const;

export function filterCommercialIntelligenceSupplierComparisons(
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): CommercialIntelligenceComparisonSummary[] {
  return comparisons
    .filter((item) => item.comparisonType === COMMERCIAL_INTELLIGENCE_SUPPLIER_COMPARISON_TYPE)
    .slice()
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc));
}

export function isCompatibleSupplierComparison(
  comparison: CommercialIntelligenceComparisonSummary,
): boolean {
  return comparison.comparableObservationCount > 0
    && comparison.comparisonOutcome !== "source-conflict"
    && comparison.comparisonOutcome !== "insufficient-data";
}

export function countSupplierComparisonAttentionRecords(
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): number {
  return filterCommercialIntelligenceSupplierComparisons(comparisons).filter(
    (item) => item.comparisonOutcome === "attention"
      || item.comparisonOutcome === "insufficient-data"
      || item.comparisonOutcome === "source-conflict",
  ).length;
}

export function buildCommercialSupplierComparisonKpiCards(input: Readonly<{
  selectedComparison: CommercialIntelligenceComparisonSummary | null;
  supplierRows: readonly CommercialIntelligenceSupplierResult[];
  supplierComparisons: readonly CommercialIntelligenceComparisonSummary[];
  isCompatibleSelection: boolean;
}>): CommercialIntelligenceKpiCard[] {
  const { selectedComparison, supplierRows, supplierComparisons, isCompatibleSelection } = input;
  const scopeLabel = selectedComparison
    ? `${selectedComparison.materialLabel} · ${selectedComparison.currentWindowLabel}`
    : "Seçili karşılaştırma yok";
  const sourceDataset = "commercial_purchase_price_comparison_runs";
  const evidenceLabel = "Tedarikçi fiyat kanıtı";
  const tiedCount = supplierRows.filter((row) => row.isTied).length;
  const attentionRecordCount = countSupplierComparisonAttentionRecords(supplierComparisons);
  const spreadValue = selectedComparison?.supplierSpreadRatio ?? null;
  const spreadLabel = formatRatioAsPercent(spreadValue);
  const supplierCount = isCompatibleSelection ? supplierRows.length : 0;
  const comparableTotal = selectedComparison?.comparableObservationCount ?? 0;
  const excludedTotal = selectedComparison?.excludedObservationCount ?? 0;

  const unavailable = (id: string, label: string, detail: string): CommercialIntelligenceKpiCard => ({
    id,
    label,
    value: "—",
    detail,
    tone: "neutral",
    scopeLabel,
    evidenceLabel,
    stateBadge: "Yok",
    sourceDataset,
    accessibleSummary: detail,
    unavailableReason: detail,
  });

  const cards: Record<string, CommercialIntelligenceKpiCard> = {
    "comparable-suppliers": isCompatibleSelection && supplierCount > 0
      ? {
          id: "comparable-suppliers",
          label: "Karşılaştırılabilir tedarikçiler",
          value: formatCount(supplierCount),
          detail: "Seçili uyumlu karşılaştırmadaki tedarikçi satırı",
          tone: "controlled",
          scopeLabel,
          evidenceLabel,
          stateBadge: "Aktif",
          sourceDataset,
          accessibleSummary: `${supplierCount} karşılaştırılabilir tedarikçi satırı`,
        }
      : {
          id: "comparable-suppliers",
          label: "Karşılaştırılabilir tedarikçiler",
          value: "—",
          detail: "Uyumlu tedarikçi karşılaştırması seçilmedi",
          tone: "neutral",
          scopeLabel,
          evidenceLabel,
          stateBadge: "Yok",
          sourceDataset,
          accessibleSummary: "Karşılaştırılabilir tedarikçi sıralaması üretilemedi",
          unavailableReason: "Uyumlu karşılaştırma yok",
        },
    "comparable-observations": selectedComparison
      ? {
          id: "comparable-observations",
          label: "Karşılaştırılabilir gözlemler",
          value: formatCount(comparableTotal),
          detail: "Seçili karşılaştırmanın dahil ettiği gözlem sayısı",
          tone: comparableTotal > 0 ? "controlled" : "insufficient",
          scopeLabel,
          evidenceLabel,
          stateBadge: comparableTotal > 0 ? "Aktif" : "Eksik",
          sourceDataset,
          accessibleSummary: `${comparableTotal} karşılaştırılabilir gözlem`,
        }
      : unavailable(
          "comparable-observations",
          "Karşılaştırılabilir gözlemler",
          "Karşılaştırılabilir gözlem bağlamı yok",
        ),
    "excluded-observations": selectedComparison
      ? {
          id: "excluded-observations",
          label: "Dışlanan gözlemler",
          value: formatCount(excludedTotal),
          detail: "Aynı karşılaştırmada dışlanan gözlem sayısı",
          tone: excludedTotal > 0 ? "attention" : "neutral",
          scopeLabel,
          evidenceLabel,
          stateBadge: excludedTotal > 0 ? "Dışlanan" : "Yok",
          sourceDataset,
          accessibleSummary: `${excludedTotal} dışlanan gözlem`,
        }
      : unavailable(
          "excluded-observations",
          "Dışlanan gözlemler",
          "Dışlanan gözlem bağlamı yok",
        ),
    "price-spread": selectedComparison && spreadValue !== null
      ? {
          id: "price-spread",
          label: "Fiyat yayılımı",
          value: spreadLabel,
          detail: "Seçili çalıştırmanın tedarikçi yayılım oranı",
          tone: spreadValue > 0 ? "informational" : "neutral",
          scopeLabel,
          evidenceLabel,
          stateBadge: spreadValue > 0 ? "Yayılım" : "Düz",
          sourceDataset,
          accessibleSummary: `Tedarikçi yayılımı ${spreadLabel}`,
        }
      : {
          id: "price-spread",
          label: "Fiyat yayılımı",
          value: "—",
          detail: "Yayılım oranı hesaplanamadı",
          tone: "neutral",
          scopeLabel,
          evidenceLabel,
          stateBadge: "Yok",
          sourceDataset,
          accessibleSummary: "Fiyat yayılımı mevcut değil",
          unavailableReason: "Yayılım oranı yok",
        },
    "tied-positions": isCompatibleSelection
      ? {
          id: "tied-positions",
          label: "Eşit fiyat konumları",
          value: formatCount(tiedCount),
          detail: "Seçili karşılaştırmadaki açık eşit sıra durumu",
          tone: tiedCount > 0 ? "informational" : "neutral",
          scopeLabel,
          evidenceLabel,
          stateBadge: tiedCount > 0 ? "Eşit" : "Yok",
          sourceDataset,
          accessibleSummary: `${tiedCount} eşit fiyat konumu`,
        }
      : {
          id: "tied-positions",
          label: "Eşit fiyat konumları",
          value: "—",
          detail: "Eşit sıra durumu üretilemedi",
          tone: "neutral",
          scopeLabel,
          evidenceLabel,
          stateBadge: "Yok",
          sourceDataset,
          accessibleSummary: "Eşit fiyat konumu yok",
          unavailableReason: "Uyumlu karşılaştırma yok",
        },
    "attention-evidence-issues": {
      id: "attention-evidence-issues",
      label: "Dikkat / kanıt sorunu",
      value: formatCount(attentionRecordCount),
      detail: "Kanıt sorunu bildiren kayıtlı karşılaştırmalar",
      tone: attentionRecordCount > 0 ? "attention" : "neutral",
      scopeLabel: "Kayıtlı tedarikçi karşılaştırmaları",
      evidenceLabel,
      stateBadge: attentionRecordCount > 0 ? "Kayıt" : "Yok",
      sourceDataset,
      accessibleSummary: `${attentionRecordCount} kayıtlı karşılaştırma dikkat veya kanıt sorunu bildirdi`,
    },
  };

  return COMMERCIAL_SUPPLIER_COMPARISON_KPI_ORDER.map((id) => cards[id]);
}

export function buildCommercialSalesPriceComparisonSecondaryLabel(
  comparison: Pick<
    CommercialSalesPriceComparisonSummary,
    | "comparisonType"
    | "materialLabel"
    | "currentWindowLabel"
    | "referenceWindowLabel"
    | "comparableObservationCount"
  >,
): string {
  if (comparison.comparisonType === COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE) {
    return `${comparison.materialLabel} · ${comparison.referenceWindowLabel} / ${comparison.currentWindowLabel}`;
  }
  const customerCount = Math.max(0, Math.floor(comparison.comparableObservationCount / 6));
  const customerLabel = customerCount > 0
    ? `${customerCount} karşılaştırılabilir müşteri`
    : "Müşteri penceresi";
  return `${comparison.materialLabel} · ${customerLabel} · ${comparison.currentWindowLabel}`;
}

export function buildCommercialSalesPriceAssessmentSecondaryLabel(
  assessment: Pick<
    CommercialSalesPriceAssessmentSummary,
    "materialLabel" | "customerLabel" | "windowLabel" | "evaluatedLabel"
  >,
): string {
  return `${assessment.materialLabel} · ${assessment.customerLabel} · ${assessment.windowLabel}`;
}

export function selectLatestCompatibleTemporalSalesComparison(
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
): CommercialSalesPriceComparisonSummary | null {
  const temporalRuns = filterCommercialSalesPriceTemporalComparisons(comparisons);
  const compatible = temporalRuns.filter(
    (item) => item.comparableObservationCount > 0
      && item.comparisonOutcome !== "source-conflict"
      && item.comparisonOutcome !== "insufficient-data",
  );
  return compatible[0] ?? temporalRuns[0] ?? null;
}

export function selectLatestCompatibleCustomerSalesComparison(
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
): CommercialSalesPriceComparisonSummary | null {
  const customerRuns = filterCommercialSalesPriceCustomerComparisons(comparisons);
  const compatible = customerRuns.filter(
    (item) => item.comparableObservationCount > 0
      && item.comparisonOutcome !== "source-conflict"
      && item.comparisonOutcome !== "insufficient-data",
  );
  return compatible[0] ?? customerRuns[0] ?? null;
}

export function selectLatestSalesAssessment(
  assessments: readonly CommercialSalesPriceAssessmentSummary[],
): CommercialSalesPriceAssessmentSummary | null {
  const ordered = assessments
    .slice()
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc));
  return ordered[0] ?? null;
}

export function resolveCommercialSalesPriceEvidenceContext(
  assessments: readonly CommercialSalesPriceAssessmentSummary[],
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
): Readonly<{
  comparableTotal: number;
  excludedTotal: number;
  candidateTotal: number;
  scopeLabel: string;
  sourceDataset: string;
  accessibleSummary: string;
}> {
  const latestTemporal = selectLatestCompatibleTemporalSalesComparison(comparisons);
  if (latestTemporal) {
    return {
      comparableTotal: latestTemporal.comparableObservationCount,
      excludedTotal: latestTemporal.excludedObservationCount,
      candidateTotal: latestTemporal.candidateObservationCount,
      scopeLabel: "Son dönemsel satış karşılaştırması",
      sourceDataset: "commercial_sales_price_comparison_runs",
      accessibleSummary: `${getCommercialIntelligenceComparisonTypeLabel(latestTemporal.comparisonType)}: ${latestTemporal.comparableObservationCount} karşılaştırılabilir, ${latestTemporal.excludedObservationCount} dışlanan gözlem`,
    };
  }

  const latestAssessment = selectLatestSalesAssessment(assessments);
  if (latestAssessment) {
    return {
      comparableTotal: latestAssessment.comparableObservationCount,
      excludedTotal: latestAssessment.excludedObservationCount,
      candidateTotal: latestAssessment.candidateObservationCount,
      scopeLabel: "Son satış fiyat analizi",
      sourceDataset: "commercial_sales_price_assessments",
      accessibleSummary: `${latestAssessment.materialLabel}: ${latestAssessment.comparableObservationCount} karşılaştırılabilir, ${latestAssessment.excludedObservationCount} dışlanan gözlem`,
    };
  }

  return {
    comparableTotal: 0,
    excludedTotal: 0,
    candidateTotal: 0,
    scopeLabel: "Kanıt bağlamı yok",
    sourceDataset: "—",
    accessibleSummary: "Karşılaştırılabilir gözlem bağlamı bulunamadı",
  };
}

export function countCustomerSpreadFindings(
  assessments: readonly CommercialSalesPriceAssessmentSummary[],
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
  customerSpreadThreshold: number | null,
): number {
  const assessmentBreaches = assessments.filter(
    (item) => item.leadingDriverCode === COMMERCIAL_SALES_PRICE_CUSTOMER_SPREAD_DRIVER,
  ).length;
  const comparisonBreaches = comparisons.filter((item) => {
    if (item.leadingDriverCode === COMMERCIAL_SALES_PRICE_CUSTOMER_SPREAD_DRIVER) return true;
    if (
      item.comparisonType === COMMERCIAL_SALES_PRICE_CUSTOMER_POSITION_TYPE
      && customerSpreadThreshold !== null
      && item.customerSpreadRatio !== null
      && item.customerSpreadRatio > customerSpreadThreshold
      && item.comparisonOutcome === "attention"
    ) {
      return true;
    }
    return false;
  }).length;
  return assessmentBreaches + comparisonBreaches;
}

export function buildCommercialSalesPriceKpiCards(input: Readonly<{
  assessments: readonly CommercialSalesPriceAssessmentSummary[];
  comparisons: readonly CommercialSalesPriceComparisonSummary[];
  customerSpreadThreshold: number | null;
  comparedCustomerCount?: number;
}>): CommercialIntelligenceKpiCard[] {
  const { assessments, comparisons, comparedCustomerCount = 0 } = input;
  const selectedTemporal = selectLatestCompatibleTemporalSalesComparison(comparisons);
  const selectedCustomer = selectLatestCompatibleCustomerSalesComparison(comparisons);
  const selectedAssessment = selectLatestSalesAssessment(assessments);

  const scopeLabel = selectedTemporal
    ? "Son dönemsel satış karşılaştırması"
    : selectedAssessment
      ? "Son satış fiyat analizi"
      : "Kanıt bağlamı yok";
  const sourceDataset = selectedTemporal
    ? "commercial_sales_price_comparison_runs"
    : selectedAssessment
      ? "commercial_sales_price_assessments"
      : "—";

  const evidenceLabel = "Satış fiyat kanıtı";
  const unavailableValue = "—";
  const hasEvidenceScope = selectedTemporal !== null || selectedAssessment !== null;

  const comparableTotal = selectedTemporal?.comparableObservationCount
    ?? selectedAssessment?.comparableObservationCount
    ?? 0;
  const excludedTotal = selectedTemporal?.excludedObservationCount
    ?? selectedAssessment?.excludedObservationCount
    ?? 0;
  const candidateTotal = selectedTemporal?.candidateObservationCount
    ?? selectedAssessment?.candidateObservationCount
    ?? comparableTotal + excludedTotal;

  const currentMedian = selectedTemporal?.currentMedianUnitPrice ?? selectedAssessment?.currentUnitPrice ?? null;
  const referenceMedian = selectedTemporal?.referenceMedianUnitPrice ?? selectedAssessment?.referenceMedianUnitPrice ?? null;
  const currencyCode = selectedTemporal?.currencyCode ?? selectedAssessment?.currencyCode ?? null;
  const normalizedUnit = selectedTemporal?.normalizedUnit ?? selectedAssessment?.normalizedUnit ?? null;

  const attentionCount = [
    selectedTemporal?.comparisonOutcome === "attention" ? selectedTemporal : null,
    selectedCustomer?.comparisonOutcome === "attention" ? selectedCustomer : null,
    selectedAssessment?.assessmentOutcome === "attention" ? selectedAssessment : null,
  ].filter(Boolean).length;

  const medianDetail = currencyCode && normalizedUnit
    ? `${currencyCode} / ${normalizedUnit}`
    : currencyCode ?? normalizedUnit ?? "Birim bağlamı yok";

  const cards: Record<string, CommercialIntelligenceKpiCard> = {
    "comparable-sales-evidence": {
      id: "comparable-sales-evidence",
      label: "Karşılaştırılabilir satış kanıtı",
      value: comparableTotal > 0 ? formatCount(comparableTotal) : unavailableValue,
      detail: "Seçili uyumlu çalıştırmadaki dahil edilen gözlemler",
      tone: comparableTotal > 0 ? "controlled" : "insufficient",
      scopeLabel,
      evidenceLabel,
      stateBadge: comparableTotal > 0 ? "Aktif" : "Eksik",
      sourceDataset,
      accessibleSummary: comparableTotal > 0
        ? `${scopeLabel}: ${comparableTotal} karşılaştırılabilir gözlem`
        : "Karşılaştırılabilir satış kanıtı yok",
    },
    "excluded-evidence": {
      id: "excluded-evidence",
      label: "Hariç tutulan kanıt",
      value: hasEvidenceScope ? formatCount(excludedTotal) : unavailableValue,
      detail: "Aynı kanıt bağlamında dışlanan gözlemler",
      tone: hasEvidenceScope && excludedTotal > 0 ? "attention" : hasEvidenceScope ? "neutral" : "insufficient",
      scopeLabel,
      evidenceLabel,
      stateBadge: hasEvidenceScope ? (excludedTotal > 0 ? "Dışlanan" : "Yok") : "Eksik",
      sourceDataset,
      accessibleSummary: `${scopeLabel}: ${excludedTotal} hariç tutulan gözlem; aday ${formatCount(candidateTotal)}`,
    },
    "current-median-sales-price": {
      id: "current-median-sales-price",
      label: "Güncel medyan satış fiyatı",
      value: currentMedian !== null ? formatCurrency(currentMedian, currencyCode) : unavailableValue,
      detail: medianDetail,
      tone: currentMedian !== null ? "informational" : "insufficient",
      scopeLabel,
      evidenceLabel,
      stateBadge: currentMedian !== null ? "Güncel" : "Yok",
      sourceDataset,
      accessibleSummary: currentMedian !== null
        ? `Güncel medyan ${formatCurrency(currentMedian, currencyCode)}`
        : "Güncel medyan satış fiyatı mevcut değil",
    },
    "reference-median-sales-price": {
      id: "reference-median-sales-price",
      label: "Referans medyan satış fiyatı",
      value: referenceMedian !== null ? formatCurrency(referenceMedian, currencyCode) : unavailableValue,
      detail: medianDetail,
      tone: referenceMedian !== null ? "informational" : "insufficient",
      scopeLabel,
      evidenceLabel,
      stateBadge: referenceMedian !== null ? "Referans" : "Yok",
      sourceDataset,
      accessibleSummary: referenceMedian !== null
        ? `Referans medyan ${formatCurrency(referenceMedian, currencyCode)}`
        : "Referans medyan satış fiyatı mevcut değil",
    },
    "attention-deviations": {
      id: "attention-deviations",
      label: "Dikkat gerektiren sapmalar",
      value: attentionCount > 0 ? formatCount(attentionCount) : unavailableValue,
      detail: "Seçili uyumlu çalıştırmalarda dikkat sonucu",
      tone: attentionCount > 0 ? "attention" : "controlled",
      scopeLabel,
      evidenceLabel,
      stateBadge: attentionCount > 0 ? "Dikkat" : "Düşük",
      sourceDataset: "commercial_sales_price_assessments, commercial_sales_price_comparison_runs",
      accessibleSummary: attentionCount > 0
        ? `${attentionCount} kayıt dikkat gerektiren sapma bildirdi`
        : "Seçili bağlamda dikkat gerektiren sapma yok",
    },
    "compared-customers": {
      id: "compared-customers",
      label: "Karşılaştırılan müşteriler",
      value: comparedCustomerCount > 0 ? formatCount(comparedCustomerCount) : unavailableValue,
      detail: selectedCustomer?.materialLabel ?? "Müşteri karşılaştırma çalışması",
      tone: comparedCustomerCount > 0 ? "controlled" : "insufficient",
      scopeLabel: selectedCustomer ? "Son müşteri karşılaştırması" : "Müşteri bağlamı yok",
      evidenceLabel,
      stateBadge: comparedCustomerCount > 0 ? "Sıralı" : "Eksik",
      sourceDataset: "commercial_sales_price_comparison_runs",
      accessibleSummary: comparedCustomerCount > 0
        ? `${comparedCustomerCount} müşteri karşılaştırılabilir medyan ile sıralandı`
        : "Karşılaştırılan müşteri yok",
    },
  };

  return COMMERCIAL_SALES_PRICE_KPI_ORDER.map((id) => cards[id]);
}

export function buildCommercialSalesPriceTemporalChartData(
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
): Readonly<{
  points: readonly CommercialIntelligenceTemporalChartPoint[];
  unavailableReason: string | null;
}> {
  const temporal = filterCommercialSalesPriceTemporalComparisons(comparisons).slice(0, 6);

  if (temporal.length === 0) {
    return {
      points: [],
      unavailableReason: "Karşılaştırılabilir dönemsel satış sapma çalışması yok",
    };
  }

  const points = temporal.map((item) => ({
    id: item.id,
    label: buildCommercialSalesPriceComparisonSecondaryLabel(item),
    primaryLabel: getCommercialIntelligenceComparisonTypeLabel(item.comparisonType),
    secondaryLabel: buildCommercialSalesPriceComparisonSecondaryLabel(item),
    traceReference: item.comparisonReference,
    currentMedian: item.currentMedianUnitPrice,
    referenceMedian: item.referenceMedianUnitPrice,
    signedVarianceRatio: item.signedVarianceRatio,
    signedVarianceLabel: item.signedVarianceLabel,
    outcome: item.comparisonOutcome,
    outcomeLabel: item.outcomeLabel,
    currencyCode: item.currencyCode,
    normalizedUnit: item.normalizedUnit,
  }));

  return { points, unavailableReason: null };
}

export function buildCommercialSalesPriceCustomerPositionRows(
  customerResults: readonly CommercialSalesPriceCustomerResult[],
): readonly CommercialSalesPriceCustomerResult[] {
  return [...customerResults].sort((left, right) => {
    if (left.priceRank !== right.priceRank) return left.priceRank - right.priceRank;
    return left.customerLabel.localeCompare(right.customerLabel, "tr");
  });
}

export function reconcileCommercialSalesPriceCustomerSpread(
  rows: readonly CommercialSalesPriceCustomerResult[],
  spreadRatio: number | null | undefined,
): string {
  if (!rows.length) return "—";
  const medians = rows
    .map((row) => row.customerMedianUnitPrice)
    .filter((value): value is number => value !== null && !Number.isNaN(value));
  if (medians.length < 2) {
    return spreadRatio === null || spreadRatio === undefined
      ? "Tek müşteri — yayılım hesaplanamaz"
      : formatRatioAsPercent(spreadRatio);
  }
  const min = Math.min(...medians);
  const max = Math.max(...medians);
  const computed = min > 0 ? (max - min) / min : null;
  if (spreadRatio !== null && spreadRatio !== undefined && computed !== null) {
    const delta = Math.abs(spreadRatio - computed);
    if (delta > 0.0001) {
      return `${formatRatioAsPercent(spreadRatio)} (medyan aralığı: ${formatRatioAsPercent(computed)})`;
    }
  }
  return formatRatioAsPercent(spreadRatio ?? computed);
}

export function mapCommercialSalesPriceCustomerResult(input: Readonly<{
  id: string;
  customerReference: string | null;
  comparableObservationCount: number;
  customerMedianUnitPrice: number | null;
  lowestComparableMedianUnitPrice: number | null;
  relativePricePositionRatio: number | null;
  priceRank: number;
  isTied: boolean;
  positionState: string;
  evidenceStatus: string;
}>, currencyCode: string): CommercialSalesPriceCustomerResult {
  return {
    id: input.id,
    customerReference: input.customerReference,
    customerLabel: localizeCommercialIntelligenceCustomerLabel(input.customerReference),
    comparableObservationCount: input.comparableObservationCount,
    customerMedianUnitPrice: input.customerMedianUnitPrice,
    customerMedianLabel: formatCurrency(input.customerMedianUnitPrice, currencyCode),
    lowestComparableMedianUnitPrice: input.lowestComparableMedianUnitPrice,
    relativePricePositionRatio: input.relativePricePositionRatio,
    relativePositionLabel: formatRatioAsPercent(input.relativePricePositionRatio),
    priceRank: input.priceRank,
    isTied: input.isTied,
    positionState: input.positionState,
    positionStateLabel: getCommercialIntelligencePositionStateLabel(input.positionState),
    evidenceStatus: input.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(input.evidenceStatus),
  };
}

export function filterCommercialSalesPriceTemporalComparisons(
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
): CommercialSalesPriceComparisonSummary[] {
  return comparisons
    .filter((item) => item.comparisonType === COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE)
    .slice()
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc));
}

export function filterCommercialSalesPriceCustomerComparisons(
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
): CommercialSalesPriceComparisonSummary[] {
  return comparisons
    .filter((item) => item.comparisonType === COMMERCIAL_SALES_PRICE_CUSTOMER_POSITION_TYPE)
    .slice()
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc));
}

export function isCompatibleCustomerSalesComparison(
  comparison: CommercialSalesPriceComparisonSummary,
): boolean {
  return comparison.comparableObservationCount > 0
    && comparison.comparisonOutcome !== "source-conflict"
    && comparison.comparisonOutcome !== "insufficient-data";
}
