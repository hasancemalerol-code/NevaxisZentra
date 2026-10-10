"use server";

import { redirect } from "next/navigation";

import { gateAccess } from "@/features/admin-access/server/require-current-permission";
import { hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";
import { acceptanceRoleRoute, requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";

import { buildCreateScenarioRunRequest, buildScenarioRecoveryHref } from "../application/scenario-recovery-links";
import { createScenarioRecoveryRun, generateScenarioRecoveryOptions } from "./scenario-recovery-api-client";

export type CreateScenarioRecoveryActionState = Readonly<{ status: "idle" } | { status: "failure"; message: string }>;

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function field(formData: FormData, name: string): string | undefined {
  const value = formData.get(name);
  return typeof value === "string" && value.trim() ? value.trim() : undefined;
}

/**
 * The ONLY path that creates a scenario run from the web app: an intentional, authorization-gated form submission.
 * Viewing /senaryo-kurtarma never reaches this action. The API re-checks the simulate permission, scopes the run to
 * the caller's tenant and facility and audits the creation.
 */
export async function createScenarioRecoveryRunAction(
  _previous: CreateScenarioRecoveryActionState,
  formData: FormData,
): Promise<CreateScenarioRecoveryActionState> {
  const gate = await gateAccess([requiredRoutePermission("senaryo")]);
  if (gate.status === "failure") {
    return {
      status: "failure",
      message: gate.code === "authorization-denied" ? "Senaryo oluşturmak için yetkiniz yok." : gate.message,
    };
  }

  if (!hasGrantedPermission(gate.access, acceptanceRoleRoute("senaryo").simulatePermissionKey ?? "")) {
    return { status: "failure", message: "Senaryo simülasyon izni gerekli." };
  }

  const lineId = field(formData, "lineId");
  if (!lineId || !GUID.test(lineId)) {
    return { status: "failure", message: "Senaryo oluşturmak için geçerli bir üretim hattı seçilmelidir." };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const created = await createScenarioRecoveryRun(
    context,
    buildCreateScenarioRunRequest({
      lineId,
      entryPoint: field(formData, "entryPoint"),
      orderReference: field(formData, "orderReference"),
      constraintStationCode: field(formData, "constraintStationCode"),
    }),
  );
  if (created.status === "failure") {
    return { status: "failure", message: created.message };
  }

  // A generation failure leaves the explicitly created run in place; its page shows the state and allows a retry.
  await generateScenarioRecoveryOptions(context, created.value.runId);
  redirect(buildScenarioRecoveryHref({ runId: created.value.runId }));
}
