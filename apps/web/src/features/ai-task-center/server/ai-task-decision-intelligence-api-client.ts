import { classifyFailedResponse, failureForRefusal } from "@/lib/api/api-failure-classifier";
import "./ai-task-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  AiTaskDecisionAnalysis,
  AiTaskDecisionWorkspace,
} from "../domain/ai-task-decision-intelligence";

export type AiTaskDecisionApiFailure = Readonly<{
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

export type AiTaskDecisionApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | AiTaskDecisionApiFailure;

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  return url.toString();
}

async function requestJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<AiTaskDecisionApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      ...init,
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        ...context.requestHeaders,
        ...init?.headers,
      },
      cache: "no-store",
    });

    const text = await response.text();
    let payload: unknown = null;
    try {
      payload = text ? JSON.parse(text) : null;
    } catch {
      return {
        status: "failure",
        code: "invalid-response",
        message: "Yapay zekâ değerlendirme yanıtı çözümlenemedi.",
        retryable: true,
      };
    }

    if (response.status === 401) {
      return { status: "failure", code: "unauthenticated", message: "Oturum doğrulanamadı.", retryable: false };
    }

    if (response.status === 403) {
      const refusal = await classifyFailedResponse(response);
      return failureForRefusal(refusal, "Yetki bulunmuyor.");
    }

    if (response.status === 404) {
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false };
    }

    if (response.status === 409) {
      return { status: "failure", code: "concurrency-conflict", message: "Eşzamanlılık çakışması.", retryable: true };
    }

    if (!response.ok) {
      const message = typeof payload === "object" && payload && "detail" in payload
        ? String((payload as { detail: unknown }).detail)
        : `Yapay zekâ isteği başarısız (${response.status}).`;
      return { status: "failure", code: "invalid-request", message, retryable: response.status >= 500 };
    }

    return { status: "success", value: payload as T };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Yapay zekâ değerlendirmesi şu anda kullanılamıyor.",
      retryable: true,
    };
  }
}

function mapAnalysis(raw: Record<string, unknown>): AiTaskDecisionAnalysis {
  return {
    id: String(raw.id),
    taskId: String(raw.taskId),
    analysisType: String(raw.analysisType),
    providerName: String(raw.providerName),
    modelName: String(raw.modelName),
    promptTemplateVersion: String(raw.promptTemplateVersion),
    confidenceBand: String(raw.confidenceBand) as AiTaskDecisionAnalysis["confidenceBand"],
    triage: (raw.triage as AiTaskDecisionAnalysis["triage"]) ?? null,
    closureReview: (raw.closureReview as AiTaskDecisionAnalysis["closureReview"]) ?? null,
    superseded: Boolean(raw.superseded),
    supersededByAnalysisId: raw.supersededByAnalysisId ? String(raw.supersededByAnalysisId) : null,
    createdAtUtc: String(raw.createdAtUtc),
    createdBy: String(raw.createdBy),
    recommendations: Array.isArray(raw.recommendations)
      ? raw.recommendations.map((item) => {
          const rec = item as Record<string, unknown>;
          return {
            id: String(rec.id),
            analysisId: String(rec.analysisId),
            actionType: String(rec.actionType),
            title: String(rec.title),
            rationale: String(rec.rationale),
            confidenceBand: String(rec.confidenceBand) as AiTaskDecisionAnalysis["confidenceBand"],
            urgencyBand: String(rec.urgencyBand),
            relatedEntityType: rec.relatedEntityType ? String(rec.relatedEntityType) : null,
            relatedEntityId: rec.relatedEntityId ? String(rec.relatedEntityId) : null,
            payloadJson: String(rec.payloadJson ?? "{}"),
            status: String(rec.status),
            feedbackDecision: rec.feedbackDecision ? String(rec.feedbackDecision) : null,
            feedbackReason: rec.feedbackReason ? String(rec.feedbackReason) : null,
            feedbackAtUtc: rec.feedbackAtUtc ? String(rec.feedbackAtUtc) : null,
            appliedResultJson: rec.appliedResultJson ? String(rec.appliedResultJson) : null,
            createdAtUtc: String(rec.createdAtUtc),
          };
        })
      : [],
  };
}

