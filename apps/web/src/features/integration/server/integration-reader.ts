import "./integration-server-boundary";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { listIntegrationConnectors, type IntegrationConnectorSummary } from "./integration-api-client";

export type IntegrationHubResult =
  | Readonly<{ status: "success"; connectors: readonly IntegrationConnectorSummary[] }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readIntegrationHub(): Promise<IntegrationHubResult> {
  const authorization = await denyUnlessGranted("integration.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.code === "authorization-denied" ? "Entegrasyonları görüntüleme yetkiniz yok." : authorization.message };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  try {
    const connectors = await listIntegrationConnectors(contextResult.context);
    return { status: "success", connectors };
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Entegrasyon verisi yüklenemedi.",
    };
  }
}
