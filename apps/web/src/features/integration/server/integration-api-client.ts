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

export type FactoryConnectorOperationsSnapshot = Readonly<{
  connectorId: string;
  name: string;
  connectorKind: string;
  healthState: string;
  healthSummary: string | null;
  lastSuccessfulSyncAtUtc: string | null;
  lastEventReceivedAtUtc: string | null;
  processedToday: number;
  quarantinedToday: number;
  failedToday: number;
  checkpointSummary: string | null;
  lastError: string | null;
}>;

export type FactoryLiveEventMonitorItem = Readonly<{
  canonicalEventId: string;
  inboundEventId: string | null;
  sourceSystem: string;
  eventType: string;
  machineId: string | null;
  orderId: string | null;
  occurredAtUtc: string;
  receivedAtUtc: string;
  processingState: string;
  routedCorrelationKey: string | null;
  routedCaseId: string | null;
}>;

export async function getConnectorOperations(
  context: ProductionOrderRequestContext,
  connectorId: string,
): Promise<FactoryConnectorOperationsSnapshot> {
  const response = await fetch(
    buildUrl(context, `/api/integration/event-spine/connectors/${connectorId}/operations`),
    { headers: context.requestHeaders, cache: "no-store" },
  );
  if (!response.ok) {
    throw new Error("Bağlantı operasyon verisi yüklenemedi.");
  }
  return (await response.json()) as FactoryConnectorOperationsSnapshot;
}

export async function listFactoryLiveEvents(
  context: ProductionOrderRequestContext,
  limit = 50,
): Promise<FactoryLiveEventMonitorItem[]> {
  const response = await fetch(
    buildUrl(context, "/api/integration/event-spine/events", { limit: String(limit) }),
    { headers: context.requestHeaders, cache: "no-store" },
  );
  if (!response.ok) {
    throw new Error("Canlı olay listesi yüklenemedi.");
  }
  return (await response.json()) as FactoryLiveEventMonitorItem[];
}
