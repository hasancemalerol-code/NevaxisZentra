import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type RealFactoryPilotWorkspace = Readonly<{
  pilotRunId: string;
  lineId: string;
  lineCode: string;
  currentPhase: string;
  acceptanceKind: string;
  connectors: ReadonlyArray<{
    connectorId: string;
    name: string;
    connectorKind: string;
    sourceSystem: string;
    healthStatus: string;
    mappingStatus: string;
    lastEventReceivedAtUtc: string | null;
    eventsIngested: number;
  }>;
  dataQuality: {
    overallState: string;
    blocksAutonomousAction: boolean;
    issues: string[];
  } | null;
  shadow: {
    mode: string;
    isActive: boolean;
    blocksPhysicalActions: boolean;
  } | null;
  activeCaseId: string | null;
  evidencePack: {
    verificationOutcome: string;
    knowledgePromoted: boolean;
  } | null;
  updatedAtUtc: string;
}>;

async function pilotFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<{ status: "success"; value: T } | { status: "failure"; code: string; message: string }> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);

  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...context.requestHeaders,
      ...(init?.headers ?? {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => ({}))) as { message?: string; code?: string };
    return {
      status: "failure",
      code: body.code ?? `http-${response.status}`,
      message: body.message ?? `Real factory pilot API failed (${response.status})`,
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function initializeRealFactoryPilot(
  context: ProductionOrderRequestContext,
): Promise<
  { status: "success"; value: RealFactoryPilotWorkspace } | { status: "failure"; code: string; message: string }
> {
  return pilotFetch<RealFactoryPilotWorkspace>(context, "/api/real-factory-pilot/initialize", { method: "POST" });
}

export async function readRealFactoryPilotWorkspace(
  context: ProductionOrderRequestContext,
  pilotRunId: string,
): Promise<
  { status: "success"; value: RealFactoryPilotWorkspace } | { status: "failure"; code: string; message: string }
> {
  return pilotFetch<RealFactoryPilotWorkspace>(
    context,
    `/api/real-factory-pilot/runs/${pilotRunId}/workspace`,
  );
}
