import "./ai-task-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  AiTaskPlaybookDependencyDefinition,
  AiTaskPlaybookDetail,
  AiTaskPlaybookGraphValidationResult,
  AiTaskPlaybookRunDetail,
  AiTaskPlaybookRunSummary,
  AiTaskPlaybookStepDefinition,
  AiTaskPlaybookSummary,
} from "../domain/ai-task-playbook";

export type AiTaskPlaybookApiFailure = Readonly<{
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

export type AiTaskPlaybookApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | AiTaskPlaybookApiFailure;

const PLAYBOOK_BASE = "/api/ai-tasks/playbooks";
const RUN_BASE = "/api/ai-tasks/playbook-runs";

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

function mapFailureStatus(status: number): AiTaskPlaybookApiFailure["code"] {
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
    return reason || detail || title || message || `Playbook isteği başarısız (${status}).`;
  }
  return `Playbook isteği başarısız (${status}).`;
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<AiTaskPlaybookApiResult<T>> {
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
        code: mapFailureStatus(response.status),
        message: failureMessage(response.status, payload),
        retryable: response.status >= 500,
      };
    }

    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "AI görev playbook API'sine ulaşılamadı.",
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
): Promise<AiTaskPlaybookApiResult<T>> {
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
        code: mapFailureStatus(response.status),
        message: failureMessage(response.status, payload),
        retryable: response.status >= 500,
      };
    }

    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "AI görev playbook API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

function parseCondition(raw: unknown) {
  if (!raw || typeof raw !== "object") return null;
  const value = raw as { field?: string; operator?: string; value?: string };
  return {
    field: String(value.field ?? ""),
    operator: String(value.operator ?? ""),
    value: String(value.value ?? ""),
  };
}

function parseStepDefinition(raw: unknown): AiTaskPlaybookStepDefinition {
  const value = raw as AiTaskPlaybookStepDefinition;
  return {
    stepKey: String(value.stepKey ?? ""),
    name: String(value.name ?? ""),
    description: value.description ?? null,
    sortOrder: Number(value.sortOrder ?? 0),
    stepType: String(value.stepType ?? "task"),
    required: Boolean(value.required),
    defaultPriority: String(value.defaultPriority ?? "medium"),
    assignmentPolicyJson: value.assignmentPolicyJson ?? null,
    slaPolicyJson: value.slaPolicyJson ?? null,
    evidencePolicyJson: value.evidencePolicyJson ?? null,
    closurePolicyJson: value.closurePolicyJson ?? null,
    condition: value.condition ? parseCondition(value.condition) : null,
    targetDurationMinutes: value.targetDurationMinutes ?? null,
  };
}

function parseDependencyDefinition(raw: unknown): AiTaskPlaybookDependencyDefinition {
  const value = raw as AiTaskPlaybookDependencyDefinition;
  return {
    stepKey: String(value.stepKey ?? ""),
    dependsOnStepKey: String(value.dependsOnStepKey ?? ""),
    dependencyType: String(value.dependencyType ?? "required-predecessor"),
  };
}

function parseSummary(raw: unknown): AiTaskPlaybookSummary {
  const value = raw as AiTaskPlaybookSummary;
  return {
    id: String(value.id ?? ""),
    playbookKey: String(value.playbookKey ?? ""),
    name: String(value.name ?? ""),
    category: String(value.category ?? ""),
    facilityId: value.facilityId ?? null,
    version: Number(value.version ?? 0),
    enabled: Boolean(value.enabled),
    stepCount: Number(value.stepCount ?? 0),
    updatedAtUtc: String(value.updatedAtUtc ?? ""),
    lastRunAtUtc: value.lastRunAtUtc ?? null,
  };
}

