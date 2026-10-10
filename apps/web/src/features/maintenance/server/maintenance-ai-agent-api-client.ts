import { classifyFailedResponse, failureForRefusal } from "@/lib/api/api-failure-classifier";
import type { MaintenanceAiDraftDetail } from "../domain/maintenance-ai-agent";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { MaintenanceApiResult } from "./maintenance-api-client";

function parseDraftDetail(payload: unknown): { status: "success"; value: MaintenanceAiDraftDetail } | { status: "failure"; message: string } {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "Invalid AI draft response." };
  }
  const p = payload as Record<string, unknown>;
  return {
    status: "success",
    value: {
      id: String(p.id),
      assetReference: String(p.assetReference),
      assetName: p.assetName ? String(p.assetName) : undefined,
      status: p.status as MaintenanceAiDraftDetail["status"],
      qualitativeConfidence: p.qualitativeConfidence as MaintenanceAiDraftDetail["qualitativeConfidence"],
      conditionEvaluationReference: String(p.conditionEvaluationReference),
      riskAssessmentId: String(p.riskAssessmentId),
      inputFingerprint: String(p.inputFingerprint),
      ruleSetVersion: String(p.ruleSetVersion),
      providerIdentifier: String(p.providerIdentifier),
      modelIdentifier: String(p.modelIdentifier),
      promptVersion: String(p.promptVersion),
      isDevelopmentProvider: Boolean(p.isDevelopmentProvider),
      dataFreshness: String(p.dataFreshness),
      dataSufficiency: String(p.dataSufficiency),
      riskBand: String(p.riskBand),
      deterministicRecommendationCategory: String(p.deterministicRecommendationCategory),
      validatedOutput: p.validatedOutput as MaintenanceAiDraftDetail["validatedOutput"],
      aiOriginalOutput: p.aiOriginalOutput as MaintenanceAiDraftDetail["aiOriginalOutput"],
      suggestedTitle: String(p.suggestedTitle),
      suggestedDescription: String(p.suggestedDescription),
      suggestedPriority: String(p.suggestedPriority),
      suggestedRequestType: String(p.suggestedRequestType),
      humanEditedTitle: p.humanEditedTitle ? String(p.humanEditedTitle) : undefined,
      humanEditedDescription: p.humanEditedDescription ? String(p.humanEditedDescription) : undefined,
      humanEditedPriority: p.humanEditedPriority ? String(p.humanEditedPriority) : undefined,
      humanEditedRequestType: p.humanEditedRequestType ? String(p.humanEditedRequestType) : undefined,
      humanEditorSubjectId: p.humanEditorSubjectId ? String(p.humanEditorSubjectId) : undefined,
      humanEditedAtUtc: p.humanEditedAtUtc ? String(p.humanEditedAtUtc) : undefined,
      generatedBySubjectId: String(p.generatedBySubjectId),
      generatedAtUtc: String(p.generatedAtUtc),
      approvedBySubjectId: p.approvedBySubjectId ? String(p.approvedBySubjectId) : undefined,
      approvedAtUtc: p.approvedAtUtc ? String(p.approvedAtUtc) : undefined,
      rejectedBySubjectId: p.rejectedBySubjectId ? String(p.rejectedBySubjectId) : undefined,
      rejectedAtUtc: p.rejectedAtUtc ? String(p.rejectedAtUtc) : undefined,
      rejectionReason: p.rejectionReason ? String(p.rejectionReason) : undefined,
      createdMaintenanceRequestId: p.createdMaintenanceRequestId ? String(p.createdMaintenanceRequestId) : undefined,
      createdMaintenanceRequestNumber: p.createdMaintenanceRequestNumber ? String(p.createdMaintenanceRequestNumber) : undefined,
      failureCode: p.failureCode ? String(p.failureCode) : undefined,
      failureMessage: p.failureMessage ? String(p.failureMessage) : undefined,
      version: Number(p.version),
      auditTrail: Array.isArray(p.auditTrail) ? (p.auditTrail as MaintenanceAiDraftDetail["auditTrail"]) : [],
    },
  };
}

