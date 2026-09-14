import "./golden-demo-p2-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type GoldenDemoP2PresentationStatusApi = Readonly<{
  productionEventId: string;
  machineCode: string;
  eventStatus: string;
  presentationStepCode: string;
  presentationStepLabel: string;
  stepNumber: number;
  totalSteps: number;
  isComplete: boolean;
  canReset: boolean;
  productionRecoveryGoodParts: number;
}>;

type FetchResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; code: string; message: string }>;

async function goldenDemoP2Fetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<FetchResult<T>> {
  const url = `${context.apiBaseUrl}${path}${path.includes("?") ? "&" : "?"}facilityId=${context.facilityId}`;
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
    return { status: "failure", code: `http-${response.status}`, message };
  }

  return { status: "success", value: json as T };
}

export async function getGoldenDemoP2PresentationStatus(context: ProductionOrderRequestContext) {
  return goldenDemoP2Fetch<GoldenDemoP2PresentationStatusApi>(
    context,
    "/api/golden-demo-p2/presentation-status",
  );
}

export async function resetGoldenDemoPilotEvent(context: ProductionOrderRequestContext, eventId: string) {
  return goldenDemoP2Fetch<{
    productionEventId: string;
    machineCode: string;
    previousStatus: string;
    resetStatus: string;
  }>(context, `/api/golden-demo-p2/events/${eventId}/reset`, { method: "POST" });
}
