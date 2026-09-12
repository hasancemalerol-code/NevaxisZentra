import "./ai-task-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  AiTaskAutomationAction,
  AiTaskAutomationCondition,
  AiTaskAutomationEvaluation,
  AiTaskAutomationExecution,
  AiTaskAutomationRuleDetail,
  AiTaskAutomationRuleRevision,
  AiTaskAutomationRuleSummary,
} from "../domain/ai-task-automation";

export type AiTaskAutomationApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "concurrency-conflict"
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type AiTaskAutomationApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | AiTaskAutomationApiFailure;

const AUTOMATION_BASE = "/api/ai-tasks/automation";

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

function mapFailureStatus(status: number, _payload: unknown): AiTaskAutomationApiFailure["code"] {
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "concurrency-conflict";
  return "source-unavailable";
}

function failureMessage(status: number, payload: unknown): string {
  if (typeof payload === "object" && payload) {
    const reason = "failureReason" in payload ? String((payload as { failureReason?: string }).failureReason ?? "") : "";
    const detail = "detail" in payload ? String((payload as { detail?: string }).detail ?? "") : "";
    const title = "title" in payload ? String((payload as { title?: string }).title ?? "") : "";
    const message = "message" in payload ? String((payload as { message?: string }).message ?? "") : "";
    return reason || detail || title || message || `Otomasyon isteği başarısız (${status}).`;
  }
  return `Otomasyon isteği başarısız (${status}).`;
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<AiTaskAutomationApiResult<T>> {
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
      message: "AI görev otomasyon API'sine ulaşılamadı.",
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
  query?: Record<string, string | undefined>,
): Promise<AiTaskAutomationApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
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
      message: "AI görev otomasyon API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

function parseCondition(raw: unknown): AiTaskAutomationCondition {
  const value = raw as AiTaskAutomationCondition;
  return {
    field: String(value.field ?? ""),
    operator: String(value.operator ?? ""),
    value: value.value ?? null,
    values: value.values ?? null,
  };
}

function parseAction(raw: unknown): AiTaskAutomationAction {
  const value = raw as AiTaskAutomationAction;
  return {
    type: String(value.type ?? ""),
    value: value.value ?? null,
    guidValue: value.guidValue ?? null,
    intValue: value.intValue ?? null,
    boolValue: value.boolValue ?? null,
    values: value.values ?? null,
  };
}

function parseRuleSummary(raw: unknown): AiTaskAutomationRuleSummary {
  const value = raw as AiTaskAutomationRuleSummary;
  return {
    id: String(value.id ?? ""),
    ruleKey: String(value.ruleKey ?? ""),
    name: String(value.name ?? ""),
    description: value.description ?? null,
    facilityId: value.facilityId ?? null,
    triggerType: String(value.triggerType ?? ""),
    sortOrder: Number(value.sortOrder ?? 0),
    enabled: Boolean(value.enabled),
    version: Number(value.version ?? 0),
    updatedAtUtc: String(value.updatedAtUtc ?? ""),
    lastMatchedAtUtc: value.lastMatchedAtUtc ?? null,
    actionSummaries: value.actionSummaries ?? [],
  };
}

function parseRuleDetail(raw: unknown): AiTaskAutomationRuleDetail {
  const value = raw as AiTaskAutomationRuleDetail & {
    conditions?: unknown[];
    actions?: unknown[];
  };
  return {
    id: String(value.id ?? ""),
    ruleKey: String(value.ruleKey ?? ""),
    name: String(value.name ?? ""),
    description: value.description ?? null,
    facilityId: value.facilityId ?? null,
    triggerType: String(value.triggerType ?? ""),
    sortOrder: Number(value.sortOrder ?? 0),
    enabled: Boolean(value.enabled),
    version: Number(value.version ?? 0),
    conditions: (value.conditions ?? []).map(parseCondition),
    actions: (value.actions ?? []).map(parseAction),
    effectiveFromUtc: value.effectiveFromUtc ?? null,
    effectiveUntilUtc: value.effectiveUntilUtc ?? null,
    createdAtUtc: String(value.createdAtUtc ?? ""),
    updatedAtUtc: String(value.updatedAtUtc ?? ""),
    lastMatchedAtUtc: value.lastMatchedAtUtc ?? null,
    createdBy: String(value.createdBy ?? ""),
    updatedBy: String(value.updatedBy ?? ""),
  };
}

function parseRevision(raw: unknown): AiTaskAutomationRuleRevision {
  const value = raw as AiTaskAutomationRuleRevision;
  return {
    id: String(value.id ?? ""),
    version: Number(value.version ?? 0),
    name: String(value.name ?? ""),
    enabled: Boolean(value.enabled),
    changedBy: String(value.changedBy ?? ""),
    changedAtUtc: String(value.changedAtUtc ?? ""),
    changeReason: String(value.changeReason ?? ""),
  };
}

function parseExecution(raw: unknown): AiTaskAutomationExecution {
  const value = raw as AiTaskAutomationExecution;
  return {
    id: String(value.id ?? ""),
    taskId: String(value.taskId ?? ""),
    ruleId: String(value.ruleId ?? ""),
    ruleName: String(value.ruleName ?? ""),
    ruleVersion: Number(value.ruleVersion ?? 0),
    triggerType: String(value.triggerType ?? ""),
    executedAtUtc: String(value.executedAtUtc ?? ""),
    actionsApplied: value.actionsApplied ?? [],
    simulated: Boolean(value.simulated),
  };
}

function parseEvaluation(raw: unknown): AiTaskAutomationEvaluation {
  const value = raw as AiTaskAutomationEvaluation & {
    matchedRules?: unknown[];
    skippedRules?: unknown[];
    proposedChanges?: unknown[];
    conflicts?: unknown[];
    actionsApplied?: unknown[];
  };
  return {
    triggerType: String(value.triggerType ?? ""),
    matchedRules: (value.matchedRules ?? []).map((item) => {
      const rule = item as AiTaskAutomationEvaluation["matchedRules"][number];
      return {
        ruleId: String(rule.ruleId ?? ""),
        ruleName: String(rule.ruleName ?? ""),
        ruleVersion: Number(rule.ruleVersion ?? 0),
        sortOrder: Number(rule.sortOrder ?? 0),
        matchedConditions: (rule.matchedConditions ?? []).map(parseCondition),
      };
    }),
    skippedRules: (value.skippedRules ?? []).map((item) => {
      const rule = item as AiTaskAutomationEvaluation["skippedRules"][number];
      return {
        ruleId: String(rule.ruleId ?? ""),
        ruleName: String(rule.ruleName ?? ""),
        reason: String(rule.reason ?? ""),
      };
    }),
    proposedChanges: (value.proposedChanges ?? []).map((item) => {
      const change = item as AiTaskAutomationEvaluation["proposedChanges"][number];
      return {
        field: String(change.field ?? ""),
        currentValue: change.currentValue ?? null,
        proposedValue: change.proposedValue ?? null,
        sourceRuleId: change.sourceRuleId ?? null,
        sourceRuleName: change.sourceRuleName ?? null,
      };
    }),
    conflicts: (value.conflicts ?? []).map((item) => {
      const conflict = item as AiTaskAutomationEvaluation["conflicts"][number];
      return {
        field: String(conflict.field ?? ""),
        competingValues: conflict.competingValues ?? [],
        resolution: String(conflict.resolution ?? ""),
        winningRuleId: conflict.winningRuleId ?? null,
      };
    }),
    actionsApplied: (value.actionsApplied ?? []).map(parseAction),
    simulated: Boolean(value.simulated),
    outcomeSummary: String(value.outcomeSummary ?? ""),
  };
}

function parseWriteResult(payload: unknown): Readonly<{ ruleId: string; version: number }> {
  const value = payload as { ruleId?: string; version?: number };
  return {
    ruleId: String(value.ruleId ?? ""),
    version: Number(value.version ?? 0),
  };
}

export async function listAiTaskAutomationRules(
  context: ProductionOrderRequestContext,
): Promise<AiTaskAutomationApiResult<readonly AiTaskAutomationRuleSummary[]>> {
  return getJson(context, `${AUTOMATION_BASE}/rules`, undefined, (payload) => {
    const value = payload as { items?: unknown[] };
    return (value.items ?? []).map(parseRuleSummary);
  });
}

export async function getAiTaskAutomationRule(
  context: ProductionOrderRequestContext,
  ruleId: string,
): Promise<AiTaskAutomationApiResult<AiTaskAutomationRuleDetail>> {
  return getJson(
    context,
    `${AUTOMATION_BASE}/rules/${ruleId}`,
    undefined,
    (payload) => parseRuleDetail(payload),
  );
}

export async function createAiTaskAutomationRule(
  context: ProductionOrderRequestContext,
  body: Readonly<{
    ruleKey: string;
    name: string;
    description?: string | null;
    facilityId?: string | null;
    triggerType: string;
    sortOrder: number;
    enabled: boolean;
    conditions: readonly AiTaskAutomationCondition[];
    actions: readonly AiTaskAutomationAction[];
    effectiveFromUtc?: string | null;
    effectiveUntilUtc?: string | null;
  }>,
): Promise<AiTaskAutomationApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(context, "POST", `${AUTOMATION_BASE}/rules`, body, parseWriteResult);
}

