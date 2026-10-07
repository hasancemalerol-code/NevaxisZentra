"use server";

import "./maintenance-server-boundary";

import {
  approveMaintenanceAiDraft,
  generateMaintenanceAiDraft,
  rejectMaintenanceAiDraft,
  updateMaintenanceAiDraft,
} from "./maintenance-ai-agent-api-client";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import type { MaintenanceActionResult } from "./maintenance-writer";

async function resolveContext(): Promise<
  | Readonly<{ kind: "context"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | Readonly<{ kind: "failure"; failure: MaintenanceActionResult }>> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status === "failure") {
    return { kind: "failure", failure: { status: "failure", code: "configuration-error", message: configResult.message } };
  }
  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { kind: "failure", failure: { status: "failure", code: contextResult.code, message: contextResult.message } };
  }
  return { kind: "context", context: contextResult.context };
}

export async function generateMaintenanceAiDraftAction(assetReference: string): Promise<MaintenanceActionResult> {
  const resolved = await resolveContext();
  if (resolved.kind === "failure") return resolved.failure;
  const result = await generateMaintenanceAiDraft(resolved.context, assetReference);
  if (result.status === "failure") return { status: "failure", code: result.code, message: result.message };
  return { status: "success", id: result.value.draftId, version: result.value.version };
}

export async function updateMaintenanceAiDraftAction(input: {
  draftId: string;
  expectedVersion: number;
  title?: string;
  description?: string;
  priority?: string;
  requestType?: string;
}): Promise<MaintenanceActionResult> {
  const resolved = await resolveContext();
  if (resolved.kind === "failure") return resolved.failure;
  const result = await updateMaintenanceAiDraft(resolved.context, input.draftId, input);
  if (result.status === "failure") return { status: "failure", code: result.code, message: result.message };
  return { status: "success", id: result.value.draftId, version: result.value.version };
}

export async function approveMaintenanceAiDraftAction(input: {
  draftId: string;
  expectedVersion: number;
}): Promise<MaintenanceActionResult> {
  const resolved = await resolveContext();
  if (resolved.kind === "failure") return resolved.failure;
  const result = await approveMaintenanceAiDraft(resolved.context, input.draftId, input.expectedVersion);
  if (result.status === "failure") return { status: "failure", code: result.code, message: result.message };
  return {
    status: "success",
    id: result.value.maintenanceRequestId ?? result.value.draftId,
    version: result.value.version,
    message: result.value.requestNumber,
  };
}

export async function rejectMaintenanceAiDraftAction(input: {
  draftId: string;
  expectedVersion: number;
  rejectionReason: string;
}): Promise<MaintenanceActionResult> {
  const resolved = await resolveContext();
  if (resolved.kind === "failure") return resolved.failure;
  const result = await rejectMaintenanceAiDraft(
    resolved.context,
    input.draftId,
    input.expectedVersion,
    input.rejectionReason,
  );
  if (result.status === "failure") return { status: "failure", code: result.code, message: result.message };
  return { status: "success", id: result.value.draftId, version: result.value.version };
}
