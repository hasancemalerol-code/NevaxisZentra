import "./enterprise-reliability-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type ComponentHealthApi = Readonly<{
  componentKind: string;
  componentKey: string;
  healthStatus: string;
  detail?: string;
  observedAtUtc?: string;
  lastHealthyAtUtc?: string;
  lastFailureAtUtc?: string;
}>;

export type IntegrationHealthApi = Readonly<{
  connectorId: string;
  name: string;
  connectorKind: string;
  healthStatus: string;
  freshnessState: string;
  retryState: string;
  circuitState: string;
  consecutiveFailures: number;
  lastSuccessfulSyncAtUtc?: string;
  lastFailureAtUtc?: string;
  failureStatus?: string;
  uiStatusLabel: string;
}>;

export type EdgeAgentHealthApi = Readonly<{
  id: string;
  agentKey: string;
  displayName: string;
  facilityId: string;
  connectivityStatus: string;
  bufferBacklogCount: number;
  bufferCapacity: number;
  agentVersion: string;
  lastHeartbeatAtUtc?: string;
  lastSyncAtUtc?: string;
}>;

export type TechnicalAlertApi = Readonly<{
  id: string;
  alertKind: string;
  severity: string;
  title: string;
  status: string;
  openedAtUtc: string;
}>;

export type DependencyHealthApi = Readonly<{
  database: string;
  messageBroker: string;
  aiProvider: string;
  backgroundJobs: string;
}>;

export type SystemStatusWorkspaceApi = Readonly<{
  operatingMode: string;
  applicationVersion: string;
  buildSha: string;
  generatedAtUtc: string;
  showsFalseHealth: boolean;
  components: readonly ComponentHealthApi[];
  integrations: readonly IntegrationHealthApi[];
  edgeAgents: readonly EdgeAgentHealthApi[];
  alerts: readonly TechnicalAlertApi[];
  dependencies: DependencyHealthApi;
}>;

type ApiResult<T> =
  | { status: "success"; value: T }
  | { status: "failure"; code: string; message: string };

async function enterpriseReliabilityFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<ApiResult<T>> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);

  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...context.requestHeaders,
      ...init?.headers,
    },
    cache: "no-store",
  });

  const text = await response.text();
  let json: unknown;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    return { status: "failure", code: "invalid-json", message: "API yanıtı okunamadı." };
  }

  if (!response.ok) {
    const message =
      typeof json === "object" && json !== null && "message" in json
        ? String((json as { message: string }).message)
        : `HTTP ${response.status}`;
    const code =
      typeof json === "object" && json !== null && "code" in json
        ? String((json as { code: string }).code)
        : `http-${response.status}`;
    return { status: "failure", code, message };
  }

  return { status: "success", value: json as T };
}

export async function getSystemStatusWorkspace(context: ProductionOrderRequestContext) {
  return enterpriseReliabilityFetch<SystemStatusWorkspaceApi>(context, "/api/system-status/workspace");
}
