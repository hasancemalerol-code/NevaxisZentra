import { stripFixtureRecords } from "@/lib/runtime/fixture-data";
import {
  classifyFailedResponse,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";
import "./mold-server-boundary";

import type {
  MachineCompatibilityProfile,
  MoldCompatibilityAssessment,
  MoldCompatibilityOverview,
  MoldCompatibilityRequirement,
  MoldDetail,
  MoldLifecycleEvent,
  MoldMaintenanceRecord,
  MoldRevisionRecord,
  MoldSummary,
  MoldTimelineEntry,
  MoldTrialRecord,
  MoldWorkspaceSummary,
  MoldWriteResult,
} from "../domain/molds";
import {
  parseMachineCompatibilityProfileList,
  parseMoldCompatibilityAssessmentList,
  parseMoldCompatibilityOverview,
  parseMoldCompatibilityRequirement,
  parseMoldDetail,
  parseMoldLifecycleEventList,
  parseMoldMaintenanceRecordList,
  parseMoldRevisionRecordList,
  parseMoldSummaryList,
  parseMoldTimelineEntryList,
  parseMoldTrialRecordList,
  parseMoldWorkspaceSummary,
  parseMoldWriteApiResponse,
} from "./mold-api-contracts";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type MoldApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "subscription-required"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "concurrency-conflict"
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type MoldApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | MoldApiFailure;

export type MoldListFilters = Readonly<{
  search?: string;
  lifecycleState?: string;
  ownershipType?: string;
  locationType?: string;
  sourceType?: string;
  page?: number;
  pageSize?: number;
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
      if (value !== undefined && value !== "") {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

function mapFailureStatus(status: number, failure?: ClassifiedApiFailure): MoldApiFailure["code"] {
  if (failure?.kind === "subscription-required") return "subscription-required";
  if (failure?.kind === "unauthenticated") return "unauthenticated";
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "concurrency-conflict";
  return "source-unavailable";
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<MoldApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (!response.ok) {
      const failure = await classifyFailedResponse(response);
      return {
        status: "failure",
        code: mapFailureStatus(response.status, failure),
        message: failure.kind === "subscription-required" ? SUBSCRIPTION_REQUIRED_MESSAGE : failure.kind === "unauthenticated" && response.status === 403 ? SESSION_REQUIRED_MESSAGE : `Kalıp isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const guarded = stripFixtureRecords(parser(payload));
    if (guarded === null) {
      // A fixture record is not a customer record outside isolated environments.
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;
    }
    return { status: "success", value: guarded };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Kalıp API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

async function mutateJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  method: "POST" | "PUT",
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<MoldApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method,
      headers: {
        ...context.requestHeaders,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
      cache: "no-store",
    });

    if (!response.ok) {
      const failure = await classifyFailedResponse(response);
      let message = `Kalıp işlemi başarısız (${response.status}).`;
      try {
        const problem = (await response.json()) as { title?: string };
        if (problem.title) {
          message = problem.title;
        }
      } catch {
        // ignore parse errors
      }

      return {
        status: "failure",
        code: mapFailureStatus(response.status, failure),
        message,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const guarded = stripFixtureRecords(parser(payload));
    if (guarded === null) {
      // A fixture record is not a customer record outside isolated environments.
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;
    }
    return { status: "success", value: guarded };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Kalıp API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

export function listMolds(
  context: ProductionOrderRequestContext,
  filters: MoldListFilters = {},
): Promise<MoldApiResult<readonly MoldSummary[]>> {
  return getJson(
    context,
    "/api/molds",
    {
      search: filters.search,
      lifecycleState: filters.lifecycleState,
      ownershipType: filters.ownershipType,
      locationType: filters.locationType,
      sourceType: filters.sourceType,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseMoldSummaryList,
  );
}

export function getMoldWorkspaceSummary(
  context: ProductionOrderRequestContext,
): Promise<MoldApiResult<MoldWorkspaceSummary>> {
  return getJson(context, "/api/molds/workspace-summary", undefined, parseMoldWorkspaceSummary);
}

export function getMold(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<MoldDetail>> {
  return getJson(context, `/api/molds/${encodeURIComponent(moldId)}`, undefined, parseMoldDetail);
}

export function listMoldLifecycleHistory(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<readonly MoldLifecycleEvent[]>> {
  return getJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/lifecycle-history`,
    undefined,
    parseMoldLifecycleEventList,
  );
}

export function createMold(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
): Promise<MoldApiResult<MoldWriteResult>> {
  return mutateJson(context, "/api/molds", "POST", body, parseMoldWriteApiResponse);
}

export function updateMold(
  context: ProductionOrderRequestContext,
  moldId: string,
  body: Record<string, unknown>,
): Promise<MoldApiResult<MoldWriteResult>> {
  return mutateJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}`,
    "PUT",
    body,
    parseMoldWriteApiResponse,
  );
}

export function recordMoldCounterSnapshot(
  context: ProductionOrderRequestContext,
  moldId: string,
  body: Record<string, unknown>,
): Promise<MoldApiResult<MoldWriteResult>> {
  return mutateJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/counter-snapshots`,
    "POST",
    body,
    parseMoldWriteApiResponse,
  );
}

export function transitionMoldLifecycle(
  context: ProductionOrderRequestContext,
  moldId: string,
  body: Record<string, unknown>,
): Promise<MoldApiResult<MoldWriteResult>> {
  return mutateJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/lifecycle-transitions`,
    "POST",
    body,
    parseMoldWriteApiResponse,
  );
}

export function listMoldMaintenanceHistory(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<readonly MoldMaintenanceRecord[]>> {
  return getJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/maintenance-history`,
    undefined,
    parseMoldMaintenanceRecordList,
  );
}

export function listMoldRevisionHistory(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<readonly MoldRevisionRecord[]>> {
  return getJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/revision-history`,
    undefined,
    parseMoldRevisionRecordList,
  );
}

export function listMoldTrialHistory(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<readonly MoldTrialRecord[]>> {
  return getJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/trial-history`,
    undefined,
    parseMoldTrialRecordList,
  );
}

export function listMoldTimeline(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<readonly MoldTimelineEntry[]>> {
  return getJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/timeline`,
    undefined,
    parseMoldTimelineEntryList,
  );
}

export function createMoldMaintenanceRecord(
  context: ProductionOrderRequestContext,
  moldId: string,
  body: Record<string, unknown>,
): Promise<MoldApiResult<MoldWriteResult>> {
  return mutateJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/maintenance-history`,
    "POST",
    body,
    parseMoldWriteApiResponse,
  );
}

export function completeMoldMaintenanceRecord(
  context: ProductionOrderRequestContext,
  moldId: string,
  maintenanceId: string,
  body: Record<string, unknown>,
): Promise<MoldApiResult<MoldWriteResult>> {
  return mutateJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/maintenance-history/${encodeURIComponent(maintenanceId)}/complete`,
    "POST",
    body,
    parseMoldWriteApiResponse,
  );
}

export function appendMoldRevisionRecord(
  context: ProductionOrderRequestContext,
  moldId: string,
  body: Record<string, unknown>,
): Promise<MoldApiResult<MoldWriteResult>> {
  return mutateJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/revision-history`,
    "POST",
    body,
    parseMoldWriteApiResponse,
  );
}

