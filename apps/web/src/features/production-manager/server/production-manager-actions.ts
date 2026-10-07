"use server";

import "./production-manager-server-boundary";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  approveProductionManagerDecision,
  executeProductionManagerDecision,
  simulateProductionManagerScenario,
  verifyProductionManagerDecision,
  type ProductionManagerDecisionApi,
  type ProductionManagerSimulationApi,
} from "./production-manager-api-client";

type ActionResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext() {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }
  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }
  return { status: "success" as const, context: contextResult.context };
}

function revalidateProductionManager(decisionId?: string) {
  revalidatePath("/operasyon/yonetim-merkezi");
  revalidatePath("/dashboard");
  revalidatePath("/vardiyalar/aktif");
  revalidatePath("/vardiyalar/teslim");
  if (decisionId) {
    revalidatePath(`/operasyon/yonetim-merkezi/kararlar/${decisionId}`);
  }
}

export async function productionManagerSimulateAction(
  input: Readonly<{
    scenarioType: string;
    machineId?: string;
    orderId?: string;
    moldId?: string;
    materialId?: string;
    delayHours?: number;
    capacityAdjustmentPercent?: number;
  }>,
): Promise<ActionResult<ProductionManagerSimulationApi>> {
  const auth = await denyUnlessGranted("production-manager.simulate");
  if (auth.status === "failure") {
    return { status: "failure", message: auth.code === "authorization-denied" ? "Simülasyon yetkiniz yok." : auth.message };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  const result = await simulateProductionManagerScenario(ctx.context, input);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  return { status: "success", value: result.value };
}

export async function productionManagerApproveAction(
  decisionId: string,
  approved: boolean,
  concurrencyToken: number,
  notes?: string,
): Promise<ActionResult<ProductionManagerDecisionApi>> {
  const auth = await denyUnlessGranted("production-manager.approve");
  if (auth.status === "failure") {
    return { status: "failure", message: auth.code === "authorization-denied" ? "Onay yetkiniz yok." : auth.message };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  const result = await approveProductionManagerDecision(ctx.context, decisionId, {
    approved,
    notes,
    concurrencyToken,
  });
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateProductionManager(decisionId);
  return { status: "success", value: result.value };
}

export async function productionManagerExecuteAction(
  decisionId: string,
  concurrencyToken: number,
  selectedOptionId?: string,
): Promise<ActionResult<ProductionManagerDecisionApi>> {
  const auth = await denyUnlessGranted("production-manager.decide");
  if (auth.status === "failure") {
    return { status: "failure", message: auth.code === "authorization-denied" ? "Karar uygulama yetkiniz yok." : auth.message };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  const result = await executeProductionManagerDecision(ctx.context, decisionId, {
    selectedOptionId,
    idempotencyKey: `web-${decisionId}-${Date.now()}`,
    concurrencyToken,
  });
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateProductionManager(decisionId);
  return { status: "success", value: result.value };
}

export async function productionManagerVerifyAction(
  decisionId: string,
  concurrencyToken: number,
  actualOutcomeJson: string,
): Promise<ActionResult<ProductionManagerDecisionApi>> {
  const auth = await denyUnlessGranted("production-manager.decide");
  if (auth.status === "failure") {
    return { status: "failure", message: auth.code === "authorization-denied" ? "Doğrulama yetkiniz yok." : auth.message };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  const result = await verifyProductionManagerDecision(ctx.context, decisionId, {
    actualOutcomeJson,
    concurrencyToken,
  });
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateProductionManager(decisionId);
  return { status: "success", value: result.value };
}
