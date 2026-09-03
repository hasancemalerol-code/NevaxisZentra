import "./shift-handover-server-boundary";

import { fetchTrustedApi } from "@/features/saas-identity/server/authenticated-server-fetch";

import { resolveShiftHandoverRequestContext } from "./shift-handover-request-context";
import type { ShiftHandoverRequestContext } from "./shift-handover-request-context";

export type { ShiftHandoverRequestContext } from "./shift-handover-request-context";
export { resolveShiftHandoverRequestContext } from "./shift-handover-request-context";

export type ShiftHandoverApiFailure = Readonly<{
  status: "failure";
  code: string;
  message: string;
}>;

export type ShiftHandoverApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ShiftHandoverApiFailure;

export type RawShiftHandoverSummary = Readonly<{
  id: string;
  handoverReference: string;
  shiftDate: string;
  shiftPeriod: string;
  department: string;
  status: string;
  outgoingLabel: string;
  incomingLabel: string | null;
  unresolvedItemCount: number;
  followUpItemCount: number;
  createdAt: string;
  handedOverAt: string | null;
  deepLinkPath: string;
}>;

export type RawShiftHandoverItem = Readonly<{
  id: string;
  sortOrder: number;
  category: string;
  title: string;
  description: string;
  severity: string;
  sourceModule: string | null;
  sourceEntityId: string | null;
  status: string;
  requiresFollowUp: boolean;
  linkedAiTaskId: string | null;
  evidenceMetadataJson: string | null;
  version: number;
  createdAt: string;
  updatedAt: string;
  taskDeepLinkPath: string | null;
}>;

export type RawShiftHandoverDetail = Readonly<{
  id: string;
  handoverReference: string;
  shiftDate: string;
  shiftPeriod: string;
  department: string;
  summary: string;
  status: string;
  outgoingMembershipId: string;
  outgoingLabel: string;
  incomingMembershipId: string | null;
  incomingLabel: string | null;
  facilityName: string;
  createdAt: string;
  updatedAt: string;
  handedOverAt: string | null;
  acknowledgedAt: string | null;
  completedAt: string | null;
  version: number;
  deepLinkPath: string;
  items: RawShiftHandoverItem[];
  events: ReadonlyArray<Readonly<{
    id: string;
    eventType: string;
    actorLabel: string;
    payloadJson: string;
    occurredAt: string;
  }>>;
  capabilities: Readonly<{
    canRead: boolean;
    canManage: boolean;
    canAcknowledge: boolean;
    canCreateTask: boolean;
  }>;
  automationDisclaimer: string;
}>;

export type RawShiftHandoverListResponse = Readonly<{
  items: RawShiftHandoverSummary[];
  capabilities: RawShiftHandoverDetail["capabilities"];
  automationDisclaimer: string;
}>;

function buildQuery(facilityId: string, extra?: Record<string, string | undefined>): string {
  const params = new URLSearchParams({ facilityId });
  if (extra) {
    for (const [key, value] of Object.entries(extra)) {
      if (value) params.set(key, value);
    }
  }
  return params.toString();
}

async function readResponseBody(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text.trim()) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function mapFailurePayload(payload: unknown, status: number): ShiftHandoverApiFailure {
  const body = (payload ?? {}) as { title?: string; code?: string };
  return {
    status: "failure",
    code: body.code ?? "request-failed",
    message: body.title ?? `─░stek ba┼şar─▒s─▒z (${status})`,
  };
}

async function request<T>(
  context: ShiftHandoverRequestContext,
  path: string,
  init?: RequestInit,
): Promise<ShiftHandoverApiResult<T>> {
  try {
    if (context.mode === "session") {
      const fetchResult = await fetchTrustedApi(path, init ?? {});
      if (fetchResult.status === "failure") {
        return {
          status: "failure",
          code: "request-failed",
          message: fetchResult.message,
        };
      }

      const response = fetchResult.response;
      const payload = await readResponseBody(response);
      if (!response.ok) {
        return mapFailurePayload(payload, response.status);
      }

      if (response.status === 204) {
        return { status: "success", value: undefined as T };
      }

      return { status: "success", value: payload as T };
    }

    const apiBaseUrl = (process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000").replace(/\/$/, "");
    const response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        ...context.headers,
        ...(init?.headers ?? {}),
      },
      cache: "no-store",
    });
    const payload = await readResponseBody(response);
    if (!response.ok) {
      return mapFailurePayload(payload, response.status);
    }

    if (response.status === 204) {
      return { status: "success", value: undefined as T };
    }

    return { status: "success", value: payload as T };
  } catch {
    return {
      status: "failure",
      code: "request-failed",
      message: "Vardiya devri API'sine ula┼ş─▒lamad─▒.",
    };
  }
}

export async function listShiftHandovers(
  context: ShiftHandoverRequestContext,
  status?: string,
): Promise<ShiftHandoverApiResult<RawShiftHandoverListResponse>> {
  const facilityId = context.facilityId;
  return request(
    context,
    `/api/shift-handovers?${buildQuery(facilityId, { status })}`,
  );
}

export async function getShiftHandover(
  context: ShiftHandoverRequestContext,
  handoverId: string,
): Promise<ShiftHandoverApiResult<RawShiftHandoverDetail>> {
  return request(
    context,
    `/api/shift-handovers/${handoverId}?${buildQuery(context.facilityId)}`,
  );
}

export async function createShiftHandover(
  context: ShiftHandoverRequestContext,
  body: unknown,
): Promise<ShiftHandoverApiResult<{ id: string; version: number }>> {
  return request(
    context,
    `/api/shift-handovers?${buildQuery(context.facilityId)}`,
    { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) },
  );
}

export async function postShiftHandoverAction(
  context: ShiftHandoverRequestContext,
  handoverId: string,
  action: string,
  body: unknown,
): Promise<ShiftHandoverApiResult<{ id: string; version: number; idempotentReplay?: boolean }>> {
  return request(
    context,
    `/api/shift-handovers/${handoverId}/${action}?${buildQuery(context.facilityId)}`,
    { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) },
  );
}

export async function addShiftHandoverItem(
  context: ShiftHandoverRequestContext,
  handoverId: string,
  body: unknown,
): Promise<ShiftHandoverApiResult<{ id: string; version: number }>> {
  return request(
    context,
    `/api/shift-handovers/${handoverId}/items?${buildQuery(context.facilityId)}`,
    { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) },
  );
}

export async function createShiftHandoverFollowUpTask(
  context: ShiftHandoverRequestContext,
  handoverId: string,
  itemId: string,
): Promise<ShiftHandoverApiResult<{ taskId: string; deepLinkPath: string; idempotentReplay?: boolean }>> {
  return request(
    context,
    `/api/shift-handovers/${handoverId}/items/${itemId}/follow-up-task?${buildQuery(context.facilityId)}`,
    { method: "POST" },
  );
}
