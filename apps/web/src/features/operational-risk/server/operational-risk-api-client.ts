import { refusalCode } from "@/lib/api/api-failure-classifier";
import "./operational-risk-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type OperationalRiskApiFailure = Readonly<{
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
    | "idempotency-conflict"
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type OperationalRiskApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | OperationalRiskApiFailure;

export type RawOperationalRiskCaseSummary = Readonly<{
  id: string;
  caseReference: string;
  category: string;
  scopeType: string;
  scopeReference: string | null;
  title: string;
  evidenceState: string;
  likelihoodLevel: number | null;
  impactLevel: number | null;
  riskScore: number | null;
  severityBand: string | null;
  assessmentOutcome: string;
  lifecycleStatus: string;
  ruleSetVersion: string;
  firstDetectedAtUtc: string;
  lastObservedAtUtc: string;
  version: number;
  isPilotData: boolean;
}>;

export type RawOperationalRiskCaseDetail = RawOperationalRiskCaseSummary & Readonly<{
  description: string;
  sourceModule: string | null;
  sourceReference: string | null;
  sourceProvenance: string;
  evidence: ReadonlyArray<Readonly<{
    id: string;
    evidenceTypeCode: string;
    evidenceState: string;
    observedAtUtc: string;
    sourceModule: string | null;
    sourceReference: string | null;
    summary: string;
    numericValue: number | null;
    unit: string | null;
    comparisonReference: string | null;
    sourceProvenance: string;
    createdAtUtc: string;
    isPilotData: boolean;
  }>>;
  statusHistory: ReadonlyArray<Readonly<{
    id: string;
    previousStatus: string;
    newStatus: string;
    reasonNote: string | null;
    actorSubjectId: string;
    occurredAtUtc: string;
    expectedCaseVersion: number;
    sourceProvenance: string;
  }>>;
}>;

export type RawOperationalRiskDetectionProfile = Readonly<{
  id: string;
  profileCode: string;
  displayName: string;
  status: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  version: number;
  rules: ReadonlyArray<Readonly<{
    id: string;
    ruleCode: string;
    sourceModule: string;
    sourceOutcome: string;
    riskCategory: string;
    defaultLikelihood: number;
    defaultImpact: number;
    thresholdVarianceBand: number | null;
    driverReasonCode: string;
    priorityOrder: number;
    isActive: boolean;
  }>>;
  isPilotData: boolean;
}>;

export type RawOperationalRiskDetectionRunSummary = Readonly<{
  id: string;
  runReference: string;
  profileId: string;
  profileCode: string;
  windowStartUtc: string;
  windowEndUtc: string;
  status: string;
  sourceCount: number;
  resultCount: number;
  ruleSetVersion: string;
  createdAtUtc: string;
  completedAtUtc: string | null;
  isPilotData: boolean;
}>;

