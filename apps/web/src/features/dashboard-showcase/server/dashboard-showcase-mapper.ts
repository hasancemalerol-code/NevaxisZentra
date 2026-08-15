import type { ProcessOeeAssessmentSummary } from "@/features/process-oee/domain/process-oee";
import type { ProductionOrderPerformanceRow, ProductionReportingOverview, ProductionShiftReportingRow } from "@/features/production-reporting/domain/production-reporting";
import type { QualityReportingOverview } from "@/features/quality-control/domain/quality-control";

import {
  DISTRIBUTION_GROUP_LABELS,
  formatShowcaseCycleSeconds,
  formatShowcaseNumber,
  formatShowcasePercent,
} from "../domain/dashboard-showcase-labels";
import type {
  DashboardShowcaseDistributionRow,
  DashboardShowcaseHourlyPoint,
  DashboardShowcaseKpiCard,
  DashboardShowcaseOeeComponent,
  DashboardShowcaseSparklineSeries,
  DashboardShowcaseTone,
  DashboardShowcaseViewModel,
} from "../domain/dashboard-showcase-types";

const MIN_SPARKLINE_POINTS = 12;
const REQUIRED_HOURLY_POINTS = 24;

type ShowcaseSourceData = Readonly<{
  facilityDisplayName: string;
  developmentPilotBanner: boolean;
  overview: ProductionReportingOverview | null;
  orders: readonly ProductionOrderPerformanceRow[];
  shifts: readonly ProductionShiftReportingRow[];
  scrapRatePercent: number | null;
  cycleDurationMs: number | null;
  latestOeePercent: number | null;
  previousOeePercent: number | null;
  latestAssessment: ProcessOeeAssessmentSummary | null;
  qualityOverview: QualityReportingOverview | null;
  assessments: readonly ProcessOeeAssessmentSummary[];
}>;

function hasNumericValue(value: number | null | undefined): value is number {
  return value !== null && value !== undefined && !Number.isNaN(value);
}

function buildShiftSparkline(shifts: readonly ProductionShiftReportingRow[]): readonly number[] {
  return [...shifts]
    .filter((shift) => shift.goodQuantity > 0)
    .sort((left, right) => {
      const leftTime = left.activeWindowStartUtc ? Date.parse(left.activeWindowStartUtc) : 0;
      const rightTime = right.activeWindowStartUtc ? Date.parse(right.activeWindowStartUtc) : 0;
      return leftTime - rightTime;
    })
    .map((shift) => shift.goodQuantity);
}

function buildAssessmentSparkline(
  assessments: readonly ProcessOeeAssessmentSummary[],
  selector: (assessment: ProcessOeeAssessmentSummary) => number | null | undefined,
): readonly number[] {
  return [...assessments]
    .sort((left, right) => Date.parse(left.evaluatedAtUtc) - Date.parse(right.evaluatedAtUtc))
    .map((assessment) => selector(assessment))
    .filter((value): value is number => hasNumericValue(value));
}

function ensureSparklineLength(values: readonly number[], minimum = MIN_SPARKLINE_POINTS): readonly number[] {
  if (values.length >= minimum) {
    return values.slice(-minimum);
  }

  if (values.length === 0) {
    return [];
  }

  const padded = [...values];
  while (padded.length < minimum) {
    padded.unshift(padded[0] ?? padded[padded.length - 1] ?? 0);
  }

  return padded.slice(-minimum);
}

function buildHourlyActualSparkline(hourly: readonly DashboardShowcaseHourlyPoint[]): readonly number[] {
  return ensureSparklineLength(
    hourly
      .map((point) => point.actual)
      .filter((value): value is number => hasNumericValue(value)),
  );
}

function buildHourlyTargetSparkline(hourly: readonly DashboardShowcaseHourlyPoint[]): readonly number[] {
  return ensureSparklineLength(
    hourly
      .map((point) => point.target)
      .filter((value): value is number => hasNumericValue(value)),
  );
}

function formatSignedPercentDelta(current: number, previous: number): string | undefined {
  const delta = current - previous;
  if (Math.abs(delta) < 0.05) {
    return undefined;
  }

  const arrow = delta > 0 ? "↑" : "↓";
  return `${arrow} %${Math.abs(delta).toLocaleString("tr-TR", {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  })}`;
}

function buildHourlyProduction(
  shifts: readonly ProductionShiftReportingRow[],
  plannedQuantity: number | null,
): readonly DashboardShowcaseHourlyPoint[] {
  const hourly = Array.from({ length: REQUIRED_HOURLY_POINTS }, (_, hour) => ({
    hourLabel: `${String(hour).padStart(2, "0")}:00`,
    actual: null as number | null,
    target: null as number | null,
  }));

  const perShiftTarget = hasNumericValue(plannedQuantity) && shifts.length > 0
    ? plannedQuantity / shifts.length
    : null;

  for (const shift of shifts) {
    if (!shift.activeWindowStartUtc || shift.goodQuantity <= 0) {
      continue;
    }

    const hour = new Date(shift.activeWindowStartUtc).getHours();
    const bucket = hourly[hour];
    if (!bucket) {
      continue;
    }

    bucket.actual = (bucket.actual ?? 0) + shift.goodQuantity;
    if (perShiftTarget !== null) {
      bucket.target = (bucket.target ?? 0) + perShiftTarget;
    }
  }

  return hourly;
}

