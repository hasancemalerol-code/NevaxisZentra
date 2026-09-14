"use server";

import "./golden-demo-p1-server-boundary";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { GOLDEN_DEMO_P1_ROUTE } from "../domain/golden-demo-p1-labels";
import {
  approveGoldenDemoPlan,
  completeGoldenDemoIntervention,
  executeGoldenDemoPlan,
  verifyGoldenDemoOutcome,
} from "./golden-demo-p1-api-client";

type ActionResult =
  | Readonly<{ status: "success" }>
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

function revalidateGoldenDemo() {
  revalidatePath(GOLDEN_DEMO_P1_ROUTE);
  revalidatePath("/operasyon/yonetim-merkezi");
  revalidatePath("/dashboard");
}

export async function goldenDemoApprovePlanAction(eventId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-p1.approve");
  if (auth.status === "failure") {
    return { status: "failure", message: "Onay yetkiniz yok." };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  const result = await approveGoldenDemoPlan(ctx.context, eventId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateGoldenDemo();
  return { status: "success" };
}

export async function goldenDemoExecutePlanAction(eventId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-p1.execute");
  if (auth.status === "failure") {
    return { status: "failure", message: "Uygulama yetkiniz yok." };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  const result = await executeGoldenDemoPlan(ctx.context, eventId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateGoldenDemo();
  return { status: "success" };
}

export async function goldenDemoCompleteInterventionAction(eventId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-p1.execute");
  if (auth.status === "failure") {
    return { status: "failure", message: "Müdahale tamamlama yetkiniz yok." };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  const result = await completeGoldenDemoIntervention(ctx.context, eventId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateGoldenDemo();
  return { status: "success" };
}

export async function goldenDemoVerifyOutcomeAction(eventId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-p1.verify");
  if (auth.status === "failure") {
    return { status: "failure", message: "Doğrulama yetkiniz yok." };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  const result = await verifyGoldenDemoOutcome(ctx.context, eventId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateGoldenDemo();
  return { status: "success" };
}