async function requestJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init: RequestInit,
  parser: (payload: unknown) => { status: "success"; value: T } | { status: "failure"; message: string },
): Promise<MaintenanceApiResult<T>> {
  try {
    const url = new URL(`${context.apiBaseUrl}${path}`);
    url.searchParams.set("facilityId", context.facilityId);
    const response = await fetch(url.toString(), {
      ...init,
      headers: { ...context.requestHeaders, "Content-Type": "application/json", ...(init.headers ?? {}) },
      cache: "no-store",
    });

    if (response.status === 401) {
      return { status: "failure", code: "unauthenticated", message: "Authentication required.", retryable: false };
    }
    if (response.status === 404) {
      return { status: "failure", code: "not-found", message: "AI draft not found.", retryable: false };
    }
    if (response.status === 403) {
      const refusal = await classifyFailedResponse(response);
      return failureForRefusal(refusal, "Authorization denied.");
    }
    if (!response.ok) {
      return {
        status: "failure",
        code: "source-unavailable",
        message: `Maintenance AI request failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const parsed = parser(payload);
    if (parsed.status === "failure") {
      return { status: "failure", code: "invalid-response", message: parsed.message, retryable: false };
    }
    return { status: "success", value: parsed.value };
  } catch {
    return { status: "failure", code: "source-unavailable", message: "Maintenance AI API is unavailable.", retryable: true };
  }
}

export function getMaintenanceAiCurrentDraft(
  context: ProductionOrderRequestContext,
  assetReference: string,
): Promise<MaintenanceApiResult<MaintenanceAiDraftDetail>> {
  return requestJson(
    context,
    `/api/maintenance/assets/${encodeURIComponent(assetReference)}/ai-drafts/current`,
    { method: "GET" },
    parseDraftDetail,
  );
}

export function generateMaintenanceAiDraft(
  context: ProductionOrderRequestContext,
  assetReference: string,
): Promise<MaintenanceApiResult<{ draftId: string; version: number }>> {
  return requestJson(
    context,
    `/api/maintenance/assets/${encodeURIComponent(assetReference)}/ai-drafts/generate`,
    { method: "POST" },
    (payload) => {
      if (!payload || typeof payload !== "object") {
        return { status: "failure", message: "Invalid generate response." };
      }
      const p = payload as Record<string, unknown>;
      return { status: "success", value: { draftId: String(p.draftId), version: Number(p.version) } };
    },
  );
}

export function updateMaintenanceAiDraft(
  context: ProductionOrderRequestContext,
  draftId: string,
  body: { expectedVersion: number; title?: string; description?: string; priority?: string; requestType?: string },
): Promise<MaintenanceApiResult<{ draftId: string; version: number }>> {
  return requestJson(
    context,
    `/api/maintenance/ai-drafts/${draftId}`,
    { method: "PUT", body: JSON.stringify({ expectedVersion: body.expectedVersion, title: body.title, description: body.description, priority: body.priority, requestType: body.requestType }) },
    (payload) => {
      const p = payload as Record<string, unknown>;
      return { status: "success", value: { draftId: String(p.draftId), version: Number(p.version) } };
    },
  );
}

export function approveMaintenanceAiDraft(
  context: ProductionOrderRequestContext,
  draftId: string,
  expectedVersion: number,
): Promise<MaintenanceApiResult<{ draftId: string; version: number; maintenanceRequestId?: string; requestNumber?: string }>> {
  return requestJson(
    context,
    `/api/maintenance/ai-drafts/${draftId}/approve`,
    { method: "POST", body: JSON.stringify({ expectedVersion }) },
    (payload) => {
      const p = payload as Record<string, unknown>;
      return {
        status: "success",
        value: {
          draftId: String(p.draftId),
          version: Number(p.version),
          maintenanceRequestId: p.maintenanceRequestId ? String(p.maintenanceRequestId) : undefined,
          requestNumber: p.requestNumber ? String(p.requestNumber) : undefined,
        },
      };
    },
  );
}

export function rejectMaintenanceAiDraft(
  context: ProductionOrderRequestContext,
  draftId: string,
  expectedVersion: number,
  rejectionReason: string,
): Promise<MaintenanceApiResult<{ draftId: string; version: number }>> {
  return requestJson(
    context,
    `/api/maintenance/ai-drafts/${draftId}/reject`,
    { method: "POST", body: JSON.stringify({ expectedVersion, rejectionReason }) },
    (payload) => {
      const p = payload as Record<string, unknown>;
      return { status: "success", value: { draftId: String(p.draftId), version: Number(p.version) } };
    },
  );
}