export type RawOperationalRiskDetectionRunDetail = RawOperationalRiskDetectionRunSummary & Readonly<{
  sourceProvenance: string;
  results: ReadonlyArray<Readonly<{
    id: string;
    sourceModule: string;
    sourceReference: string;
    riskCategory: string;
    scopeType: string;
    scopeReference: string | null;
    evidenceState: string;
    sourceOutcome: string;
    detectionOutcome: string;
    actualValue: number | null;
    thresholdValue: number | null;
    unit: string | null;
    normalizedVariance: number | null;
    likelihoodLevel: number | null;
    impactLevel: number | null;
    riskScore: number | null;
    severityBand: string | null;
    leadingDriverCode: string | null;
    linkedCaseId: string | null;
    detectionKey: string;
    sourceAssessmentReference: string;
    evaluatedAtUtc: string;
    drivers: ReadonlyArray<Readonly<{
      id: string;
      reasonCode: string;
      severity: string;
      actualValue: number | null;
      thresholdValue: number | null;
      unit: string | null;
      normalizedVariance: number | null;
      supportingText: string;
      sortOrder: number;
    }>>;
  }>>;
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

function mapFailureStatus(status: number, payload: unknown): OperationalRiskApiFailure["code"] {
  if (status === 401) return "unauthenticated";
  if (status === 403) return refusalCode(status, payload);
  if (status === 404) return "not-found";
  if (status === 409) {
    const code = typeof payload === "object" && payload && "failureCode" in payload
      ? String((payload as { failureCode?: string }).failureCode ?? "")
      : "";
    if (code.includes("idempotency")) return "idempotency-conflict";
    return "concurrency-conflict";
  }
  return "source-unavailable";
}

function failureMessage(status: number, payload: unknown): string {
  if (typeof payload === "object" && payload) {
    const reason = "failureReason" in payload ? String((payload as { failureReason?: string }).failureReason ?? "") : "";
    const detail = "detail" in payload ? String((payload as { detail?: string }).detail ?? "") : "";
    const title = "title" in payload ? String((payload as { title?: string }).title ?? "") : "";
    return reason || detail || title || `Operasyonel risk isteği başarısız (${status}).`;
  }
  return `Operasyonel risk isteği başarısız (${status}).`;
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<OperationalRiskApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    let payload: unknown = null;
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }

    if (!response.ok) {
      return {
        status: "failure",
        code: mapFailureStatus(response.status, payload),
        message: failureMessage(response.status, payload),
        retryable: response.status >= 500,
      };
    }

    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Operasyonel risk API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

async function writeJson<T>(
  context: ProductionOrderRequestContext,
  method: "POST" | "PUT",
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<OperationalRiskApiResult<T>> {
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

    let payload: unknown = null;
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }

    if (!response.ok) {
      return {
        status: "failure",
        code: mapFailureStatus(response.status, payload),
        message: failureMessage(response.status, payload),
        retryable: response.status >= 500,
      };
    }

    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Operasyonel risk yazma isteği başarısız oldu.",
      retryable: true,
    };
  }
}

function parseCaseSummary(value: unknown): RawOperationalRiskCaseSummary {
  const item = value as RawOperationalRiskCaseSummary;
  return item;
}

function parseCaseList(payload: unknown): Readonly<{
  items: readonly RawOperationalRiskCaseSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}> {
  const data = payload as {
    items: RawOperationalRiskCaseSummary[];
    totalCount: number;
    page: number;
    pageSize: number;
    ruleSetVersion: string;
    provenanceDisclaimer: string;
    capabilities: { canRead: boolean; canManage: boolean };
  };
  return {
    items: data.items ?? [],
    totalCount: data.totalCount ?? 0,
    page: data.page ?? 1,
    pageSize: data.pageSize ?? 25,
    ruleSetVersion: data.ruleSetVersion ?? "",
    provenanceDisclaimer: data.provenanceDisclaimer ?? "",
    capabilities: data.capabilities ?? { canRead: true, canManage: false },
  };
}

