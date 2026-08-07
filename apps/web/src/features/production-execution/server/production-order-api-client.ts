import "./production-order-server-boundary";

import type { ProductionOrder, ProductionOrderSummary } from "../domain/production-order";
import type {
  ProductionDowntimeSummary,
  ProductionSetupSessionSummary,
  ProductionCycleEntrySummary,
  ProductionRuntimeSummary,
} from "../domain/production-execution-operations";
import type {
  ProductionExecutionExceptionSummary,
  ProductionExecutionHealthSummary,
} from "../domain/production-execution-exceptions";
import {
  parseProductionOrderDetailApiResponse,
  parseProductionOrderListApiResponse,
} from "./production-order-api-contracts";
import {
  parseDowntimeListApiResponse,
  parseSetupSessionListApiResponse,
  parseCycleEntryListApiResponse,
  parseRuntimeSummaryApiResponse,
} from "./production-execution-operations-api-contracts";
import {
  parseExceptionListApiResponse,
  parseHealthSummaryApiResponse,
} from "./production-execution-exceptions-api-contracts";
import type { ProductionOrderRequestContext } from "./production-order-request-context";

export type ProductionOrderApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "not-found"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "conflict";
  message: string;
  retryable: boolean;
}>;

export type ProductionOrderApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ProductionOrderApiFailure;

export type ProductionOrderWriteResult = Readonly<{
  status: "success" | "failure";
  code?: string;
  message?: string;
  id?: string;
  referenceCode?: string;
  orderStatus?: string;
  version?: number;
}>;

function fail(
  code: ProductionOrderApiFailure["code"],
  message: string,
  retryable: boolean,
): ProductionOrderApiFailure {
  return { status: "failure", code, message, retryable };
}

function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, "");
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

function mapHttpStatus(
  status: number,
  body: unknown,
): ProductionOrderApiFailure {
  const failureCode =
    typeof body === "object" &&
    body !== null &&
    typeof (body as Record<string, unknown>)["failureCode"] === "string"
      ? String((body as Record<string, unknown>)["failureCode"])
      : undefined;

  if (status === 401) {
    return fail("unauthenticated", "Authentication required.", false);
  }

  if (status === 403) {
    return fail("permission-denied", "Authorization denied.", false);
  }

  if (status === 404) {
    return fail("not-found", "Production order not found.", false);
  }

  if (status === 409) {
    const msg =
      typeof body === "object" &&
      body !== null &&
      typeof (body as Record<string, unknown>)["title"] === "string"
        ? String((body as Record<string, unknown>)["title"])
        : "Conflict.";
    return fail("conflict", `${failureCode ?? "conflict"}: ${msg}`, false);
  }

  if (status >= 500) {
    return fail("source-unavailable", "Production execution service is unavailable.", true);
  }

  return fail("invalid-response", `Unexpected status: ${status}`, false);
}

async function executeGet<T>(
  context: ProductionOrderRequestContext,
  url: string,
  parse: (body: unknown) => ProductionOrderApiResult<T>,
): Promise<ProductionOrderApiResult<T>> {
  if (!context.apiBaseUrl.trim()) {
    return fail("configuration-error", "API base URL is not configured.", false);
  }

  let response: Response;

  try {
    response = await fetch(url, {
      method: "GET",
      headers: {
        Accept: "application/json",
        ...context.requestHeaders,
      },
      cache: "no-store",
    });
  } catch {
    return fail(
      "source-unavailable",
      "The production execution service is unavailable.",
      true,
    );
  }

  const body = await readResponseBody(response);

  if (!response.ok) {
    return mapHttpStatus(response.status, body);
  }

  return parse(body);
}

async function executePost(
  context: ProductionOrderRequestContext,
  url: string,
  requestBody: unknown,
): Promise<ProductionOrderWriteResult> {
  if (!context.apiBaseUrl.trim()) {
    return {
      status: "failure",
      code: "configuration-error",
      message: "API base URL is not configured.",
    };
  }

  let response: Response;

  try {
    response = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        ...context.requestHeaders,
      },
      body: JSON.stringify(requestBody),
      cache: "no-store",
    });
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "The production execution service is unavailable.",
    };
  }

  const body = await readResponseBody(response);

  if (!response.ok) {
    const failure = mapHttpStatus(response.status, body);
    return {
      status: "failure",
      code: failure.code,
      message: failure.message,
    };
  }

  if (typeof body === "object" && body !== null) {
    const o = body as Record<string, unknown>;
    return {
      status: "success",
      id: typeof o.id === "string" ? o.id : undefined,
      referenceCode: typeof o.referenceCode === "string" ? o.referenceCode : undefined,
      orderStatus: typeof o.status === "string" ? o.status : undefined,
      version: typeof o.version === "number" ? o.version : undefined,
    };
  }

  return { status: "success" };
}