export function appendMoldTrialRecord(
  context: ProductionOrderRequestContext,
  moldId: string,
  body: Record<string, unknown>,
): Promise<MoldApiResult<MoldWriteResult>> {
  return mutateJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/trial-history`,
    "POST",
    body,
    parseMoldWriteApiResponse,
  );
}

export function getMoldCompatibilityRequirement(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<MoldCompatibilityRequirement | null>> {
  return getJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/compatibility-requirement`,
    undefined,
    parseMoldCompatibilityRequirement,
  );
}

export function getMoldCompatibilityOverview(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<MoldCompatibilityOverview>> {
  return getJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/compatibility-overview`,
    undefined,
    parseMoldCompatibilityOverview,
  );
}

export function listMachineCompatibilityProfiles(
  context: ProductionOrderRequestContext,
): Promise<MoldApiResult<readonly MachineCompatibilityProfile[]>> {
  return getJson(
    context,
    "/api/molds/machine-compatibility-profiles",
    undefined,
    parseMachineCompatibilityProfileList,
  );
}

export function evaluateMoldCompatibilityAll(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MoldApiResult<readonly MoldCompatibilityAssessment[]>> {
  return mutateJson(
    context,
    `/api/molds/${encodeURIComponent(moldId)}/compatibility-evaluations`,
    "POST",
    {},
    parseMoldCompatibilityAssessmentList,
  );
}

export async function probeMoldReadPermission(
  context: ProductionOrderRequestContext,
): Promise<boolean> {
  const maxAttempts = 3;

  for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
    const result = await getMoldWorkspaceSummary(context);
    if (result.status === "success") {
      return result.value.capabilities.canRead;
    }

    if (result.code === "permission-denied" || result.code === "unauthenticated") {
      return false;
    }

    if (!result.retryable || attempt === maxAttempts - 1) {
      return false;
    }

    await new Promise((resolve) => {
      setTimeout(resolve, 250 * (attempt + 1));
    });
  }

  return false;
}

export async function probeMoldManagePermission(
  context: ProductionOrderRequestContext,
): Promise<boolean> {
  const maxAttempts = 3;

  for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
    const result = await getMoldWorkspaceSummary(context);
    if (result.status === "success") {
      return result.value.capabilities.canManage;
    }

    if (result.code === "permission-denied" || result.code === "unauthenticated") {
      return false;
    }

    if (!result.retryable || attempt === maxAttempts - 1) {
      return false;
    }

    await new Promise((resolve) => {
      setTimeout(resolve, 250 * (attempt + 1));
    });
  }

  return false;
}
