import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type MixedProductionLineSummaryApi = Readonly<{
  lineId: string;
  code: string;
  name: string;
  currentOrderReference?: string;
  currentProductCode?: string;
  currentShiftCode?: string;
  lineState: string;
  goodOutputToday: number;
  rejectOutputToday: number;
}>;

export type MixedProductionStationSnapshotApi = Readonly<{
  stationId: string;
  code: string;
  name: string;
  stationKind: string;
  sequenceOrder: number;
  state: string;
  resourceCode?: string;
  resourceType?: string;
  operationCode?: string;
  operatorCode?: string;
  bufferQuantity?: number;
  bufferCapacity?: number;
  goodCount: number;
  rejectCount: number;
}>;

export type MixedProductionTimelineEntryApi = Readonly<{
  eventId: string;
  occurredAtUtc: string;
  eventType: string;
  sourceCategory: string;
  stationCode: string;
  resourceCode?: string;
  operatorCode?: string;
  summary: string;
  quantity?: number;
  correlationKey?: string;
}>;

export type MixedProductionLineDetailApi = Readonly<{
  line: MixedProductionLineSummaryApi;
  stations: readonly MixedProductionStationSnapshotApi[];
  timeline: readonly MixedProductionTimelineEntryApi[];
  playbackWindowLabel: string;
}>;

export type MixedProductionPlaybackFrameApi = Readonly<{
  timestampUtc: string;
  lineState: string;
  stations: readonly Readonly<{
    stationCode: string;
    state: string;
    bufferQuantity?: number;
    operatorCode?: string;
    lastEventType?: string;
  }>[];
}>;

export type MixedProductionPlaybackApi = Readonly<{
  lineId: string;
  lineCode: string;
  windowStartUtc: string;
  windowEndUtc: string;
  frames: readonly MixedProductionPlaybackFrameApi[];
}>;

async function mixedProductionFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Readonly<Record<string, string>>,
): Promise<{ status: "success"; value: T } | { status: "failure"; code: string; message: string }> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      url.searchParams.set(key, value);
    }
  }

  const response = await fetch(url, {
    headers: {
      Accept: "application/json",
      ...context.requestHeaders,
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    return {
      status: "failure",
      code: body.code ?? `http-${response.status}`,
      message: body.message ?? body.title ?? "İstek başarısız.",
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readMixedProductionLines(
  context: ProductionOrderRequestContext,
): Promise<{ status: "success"; value: readonly MixedProductionLineSummaryApi[] } | { status: "failure"; code: string; message: string }> {
  return mixedProductionFetch<readonly MixedProductionLineSummaryApi[]>(context, "/api/mixed-production/lines");
}

export async function readMixedProductionLineDetail(
  context: ProductionOrderRequestContext,
  lineId: string,
): Promise<{ status: "success"; value: MixedProductionLineDetailApi } | { status: "failure"; code: string; message: string }> {
  return mixedProductionFetch<MixedProductionLineDetailApi>(context, `/api/mixed-production/lines/${lineId}`);
}

export async function readMixedProductionPlayback(
  context: ProductionOrderRequestContext,
  lineId: string,
  fromUtc: string,
  toUtc: string,
): Promise<{ status: "success"; value: MixedProductionPlaybackApi } | { status: "failure"; code: string; message: string }> {
  return mixedProductionFetch<MixedProductionPlaybackApi>(
    context,
    `/api/mixed-production/lines/${lineId}/playback`,
    { fromUtc, toUtc },
  );
}
