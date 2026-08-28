import "./ai-task-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type AiTaskApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "concurrency-conflict"
    | "idempotency-conflict"
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type AiTaskApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | AiTaskApiFailure;

export type RawAiTaskSummary = Readonly<{
  id: string;
  taskReference: string;
  title: string;
  summary: string;
  category: string;
  priority: string;
  riskLevel: string;
  status: string;
  approval: Readonly<{ state: string }>;
  sourceModule: string | null;
  sourceEntityId: string | null;
  dueAt: string | null;
  createdAt: string;
  updatedAt: string;
  version: number;
}>;

export type RawAiTaskDetail = Readonly<{
  id: string;
  taskReference: string;
  title: string;
  summary: string;
  category: string;
  priority: string;
  riskLevel: string;
  status: string;
  detectedIssue: string;
  reasoning: string;
  recommendation: Readonly<{
    title: string;
    description: string;
    actionType: string;
    requiresApproval: boolean;
    reversible: boolean;
    targetSystem: string | null;
  }>;
  confidence: Readonly<{
    score: number;
    explanation: string;
    modelVersion: string | null;
  }>;
  impact: Readonly<{
    financialAmount: number | null;
    currency: string | null;
    productionQuantity: number | null;
    productionUnit: string | null;
    estimatedDelayMinutes: number | null;
    estimatedDowntimeMinutes: number | null;
    estimatedSavingsAmount: number | null;
    summary: string;
  }>;
  sources: ReadonlyArray<Readonly<{
    sourceType: string;
    sourceId: string | null;
    sourceLabel: string;
    observedAt: string | null;
    valueSummary: string | null;
  }>>;
  assignee: Readonly<{
    userId: string;
    displayName: string;
    role: string | null;
  }> | null;
  approval: Readonly<{
    state: string;
    requestedAt: string | null;
    requestedBy: string | null;
    decidedAt: string | null;
    decidedBy: string | null;
    decisionNote: string | null;
  }>;
  execution: Readonly<{
    startedAt: string | null;
    completedAt: string | null;
    targetSystem: string | null;
    externalReferenceId: string | null;
    previousValue: string | null;
    appliedValue: string | null;
    errorCode: string | null;
    errorMessage: string | null;
  }> | null;
  siteId: string | null;
  productionLineId: string | null;
  machineId: string | null;
  moldId: string | null;
  productionOrderId: string | null;
  salesOrderId: string | null;
  materialId: string | null;
  supplierId: string | null;
  customerId: string | null;
  sourceModule: string | null;
  sourceEntityId: string | null;
  ruleSetVersion: string;
  dueAt: string | null;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
  version: number;
  auditTrail: ReadonlyArray<Readonly<{
    id: string;
    taskId: string;
    eventType: string;
    occurredAt: string;
    actorType: string;
    actorId: string | null;
    actorLabel: string;
    note: string | null;
    previousStatus: string | null;
    nextStatus: string | null;
  }>>;
}>;

export type RawAiTaskListResponse = Readonly<{
  items: readonly RawAiTaskSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  capabilities: Readonly<{
    canRead: boolean;
    canCreate: boolean;
    canReview: boolean;
  }>;
}>;

export type CreateAiTaskPayload = Readonly<{
  title: string;
  summary: string;
  category: string;
  priority: string;
  riskLevel: string;
  detectedIssue: string;
  reasoning: string;
  recommendation: Readonly<{
    title: string;
    description: string;
    actionType: string;
    requiresApproval: boolean;
    reversible: boolean;
    targetSystem?: string | null;
  }>;
  confidence: Readonly<{
    score: number;
    explanation: string;
    modelVersion?: string | null;
  }>;
  impact: Readonly<{
    financialAmount?: number | null;
    currency?: string | null;
    productionQuantity?: number | null;
    productionUnit?: string | null;
    estimatedDelayMinutes?: number | null;
    estimatedDowntimeMinutes?: number | null;
    estimatedSavingsAmount?: number | null;
    summary: string;
  }>;
  sources: ReadonlyArray<Readonly<{
    sourceType: string;
    sourceId?: string | null;
    sourceLabel: string;
    observedAt?: string | null;
    valueSummary?: string | null;
  }>>;
  siteId?: string | null;
  productionLineId?: string | null;
  machineId?: string | null;
  moldId?: string | null;
  productionOrderId?: string | null;
  salesOrderId?: string | null;
  materialId?: string | null;
  supplierId?: string | null;
  customerId?: string | null;
  dueAtUtc?: string | null;
}>;