export async function updateAiTaskAutomationRule(
  context: ProductionOrderRequestContext,
  ruleId: string,
  body: Readonly<{
    expectedVersion: number;
    name: string;
    description?: string | null;
    triggerType: string;
    sortOrder: number;
    enabled: boolean;
    conditions: readonly AiTaskAutomationCondition[];
    actions: readonly AiTaskAutomationAction[];
    effectiveFromUtc?: string | null;
    effectiveUntilUtc?: string | null;
    changeReason: string;
  }>,
): Promise<AiTaskAutomationApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "PUT",
    `${AUTOMATION_BASE}/rules/${ruleId}`,
    body,
    parseWriteResult,
  );
}

export async function enableAiTaskAutomationRule(
  context: ProductionOrderRequestContext,
  ruleId: string,
  expectedVersion: number,
): Promise<AiTaskAutomationApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${AUTOMATION_BASE}/rules/${ruleId}/enable`,
    { expectedVersion },
    parseWriteResult,
  );
}

export async function disableAiTaskAutomationRule(
  context: ProductionOrderRequestContext,
  ruleId: string,
  expectedVersion: number,
): Promise<AiTaskAutomationApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${AUTOMATION_BASE}/rules/${ruleId}/disable`,
    { expectedVersion },
    parseWriteResult,
  );
}