function parseStep(raw: unknown) {
  const value = raw as {
    id?: string;
    stepKey?: string;
    name?: string;
    description?: string | null;
    sortOrder?: number;
    stepType?: string;
    required?: boolean;
    defaultPriority?: string;
    assignmentPolicyJson?: string | null;
    slaPolicyJson?: string | null;
    evidencePolicyJson?: string | null;
    closurePolicyJson?: string | null;
    condition?: unknown;
    targetDurationMinutes?: number | null;
  };
  return {
    id: String(value.id ?? ""),
    stepKey: String(value.stepKey ?? ""),
    name: String(value.name ?? ""),
    description: value.description ?? null,
    sortOrder: Number(value.sortOrder ?? 0),
    stepType: String(value.stepType ?? "task"),
    required: Boolean(value.required),
    defaultPriority: String(value.defaultPriority ?? "medium"),
    assignmentPolicyJson: value.assignmentPolicyJson ?? null,
    slaPolicyJson: value.slaPolicyJson ?? null,
    evidencePolicyJson: value.evidencePolicyJson ?? null,
    closurePolicyJson: value.closurePolicyJson ?? null,
    condition: value.condition ? parseCondition(value.condition) : null,
    targetDurationMinutes: value.targetDurationMinutes ?? null,
  };
}

function parseDependency(raw: unknown) {
  const value = raw as {
    id?: string;
    stepKey?: string;
    dependsOnStepKey?: string;
    dependencyType?: string;
  };
  return {
    id: String(value.id ?? ""),
    stepKey: String(value.stepKey ?? ""),
    dependsOnStepKey: String(value.dependsOnStepKey ?? ""),
    dependencyType: String(value.dependencyType ?? "required-predecessor"),
  };
}

function parseDetail(raw: unknown): AiTaskPlaybookDetail {
  const value = raw as AiTaskPlaybookDetail & { steps?: unknown[]; dependencies?: unknown[] };
  return {
    id: String(value.id ?? ""),
    playbookKey: String(value.playbookKey ?? ""),
    name: String(value.name ?? ""),
    description: value.description ?? null,
    category: String(value.category ?? ""),
    triggerContext: value.triggerContext ?? null,
    facilityId: value.facilityId ?? null,
    version: Number(value.version ?? 0),
    enabled: Boolean(value.enabled),
    effectiveFromUtc: value.effectiveFromUtc ?? null,
    effectiveUntilUtc: value.effectiveUntilUtc ?? null,
    steps: (value.steps ?? []).map(parseStep),
    dependencies: (value.dependencies ?? []).map(parseDependency),
    createdAtUtc: String(value.createdAtUtc ?? ""),
    updatedAtUtc: String(value.updatedAtUtc ?? ""),
  };
}

function parseRunSummary(raw: unknown): AiTaskPlaybookRunSummary {
  const value = raw as AiTaskPlaybookRunSummary & { runId?: string };
  return {
    id: String(value.id ?? value.runId ?? ""),
    playbookId: String(value.playbookId ?? ""),
    playbookName: String(value.playbookName ?? ""),
    status: String(value.status ?? ""),
    facilityId: String(value.facilityId ?? ""),
    triggerSource: String(value.triggerSource ?? ""),
    sourceTaskId: value.sourceTaskId ?? null,
    requiredStepsCompleted: Number(value.requiredStepsCompleted ?? 0),
    requiredStepsTotal: Number(value.requiredStepsTotal ?? 0),
    optionalStepsCompleted: Number(value.optionalStepsCompleted ?? 0),
    optionalStepsTotal: Number(value.optionalStepsTotal ?? 0),
    blockedStepsCount: Number(value.blockedStepsCount ?? 0),
    approvalsPending: Number(value.approvalsPending ?? 0),
    atRisk: Boolean(value.atRisk),
    startedAtUtc: String(value.startedAtUtc ?? ""),
    completedAtUtc: value.completedAtUtc ?? null,
  };
}

function parseRunStep(raw: unknown) {
  const value = raw as {
    id?: string;
    stepKey?: string;
    name?: string;
    required?: boolean;
    stepType?: string;
    status?: string;
    dependencyState?: string;
    linkedTaskId?: string | null;
    blockReason?: string | null;
    sortOrder?: number;
    readyAtUtc?: string | null;
    startedAtUtc?: string | null;
    completedAtUtc?: string | null;
  };
  return {
    id: String(value.id ?? ""),
    stepKey: String(value.stepKey ?? ""),
    name: String(value.name ?? ""),
    required: Boolean(value.required),
    stepType: String(value.stepType ?? "task"),
    status: String(value.status ?? ""),
    dependencyState: String(value.dependencyState ?? ""),
    linkedTaskId: value.linkedTaskId ?? null,
    blockReason: value.blockReason ?? null,
    sortOrder: Number(value.sortOrder ?? 0),
    readyAtUtc: value.readyAtUtc ?? null,
    startedAtUtc: value.startedAtUtc ?? null,
    completedAtUtc: value.completedAtUtc ?? null,
  };
}

