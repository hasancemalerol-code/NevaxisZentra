"use server";

import "./golden-demo-p2-server-boundary";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";
import { GOLDEN_DEMO_P1_ROUTE } from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";

import { resetGoldenDemoPilotEvent } from "./golden-demo-p2-api-client";

type ActionResult =
  | Readonly<{ status: "success" }>
  | Readonly<{ status: "failure"; message: string }>;

function revalidateGoldenDemoRoutes() {
  revalidatePath(GOLDEN_DEMO_P1_ROUTE);
  revalidatePath("/operasyon/yonetim-merkezi");
  revalidatePath("/dashboard");
}

export async function goldenDemoResetPilotEventAction(eventId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("golden-demo-p2.reset");
  if (auth.status === "failure") {
    return { status: "failure", message: "Altın demo sıfırlama yetkiniz yok." };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await resetGoldenDemoPilotEvent(contextResult.context, eventId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidateGoldenDemoRoutes();
  return { status: "success" };
}