export async function listProductionOrders(
  context: ProductionOrderRequestContext,
  query: { facilityId: string; status?: string; search?: string; limit?: number },
): Promise<ProductionOrderApiResult<ProductionOrderSummary[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", query.facilityId.trim());

  if (query.status?.trim()) {
    params.set("status", query.status.trim());
  }

  if (query.search?.trim()) {
    params.set("search", query.search.trim());
  }

  if (query.limit !== undefined) {
    params.set("limit", String(query.limit));
  }

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders?${params.toString()}`;

  return executeGet(context, url, (body) => {
    const parsed = parseProductionOrderListApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function getProductionOrderById(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  facilityId: string,
): Promise<ProductionOrderApiResult<ProductionOrder>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}?${params.toString()}`;

  return executeGet(context, url, (body) => {
    const parsed = parseProductionOrderDetailApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function startProductionOrder(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/start?${params.toString()}`;

  return executePost(context, url, { expectedVersion });
}

export async function pauseProductionOrder(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/pause?${params.toString()}`;

  return executePost(context, url, { expectedVersion, reason });
}

export async function resumeProductionOrder(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/resume?${params.toString()}`;

  return executePost(context, url, { expectedVersion });
}

export async function completeProductionOrder(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
  options?: { allowShortCompletion?: boolean; completionReason?: string },
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/complete?${params.toString()}`;

  return executePost(context, url, {
    expectedVersion,
    allowShortCompletion: options?.allowShortCompletion ?? false,
    completionReason: options?.completionReason,
  });
}

export async function recordGoodProduction(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
  quantity: number,
  comment?: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/quantities/good?${params.toString()}`;

  return executePost(context, url, { expectedVersion, quantity, comment });
}

export async function recordScrap(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
  quantity: number,
  reasonCode: string,
  comment?: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/quantities/scrap?${params.toString()}`;

  return executePost(context, url, { expectedVersion, quantity, reasonCode, comment });
}

export async function reverseEntry(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  entryId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/quantity-entries/${encodeURIComponent(entryId)}/reverse?${params.toString()}`;

  return executePost(context, url, { expectedVersion, reason });
}

export async function listQuantityEntries(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  facilityId: string,
  limit?: number,
): Promise<ProductionOrderApiResult<unknown[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());
  if (limit !== undefined) {
    params.set("limit", String(limit));
  }

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/quantity-entries?${params.toString()}`;

  return executeGet(context, url, (body) => {
    if (!Array.isArray(body)) {
      return fail("invalid-response", "Expected an array.", false);
    }
    return { status: "success", value: body as unknown[] };
  });
}

export async function consumeHandoff(
  context: ProductionOrderRequestContext,
  handoffId: string,
  expectedHandoffVersion: number,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/handoffs/${encodeURIComponent(handoffId.trim())}/consume?${params.toString()}`;

  return executePost(context, url, { expectedHandoffVersion });
}

export async function startDowntime(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
  reasonCode: string,
  comment?: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/downtimes/start?${params.toString()}`;
  return executePost(context, url, { expectedVersion, reasonCode, comment });
}

export async function endDowntime(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  downtimeId: string,
  expectedVersion: number,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/downtimes/${encodeURIComponent(downtimeId)}/end?${params.toString()}`;
  return executePost(context, url, { expectedVersion });
}

export async function listDowntimes(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  facilityId: string,
  limit?: number,
): Promise<ProductionOrderApiResult<ProductionDowntimeSummary[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());
  if (limit !== undefined) params.set("limit", String(limit));
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/downtimes?${params.toString()}`;
  return executeGet(context, url, (body) => {
    const parsed = parseDowntimeListApiResponse(body);
    if (parsed.status === "failure") return fail("invalid-response", parsed.message, false);
    return { status: "success", value: parsed.value };
  });
}

export async function startSetupSession(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
  setupType: string,
  comment?: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/setups/start?${params.toString()}`;
  return executePost(context, url, { expectedVersion, setupType, comment });
}

export async function completeSetupSession(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  setupId: string,
  expectedVersion: number,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/setups/${encodeURIComponent(setupId)}/complete?${params.toString()}`;
  return executePost(context, url, { expectedVersion });
}

export async function listSetupSessions(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  facilityId: string,
  limit?: number,
): Promise<ProductionOrderApiResult<ProductionSetupSessionSummary[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());
  if (limit !== undefined) params.set("limit", String(limit));
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/setups?${params.toString()}`;
  return executeGet(context, url, (body) => {
    const parsed = parseSetupSessionListApiResponse(body);
    if (parsed.status === "failure") return fail("invalid-response", parsed.message, false);
    return { status: "success", value: parsed.value };
  });
}

export async function recordManualCycle(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
  cycleDurationMilliseconds: number,
  quantityProduced?: number,
  comment?: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/cycles/manual?${params.toString()}`;
  return executePost(context, url, { expectedVersion, cycleDurationMilliseconds, quantityProduced, comment });
}

export async function listCycleEntries(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  facilityId: string,
  limit?: number,
): Promise<ProductionOrderApiResult<ProductionCycleEntrySummary[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());
  if (limit !== undefined) params.set("limit", String(limit));
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/cycles?${params.toString()}`;
  return executeGet(context, url, (body) => {
    const parsed = parseCycleEntryListApiResponse(body);
    if (parsed.status === "failure") return fail("invalid-response", parsed.message, false);
    return { status: "success", value: parsed.value };
  });
}

export async function getRuntimeSummary(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  facilityId: string,
): Promise<ProductionOrderApiResult<ProductionRuntimeSummary>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/runtime-summary?${params.toString()}`;
  return executeGet(context, url, (body) => {
    const parsed = parseRuntimeSummaryApiResponse(body);
    if (parsed.status === "failure") return fail("invalid-response", parsed.message, false);
    return { status: "success", value: parsed.value };
  });
}

// ── 4L: Exceptions ────────────────────────────────────────────────────────────

export async function listExceptions(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  facilityId: string,
  limit?: number,
): Promise<ProductionOrderApiResult<ProductionExecutionExceptionSummary[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());
  if (limit !== undefined) params.set("limit", String(limit));
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/exceptions?${params.toString()}`;
  return executeGet(context, url, (body) => {
    const parsed = parseExceptionListApiResponse(body);
    if (parsed.status === "failure") return fail("invalid-response", parsed.message, false);
    return { status: "success", value: parsed.value };
  });
}

export async function getHealthSummary(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  facilityId: string,
): Promise<ProductionOrderApiResult<ProductionExecutionHealthSummary>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/health-summary?${params.toString()}`;
  return executeGet(context, url, (body) => {
    const parsed = parseHealthSummaryApiResponse(body);
    if (parsed.status === "failure") return fail("invalid-response", parsed.message, false);
    return { status: "success", value: parsed.value };
  });
}

