import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  readFactoryIntelligenceOverview,
  readFactoryIntelligenceRisks,
  type FactoryIntelligenceOverviewApi,
  type FactoryIntelligenceRiskCardApi,
} from "./factory-intelligence-api-client";

export async function loadFactoryIntelligenceWorkspace(): Promise<
  | { status: "success"; overview: FactoryIntelligenceOverviewApi; risks: readonly FactoryIntelligenceRiskCardApi[] }
  | { status: "failure"; message: string }
> {
  const authorization = await denyUnlessGranted("factory-intelligence.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.code === "authorization-denied" ? "Fabrika zekâsı görüntüleme yetkiniz yok." : authorization.message };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const [overviewResult, risksResult] = await Promise.all([
    readFactoryIntelligenceOverview(contextResult.context),
    readFactoryIntelligenceRisks(contextResult.context),
  ]);

  if (overviewResult.status === "failure") {
    return { status: "failure", message: overviewResult.message };
  }

  if (risksResult.status === "failure") {
    return { status: "failure", message: risksResult.message };
  }

  return {
    status: "success",
    overview: overviewResult.value,
    risks: risksResult.value,
  };
}
