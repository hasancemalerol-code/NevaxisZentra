import "./integration-server-boundary";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  getConnectorOperations,
  listFactoryLiveEvents,
  listIntegrationConnectors,
  type FactoryConnectorOperationsSnapshot,
  type FactoryLiveEventMonitorItem,
  type IntegrationConnectorSummary,
} from "./integration-api-client";

export type IntegrationOperationsCenterResult =
  | Readonly<{
      status: "success";
      connectors: readonly IntegrationConnectorSummary[];
      operations: readonly FactoryConnectorOperationsSnapshot[];
      events: readonly FactoryLiveEventMonitorItem[];
    }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readIntegrationOperationsCenter(): Promise<IntegrationOperationsCenterResult> {
  const authorization = await denyUnlessGranted("integration.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Entegrasyon merkezini görüntüleme yetkiniz yok." };
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
    const operations = await Promise.all(
      connectors.slice(0, 10).map((connector) =>
        getConnectorOperations(contextResult.context, connector.id).catch(() => null),
      ),
    );
    const events = await listFactoryLiveEvents(contextResult.context, 30);
    return {
      status: "success",
      connectors,
      operations: operations.filter((item): item is FactoryConnectorOperationsSnapshot => item !== null),
      events,
    };
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Entegrasyon merkezi yüklenemedi.",
    };
  }
}
