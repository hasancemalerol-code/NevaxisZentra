"use server";

import "./mold-server-boundary";

import { revalidatePath } from "next/cache";

import {
  createMold,
  recordMoldCounterSnapshot,
  transitionMoldLifecycle,
  updateMold,
} from "./mold-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

type ActionResult =
  | Readonly<{ status: "success"; id: string; version: number }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext() {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

function mapResult(result: Awaited<ReturnType<typeof createMold>>): ActionResult {
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/kaliplar");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function createMoldAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const body = {
    code: String(formData.get("code") ?? ""),
    name: String(formData.get("name") ?? ""),
    description: String(formData.get("description") ?? "") || null,
    ownershipType: String(formData.get("ownershipType") ?? "company-owned"),
    ownerReference: String(formData.get("ownerReference") ?? "") || null,
    locationType: String(formData.get("locationType") ?? "warehouse"),
    locationLabel: String(formData.get("locationLabel") ?? "") || null,
    sourceType: String(formData.get("sourceType") ?? "manual"),
    externalSourceReference: String(formData.get("externalSourceReference") ?? "") || null,
    cavityCount: formData.get("cavityCount") ? Number(formData.get("cavityCount")) : null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await createMold(resolved.context, body));
}

export async function updateMoldAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const moldId = String(formData.get("moldId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    name: String(formData.get("name") ?? ""),
    description: String(formData.get("description") ?? "") || null,
    ownershipType: String(formData.get("ownershipType") ?? "company-owned"),
    ownerReference: String(formData.get("ownerReference") ?? "") || null,
    locationType: String(formData.get("locationType") ?? "warehouse"),
    locationLabel: String(formData.get("locationLabel") ?? "") || null,
    cavityCount: formData.get("cavityCount") ? Number(formData.get("cavityCount")) : null,
    notes: String(formData.get("notes") ?? "") || null,
  };

  return mapResult(await updateMold(resolved.context, moldId, body));
}

export async function recordMoldCounterAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const moldId = String(formData.get("moldId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    shotCount: Number(formData.get("shotCount") ?? 0),
    sourceType: "manual",
    note: String(formData.get("note") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await recordMoldCounterSnapshot(resolved.context, moldId, body));
}

export async function transitionMoldLifecycleAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const moldId = String(formData.get("moldId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    targetState: String(formData.get("targetState") ?? ""),
    reason: String(formData.get("reason") ?? ""),
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await transitionMoldLifecycle(resolved.context, moldId, body));
}
