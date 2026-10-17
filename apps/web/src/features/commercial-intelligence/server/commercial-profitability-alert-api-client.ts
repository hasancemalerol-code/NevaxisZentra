import { stripFixtureRecords } from "@/lib/runtime/fixture-data";
import { refusalCode } from "@/lib/api/api-failure-classifier";
import "./commercial-intelligence-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  CommercialIntelligenceApiFailure,
  CommercialIntelligenceApiResult,
} from "./commercial-intelligence-api-client";
import type { RawCommercialProfitabilityComparisonDetail } from "./commercial-profitability-api-client";

export type RawCommercialProfitabilityAlertWorkspace = Readonly<{
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  kpis: Readonly<{
    activeCount: number;
    highOrCriticalCount: number;
    acknowledgedCount: number;
    taskCreatedCount: number;
    evidenceReviewCount: number;
  }>;
  capabilities: Readonly<{
    canRead: boolean;
    canManage: boolean;
    canCreateTask: boolean;
  }>;
  automationDisclaimer: string;
}>;

export type RawCommercialProfitabilityAlertSummary = Readonly<{
  id: string;
  alertReference: string;
  alertCategory: string;
  severity: string;
  lifecycleStatus: string;
  sourceComparisonId: string;
  sourceComparisonType: string;
  title: string;
  evidenceSummary: string;
  evidenceStatus: string;
  comparisonOutcome: string;
  leadingDriverCode: string | null;
  linkedAiTaskId: string | null;
  materializedAtUtc: string;
  updatedAtUtc: string;
  version: number;
}>;

export type RawCommercialProfitabilityAlertAuditEvent = Readonly<{
  id: string;
  eventType: string;
  payloadJson: string;
  actorSubjectId: string;
  occurredAtUtc: string;
}>;

export type RawCommercialProfitabilityAlertDetail = RawCommercialProfitabilityAlertSummary & Readonly<{
  sourceSnapshotJson: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  sourceComparison: RawCommercialProfitabilityComparisonDetail;
  auditEvents: readonly RawCommercialProfitabilityAlertAuditEvent[];
  automationDisclaimer: string;
}>;

export type RawCommercialProfitabilityAlertMaterialization = Readonly<{
  createdCount: number;
  refreshedCount: number;
  skippedCount: number;
  alertIds: readonly string[];
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

function mapFailureStatus(status: number, payload: unknown): CommercialIntelligenceApiFailure["code"] {
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
  if (status === 400) return "invalid-request";
  return "source-unavailable";
}

function failureMessage(status: number, payload: unknown): string {
  if (typeof payload === "object" && payload) {
    const reason = "failureReason" in payload ? String((payload as { failureReason?: string }).failureReason ?? "") : "";
    const detail = "detail" in payload ? String((payload as { detail?: string }).detail ?? "") : "";
    const title = "title" in payload ? String((payload as { title?: string }).title ?? "") : "";
    return reason || detail || title || `Kârlılık uyarısı isteği başarısız (${status}).`;
  }
  return `Kârlılık uyarısı isteği başarısız (${status}).`;
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<CommercialIntelligenceApiResult<T>> {
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
      message: "Kârlılık uyarısı API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

async function postJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<CommercialIntelligenceApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method: "POST",
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
      message: "Kârlılık uyarısı yazma isteği başarısız oldu.",
      retryable: true,
    };
  }
}

