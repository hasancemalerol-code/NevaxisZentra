import type {
  CommercialProfitabilityComparisonSummary,
  CommercialProfitabilityKpiCard,
  CommercialProfitabilityPositionRow,
  CommercialProfitabilityTemporalChartPoint,
} from "../types/commercial-profitability-types";
import type { CommercialCustomerProfitabilityTableRow } from "../types/commercial-customer-profitability-types";
import { formatCount, formatDateTime } from "../domain/commercial-intelligence-formatters";
import {
  getCommercialProfitabilityComparisonTypeLabel,
  getCommercialProfitabilityOutcomeLabel,
} from "../domain/commercial-profitability-labels";
import { resolveCommercialIntelligenceKpiToneClass } from "./commercial-intelligence-presentation";
import type { CommercialIntelligenceKpiTone } from "../types/commercial-intelligence-types";
import { localizeCommercialIntelligenceMaterialLabel } from "./commercial-intelligence-presentation";

const TEMPORAL_TYPE = "temporal-contribution-trend";
const COST_MIX_TYPE = "cost-component-mix-change";
const CUSTOMER_POSITION_TYPE = "customer-contribution-position";
const MATERIAL_POSITION_TYPE = "material-contribution-position";

export const PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE = CUSTOMER_POSITION_TYPE;

