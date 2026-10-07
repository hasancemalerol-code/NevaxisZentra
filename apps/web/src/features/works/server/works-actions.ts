"use server";

import "./works-server-boundary";

import { revalidatePath } from "next/cache";

import { decideApproval } from "@/features/digital-workforce/server/digital-workforce-api-client";
import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { readWorksCaseDetail } from "./works-queries";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

async function resolveContext(): Promise<
  | Readonly<{ status: "success"; context: import("@/features/production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

function revalidateWorksPaths() {
  revalidatePath("/isler");
  revalidatePath("/bugun");
}

export async function decideWorkApprovalAction(
  approvalId: string,
  decision: "approved" | "rejected" | "modified",
  concurrencyToken: number,
): Promise<Readonly<{ status: "success" } | ActionFailure>> {
  const auth = await denyUnlessGranted("digital-workforce.approval.decide");
  if (auth.status === "failure") {
    return { status: "failure", message: auth.code === "authorization-denied" ? "Onay kararı yetkiniz yok." : auth.message };
  }

  const ctx = await resolveContext();
  if (ctx.status === "failure") {
    return ctx;
  }

  try {
    await decideApproval(ctx.context, approvalId, decision, concurrencyToken);
    revalidateWorksPaths();
    return { status: "success" };
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Onay kararı kaydedilemedi.",
    };
  }
}

export async function loadCaseDetailAction(caseId: string) {
  const auth = await denyUnlessGranted("digital-workforce.read");
  if (auth.status === "failure") {
    return { status: "failure" as const, message: auth.code === "authorization-denied" ? "Görüntüleme yetkiniz yok." : auth.message };
  }

  const result = await readWorksCaseDetail(caseId);
  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: result.message,
      code: "code" in result ? result.code : undefined,
    };
  }

  return { status: "success" as const, detail: result.detail };
}
