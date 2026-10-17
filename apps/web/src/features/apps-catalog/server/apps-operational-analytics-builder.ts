import { FACILITY_TIME_ZONE } from "@/lib/presentation/format";
import type { ProductionReportingOverview, ProductionShiftReportingRow } from "@/features/production-reporting/domain/production-reporting";
import type { ProductionResourceWorkspaceApi } from "@/features/production-resources/server/production-resources-api-client";
import {
  getProductionResourceTypeLabel,
  PRODUCTION_RESOURCE_STATE_FILTER_OPTIONS,
} from "@/features/production-resources/domain/production-resources-labels";

import type {
  AppsOperationalAnalyticsModel,
  AppsOperationalKpi,
  AppsOperationalQuantityBar,
  AppsOperationalTrendPoint,
} from "../domain/apps-operational-analytics-types";
import { EMPTY_APPS_OPERATIONAL_ANALYTICS } from "../domain/apps-operational-analytics-types";

type BuildAnalyticsInput = Readonly<{
  overview: ProductionReportingOverview | null;
  shifts: readonly ProductionShiftReportingRow[];
  resources: ProductionResourceWorkspaceApi["resources"];
  scrapRatePercent: number | null;
  scrapQuantity: number | null;
  goodQuantityFromScrap: number | null;
  downtimeActiveCount: number | null;
  downtimeTotalMinutes: number | null;
}>;

function buildShiftTrendPoints(
  shifts: readonly ProductionShiftReportingRow[],
  plannedQuantity: number | null,
): readonly AppsOperationalTrendPoint[] {
  const sorted = [...shifts]
    .filter((shift) => shift.goodQuantity > 0 && shift.activeWindowStartUtc)
    .sort(
      (left, right) =>
        Date.parse(left.activeWindowStartUtc ?? "") - Date.parse(right.activeWindowStartUtc ?? ""),
    );

  if (sorted.length === 0) {
    return [];
  }

  const perShiftTarget =
    plannedQuantity !== null && plannedQuantity > 0 && sorted.length > 0
      ? plannedQuantity / sorted.length
      : null;

  return sorted.map((shift) => {
    const start = shift.activeWindowStartUtc ? new Date(shift.activeWindowStartUtc) : null;
    const label = start
      ? start.toLocaleString("tr-TR", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit", timeZone: FACILITY_TIME_ZONE })
      : shift.shiftCode;

    return {
      label,
      goodQuantity: shift.goodQuantity,
      targetQuantity: perShiftTarget,
    };
  });
}

function buildResourceTypeBars(
  resources: ProductionResourceWorkspaceApi["resources"],
): readonly AppsOperationalQuantityBar[] {
  const counts = new Map<string, number>();
  for (const row of resources) {
    counts.set(row.resourceType, (counts.get(row.resourceType) ?? 0) + 1);
  }

  return [...counts.entries()]
    .map(([resourceType, count]) => ({
      label: getProductionResourceTypeLabel(resourceType),
      count,
    }))
    .sort((left, right) => right.count - left.count);
}

function buildResourceStateBars(
  resources: ProductionResourceWorkspaceApi["resources"],
): readonly AppsOperationalQuantityBar[] {
  const counts = new Map<string, number>();
  for (const row of resources) {
    const key = row.state.toLowerCase();
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }

  const stateLabels = new Map(
    PRODUCTION_RESOURCE_STATE_FILTER_OPTIONS.filter((option) => option.value).map((option) => [
      option.value,
      option.label,
    ]),
  );

  return [...counts.entries()]
    .map(([state, count]) => ({
      label: stateLabels.get(state as (typeof PRODUCTION_RESOURCE_STATE_FILTER_OPTIONS)[number]["value"]) ?? state,
      count,
    }))
    .sort((left, right) => right.count - left.count);
}

function buildKpis(overview: ProductionReportingOverview | null, resourceCount: number): readonly AppsOperationalKpi[] {
  if (!overview) {
    if (resourceCount <= 0) {
      return [];
    }

    return [{ label: "Kaynak", value: String(resourceCount) }];
  }

  return [
    { label: "Aktif sipariş", value: String(overview.activeOrderCount) },
    { label: "İyi çıktı", value: String(overview.goodQuantity) },
    { label: "Açık istisna", value: String(overview.openExceptionCount) },
    { label: "Aktif duruş", value: String(overview.activeDowntimeCount) },
  ];
}

export function buildAppsOperationalAnalytics(input: BuildAnalyticsInput): AppsOperationalAnalyticsModel {
  const resources = input.resources;
  const overview = input.overview;

  const hasProductionSignal =
    (overview?.goodQuantity ?? 0) > 0
    || (overview?.activeOrderCount ?? 0) > 0
    || resources.length > 0
    || input.shifts.some((shift) => shift.goodQuantity > 0);

  if (!hasProductionSignal) {
    return EMPTY_APPS_OPERATIONAL_ANALYTICS;
  }

  const outputTrend = buildShiftTrendPoints(input.shifts, overview?.plannedQuantity ?? null);
  const resourceTypeBars = buildResourceTypeBars(resources);
  const resourceStateBars = buildResourceStateBars(resources);

  const riskHighlights =
    overview?.riskSignals
      ?.filter((signal) => signal.severity === "high" || signal.severity === "critical")
      .map((signal) => signal.label)
      .slice(0, 4) ?? [];

  const goodFromOverview = overview?.goodQuantity ?? 0;
  const scrapFromOverview = overview?.scrapQuantity ?? 0;
  const scrapGood = input.goodQuantityFromScrap ?? goodFromOverview;
  const scrapQty = input.scrapQuantity ?? scrapFromOverview;
  const scrapRate =
    input.scrapRatePercent
    ?? (scrapGood + scrapQty > 0 ? (scrapQty / (scrapGood + scrapQty)) * 100 : null);

  const scrapSummary =
    scrapGood + scrapQty > 0
      ? {
          goodQuantity: scrapGood,
          scrapQuantity: scrapQty,
          scrapRatePercent: scrapRate,
        }
      : null;

  const downtimeSummary =
    input.downtimeActiveCount !== null || input.downtimeTotalMinutes !== null
      ? {
          activeCount: input.downtimeActiveCount ?? overview?.activeDowntimeCount ?? 0,
          totalMinutes: input.downtimeTotalMinutes ?? overview?.totalDowntimeMinutes ?? 0,
        }
      : overview
        ? {
            activeCount: overview.activeDowntimeCount,
            totalMinutes: overview.totalDowntimeMinutes,
          }
        : null;

  return {
    state: "ready",
    kpis: buildKpis(overview, resources.length),
    outputTrend,
    resourceTypeBars,
    resourceStateBars,
    scrapSummary,
    downtimeSummary,
    riskHighlights,
  };
}