function parseRunDetail(raw: unknown): AiTaskPlaybookRunDetail {
  const value = raw as AiTaskPlaybookRunDetail & { steps?: unknown[] };
  return {
    id: String(value.id ?? ""),
    playbookId: String(value.playbookId ?? ""),
    playbookName: String(value.playbookName ?? ""),
    playbookVersion: Number(value.playbookVersion ?? 0),
    status: String(value.status ?? ""),
    facilityId: String(value.facilityId ?? ""),
    triggerSource: String(value.triggerSource ?? ""),
    sourceTaskId: value.sourceTaskId ?? null,
    sourceEntityJson: value.sourceEntityJson ?? null,
    parentTaskId: value.parentTaskId ?? null,
    requiredStepsCompleted: Number(value.requiredStepsCompleted ?? 0),
    requiredStepsTotal: Number(value.requiredStepsTotal ?? 0),
    optionalStepsCompleted: Number(value.optionalStepsCompleted ?? 0),
    optionalStepsTotal: Number(value.optionalStepsTotal ?? 0),
    blockedStepsCount: Number(value.blockedStepsCount ?? 0),
    approvalsPending: Number(value.approvalsPending ?? 0),
    atRisk: Boolean(value.atRisk),
    blockReason: value.blockReason ?? null,
    criticalPathJson: value.criticalPathJson ?? null,
    steps: (value.steps ?? []).map(parseRunStep),
    startedAtUtc: String(value.startedAtUtc ?? ""),
    completedAtUtc: value.completedAtUtc ?? null,
    cancelledAtUtc: value.cancelledAtUtc ?? null,
    cancellationReason: value.cancellationReason ?? null,
  };
}

function parseWriteResult(payload: unknown): Readonly<{ id: string; version: number; runId: string | null }> {
  const value = payload as { id?: string; version?: number; runId?: string | null };
  return {
    id: String(value.id ?? ""),
    version: Number(value.version ?? 0),
    runId: value.runId ?? null,
  };
}

export async function listAiTaskPlaybooks(
  context: ProductionOrderRequestContext,
): Promise<AiTaskPlaybookApiResult<readonly AiTaskPlaybookSummary[]>> {
  return getJson(context, `${PLAYBOOK_BASE}/`, undefined, (payload) => {
    const value = payload as { items?: unknown[] };
    return (value.items ?? []).map(parseSummary);
  });
}

export async function getAiTaskPlaybook(
  context: ProductionOrderRequestContext,
  playbookId: string,
): Promise<AiTaskPlaybookApiResult<AiTaskPlaybookDetail>> {
  return getJson(
    context,
    `${PLAYBOOK_BASE}/${playbookId}`,
    undefined,
    (payload) => parseDetail(payload),
  );
}

export async function createAiTaskPlaybook(
  context: ProductionOrderRequestContext,
  body: Readonly<{
    playbookKey: string;
    name: string;
    description?: string | null;
    category: string;
    triggerContext?: string | null;
    enabled: boolean;
    effectiveFromUtc?: string | null;
    effectiveUntilUtc?: string | null;
    steps: readonly AiTaskPlaybookStepDefinition[];
    dependencies: readonly AiTaskPlaybookDependencyDefinition[];
  }>,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(context, "POST", `${PLAYBOOK_BASE}/`, body, parseWriteResult);
}

export async function updateAiTaskPlaybook(
  context: ProductionOrderRequestContext,
  playbookId: string,
  body: Readonly<{
    expectedVersion: number;
    name: string;
    description?: string | null;
    category: string;
    triggerContext?: string | null;
    enabled: boolean;
    effectiveFromUtc?: string | null;
    effectiveUntilUtc?: string | null;
    steps: readonly AiTaskPlaybookStepDefinition[];
    dependencies: readonly AiTaskPlaybookDependencyDefinition[];
    changeReason: string;
  }>,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "PUT",
    `${PLAYBOOK_BASE}/${playbookId}`,
    body,
    parseWriteResult,
  );
}

