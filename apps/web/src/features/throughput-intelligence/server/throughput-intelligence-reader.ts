import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  readScenarioComparison,
  readThroughputIntelligenceOverview,
  type ScenarioComparisonRowApi,
  type ThroughputIntelligenceOverviewApi,
} from "./throughput-intelligence-api-client";

async function resolveContext() {
  const authorization = await denyUnlessGranted("throughput-intelligence.read");
  if (authorization.status === "failure") {
    return { status: "failure" as const, message: "Verim analizi görüntüleme yetkiniz yok." };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function loadThroughputIntelligenceWorkspace(lineId: string): Promise<
  | {
      status: "success";
      overview: ThroughputIntelligenceOverviewApi;
      comparison: readonly ScenarioComparisonRowApi[];
    }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const overviewResult = await readThroughputIntelligenceOverview(resolved.context, lineId);
  if (overviewResult.status === "failure") {
    return { status: "failure", message: overviewResult.message };
  }

  const comparisonResult = await readScenarioComparison(resolved.context, lineId);
  const comparison = comparisonResult.status === "success" ? comparisonResult.value : [];

  return {
    status: "success",
    overview: overviewResult.value,
    comparison,
  };
}
