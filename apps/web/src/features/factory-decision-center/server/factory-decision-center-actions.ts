"use server";

import "./factory-decision-center-server-boundary";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  postFactoryDecisionApproval,
  postFactoryDecisionExecute,
  postFactoryDecisionReset,
  postFactoryDecisionVerify,
  type FactoryDecisionCaseDetailApi,
  type FactoryDecisionCenterWorkspaceApi,
} from "./factory-decision-center-api-client";

type ActionResult<T> =
  | { status: "success"; value: T }
  | { status: "failure"; message: string };

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

function revalidateFactoryDecisionCenter(caseId?: string) {
  revalidatePath("/fabrika-karar-merkezi");
  revalidatePath("/");
  revalidatePath("/dijital-is-gucu");
  if (caseId) {
    revalidatePath(`/fabrika-karar-merkezi/vakalar/${caseId}`);
  }
}

export async function factoryDecisionApproveAction(
  caseId: string,
  approvalId: string,
  approved: boolean,
  concurrencyToken: number,
  note?: string,
): Promise<ActionResult<FactoryDecisionCaseDetailApi>> {
  const authorization = await denyUnlessGranted("digital-workforce.approval.decide");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.code === "authorization-denied" ? "Onay yetkiniz yok." : authorization.message };
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await postFactoryDecisionApproval(resolved.context, caseId, approvalId, {
    approved,
    note,
    concurrencyToken,
  });
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateFactoryDecisionCenter(caseId);
  return { status: "success", value: result.value };
}

export async function factoryDecisionExecuteAction(
  caseId: string,
  concurrencyToken: number,
): Promise<ActionResult<FactoryDecisionCaseDetailApi>> {
  const authorization = await denyUnlessGranted("factory-case.manage");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.code === "authorization-denied" ? "Yürütme yetkiniz yok." : authorization.message };
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await postFactoryDecisionExecute(resolved.context, caseId, concurrencyToken);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateFactoryDecisionCenter(caseId);
  return { status: "success", value: result.value };
}

export async function factoryDecisionVerifyAction(
  caseId: string,
  concurrencyToken: number,
): Promise<ActionResult<FactoryDecisionCaseDetailApi>> {
  const authorization = await denyUnlessGranted("factory-case.verify");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.code === "authorization-denied" ? "Doğrulama yetkiniz yok." : authorization.message };
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await postFactoryDecisionVerify(resolved.context, caseId, concurrencyToken);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateFactoryDecisionCenter(caseId);
  return { status: "success", value: result.value };
}

export async function factoryDecisionResetAction(): Promise<ActionResult<FactoryDecisionCenterWorkspaceApi>> {
  const authorization = await denyUnlessGranted("factory-case.manage");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.code === "authorization-denied" ? "Sıfırlama yetkiniz yok." : authorization.message };
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await postFactoryDecisionReset(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  revalidateFactoryDecisionCenter();
  return { status: "success", value: result.value };
}
