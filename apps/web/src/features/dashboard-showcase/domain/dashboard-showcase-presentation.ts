import type { ProcessOeeAssessmentSummary } from "@/features/process-oee/domain/process-oee";
import type {
  ProductionOrderPerformanceRow,
  ProductionShiftReportingRow,
} from "@/features/production-reporting/domain/production-reporting";

import type { ShowcaseSourceData } from "../server/dashboard-showcase-mapper";

const MIN_SPARKLINE_POINTS = 12;
const REQUIRED_HOURLY_POINTS = 24;
const REQUIRED_DISTRIBUTION_GROUPS = 4;

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

function buildHourlyProduction(
  shifts: readonly ProductionShiftReportingRow[],
  plannedQuantity: number | null,
): readonly { hourLabel: string; actual: number | null; target: number | null }[] {
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

function isHourlySeriesComplete(
  hourly: readonly { actual: number | null; target: number | null }[],
): boolean {
  if (hourly.length !== REQUIRED_HOURLY_POINTS) {
    return false;
  }

  return hourly.every((point) => hasNumericValue(point.actual) && hasNumericValue(point.target) && point.actual > 0);
}

function isDistributionComplete(
  orders: readonly ProductionOrderPerformanceRow[],
  goodQuantity: number | null,
): boolean {
  if (!hasNumericValue(goodQuantity) || goodQuantity <= 0) {
    return false;
  }

  const quantityByGroup = new Map<string, number>();
  for (const order of orders) {
    const quantity = order.planActual.goodQuantity;
    if (quantity <= 0) {
      continue;
    }

    const groupKey = order.productReference?.trim() || "ungrouped";
    quantityByGroup.set(groupKey, (quantityByGroup.get(groupKey) ?? 0) + quantity);
  }

  const ranked = [...quantityByGroup.values()].sort((left, right) => right - left);
  if (ranked.length < REQUIRED_DISTRIBUTION_GROUPS) {
    return false;
  }

  const total = ranked.reduce((sum, quantity) => sum + quantity, 0);
  return Math.abs(total - goodQuantity) <= 1;
}

export function isLiveShowcaseSourceComplete(
  source: ShowcaseSourceData,
  assessments: readonly ProcessOeeAssessmentSummary[],
): boolean {
  const overview = source.overview;
  const goodQuantity = overview?.goodQuantity ?? null;
  const plannedQuantity = overview?.plannedQuantity ?? null;
  const completionRate = overview?.completionRatePercent ?? null;
  const scrapRate = source.scrapRatePercent
    ?? (overview && overview.goodQuantity + overview.scrapQuantity > 0
      ? (overview.scrapQuantity / (overview.goodQuantity + overview.scrapQuantity)) * 100
      : null);
  const qualityPercent = source.latestAssessment?.qualityPercent
    ?? source.qualityOverview?.passRatePercent
    ?? source.qualityOverview?.firstPassYieldPercent
    ?? null;

  if (
    !hasNumericValue(goodQuantity)
    || !hasNumericValue(plannedQuantity)
    || !hasNumericValue(completionRate)
    || !hasNumericValue(source.latestOeePercent)
    || !hasNumericValue(scrapRate)
    || !hasNumericValue(source.cycleDurationMs)
    || !hasNumericValue(qualityPercent)
  ) {
    return false;
  }

  const assessment = source.latestAssessment;
  if (
    !assessment
    || !hasNumericValue(assessment.availabilityPercent)
    || !hasNumericValue(assessment.performancePercent)
    || !hasNumericValue(assessment.qualityPercent)
  ) {
    return false;
  }

  const computedOee = (assessment.availabilityPercent * assessment.performancePercent * assessment.qualityPercent)
    / 10_000;
  if (Math.abs(computedOee - source.latestOeePercent) > 1.5) {
    return false;
  }

  const hourly = buildHourlyProduction(source.shifts, plannedQuantity);
  if (!isHourlySeriesComplete(hourly)) {
    return false;
  }

  if (!isDistributionComplete(source.orders, goodQuantity)) {
    return false;
  }

  const shiftSparkline = buildShiftSparkline(source.shifts);
  const oeeSparkline = buildAssessmentSparkline(assessments, (entry) => entry.oeePercent);
  const qualitySparkline = buildAssessmentSparkline(assessments, (entry) => entry.qualityPercent);

  return shiftSparkline.length >= MIN_SPARKLINE_POINTS
    && oeeSparkline.length >= MIN_SPARKLINE_POINTS
    && qualitySparkline.length >= MIN_SPARKLINE_POINTS;
}

export function isDevelopmentShowcaseRuntime(mode: string): boolean {
  return mode === "development-api" || mode === "development-preview";
}