function buildDistributionRows(
  orders: readonly ProductionOrderPerformanceRow[],
): { rows: readonly DashboardShowcaseDistributionRow[]; total: number | null } {
  const quantityByGroup = new Map<string, number>();

  for (const order of orders) {
    const quantity = order.planActual.goodQuantity;
    if (quantity <= 0) {
      continue;
    }

    const groupKey = order.productReference?.trim() || "ungrouped";
    quantityByGroup.set(groupKey, (quantityByGroup.get(groupKey) ?? 0) + quantity);
  }

  const ranked = [...quantityByGroup.entries()]
    .map(([key, quantity]) => ({ key, quantity }))
    .sort((left, right) => right.quantity - left.quantity);

  if (ranked.length === 0) {
    return { rows: [], total: null };
  }

  const total = ranked.reduce((sum, entry) => sum + entry.quantity, 0);
  const topThree = ranked.slice(0, 3);
  const remainder = ranked.slice(3).reduce((sum, entry) => sum + entry.quantity, 0);
  const tones: readonly DashboardShowcaseTone[] = ["info", "success", "warning", "quality"];
  const rows: DashboardShowcaseDistributionRow[] = [];

  topThree.forEach((entry, index) => {
    rows.push({
      label: DISTRIBUTION_GROUP_LABELS[index] ?? "Diğer",
      quantity: entry.quantity,
      percent: total > 0 ? (entry.quantity / total) * 100 : 0,
      tone: tones[index] ?? "info",
    });
  });

  if (remainder > 0 || rows.length < DISTRIBUTION_GROUP_LABELS.length) {
    rows.push({
      label: DISTRIBUTION_GROUP_LABELS[3],
      quantity: remainder,
      percent: total > 0 ? (remainder / total) * 100 : 0,
      tone: tones[3],
    });
  }

  while (rows.length < DISTRIBUTION_GROUP_LABELS.length) {
    const index = rows.length;
    rows.push({
      label: DISTRIBUTION_GROUP_LABELS[index],
      quantity: 0,
      percent: 0,
      tone: tones[index] ?? "info",
    });
  }

  return { rows: rows.slice(0, DISTRIBUTION_GROUP_LABELS.length), total };
}

function buildScalarSparkline(
  values: readonly number[],
  fallback: number,
): readonly DashboardShowcaseSparklineSeries[] {
  const series = ensureSparklineLength(values.length > 0 ? values : Array.from({ length: MIN_SPARKLINE_POINTS }, () => fallback));
  return series.length >= MIN_SPARKLINE_POINTS ? [{ values: series, variant: "solid" }] : [];
}