export async function getAiTaskDecisionWorkspace(
  context: ProductionOrderRequestContext,
  taskId: string,
): Promise<AiTaskDecisionApiResult<AiTaskDecisionWorkspace>> {
  const result = await requestJson<Record<string, unknown>>(
    context,
    `/api/ai-tasks/${taskId}/ai`,
  );

  if (result.status === "failure") return result;

  const raw = result.value;
  return {
    status: "success",
    value: {
      latestAnalysis: raw.latestAnalysis
        ? mapAnalysis(raw.latestAnalysis as Record<string, unknown>)
        : null,
      history: Array.isArray(raw.history)
        ? raw.history.map((item) => mapAnalysis(item as Record<string, unknown>))
        : [],
      relationships: Array.isArray(raw.relationships)
        ? raw.relationships.map((item) => {
            const rel = item as Record<string, unknown>;
            return {
              id: String(rel.id),
              sourceTaskId: String(rel.sourceTaskId),
              targetTaskId: String(rel.targetTaskId),
              targetTaskTitle: String(rel.targetTaskTitle),
              targetTaskStatus: String(rel.targetTaskStatus),
              targetTaskPriority: String(rel.targetTaskPriority),
              targetSourceModule: rel.targetSourceModule ? String(rel.targetSourceModule) : null,
              relationshipType: String(rel.relationshipType),
              status: String(rel.status),
              confidenceBand: String(rel.confidenceBand) as AiTaskDecisionWorkspace["suggestedRelated"][number]["confidenceBand"],
              explanation: rel.explanation ? String(rel.explanation) : null,
              confirmedAtUtc: rel.confirmedAtUtc ? String(rel.confirmedAtUtc) : null,
              createdAtUtc: String(rel.createdAtUtc),
            };
          })
        : [],
      suggestedRelated: Array.isArray(raw.suggestedRelated)
        ? raw.suggestedRelated.map((item) => {
            const candidate = item as Record<string, unknown>;
            return {
              candidateTaskId: String(candidate.candidateTaskId),
              relationshipType: String(candidate.relationshipType),
              confidenceBand: String(candidate.confidenceBand) as AiTaskDecisionWorkspace["suggestedRelated"][number]["confidenceBand"],
              explanation: String(candidate.explanation),
              candidateTaskTitle: candidate.candidateTaskTitle ? String(candidate.candidateTaskTitle) : undefined,
              candidateTaskStatus: candidate.candidateTaskStatus ? String(candidate.candidateTaskStatus) : undefined,
              candidateTaskPriority: candidate.candidateTaskPriority ? String(candidate.candidateTaskPriority) : undefined,
            };
          })
        : [],
      providerAvailable: Boolean(raw.providerAvailable),
      providerStatusMessage: raw.providerStatusMessage ? String(raw.providerStatusMessage) : null,
    },
  };
}

export async function analyzeAiTaskDecision(
  context: ProductionOrderRequestContext,
  taskId: string,
  input: Readonly<{ analysisType?: string; idempotencyKey: string; triggerReason?: string }>,
): Promise<AiTaskDecisionApiResult<{ analysis: AiTaskDecisionAnalysis; taskVersion: number; idempotentReplay: boolean }>> {
  const result = await requestJson<Record<string, unknown>>(
    context,
    `/api/ai-tasks/${taskId}/ai/analyze`,
    {
      method: "POST",
      body: JSON.stringify(input),
    },
  );

  if (result.status === "failure") return result;

  return {
    status: "success",
    value: {
      analysis: mapAnalysis(result.value.analysis as Record<string, unknown>),
      taskVersion: Number(result.value.taskVersion ?? 0),
      idempotentReplay: Boolean(result.value.idempotentReplay),
    },
  };
}

export async function acceptAiTaskRecommendation(
  context: ProductionOrderRequestContext,
  taskId: string,
  recommendationId: string,
  input: Readonly<{ expectedVersion: number; reason?: string }>,
): Promise<AiTaskDecisionApiResult<{ taskVersion: number }>> {
  const result = await requestJson<Record<string, unknown>>(
    context,
    `/api/ai-tasks/${taskId}/ai/recommendations/${recommendationId}/accept`,
    {
      method: "POST",
      body: JSON.stringify({
        expectedVersion: input.expectedVersion,
        reason: input.reason ?? null,
      }),
    },
  );

  if (result.status === "failure") return result;
  return { status: "success", value: { taskVersion: Number(result.value.taskVersion ?? 0) } };
}

export async function rejectAiTaskRecommendation(
  context: ProductionOrderRequestContext,
  taskId: string,
  recommendationId: string,
  reason: string,
): Promise<AiTaskDecisionApiResult<{ taskVersion: number }>> {
  const result = await requestJson<Record<string, unknown>>(
    context,
    `/api/ai-tasks/${taskId}/ai/recommendations/${recommendationId}/reject`,
    {
      method: "POST",
      body: JSON.stringify({ reason }),
    },
  );

  if (result.status === "failure") return result;
  return { status: "success", value: { taskVersion: Number(result.value.taskVersion ?? 0) } };
}

export async function confirmAiTaskRelationship(
  context: ProductionOrderRequestContext,
  taskId: string,
  input: Readonly<{
    targetTaskId: string;
    relationshipType: string;
    status: string;
    explanation?: string;
  }>,
): Promise<AiTaskDecisionApiResult<{ taskVersion: number }>> {
  const result = await requestJson<Record<string, unknown>>(
    context,
    `/api/ai-tasks/${taskId}/relationships`,
    {
      method: "POST",
      body: JSON.stringify(input),
    },
  );

  if (result.status === "failure") return result;
  return { status: "success", value: { taskVersion: Number(result.value.taskVersion ?? 0) } };
}

export async function reviewAiTaskClosure(
  context: ProductionOrderRequestContext,
  taskId: string,
  idempotencyKey: string,
): Promise<AiTaskDecisionApiResult<{ review: AiTaskDecisionAnalysis["closureReview"]; idempotentReplay: boolean }>> {
  const result = await requestJson<Record<string, unknown>>(
    context,
    `/api/ai-tasks/${taskId}/ai/closure-review`,
    {
      method: "POST",
      body: JSON.stringify({ idempotencyKey }),
    },
  );

  if (result.status === "failure") return result;

  return {
    status: "success",
    value: {
      review: (result.value.review as AiTaskDecisionAnalysis["closureReview"]) ?? null,
      idempotentReplay: Boolean(result.value.idempotentReplay),
    },
  };
}
