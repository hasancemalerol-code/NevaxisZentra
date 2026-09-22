import "server-only";

import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";
import { getProductionReportingOverview } from "@/features/production-reporting/server/production-reporting-api-client";
import { readProductionResourceWorkspace } from "@/features/production-resources/server/production-resources-api-client";
import { getProductionResourceTypeLabel } from "@/features/production-resources/domain/production-resources-labels";

export type AppsOperationalOverviewMetric = Readonly<{
  label: string;
  value: string;
}>;

export type AppsOperationalResourceBar = Readonly<{
  resourceType: string;
  label: string;
  count: number;
}>;

export type AppsOperationalOverviewModel = Readonly<{
  state: "empty" | "ready";
  metrics: readonly AppsOperationalOverviewMetric[];
  resourceDistribution: readonly AppsOperationalResourceBar[];
  riskHighlights: readonly string[];
}>;

const EMPTY_OVERVIEW: AppsOperationalOverviewModel = {
  state: "empty",
  metrics: [],
  resourceDistribution: [],
  riskHighlights: [],
};

function buildResourceDistribution(
  resources: readonly { resourceType: string }[],
): readonly AppsOperationalResourceBar[] {
  const counts = new Map<string, number>();
  for (const row of resources) {
    counts.set(row.resourceType, (counts.get(row.resourceType) ?? 0) + 1);
  }

  return [...counts.entries()]
    .map(([resourceType, count]) => ({
      resourceType,
      label: getProductionResourceTypeLabel(resourceType),
      count,
    }))
    .sort((left, right) => right.count - left.count);
}

export async function readAppsOperationalOverview(): Promise<AppsOperationalOverviewModel> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return EMPTY_OVERVIEW;
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return EMPTY_OVERVIEW;
  }

  const context = contextResult.context;
  const [overviewResult, resourcesResult] = await Promise.all([
    getProductionReportingOverview(context),
    readProductionResourceWorkspace(context),
  ]);

  const overview = overviewResult.status === "success" ? overviewResult.value : null;
  const resources =
    resourcesResult.status === "success" ? resourcesResult.value.resources : [];

  const resourceDistribution = buildResourceDistribution(resources);
  const hasProductionSignal =
    (overview?.goodQuantity ?? 0) > 0
    || (overview?.activeOrderCount ?? 0) > 0
    || resources.length > 0;

  if (!hasProductionSignal) {
    return EMPTY_OVERVIEW;
  }

  const metrics: AppsOperationalOverviewMetric[] = [];
  if (overview) {
    metrics.push(
      { label: "Aktif sipariş", value: String(overview.activeOrderCount) },
      { label: "İyi çıktı", value: String(overview.goodQuantity) },
      { label: "Açık istisna", value: String(overview.openExceptionCount) },
      { label: "Aktif duruş", value: String(overview.activeDowntimeCount) },
    );
  } else if (resources.length > 0) {
    const running = resources.filter((row) => row.state.toLowerCase() === "running").length;
    metrics.push(
      { label: "Kaynak", value: String(resources.length) },
      { label: "Çalışan", value: String(running) },
    );
  }

  const riskHighlights =
    overview?.riskSignals
      ?.filter((signal) => signal.severity === "high" || signal.severity === "critical")
      .map((signal) => signal.label)
      .slice(0, 3) ?? [];

  return {
    state: "ready",
    metrics,
    resourceDistribution,
    riskHighlights,
  };
}
