import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type ThroughputMeasurementApi = Readonly<{
  goodUnitsPerHour: number;
  grossUnitsPerHour: number;
  rejectUnitsPerHour: number;
  reworkUnitsPerHour: number;
  targetGoodUnitsPerHour?: number;
  throughputGapPerHour?: number;
  goodCount: number;
  dataQuality: string;
  windowLabel: string;
}>;

export type ConstraintResultApi = Readonly<{
  role: string;
  stationCode?: string;
  stationName?: string;
  constraintType: string;
  impactOnGoodThroughputPerHour: number;
  explanation: string;
  dataQuality: string;
}>;

export type LossTreeNodeApi = Readonly<{
  category: string;
  label: string;
  lostGoodUnitsPerHour: number;
  attributionQuality: string;
  stationCode?: string;
}>;

export type ThroughputTopologyStationApi = Readonly<{
  stationId: string;
  code: string;
  name: string;
  state: string;
  effectiveCycleSeconds?: number;
  effectiveGoodCapacityPerHour?: number;
  wip?: number;
  operatorCode?: string;
  goodCount: number;
  rejectCount: number;
  isCurrentConstraint: boolean;
  isSecondaryConstraint: boolean;
  constraintType?: string;
}>;

export type ThroughputIntelligenceOverviewApi = Readonly<{
  lineId: string;
  lineCode: string;
  lineName: string;
  measurement: ThroughputMeasurementApi;
  productionGap: Readonly<{
    requiredGoodPerHour: number;
    currentGoodPerHour: number;
    gapPerHour: number;
    attributionQuality: string;
  }>;
  constraints: readonly ConstraintResultApi[];
  lossTree: Readonly<{
    nodes: readonly LossTreeNodeApi[];
    totalGapPerHour: number;
    attributedLossPerHour: number;
    reconciliationQuality: string;
  }>;
  topology: readonly ThroughputTopologyStationApi[];
  dataQuality: string;
  analysisDurationMs: number;
  eventsConsidered: number;
}>;

export type ScenarioComparisonRowApi = Readonly<{
  scenarioCode: string;
  scenarioType: string;
  title: string;
  predictedGoodThroughputPerHour: number;
  deltaGoodThroughputPerHour: number;
  constraintBeforeCode?: string;
  constraintAfterCode?: string;
  eligibility: string;
  dataQuality: string;
}>;

async function throughputFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Readonly<Record<string, string>>,
): Promise<{ status: "success"; value: T } | { status: "failure"; code: string; message: string }> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      url.searchParams.set(key, value);
    }
  }

  const response = await fetch(url, {
    headers: {
      Accept: "application/json",
      ...context.requestHeaders,
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => ({}))) as { message?: string; code?: string };
    return {
      status: "failure",
      code: body.code ?? `http-${response.status}`,
      message: body.message ?? `Throughput API failed (${response.status})`,
    };
  }

  const value = (await response.json()) as T;
  return { status: "success", value };
}

export async function readThroughputIntelligenceOverview(
  context: ProductionOrderRequestContext,
  lineId: string,
  window = "recent",
): Promise<
  { status: "success"; value: ThroughputIntelligenceOverviewApi } | { status: "failure"; code: string; message: string }
> {
  return throughputFetch<ThroughputIntelligenceOverviewApi>(
    context,
    `/api/throughput-intelligence/lines/${lineId}/overview`,
    { window },
  );
}

export async function readScenarioComparison(
  context: ProductionOrderRequestContext,
  lineId: string,
): Promise<
  { status: "success"; value: readonly ScenarioComparisonRowApi[] } | { status: "failure"; code: string; message: string }
> {
  return throughputFetch<readonly ScenarioComparisonRowApi[]>(
    context,
    `/api/throughput-intelligence/lines/${lineId}/scenarios/compare`,
  );
}