export async function listOperationalRiskCases(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    category?: string;
    lifecycleStatus?: string;
    severityBand?: string;
    assessmentOutcome?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<OperationalRiskApiResult<ReturnType<typeof parseCaseList>>> {
  return getJson(context, "/api/operational-risks/cases", {
    category: filters.category,
    lifecycleStatus: filters.lifecycleStatus,
    severityBand: filters.severityBand,
    assessmentOutcome: filters.assessmentOutcome,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, parseCaseList);
}

export async function getOperationalRiskCase(
  context: ProductionOrderRequestContext,
  caseId: string,
): Promise<OperationalRiskApiResult<RawOperationalRiskCaseDetail>> {
  return getJson(context, `/api/operational-risks/cases/${caseId}`, undefined, (payload) => payload as RawOperationalRiskCaseDetail);
}

export async function transitionOperationalRiskCase(
  context: ProductionOrderRequestContext,
  caseId: string,
  input: Readonly<{
    expectedVersion: number;
    targetStatus: string;
    note?: string;
    sourceProvenance: string;
  }>,
): Promise<OperationalRiskApiResult<Readonly<{ entityId: string; version: number; idempotentReplay: boolean }>>> {
  return writeJson(context, "POST", `/api/operational-risks/cases/${caseId}/transitions`, {
    expectedVersion: input.expectedVersion,
    targetStatus: input.targetStatus,
    note: input.note ?? null,
    sourceProvenance: input.sourceProvenance,
  }, (payload) => {
    const data = payload as { id: string; version: number; idempotentReplay?: boolean };
    return {
      entityId: data.id,
      version: data.version,
      idempotentReplay: data.idempotentReplay === true,
    };
  });
}

export async function listOperationalRiskDetectionProfiles(
  context: ProductionOrderRequestContext,
): Promise<OperationalRiskApiResult<Readonly<{
  items: readonly RawOperationalRiskDetectionProfile[];
  ruleSetVersion: string;
  provenanceDisclaimer: string;
}>>> {
  return getJson(context, "/api/operational-risks/detection/profiles", undefined, (payload) => {
    const data = payload as {
      items: RawOperationalRiskDetectionProfile[];
      ruleSetVersion: string;
      provenanceDisclaimer: string;
    };
    return {
      items: data.items ?? [],
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
    };
  });
}

export async function updateOperationalRiskDetectionProfile(
  context: ProductionOrderRequestContext,
  profileId: string,
  input: Readonly<{
    expectedVersion: number;
    displayName: string;
    status: string;
    sourceProvenance: string;
  }>,
): Promise<OperationalRiskApiResult<Readonly<{ entityId: string; version: number; idempotentReplay: boolean }>>> {
  return writeJson(context, "PUT", `/api/operational-risks/detection/profiles/${profileId}`, input, (payload) => {
    const data = payload as { id: string; version: number; idempotentReplay?: boolean };
    return {
      entityId: data.id,
      version: data.version,
      idempotentReplay: data.idempotentReplay === true,
    };
  });
}

export async function listOperationalRiskDetectionRuns(
  context: ProductionOrderRequestContext,
  page = 1,
  pageSize = 25,
): Promise<OperationalRiskApiResult<Readonly<{
  items: readonly RawOperationalRiskDetectionRunSummary[];
  page: number;
  pageSize: number;
  ruleSetVersion: string;
}>>> {
  return getJson(context, "/api/operational-risks/detection/runs", {
    page: String(page),
    pageSize: String(pageSize),
  }, (payload) => {
    const data = payload as {
      items: RawOperationalRiskDetectionRunSummary[];
      page: number;
      pageSize: number;
      ruleSetVersion: string;
    };
    return {
      items: data.items ?? [],
      page: data.page ?? page,
      pageSize: data.pageSize ?? pageSize,
      ruleSetVersion: data.ruleSetVersion ?? "",
    };
  });
}

export async function getOperationalRiskDetectionRun(
  context: ProductionOrderRequestContext,
  runId: string,
): Promise<OperationalRiskApiResult<RawOperationalRiskDetectionRunDetail>> {
  return getJson(
    context,
    `/api/operational-risks/detection/runs/${runId}`,
    undefined,
    (payload) => payload as RawOperationalRiskDetectionRunDetail,
  );
}

export async function executeOperationalRiskDetectionRun(
  context: ProductionOrderRequestContext,
  input: Readonly<{
    profileId: string;
    windowStartUtc: string;
    windowEndUtc: string;
    sourceProvenance: string;
    idempotencyKey: string;
  }>,
): Promise<OperationalRiskApiResult<Readonly<{ entityId: string; version: number; idempotentReplay: boolean }>>> {
  return writeJson(context, "POST", "/api/operational-risks/detection/runs", input, (payload) => {
    const data = payload as { id: string; version: number; idempotentReplay?: boolean };
    return {
      entityId: data.id,
      version: data.version,
      idempotentReplay: data.idempotentReplay === true,
    };
  });
}

export { parseCaseSummary };
