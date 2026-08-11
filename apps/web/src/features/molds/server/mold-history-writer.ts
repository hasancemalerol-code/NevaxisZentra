"use server";

import {
  appendMoldRevisionRecord,
  appendMoldTrialRecord,
  completeMoldMaintenanceRecord,
  createMoldMaintenanceRecord,
} from "./mold-api-client";
import { resolveMoldRequestContext } from "./mold-request-context";

async function resolveWritableContext() {
  const contextResult = resolveMoldRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function createMoldMaintenanceAction(formData: FormData) {
  const resolved = await resolveWritableContext();
  if (resolved.status === "failure") return resolved;

  const moldId = String(formData.get("moldId") ?? "");
  const result = await createMoldMaintenanceRecord(resolved.context, moldId, {
    maintenanceType: String(formData.get("maintenanceType") ?? "preventive"),
    status: String(formData.get("status") ?? "planned"),
    openedAtUtc: new Date().toISOString(),
    summary: String(formData.get("summary") ?? ""),
    findings: String(formData.get("findings") ?? "") || undefined,
    performedByDisplayName: String(formData.get("performedByDisplayName") ?? "") || undefined,
    externalMaintenanceRequestReference:
      String(formData.get("externalMaintenanceRequestReference") ?? "") || undefined,
    externalWorkOrderReference: String(formData.get("externalWorkOrderReference") ?? "") || undefined,
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceReference: String(formData.get("sourceReference") ?? "") || undefined,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const };
}

export async function completeMoldMaintenanceAction(formData: FormData) {
  const resolved = await resolveWritableContext();
  if (resolved.status === "failure") return resolved;

  const moldId = String(formData.get("moldId") ?? "");
  const maintenanceId = String(formData.get("maintenanceId") ?? "");
  const result = await completeMoldMaintenanceRecord(resolved.context, moldId, maintenanceId, {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    completedAtUtc: new Date().toISOString(),
    findings: String(formData.get("findings") ?? "") || undefined,
    actionsTaken: String(formData.get("actionsTaken") ?? "") || undefined,
    performedByDisplayName: String(formData.get("performedByDisplayName") ?? "") || undefined,
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const };
}

export async function appendMoldRevisionAction(formData: FormData) {
  const resolved = await resolveWritableContext();
  if (resolved.status === "failure") return resolved;

  const moldId = String(formData.get("moldId") ?? "");
  const result = await appendMoldRevisionRecord(resolved.context, moldId, {
    revisionNumber: String(formData.get("revisionNumber") ?? ""),
    revisionType: String(formData.get("revisionType") ?? "other"),
    revisionDate: String(formData.get("revisionDate") ?? new Date().toISOString().slice(0, 10)),
    summary: String(formData.get("summary") ?? ""),
    reason: String(formData.get("reason") ?? "") || undefined,
    technicalChange: String(formData.get("technicalChange") ?? "") || undefined,
    previousCavityCount: formData.get("previousCavityCount")
      ? Number(formData.get("previousCavityCount"))
      : undefined,
    newCavityCount: formData.get("newCavityCount") ? Number(formData.get("newCavityCount")) : undefined,
    performedByDisplayName: String(formData.get("performedByDisplayName") ?? "") || undefined,
    externalReference: String(formData.get("externalReference") ?? "") || undefined,
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceReference: String(formData.get("sourceReference") ?? "") || undefined,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const };
}

export async function appendMoldTrialAction(formData: FormData) {
  const resolved = await resolveWritableContext();
  if (resolved.status === "failure") return resolved;

  const moldId = String(formData.get("moldId") ?? "");
  const result = await appendMoldTrialRecord(resolved.context, moldId, {
    trialNumber: String(formData.get("trialNumber") ?? ""),
    trialDate: String(formData.get("trialDate") ?? new Date().toISOString().slice(0, 10)),
    result: String(formData.get("result") ?? "pending"),
    machineReference: String(formData.get("machineReference") ?? "") || undefined,
    productionOrderReference: String(formData.get("productionOrderReference") ?? "") || undefined,
    productReference: String(formData.get("productReference") ?? "") || undefined,
    materialReference: String(formData.get("materialReference") ?? "") || undefined,
    cycleTimeSeconds: formData.get("cycleTimeSeconds")
      ? Number(formData.get("cycleTimeSeconds"))
      : undefined,
    shotCount: formData.get("shotCount") ? Number(formData.get("shotCount")) : undefined,
    goodPartCount: formData.get("goodPartCount") ? Number(formData.get("goodPartCount")) : undefined,
    scrapPartCount: formData.get("scrapPartCount") ? Number(formData.get("scrapPartCount")) : undefined,
    findings: String(formData.get("findings") ?? "") || undefined,
    decision: String(formData.get("decision") ?? "") || undefined,
    performedByDisplayName: String(formData.get("performedByDisplayName") ?? "") || undefined,
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceReference: String(formData.get("sourceReference") ?? "") || undefined,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const };
}
