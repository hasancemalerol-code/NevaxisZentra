import {
  classifyFailedResponse,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";
import "./shift-workforce-server-boundary";

import {
  getAvailabilityLabel,
  getCompetencyCodeLabel,
  getCompetencyLevelLabel,
  getFreshnessLabel,
  getSourceSystemLabel,
  getVerificationStatusLabel,
  LOCAL_PILOT_SOURCE_SYSTEM,
} from "../domain/shift-workforce-labels";
import type {
  CreateShiftWorkforceCompetencyCommand,
  ShiftWorkforceCompetencyAssignment,
  ShiftWorkforceCompetencyDefinition,
  ShiftWorkforceListFilters,
  ShiftWorkforceOverview,
  ShiftWorkforcePersonDetail,
  ShiftWorkforcePersonListResult,
  ShiftWorkforcePersonSummary,
  ShiftWorkforceWriteResult,
  UpdateShiftWorkforceCompetencyCommand,
} from "../domain/shift-workforce-types";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type ShiftWorkforceApiFailure = Readonly<{
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

export type ShiftWorkforceApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ShiftWorkforceApiFailure;

function readString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function readNumber(value: unknown): number | null {
  if (value === null || value === undefined) {
    return null;
  }
  return typeof value === "number" ? value : null;
}

function readBoolean(value: unknown): boolean {
  return value === true;
}

function parsePersonSummary(value: unknown): ShiftWorkforcePersonSummary {
  const item = value as Record<string, unknown>;
  const availabilityState = String(item.availabilityState ?? "unknown");
  const freshnessState = String(item.freshnessState ?? "unknown");
  const sourceSystem = String(item.sourceSystem ?? "");
  return {
    id: String(item.id),
    employeeCode: String(item.employeeCode ?? ""),
    displayName: String(item.displayName ?? ""),
    department: readString(item.department),
    jobTitle: readString(item.jobTitle),
    isActive: readBoolean(item.isActive),
    availabilityState,
    availabilityLabel: getAvailabilityLabel(
      availabilityState,
      readString(item.availabilityLabel),
    ),
    shiftLabel: readString(item.shiftLabel),
    sourceSystem,
    sourceLabel: getSourceSystemLabel(sourceSystem, readString(item.sourceLabel)),
    freshnessState,
    freshnessLabel: getFreshnessLabel(freshnessState, readString(item.freshnessLabel)),
    competencySummary: String(item.competencySummary ?? "—"),
    competencyAttention: readBoolean(item.competencyAttention),
  };
}

function parseCompetencyAssignment(value: unknown): ShiftWorkforceCompetencyAssignment {
  const item = value as Record<string, unknown>;
  const competencyCode = String(item.competencyCode ?? "");
  const competencyLevel = String(item.competencyLevel ?? "");
  const verificationStatus = String(item.verificationStatus ?? "");
  return {
    id: String(item.id),
    competencyCode,
    competencyLabel: getCompetencyCodeLabel(
      competencyCode,
      readString(item.competencyLabel),
    ),
    machineFamilyScope: readString(item.machineFamilyScope),
    processScope: readString(item.processScope),
    materialScope: readString(item.materialScope),
    equipmentScope: readString(item.equipmentScope),
    competencyLevel,
    competencyLevelLabel: getCompetencyLevelLabel(
      competencyLevel,
      readString(item.competencyLevelLabel),
    ),
    verificationStatus,
    verificationStatusLabel: getVerificationStatusLabel(
      verificationStatus,
      readString(item.verificationStatusLabel),
    ),
    validFromUtc: readString(item.validFromUtc),
    validUntilUtc: readString(item.validUntilUtc),
    verifiedAtUtc: readString(item.verifiedAtUtc),
    operationalNote: readString(item.operationalNote),
    version: Number(item.version ?? 0),
    requiresAttention: readBoolean(item.requiresAttention),
  };
}

function parsePersonDetail(value: unknown): ShiftWorkforcePersonDetail {
  const item = value as Record<string, unknown>;
  const summary = parsePersonSummary(item);
  const sourceSystem = String(item.sourceSystem ?? "");
  const competencies = Array.isArray(item.competencies)
    ? item.competencies.map(parseCompetencyAssignment)
    : [];
  return {
    ...summary,
    sourceShiftReference: readString(item.sourceShiftReference),
    sourceCalendarReference: readString(item.sourceCalendarReference),
    sourceUpdatedAtUtc: readString(item.sourceUpdatedAtUtc),
    synchronizedAtUtc: readString(item.synchronizedAtUtc),
    sourceFingerprint: readString(item.sourceFingerprint),
    sourceLimitations: Array.isArray(item.sourceLimitations)
      ? item.sourceLimitations.map(String)
      : [],
    isPilotData: readBoolean(item.isPilotData) || sourceSystem === LOCAL_PILOT_SOURCE_SYSTEM,
    competencies,
    version: Number(item.version ?? 0),
  };
}

function parseOverview(payload: unknown): ShiftWorkforceOverview {
  const item = payload as Record<string, unknown>;
  const capabilities = item.capabilities as Record<string, unknown> | undefined;
  return {
    totalWorkforce: readNumber(item.totalWorkforce),
    activeWorkforce: readNumber(item.activeWorkforce),
    currentlyAvailable: readNumber(item.currentlyAvailable),
    unavailableOrLeave: readNumber(item.unavailableOrLeave),
    staleSourceRecords: readNumber(item.staleSourceRecords),
    competencyAttentionCount: readNumber(item.competencyAttentionCount),
    capabilities: {
      canRead: capabilities?.canRead === true,
      canManage: capabilities?.canManage === true,
    },
  };
}

function parsePersonList(payload: unknown): ShiftWorkforcePersonListResult {
  const item = payload as Record<string, unknown>;
  const items = Array.isArray(item.items) ? item.items.map(parsePersonSummary) : [];
  return {
    items,
    totalCount: Number(item.totalCount ?? items.length),
    page: Number(item.page ?? 1),
    pageSize: Number(item.pageSize ?? items.length),
  };
}

function parseCompetencyDefinition(value: unknown): ShiftWorkforceCompetencyDefinition {
  const item = value as Record<string, unknown>;
  const code = String(item.code ?? "");
  return {
    code,
    displayLabelTr: String(item.displayLabelTr ?? getCompetencyCodeLabel(code)),
    descriptionTr: readString(item.descriptionTr),
    isActive: readBoolean(item.isActive),
    sortOrder: Number(item.sortOrder ?? 0),
  };
}

function parseWriteResult(payload: unknown): ShiftWorkforceWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    version: Number(item.version ?? 0),
    idempotentReplay: item.idempotentReplay === true,
  };
}

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