export type CreateSourceLinkedAiTaskPayload = CreateAiTaskPayload & Readonly<{
  sourceModule: string;
  sourceEntityId: string;
  idempotencyKey: string;
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

function mapFailureStatus(status: number, payload: unknown): AiTaskApiFailure["code"] {
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
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
    return reason || detail || title || `AI görev isteği başarısız (${status}).`;
  }
  return `AI görev isteği başarısız (${status}).`;
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<AiTaskApiResult<T>> {
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
      message: "AI Görev Merkezi API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

async function writeJson<T>(
  context: ProductionOrderRequestContext,
  method: "POST",
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<AiTaskApiResult<T>> {
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
      message: "AI Görev Merkezi API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

function parseList(payload: unknown): RawAiTaskListResponse {
  const value = payload as RawAiTaskListResponse;
  return {
    items: value.items ?? [],
    totalCount: value.totalCount ?? 0,
    page: value.page ?? 1,
    pageSize: value.pageSize ?? 25,
    ruleSetVersion: value.ruleSetVersion ?? "",
    provenanceDisclaimer: value.provenanceDisclaimer ?? "",
    capabilities: value.capabilities ?? { canRead: true, canCreate: false, canReview: false },
  };
}

function parseWrite(payload: unknown): Readonly<{ entityId: string; version: number; idempotentReplay: boolean }> {
  const value = payload as { id?: string; version?: number; idempotentReplay?: boolean };
  return {
    entityId: String(value.id ?? ""),
    version: Number(value.version ?? 0),
    idempotentReplay: Boolean(value.idempotentReplay),
  };
}

export async function listAiTasks(
  context: ProductionOrderRequestContext,
  filters?: Readonly<{
    status?: string;
    category?: string;
    priority?: string;
    sourceModule?: string;
    page?: number;
    pageSize?: number;
  }>,
): Promise<AiTaskApiResult<RawAiTaskListResponse>> {
  return getJson(context, "/api/ai-tasks", {
    status: filters?.status,
    category: filters?.category,
    priority: filters?.priority,
    sourceModule: filters?.sourceModule,
    page: filters?.page !== undefined ? String(filters.page) : undefined,
    pageSize: filters?.pageSize !== undefined ? String(filters.pageSize) : undefined,
  }, parseList);
}

export async function getAiTask(
  context: ProductionOrderRequestContext,
  taskId: string,
): Promise<AiTaskApiResult<RawAiTaskDetail>> {
  return getJson(context, `/api/ai-tasks/${taskId}`, undefined, (payload) => payload as RawAiTaskDetail);
}

export async function getAiTaskByIdempotencyKey(
  context: ProductionOrderRequestContext,
  idempotencyKey: string,
): Promise<AiTaskApiResult<RawAiTaskDetail>> {
  return getJson(
    context,
    `/api/ai-tasks/by-idempotency/${encodeURIComponent(idempotencyKey)}`,
    undefined,
    (payload) => payload as RawAiTaskDetail,
  );
}

export async function getAiTaskBySource(
  context: ProductionOrderRequestContext,
  sourceModule: string,
  sourceEntityId: string,
): Promise<AiTaskApiResult<RawAiTaskDetail>> {
  return getJson(context, "/api/ai-tasks/by-source", {
    sourceModule,
    sourceEntityId,
  }, (payload) => payload as RawAiTaskDetail);
}

export async function createAiTask(
  context: ProductionOrderRequestContext,
  body: CreateAiTaskPayload,
): Promise<AiTaskApiResult<ReturnType<typeof parseWrite>>> {
  return writeJson(context, "POST", "/api/ai-tasks", body, parseWrite);
}

export async function createSourceLinkedAiTask(
  context: ProductionOrderRequestContext,
  body: CreateSourceLinkedAiTaskPayload,
): Promise<AiTaskApiResult<ReturnType<typeof parseWrite>>> {
  return writeJson(context, "POST", "/api/ai-tasks/source-linked", body, parseWrite);
}

export async function transitionAiTask(
  context: ProductionOrderRequestContext,
  taskId: string,
  body: Readonly<{ expectedVersion: number; targetStatus: string; note?: string | null }>,
): Promise<AiTaskApiResult<ReturnType<typeof parseWrite>>> {
  return writeJson(context, "POST", `/api/ai-tasks/${taskId}/transitions`, body, parseWrite);
}
