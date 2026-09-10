import "./integration-server-boundary";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { listIntegrationConnectors, type IntegrationConnectorSummary } from "./integration-api-client";

export type IntegrationHubResult =
  | Readonly<{ status: "success"; connectors: readonly IntegrationConnectorSummary[] }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readIntegrationHub(): Promise<IntegrationHubResult> {
  const authorization = await denyUnlessGranted("integration.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Entegrasyonları görüntüleme yetkiniz yok." };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
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
