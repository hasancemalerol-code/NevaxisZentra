import "./integration-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type IntegrationConnectorSummary = Readonly<{
  id: string;
  facilityId: string;
  name: string;
  connectorKind: string;
  sourceSystemType: string;
  sourceSystemName: string;
  environment: string;
  status: string;
  enabled: boolean;
  healthStatus: string;
  freshnessState: string;
  qualityState: string;
  version: number;
}>;

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value) {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

export async function listIntegrationConnectors(
  context: ProductionOrderRequestContext,
): Promise<IntegrationConnectorSummary[]> {
  const response = await fetch(buildUrl(context, "/api/integration/connectors"), {
    headers: context.requestHeaders,
    cache: "no-store",
  });
  if (!response.ok) {
    throw new Error("Entegrasyon bağlantıları yüklenemedi.");
  }
  return (await response.json()) as IntegrationConnectorSummary[];
}