export function filterProfitabilityCustomerComparisons(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialProfitabilityComparisonSummary[] {
  return comparisons.filter((item) => item.comparisonType === CUSTOMER_POSITION_TYPE);
}

export function filterProfitabilityTemporalComparisons(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialProfitabilityComparisonSummary[] {
  return comparisons.filter((item) => item.comparisonType === TEMPORAL_TYPE);
}

export function filterProfitabilityCostMixComparisons(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialProfitabilityComparisonSummary[] {
  return comparisons.filter((item) => item.comparisonType === COST_MIX_TYPE);
}

export function filterProfitabilityPositionComparisons(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialProfitabilityComparisonSummary[] {
  return comparisons.filter(
    (item) => item.comparisonType === CUSTOMER_POSITION_TYPE || item.comparisonType === MATERIAL_POSITION_TYPE,
  );
}

export function selectLatestProfitabilityComparison(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): CommercialProfitabilityComparisonSummary | null {
  if (comparisons.length === 0) return null;
  return [...comparisons].sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc))[0] ?? null;
}

export function buildCustomerProfitabilityKpiCards(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialProfitabilityKpiCard[] {
  const improvement = comparisons.filter((item) => item.comparisonOutcome === "contribution-improvement").length;
  const deterioration = comparisons.filter((item) => item.comparisonOutcome === "contribution-deterioration").length;
  const stable = comparisons.filter((item) => item.comparisonOutcome === "within-comparison-band").length;
  const insufficient = comparisons.filter((item) => item.comparisonOutcome === "insufficient-data").length;
  const conflict = comparisons.filter((item) => item.comparisonOutcome === "source-conflict").length;

  return [
    {
      id: "customer-comparisons",
      label: "Müşteri karşılaştırması",
      value: formatCount(comparisons.length),
      hint: "Bu sayfadaki sonuç kümesi",
      tone: "neutral",
    },
    {
      id: "customer-improvement",
      label: "Katkı iyileşmesi",
      value: formatCount(improvement),
      hint: "Müşteri katkı konumu",
      tone: improvement > 0 ? "positive" : "neutral",
    },
    {
      id: "customer-deterioration",
      label: "Katkı bozulması",
      value: formatCount(deterioration),
      hint: "Müşteri katkı konumu",
      tone: deterioration > 0 ? "negative" : "neutral",
    },
    {
      id: "customer-stable",
      label: "Kararlı / nötr",
      value: formatCount(stable),
      hint: "Karşılaştırma bandı içinde",
      tone: "neutral",
    },
    {
      id: "customer-insufficient-disputed",
      label: "Yetersiz / çakışma",
      value: formatCount(insufficient + conflict),
      hint: "Sıfır olarak gösterilmez",
      tone: insufficient + conflict > 0 ? "warning" : "neutral",
    },
  ];
}

export function buildProfitabilityKpiCards(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialProfitabilityKpiCard[] {
  const comparableTotal = comparisons.reduce((sum, item) => sum + item.comparableAssessmentCount, 0);
  const excludedTotal = comparisons.reduce((sum, item) => sum + item.excludedAssessmentCount, 0);
  const deterioration = comparisons.filter((item) => item.comparisonOutcome === "contribution-deterioration").length;
  const improvement = comparisons.filter((item) => item.comparisonOutcome === "contribution-improvement").length;
  const insufficient = comparisons.filter((item) => item.comparisonOutcome === "insufficient-data").length;
  const conflict = comparisons.filter((item) => item.comparisonOutcome === "source-conflict").length;

  return [
    {
      id: "comparable-assessments",
      label: "Karşılaştırılabilir değerlendirme",
      value: formatCount(comparableTotal),
      hint: `${formatCount(excludedTotal)} hariç tutuldu`,
      tone: "neutral",
    },
    {
      id: "deterioration-runs",
      label: "Katkı bozulması",
      value: formatCount(deterioration),
      hint: "Zamansal veya maliyet karşılaştırmaları",
      tone: deterioration > 0 ? "negative" : "neutral",
    },
    {
      id: "improvement-runs",
      label: "Katkı iyileşmesi",
      value: formatCount(improvement),
      hint: "Olumlu marj değişimi",
      tone: improvement > 0 ? "positive" : "neutral",
    },
    {
      id: "insufficient-or-conflict",
      label: "Yetersiz / çakışma",
      value: formatCount(insufficient + conflict),
      hint: "Sıfır olarak gösterilmez",
      tone: insufficient + conflict > 0 ? "warning" : "neutral",
    },
  ];
}

export function buildProfitabilityTemporalChartData(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialProfitabilityTemporalChartPoint[] {
  return filterProfitabilityTemporalComparisons(comparisons)
    .slice()
    .sort((left, right) => Date.parse(left.evaluatedAtUtc) - Date.parse(right.evaluatedAtUtc))
    .map((item) => ({
      id: item.id,
      label: item.materialLabel !== "—" ? item.materialLabel : item.comparisonReference,
      referenceMarginPercent: item.referenceMedianContributionMarginRatio === null
        ? null
        : item.referenceMedianContributionMarginRatio * 100,
      currentMarginPercent: item.currentMedianContributionMarginRatio === null
        ? null
        : item.currentMedianContributionMarginRatio * 100,
    }));
}

export function buildCustomerProfitabilityTableRows(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialCustomerProfitabilityTableRow[] {
  return [...comparisons]
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc))
    .map((item) => ({
      id: item.id,
      customerLabel: item.customerLabel !== "—" ? item.customerLabel : item.comparisonReference,
      customerReference: item.customerReference,
      referenceMarginLabel: item.referenceMarginLabel,
      currentMarginLabel: item.currentMarginLabel,
      marginDeltaLabel: item.marginDeltaLabel,
      unitContributionLabel: "—",
      positionStateLabel: getCommercialProfitabilityOutcomeLabel(item.comparisonOutcome),
      outcomeLabel: item.outcomeLabel,
      evidenceLabel: item.evidenceLabel,
      coverageLabel: `${formatCount(item.comparableAssessmentCount)} / ${formatCount(item.excludedAssessmentCount)}`,
      evaluatedLabel: item.evaluatedLabel || formatDateTime(item.evaluatedAtUtc),
      comparisonOutcome: item.comparisonOutcome,
      evidenceStatus: item.evidenceStatus,
    }));
}

export function buildProfitabilityPositionPreviewRows(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
): readonly CommercialProfitabilityPositionRow[] {
  return filterProfitabilityPositionComparisons(comparisons)
    .slice(0, 6)
    .map((item) => ({
      id: item.id,
      scopeLabel: item.comparisonType === CUSTOMER_POSITION_TYPE
        ? item.customerLabel
        : item.materialLabel,
      marginLabel: item.currentMarginLabel,
      unitContributionLabel: "—",
      comparableCountLabel: formatCount(item.comparableAssessmentCount),
      positionStateLabel: getCommercialProfitabilityOutcomeLabel(item.comparisonOutcome),
      isTied: item.leadingDriverCode === "contribution-position-tie",
      rank: null,
    }));
}

export function buildProfitabilityComparisonSecondaryLabel(
  comparison: CommercialProfitabilityComparisonSummary,
): string {
  const typeLabel = getCommercialProfitabilityComparisonTypeLabel(comparison.comparisonType);
  return `${typeLabel} · ${comparison.currentWindowLabel}`;
}

function mapProfitabilityKpiTone(tone: CommercialProfitabilityKpiCard["tone"]): CommercialIntelligenceKpiTone {
  if (tone === "positive") return "controlled";
  if (tone === "negative") return "critical";
  if (tone === "warning") return "insufficient";
  return "neutral";
}

export function resolveProfitabilityKpiToneClass(tone: CommercialProfitabilityKpiCard["tone"]): string {
  return resolveCommercialIntelligenceKpiToneClass(mapProfitabilityKpiTone(tone));
}

export function resolveProfitabilityMaterialLabel(materialId: string | null | undefined): string {
  if (!materialId) return "—";
  return localizeCommercialIntelligenceMaterialLabel(materialId);
}

export function reconcileProfitabilityAssessmentCounts(
  candidate: number,
  comparable: number,
  excluded: number,
): boolean {
  return candidate === comparable + excluded;
}