export async function listAiTaskAutomationRuleHistory(
  context: ProductionOrderRequestContext,
  ruleId: string,
): Promise<AiTaskAutomationApiResult<readonly AiTaskAutomationRuleRevision[]>> {
  return getJson(
    context,
    `${AUTOMATION_BASE}/rules/${ruleId}/history`,
    undefined,
    (payload) => {
      const value = payload as { items?: unknown[] };
      return (value.items ?? []).map(parseRevision);
    },
  );
}

export async function simulateAiTaskAutomation(
  context: ProductionOrderRequestContext,
  body: Readonly<{
    ruleId?: string | null;
    taskId: string;
    triggerType: string;
  }>,
): Promise<AiTaskAutomationApiResult<AiTaskAutomationEvaluation>> {
  return writeJson(
    context,
    "POST",
    `${AUTOMATION_BASE}/simulate`,
    {
      ruleId: body.ruleId ?? null,
      taskId: body.taskId,
      triggerType: body.triggerType,
    },
    parseEvaluation,
  );
}

export async function listAiTaskAutomationExecutions(
  context: ProductionOrderRequestContext,
  filters?: Readonly<{ taskId?: string; page?: number; pageSize?: number }>,
): Promise<AiTaskAutomationApiResult<readonly AiTaskAutomationExecution[]>> {
  return getJson(
    context,
    `${AUTOMATION_BASE}/executions`,
    {
      taskId: filters?.taskId,
      page: filters?.page !== undefined ? String(filters.page) : undefined,
      pageSize: filters?.pageSize !== undefined ? String(filters.pageSize) : undefined,
    },
    (payload) => {
      const value = payload as { items?: unknown[] };
      return (value.items ?? []).map(parseExecution);
    },
  );
}
