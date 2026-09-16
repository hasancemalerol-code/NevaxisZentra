import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type FactoryIntelligenceOverviewApi = Readonly<{
  activePredictions: number;
  watchCount: number;
  warningCount: number;
  criticalCount: number;
  avertedLast24h: number;
  confirmedLast24h: number;
  falseAlarmsLast24h: number;
  averageLeadTimeMinutes: number;
  dataQualitySummary: string;
}>;

export type FactoryIntelligenceRiskCardApi = Readonly<{
  riskId: string;
  linkedCaseId?: string;
  riskType: string;
  riskState: string;
  machineCode: string;
  orderReference?: string;
  observedSummary: string;
  predictedSummary: string;
  timeToImpactLabel: string;
  confidenceBand: string;
  horizon: string;
  evidence: readonly string[];
  recommendedActions: readonly string[];
  dataQuality: string;
  generatedAtUtc: string;
}>;

async function factoryIntelligenceFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
): Promise<{ status: "success"; value: T } | { status: "failure"; code: string; message: string }> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);

  const response = await fetch(url, {
    headers: {
      Accept: "application/json",
      ...context.requestHeaders,
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    return {
      status: "failure",
      code: body.code ?? `http-${response.status}`,
      message: body.message ?? body.title ?? "İstek başarısız.",
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readFactoryIntelligenceOverview(
  context: ProductionOrderRequestContext,
): Promise<{ status: "success"; value: FactoryIntelligenceOverviewApi } | { status: "failure"; code: string; message: string }> {
  return factoryIntelligenceFetch<FactoryIntelligenceOverviewApi>(context, "/api/factory-intelligence/overview");
}

export async function readFactoryIntelligenceRisks(
  context: ProductionOrderRequestContext,
): Promise<{ status: "success"; value: readonly FactoryIntelligenceRiskCardApi[] } | { status: "failure"; code: string; message: string }> {
  return factoryIntelligenceFetch<readonly FactoryIntelligenceRiskCardApi[]>(context, "/api/factory-intelligence/risks");
}