export async function getCommercialProfitabilityAlertWorkspace(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<RawCommercialProfitabilityAlertWorkspace>> {
  return getJson(context, "/api/commercial-intelligence/profitability/alerts/workspace", undefined, (payload) => {
    const data = payload as RawCommercialProfitabilityAlertWorkspace;
    return {
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
      kpis: {
        activeCount: data.kpis?.activeCount ?? 0,
        highOrCriticalCount: data.kpis?.highOrCriticalCount ?? 0,
        acknowledgedCount: data.kpis?.acknowledgedCount ?? 0,
        taskCreatedCount: data.kpis?.taskCreatedCount ?? 0,
        evidenceReviewCount: data.kpis?.evidenceReviewCount ?? 0,
      },
      capabilities: {
        canRead: data.capabilities?.canRead !== false,
        canManage: data.capabilities?.canManage === true,
        canCreateTask: data.capabilities?.canCreateTask === true,
      },
      automationDisclaimer: data.automationDisclaimer ?? "Otomatik ticari işlem yapılmaz.",
    };
  });
}

export async function listCommercialProfitabilityAlerts(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    category?: string;
    severity?: string;
    lifecycleStatus?: string;
    evidenceStatus?: string;
    comparisonType?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialProfitabilityAlertSummary[];
  ruleSetVersion: string;
  automationDisclaimer: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/profitability/alerts", {
    category: filters.category,
    severity: filters.severity,
    lifecycleStatus: filters.lifecycleStatus,
    evidenceStatus: filters.evidenceStatus,
    comparisonType: filters.comparisonType,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, (payload) => {
    const data = payload as {
      items: RawCommercialProfitabilityAlertSummary[];
      ruleSetVersion: string;
      automationDisclaimer: string;
    };
    return {
      items: data.items ?? [],
      ruleSetVersion: data.ruleSetVersion ?? "",
      automationDisclaimer: data.automationDisclaimer ?? "Otomatik ticari işlem yapılmaz.",
    };
  });
}

export async function getCommercialProfitabilityAlert(
  context: ProductionOrderRequestContext,
  alertId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialProfitabilityAlertDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/profitability/alerts/${alertId}`,
    undefined,
    (payload) => payload as RawCommercialProfitabilityAlertDetail,
  );
}

export async function materializeCommercialProfitabilityAlerts(
  context: ProductionOrderRequestContext,
  input: Readonly<{ idempotencyKey?: string | null }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  entityId: string | null;
  version: number;
  idempotentReplay: boolean;
  alert: RawCommercialProfitabilityAlertDetail | null;
  materialization: RawCommercialProfitabilityAlertMaterialization | null;
}>>> {
  return postJson(
    context,
    "/api/commercial-intelligence/profitability/alerts/materialize",
    { idempotencyKey: input.idempotencyKey ?? null },
    (payload) => {
      const data = payload as {
        entityId?: string | null;
        version?: number;
        idempotentReplay?: boolean;
        alert?: RawCommercialProfitabilityAlertDetail | null;
        materialization?: RawCommercialProfitabilityAlertMaterialization | null;
      };
      return {
        entityId: data.entityId ?? null,
        version: data.version ?? 0,
        idempotentReplay: data.idempotentReplay === true,
        alert: data.alert ?? null,
        materialization: data.materialization ?? null,
      };
    },
  );
}

export async function transitionCommercialProfitabilityAlert(
  context: ProductionOrderRequestContext,
  alertId: string,
  input: Readonly<{
    expectedVersion: number;
    targetLifecycleStatus: string;
    note?: string | null;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  entityId: string;
  version: number;
  idempotentReplay: boolean;
  alert: RawCommercialProfitabilityAlertDetail | null;
}>>> {
  return postJson(
    context,
    `/api/commercial-intelligence/profitability/alerts/${alertId}/transitions`,
    {
      expectedVersion: input.expectedVersion,
      targetLifecycleStatus: input.targetLifecycleStatus,
      note: input.note ?? null,
    },
    (payload) => {
      const data = payload as {
        entityId: string;
        version: number;
        idempotentReplay?: boolean;
        alert?: RawCommercialProfitabilityAlertDetail | null;
      };
      return {
        entityId: data.entityId,
        version: data.version,
        idempotentReplay: data.idempotentReplay === true,
        alert: data.alert ?? null,
      };
    },
  );
}

export async function createCommercialProfitabilityAlertTaskDraft(
  context: ProductionOrderRequestContext,
  alertId: string,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  taskId: string;
  alertId: string;
  idempotentReplay: boolean;
  alert: RawCommercialProfitabilityAlertDetail;
}>>> {
  return postJson(
    context,
    `/api/commercial-intelligence/profitability/alerts/${alertId}/task-draft`,
    {},
    (payload) => {
      const data = payload as {
        taskId: string;
        alertId: string;
        idempotentReplay?: boolean;
        alert: RawCommercialProfitabilityAlertDetail;
      };
      return {
        taskId: data.taskId,
        alertId: data.alertId,
        idempotentReplay: data.idempotentReplay === true,
        alert: data.alert,
      };
    },
  );
}