function mapFailureStatus(status: number, failure?: ClassifiedApiFailure): ShiftWorkforceApiFailure["code"] {
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
): Promise<ShiftWorkforceApiResult<T>> {
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
        message: failure.kind === "subscription-required" ? SUBSCRIPTION_REQUIRED_MESSAGE : failure.kind === "unauthenticated" && response.status === 403 ? SESSION_REQUIRED_MESSAGE : `Vardiya personel isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Vardiya personel API bağlantısı kurulamadı.",
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
): Promise<ShiftWorkforceApiResult<T>> {
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
      let message = `Vardiya personel işlemi başarısız (${response.status}).`;
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
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Vardiya personel API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

export function getShiftWorkforceOverview(
  context: ProductionOrderRequestContext,
): Promise<ShiftWorkforceApiResult<ShiftWorkforceOverview>> {
  return getJson(context, "/api/shift-workforce/overview", undefined, parseOverview);
}

export function listShiftWorkforcePersons(
  context: ProductionOrderRequestContext,
  filters: ShiftWorkforceListFilters = {},
): Promise<ShiftWorkforceApiResult<ShiftWorkforcePersonListResult>> {
  return getJson(
    context,
    "/api/shift-workforce/persons",
    {
      search: filters.search,
      isActive: filters.isActive,
      availability: filters.availability,
      competencyCode: filters.competencyCode,
      freshness: filters.freshness,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parsePersonList,
  );
}

export function getShiftWorkforcePerson(
  context: ProductionOrderRequestContext,
  personId: string,
): Promise<ShiftWorkforceApiResult<ShiftWorkforcePersonDetail>> {
  return getJson(
    context,
    `/api/shift-workforce/persons/${encodeURIComponent(personId)}`,
    undefined,
    parsePersonDetail,
  );
}

export function listShiftWorkforceCompetencyDefinitions(
  context: ProductionOrderRequestContext,
): Promise<ShiftWorkforceApiResult<readonly ShiftWorkforceCompetencyDefinition[]>> {
  return getJson(
    context,
    "/api/shift-workforce/competency-definitions",
    undefined,
    (payload) => {
      if (!Array.isArray(payload)) {
        throw new Error("Expected competency definition list array.");
      }
      return payload.map(parseCompetencyDefinition);
    },
  );
}

export function listShiftWorkforcePersonCompetencies(
  context: ProductionOrderRequestContext,
  personId: string,
): Promise<ShiftWorkforceApiResult<readonly ShiftWorkforceCompetencyAssignment[]>> {
  return getJson(
    context,
    `/api/shift-workforce/persons/${encodeURIComponent(personId)}/competencies`,
    undefined,
    (payload) => {
      if (!Array.isArray(payload)) {
        throw new Error("Expected competency assignment list array.");
      }
      return payload.map(parseCompetencyAssignment);
    },
  );
}

export function createShiftWorkforceCompetency(
  context: ProductionOrderRequestContext,
  personId: string,
  command: CreateShiftWorkforceCompetencyCommand,
): Promise<ShiftWorkforceApiResult<ShiftWorkforceWriteResult>> {
  return mutateJson(
    context,
    `/api/shift-workforce/persons/${encodeURIComponent(personId)}/competencies`,
    "POST",
    command,
    parseWriteResult,
  );
}

export function updateShiftWorkforceCompetency(
  context: ProductionOrderRequestContext,
  personId: string,
  assignmentId: string,
  command: UpdateShiftWorkforceCompetencyCommand,
): Promise<ShiftWorkforceApiResult<ShiftWorkforceWriteResult>> {
  return mutateJson(
    context,
    `/api/shift-workforce/persons/${encodeURIComponent(personId)}/competencies/${encodeURIComponent(assignmentId)}`,
    "PUT",
    command,
    parseWriteResult,
  );
}

export function synchronizeShiftWorkforce(
  context: ProductionOrderRequestContext,
): Promise<ShiftWorkforceApiResult<{ createdCount: number; updatedCount: number; unchangedCount: number }>> {
  return mutateJson(
    context,
    "/api/shift-workforce/synchronize",
    "POST",
    {},
    (payload) => {
      const item = payload as Record<string, unknown>;
      return {
        createdCount: Number(item.createdCount ?? 0),
        updatedCount: Number(item.updatedCount ?? 0),
        unchangedCount: Number(item.unchangedCount ?? 0),
      };
    },
  );
}

export async function probeShiftWorkforceReadPermission(
  context: ProductionOrderRequestContext,
): Promise<boolean> {
  const result = await getShiftWorkforceOverview(context);
  if (result.status === "success") {
    return result.value.capabilities.canRead;
  }

  return false;
}
