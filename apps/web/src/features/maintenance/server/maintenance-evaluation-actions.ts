"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { evaluateMaintenanceCondition } from "./maintenance-condition-api-client";
import { evaluateMaintenancePredictiveRisk } from "./maintenance-predictive-risk-api-client";

export type MaintenanceEvaluationActionResult =
  | Readonly<{ status: "success" }>
  | Readonly<{ status: "failure"; message: string }>;

const EVALUATION_FAILED_MESSAGE = "Değerlendirme güncellenemedi. Biraz sonra tekrar deneyin.";

/**
 * The only path that recomputes the condition and risk assessments from the web app. Opening a Maintenance page only
 * reads; evaluation is an explicit, permission-checked user action (the API enforces `maintenance.manage` as well).
 */
export async function evaluateMaintenanceConditionAction(): Promise<MaintenanceEvaluationActionResult> {
  const authorization = await denyUnlessGranted("maintenance.manage");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const condition = await evaluateMaintenanceCondition(contextResult.context);
  if (condition.status === "failure") {
    return { status: "failure", message: EVALUATION_FAILED_MESSAGE };
  }

  const risk = await evaluateMaintenancePredictiveRisk(contextResult.context);
  if (risk.status === "failure") {
    return { status: "failure", message: EVALUATION_FAILED_MESSAGE };
  }

  revalidatePath("/bakim/durum-zekasi");
  revalidatePath("/bakim/kestirimci-riskler");
  return { status: "success" };
}
