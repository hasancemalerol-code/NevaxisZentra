"use server";

import "./golden-demo-customs-file-server-boundary";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { GOLDEN_DEMO_CUSTOMS_FILE_ROUTE } from "../domain/golden-demo-customs-file-labels";
import {
  approveGoldenDemoCustomsPackage,
  assembleGoldenDemoCustomsPackage,
  assignGoldenDemoCustomsOutcome,
  completeGoldenDemoCustomsCorrectiveWork,
  evaluateGoldenDemoCustomsWorker,
  recheckGoldenDemoCustomsEvidence,
  resetGoldenDemoCustomsOutcome,
  verifyGoldenDemoCustomsOutcome,
} from "./golden-demo-customs-file-api-client";

type ActionResult =
  | Readonly<{ status: "success" }>
  | Readonly<{ status: "failure"; message: string }>;

function revalidateCustomsDemo() {
  revalidatePath(GOLDEN_DEMO_CUSTOMS_FILE_ROUTE);
}

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

export async function goldenDemoCustomsAssignAction(outcomeId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-customs-file.manage");
  if (auth.status === "failure") return { status: "failure", message: "Yetki yok." };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  const result = await assignGoldenDemoCustomsOutcome(ctx.context, outcomeId);
  if (result.status === "failure") return { status: "failure", message: result.message };
  revalidateCustomsDemo();
  return { status: "success" };
}

export async function goldenDemoCustomsEvaluateAction(outcomeId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-customs-file.execute");
  if (auth.status === "failure") return { status: "failure", message: "Yetki yok." };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  const result = await evaluateGoldenDemoCustomsWorker(ctx.context, outcomeId);
  if (result.status === "failure") return { status: "failure", message: result.message };
  revalidateCustomsDemo();
  return { status: "success" };
}

export async function goldenDemoCustomsCompleteWorkAction(
  outcomeId: string,
  workId: string,
): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-customs-file.execute");
  if (auth.status === "failure") return { status: "failure", message: "Yetki yok." };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  const result = await completeGoldenDemoCustomsCorrectiveWork(ctx.context, outcomeId, workId);
  if (result.status === "failure") return { status: "failure", message: result.message };
  revalidateCustomsDemo();
  return { status: "success" };
}

export async function goldenDemoCustomsRecheckAction(outcomeId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-customs-file.execute");
  if (auth.status === "failure") return { status: "failure", message: "Yetki yok." };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  const result = await recheckGoldenDemoCustomsEvidence(ctx.context, outcomeId);
  if (result.status === "failure") return { status: "failure", message: result.message };
  revalidateCustomsDemo();
  return { status: "success" };
}

export async function goldenDemoCustomsApproveAction(outcomeId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-customs-file.approve");
  if (auth.status === "failure") return { status: "failure", message: "Onay yetkiniz yok." };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  const result = await approveGoldenDemoCustomsPackage(ctx.context, outcomeId);
  if (result.status === "failure") return { status: "failure", message: result.message };
  revalidateCustomsDemo();
  return { status: "success" };
}

export async function goldenDemoCustomsAssembleAction(outcomeId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-customs-file.execute");
  if (auth.status === "failure") return { status: "failure", message: "Yetki yok." };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  const result = await assembleGoldenDemoCustomsPackage(ctx.context, outcomeId);
  if (result.status === "failure") return { status: "failure", message: result.message };
  revalidateCustomsDemo();
  return { status: "success" };
}

export async function goldenDemoCustomsVerifyAction(outcomeId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-customs-file.verify");
  if (auth.status === "failure") return { status: "failure", message: "Doğrulama yetkiniz yok." };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  const result = await verifyGoldenDemoCustomsOutcome(ctx.context, outcomeId);
  if (result.status === "failure") return { status: "failure", message: result.message };
  revalidateCustomsDemo();
  return { status: "success" };
}

export async function goldenDemoCustomsResetAction(outcomeId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-customs-file.reset");
  if (auth.status === "failure") return { status: "failure", message: "Sıfırlama yetkiniz yok." };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  const result = await resetGoldenDemoCustomsOutcome(ctx.context, outcomeId);
  if (result.status === "failure") return { status: "failure", message: result.message };
  revalidateCustomsDemo();
  return { status: "success" };
}