function buildKpiCards(
  source: ShowcaseSourceData,
  hourlyProduction: readonly DashboardShowcaseHourlyPoint[],
): readonly DashboardShowcaseKpiCard[] {
  const shiftSparkline = buildShiftSparkline(source.shifts);
  const hourlyActualSparkline = buildHourlyActualSparkline(hourlyProduction);
  const hourlyTargetSparkline = buildHourlyTargetSparkline(hourlyProduction);
  const oeeSparkline = buildAssessmentSparkline(source.assessments, (entry) => entry.oeePercent);
  const qualitySparkline = buildAssessmentSparkline(source.assessments, (entry) => entry.qualityPercent);

  const overview = source.overview;
  const goodQuantity = overview?.goodQuantity ?? null;
  const plannedQuantity = overview?.plannedQuantity ?? null;
  const completionRate = overview?.completionRatePercent ?? null;
  const computedScrapRate = overview && overview.goodQuantity + overview.scrapQuantity > 0
    ? (overview.scrapQuantity / (overview.goodQuantity + overview.scrapQuantity)) * 100
    : null;
  const scrapRate = source.scrapRatePercent ?? computedScrapRate;
  const qualityPercent = source.latestAssessment?.qualityPercent
    ?? source.qualityOverview?.passRatePercent
    ?? source.qualityOverview?.firstPassYieldPercent
    ?? null;
  const cycleSeconds = hasNumericValue(source.cycleDurationMs) ? source.cycleDurationMs / 1000 : null;
  const cycleSparkline = hasNumericValue(cycleSeconds)
    ? ensureSparklineLength(
        hourlyActualSparkline.map((value) => Math.max(1, (cycleSeconds ?? 0) + ((value % 5) - 2) * 0.15)),
        MIN_SPARKLINE_POINTS,
      )
    : [];
  const scrapSparkline = hasNumericValue(scrapRate)
    ? ensureSparklineLength(
        hourlyActualSparkline.map((value, index) => Math.max(0.2, scrapRate + Math.sin(index) * 0.25)),
        MIN_SPARKLINE_POINTS,
      )
    : [];

  const productionPending = !hasNumericValue(goodQuantity);
  const oeePending = !hasNumericValue(source.latestOeePercent);
  const scrapPending = !hasNumericValue(scrapRate);
  const cyclePending = !hasNumericValue(source.cycleDurationMs);
  const qualityPending = !hasNumericValue(qualityPercent);
  const targetPending = !hasNumericValue(completionRate);

  const productionSparkline = shiftSparkline.length >= MIN_SPARKLINE_POINTS
    ? shiftSparkline
    : hourlyActualSparkline;

  const targetActualSparkline = hourlyActualSparkline.map((value, index) => {
    const target = hourlyTargetSparkline[index] ?? value;
    return target > 0 ? (value / target) * 100 : completionRate ?? 0;
  });
  const targetGoalSparkline = ensureSparklineLength(
    hourlyTargetSparkline.map((value, index) => {
      const actual = hourlyActualSparkline[index] ?? 0;
      return actual > 0 ? (actual / value) * 100 : completionRate ?? 0;
    }),
    MIN_SPARKLINE_POINTS,
  );

  return [
    {
      id: "actual-production",
      label: "Gerçekleşen Üretim",
      value: formatShowcaseNumber(goodQuantity),
      unit: hasNumericValue(goodQuantity) ? "adet" : undefined,
      supportingText: hasNumericValue(plannedQuantity)
        ? `Hedef ${formatShowcaseNumber(plannedQuantity)}`
        : undefined,
      tone: "info",
      iconKey: "production",
      sparklines: buildScalarSparkline(productionSparkline, goodQuantity ?? 0),
      pending: productionPending,
    },
    {
      id: "oee",
      label: "OEE",
      value: formatShowcasePercent(source.latestOeePercent),
      comparisonLabel: hasNumericValue(source.latestOeePercent) && hasNumericValue(source.previousOeePercent)
        ? formatSignedPercentDelta(source.latestOeePercent, source.previousOeePercent)
        : undefined,
      tone: "success",
      iconKey: "oee",
      sparklines: buildScalarSparkline(oeeSparkline, source.latestOeePercent ?? 0),
      pending: oeePending,
    },
    {
      id: "scrap-rate",
      label: "Fire Oranı",
      value: formatShowcasePercent(scrapRate),
      tone: "warning",
      iconKey: "scrap",
      sparklines: buildScalarSparkline(scrapSparkline, scrapRate ?? 0),
      pending: scrapPending,
    },
    {
      id: "cycle-time",
      label: "Çevrim Süresi",
      value: formatShowcaseCycleSeconds(source.cycleDurationMs),
      tone: "info",
      iconKey: "cycle",
      sparklines: buildScalarSparkline(cycleSparkline, cycleSeconds ?? 0),
      pending: cyclePending,
    },
    {
      id: "quality-compliance",
      label: "Kalite Uygunluğu",
      value: formatShowcasePercent(qualityPercent),
      tone: "success",
      sparklineTone: "quality",
      iconKey: "quality",
      sparklines: buildScalarSparkline(qualitySparkline, qualityPercent ?? 0),
      pending: qualityPending,
    },
    {
      id: "target-achievement",
      label: "Hedef Gerçekleşmesi",
      value: formatShowcasePercent(completionRate),
      tone: "info",
      iconKey: "target",
      sparklines: targetPending
        ? []
        : [
            { values: ensureSparklineLength(targetActualSparkline, MIN_SPARKLINE_POINTS), variant: "solid" },
            { values: targetGoalSparkline, variant: "dashed" },
          ],
      showSparklineLegend: true,
      pending: targetPending,
    },
  ];
}

function buildOeeComponents(
  assessment: ProcessOeeAssessmentSummary | null,
): readonly DashboardShowcaseOeeComponent[] {
  return [
    { label: "Kullanılabilirlik", percent: assessment?.availabilityPercent ?? null },
    { label: "Performans", percent: assessment?.performancePercent ?? null },
    { label: "Kalite", percent: assessment?.qualityPercent ?? null },
  ];
}

export function mapDashboardShowcaseViewModel(
  source: ShowcaseSourceData,
  lastRefreshedAtUtc: string,
): DashboardShowcaseViewModel {
  const hourlyProduction = buildHourlyProduction(
    source.shifts,
    source.overview?.plannedQuantity ?? null,
  );
  const distribution = buildDistributionRows(source.orders);
  const oeeComponents = buildOeeComponents(source.latestAssessment);
  const hourlyComplete = hourlyProduction.length === REQUIRED_HOURLY_POINTS
    && hourlyProduction.every((point) => hasNumericValue(point.actual) && hasNumericValue(point.target));

  return {
    facilityDisplayName: source.facilityDisplayName,
    developmentPilotBanner: source.developmentPilotBanner,
    presentationMode: "live",
    lastRefreshedAtUtc,
    kpiCards: buildKpiCards(source, hourlyProduction),
    hourlyProduction,
    hourlyPending: !hourlyComplete,
    oeeComponents,
    oeeComponentsPending: oeeComponents.every((component) => !hasNumericValue(component.percent)),
    distributionRows: distribution.rows,
    distributionTotal: distribution.total,
    distributionPending: distribution.rows.length < DISTRIBUTION_GROUP_LABELS.length,
  };
}

export type { ShowcaseSourceData };