export async function enableAiTaskPlaybook(
  context: ProductionOrderRequestContext,
  playbookId: string,
  expectedVersion: number,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${PLAYBOOK_BASE}/${playbookId}/enable`,
    { expectedVersion },
    parseWriteResult,
  );
}

export async function disableAiTaskPlaybook(
  context: ProductionOrderRequestContext,
  playbookId: string,
  expectedVersion: number,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${PLAYBOOK_BASE}/${playbookId}/disable`,
    { expectedVersion },
    parseWriteResult,
  );
}

export async function validateAiTaskPlaybookGraph(
  context: ProductionOrderRequestContext,
  body: Readonly<{
    steps: readonly AiTaskPlaybookStepDefinition[];
    dependencies: readonly AiTaskPlaybookDependencyDefinition[];
  }>,
): Promise<AiTaskPlaybookApiResult<AiTaskPlaybookGraphValidationResult>> {
  return writeJson(
    context,
    "POST",
    `${PLAYBOOK_BASE}/validate-graph`,
    body,
    (payload) => {
      const value = payload as { isValid?: boolean; errors?: string[] };
      return {
        isValid: Boolean(value.isValid),
        errors: value.errors ?? [],
      };
    },
  );
}

export async function startAiTaskPlaybookRun(
  context: ProductionOrderRequestContext,
  playbookId: string,
  body: Readonly<{
    idempotencyKey?: string | null;
    triggerSource?: string | null;
    sourceTaskId?: string | null;
    sourceEntityJson?: string | null;
    correlationId?: string | null;
    contextValues?: Readonly<Record<string, string>> | null;
  }>,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${PLAYBOOK_BASE}/${playbookId}/runs`,
    body,
    parseWriteResult,
  );
}

export async function listAiTaskPlaybookRuns(
  context: ProductionOrderRequestContext,
  filters?: Readonly<{ playbookId?: string; status?: string }>,
): Promise<AiTaskPlaybookApiResult<readonly AiTaskPlaybookRunSummary[]>> {
  return getJson(
    context,
    `${RUN_BASE}/`,
    {
      playbookId: filters?.playbookId,
      status: filters?.status,
    },
    (payload) => {
      const value = payload as { items?: unknown[] };
      return (value.items ?? []).map(parseRunSummary);
    },
  );
}

export async function getAiTaskPlaybookRun(
  context: ProductionOrderRequestContext,
  runId: string,
): Promise<AiTaskPlaybookApiResult<AiTaskPlaybookRunDetail>> {
  return getJson(
    context,
    `${RUN_BASE}/${runId}`,
    undefined,
    (payload) => parseRunDetail(payload),
  );
}

export async function cancelAiTaskPlaybookRun(
  context: ProductionOrderRequestContext,
  runId: string,
  reason: string,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${RUN_BASE}/${runId}/cancel`,
    { reason },
    parseWriteResult,
  );
}

export async function blockAiTaskPlaybookRunStep(
  context: ProductionOrderRequestContext,
  runId: string,
  stepId: string,
  reason: string,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${RUN_BASE}/${runId}/steps/${stepId}/block`,
    { reason },
    parseWriteResult,
  );
}

export async function unblockAiTaskPlaybookRunStep(
  context: ProductionOrderRequestContext,
  runId: string,
  stepId: string,
  reason: string,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${RUN_BASE}/${runId}/steps/${stepId}/unblock`,
    { reason },
    parseWriteResult,
  );
}

export async function skipAiTaskPlaybookRunStep(
  context: ProductionOrderRequestContext,
  runId: string,
  stepId: string,
  reason: string,
): Promise<AiTaskPlaybookApiResult<ReturnType<typeof parseWriteResult>>> {
  return writeJson(
    context,
    "POST",
    `${RUN_BASE}/${runId}/steps/${stepId}/skip`,
    { reason },
    parseWriteResult,
  );
}

export { parseStepDefinition, parseDependencyDefinition };