export async function raiseException(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  expectedVersion: number,
  exceptionType: string,
  severity: string,
  message: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/exceptions?${params.toString()}`;
  return executePost(context, url, { expectedVersion, exceptionType, severity, message });
}

export async function acknowledgeException(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  exceptionId: string,
  expectedVersion: number,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/exceptions/${encodeURIComponent(exceptionId)}/acknowledge?${params.toString()}`;
  return executePost(context, url, { expectedVersion });
}

export async function resolveException(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  exceptionId: string,
  expectedVersion: number,
  resolutionNote: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/exceptions/${encodeURIComponent(exceptionId)}/resolve?${params.toString()}`;
  return executePost(context, url, { expectedVersion, resolutionNote });
}

export async function recoverDowntime(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  downtimeId: string,
  expectedVersion: number,
  resolution: string,
  resumeProduction: boolean,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/downtimes/${encodeURIComponent(downtimeId)}/recover?${params.toString()}`;
  return executePost(context, url, { expectedVersion, resolution, resumeProduction });
}

export async function recoverSetupSession(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
  setupId: string,
  expectedVersion: number,
  resolution: string,
): Promise<ProductionOrderWriteResult> {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/production/orders/${encodeURIComponent(productionOrderId.trim())}/setups/${encodeURIComponent(setupId)}/recover?${params.toString()}`;
  return executePost(context, url, { expectedVersion, resolution });
}
